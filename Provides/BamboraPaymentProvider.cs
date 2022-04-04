using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Lib.Payment.Models;
using Lib.Payment.Models.ProviderConditions;
using Lib.Payment.Provides.Request;
using Lib.Payment.Provides.Response;
using Lib.Payment.Requests;
using Lib.Payment.Results;
using Lib.Payment.Services;
using Lib.Extension.v2;

namespace Lib.Payment.Providers
{
    public class BamboraPaymentProvider : IPaymentProvider
    {
        public Dictionary<string, string> TestParameters => new Dictionary<string, string>
        {
            { "AccessKey", "GKAGIgWn4bcprRuJne5f" },
            { "MerchantID", "T172185701" },
            { "SecretKey", "ml2u4e5GDQdlSPy8CoOsDx02WGW1WEmuSUg5rqlD" },
            { "ApiUrl", "https://api.v1.checkout.bambora.com" },
        };

        private static readonly Dictionary<PaymentLanguage, string> LanguageCodes = new Dictionary<PaymentLanguage, string>
        {
            { PaymentLanguage.en, "en-GB" },
            { PaymentLanguage.da, "da-DK" },
            { PaymentLanguage.sv, "sv-SE" },
            { PaymentLanguage.nb, "nb-NO" },
            { PaymentLanguage.fi, "fi-FI" },
            { PaymentLanguage.fr, "fr-FR" },
            { PaymentLanguage.de, "de-DE" },
        };

        public CancelPaymentResult CancelRequest(CancelPaymentRequest request)
        {
            throw new NotImplementedException();
        }

        public HostedPaymentResult HostedPaymentPageRequest(HostedPaymentRequest request)
        {
            Models.LogModel.Log(request.ReservationID.ToInt(), "BamboraPaymentProvider: HostedPaymentPageRequest - Start", request, null, request.CustomerCode);
            PaymentCredentialParameter credential = new PaymentCredentialParameter().GetCredential(request.CustomerCode, request.BankServiceName, request.Market);

            BamboraConditions bamboraConditions = new BamboraConditions();
            bool credentialConditionResult = bamboraConditions.Credential(credential);
            if (!credentialConditionResult) return HostedPaymentResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "Please fill all criteria.", "TNC-02");

            var generalInfo = new GeneralServices().GetPaymentGeneralInfo(BaseSiteUrl: request.BaseSiteUrl, ReservationID: request.ReservationID, ShoppingFileID: request.AuthCode);

            String PurchAmount = (request.Amount * 100).ToString(new CultureInfo("en-US")).Replace(".", "");
            BamboraHostedPaymentRequest bamboraHostedPaymentRequest = new BamboraHostedPaymentRequest
            {
                order = new Order()
                {
                    amount = PurchAmount,
                    currency = request.CurrencyIsoCode.ToString(),
                    id = generalInfo.OrderID.ToString(),
                },
                url = new Url()
                {
                    accept = generalInfo.CallbackUrl,
                    cancel = generalInfo.CallbackUrl
                },
                paymentwindow = new Paymentwindow
                {
                    language = LanguageCodes[request.LanguageIsoCode]
                }
            };

            Models.LogModel.Log(request.ReservationID.ToInt(), "BamboraPaymentProvider: HostedPaymentPageRequest Model - Start", bamboraHostedPaymentRequest, null, request.CustomerCode);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            string authenticationStr = GetHeaderAuthentication(credential);
            var restrequest = new RestRequest("/sessions", Method.POST);
            restrequest.AddHeader("Authorization", $"Basic {authenticationStr}");
            restrequest.AddHeader("Content-Type", $"application/json");
            restrequest.AddHeader("Accept", $"application/json");
            restrequest.AddJsonBody(bamboraHostedPaymentRequest);
            var client = new RestClient(credential.ApiUrl);
            var response = client.Execute(restrequest);
            Models.LogModel.Log(request.ReservationID.ToInt(), "BamboraPaymentProvider: HostedPaymentPageRequest Model - End", bamboraHostedPaymentRequest, response.Content, request.CustomerCode);
            BamboraHostedPaymentResponse sessionData = JsonConvert.DeserializeObject<BamboraHostedPaymentResponse>(response.Content);
            if (sessionData.meta.result) return HostedPaymentResult.Successed(sessionData.url, sessionData.meta.action.code, sessionData.token, sessionData.meta.message.enduser);
            else return HostedPaymentResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, sessionData.meta.message.enduser, sessionData.meta.action.code);
        }

        public PaymentDetailResult PaymentDetailRequest(PaymentDetailRequest request)
        {
            throw new NotImplementedException();
        }

        public RefundPaymentResult RefundRequest(RefundPaymentRequest request)
        {
            throw new NotImplementedException();
        }

        public PaymentGatewayResult ThreeDGatewayRequest(PaymentGatewayRequest request)
        {
            throw new NotImplementedException();
        }

        public VerifyGatewayResult VerifyGateway(VerifyGatewayRequest request, System.Web.HttpRequest httpRequest)
        {
            PaymentCredentialParameter credential = new PaymentCredentialParameter().GetCredential(request.CustomerCode, request.BankServiceName, request.Market);
            #region Hash 
            var queryStrings = httpRequest.QueryString;
            if (string.IsNullOrEmpty(queryStrings["txnid"])) return VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "No GET(txnid) was supplied to the system!", queryStrings["orderid"]);
            if (string.IsNullOrEmpty(queryStrings["orderid"])) return VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "No GET(orderid) was supplied to the system!", queryStrings["orderid"]);
            if (string.IsNullOrEmpty(queryStrings["hash"])) return VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "No GET(hash) was supplied to the system!", queryStrings["orderid"]);

            string merchantMd5 = credential.MD5Key;
            string concatenatedValues = "";
            foreach (string key in queryStrings.AllKeys)
            {
                if (key.Equals("hash")) { break; }
                concatenatedValues += queryStrings[key];
            }
            string md5GenHashString = "";
            var md5GenBytes = System.Text.Encoding.UTF8.GetBytes(concatenatedValues + merchantMd5);
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                var md5GenHashBytes = md5.ComputeHash(md5GenBytes);
                System.Text.StringBuilder strBuilder = new System.Text.StringBuilder();
                foreach (byte b in md5GenHashBytes)
                {
                    strBuilder.Append(b.ToString("x2").ToLower());
                }
                md5GenHashString = strBuilder.ToString().ToLower();
            }

            if (!md5GenHashString.Equals(queryStrings["hash"])) return VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "Hash validation failed - Please check your MD5 key", queryStrings["orderid"]);

            #endregion

            #region Payment Control
            string accessToken = credential.AccessKey;
            string merchantNumber = credential.MerchantID;
            string secretToken = credential.SecretKey;
            string unencodedApiKey = accessToken + "@" + merchantNumber + ":" + secretToken;
            byte[] unencodedApiKeyAsBytes = System.Text.Encoding.UTF8.GetBytes(unencodedApiKey);
            string apiKey = "Basic " + System.Convert.ToBase64String(unencodedApiKeyAsBytes);

            string transactionId = queryStrings["txnid"];
            string endpoint = credential.GatewayUrl + "/transactions/" + transactionId;

            System.Net.WebClient client = new System.Net.WebClient();
            client.Headers.Add(System.Net.HttpRequestHeader.ContentType, "application/json");
            client.Headers.Add(System.Net.HttpRequestHeader.Authorization, apiKey);
            client.Headers.Add(System.Net.HttpRequestHeader.Accept, "application/json");
            var responseJson = client.DownloadString(endpoint);

            BamboraHostedPaymentGatewayResponse sessionData = JsonConvert.DeserializeObject<BamboraHostedPaymentGatewayResponse>(responseJson);
            if (sessionData.meta.result) return VerifyGatewayResult.Successed(request.ReservationID, request.AuthCode, queryStrings["orderid"], responseCode: "0000", message: sessionData.transaction.status);
            else
            {
                return VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, sessionData.meta.message.enduser + " - " + sessionData.meta.message.merchant, queryStrings["orderid"], sessionData.meta.action.code);
            }
            #endregion
        }

        public string GetHeaderAuthentication(PaymentCredentialParameter credential)
        {
            string beforeEncoding = credential.AccessKey + "@" + credential.MerchantID + ":" + credential.SecretKey;
            byte[] data = System.Text.ASCIIEncoding.ASCII.GetBytes(beforeEncoding);
            string base64Encoded = System.Convert.ToBase64String(data);
            return base64Encoded;
        }
    }
}
