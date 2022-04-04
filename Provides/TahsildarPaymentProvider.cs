using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
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
    public class TahsildarPaymentProvider : IPaymentProvider
    {
        public PaymentGatewayResult ThreeDGatewayRequest(PaymentGatewayRequest request)
        {
            Models.LogModel.Log(request.ReservationID.ToInt(), "TahsildarPaymentProvider: HostedPaymentPageRequest - Start", request, null, request.CustomerCode);
            PaymentCredentialParameter credential = new PaymentCredentialParameter().GetCredential(request.CustomerCode, request.BankServiceName, request.Market);

            if (request.Development && credential == null)
            {
                credential = new PaymentCredentialParameter();
                credential.ApiUrl = "https://karavancruises.tahsildar.com.tr/rest1/virtual-pos/";
                credential.ApiKey = "380PPCNDGFBQBYYMYLHTLGBJQKLLTKJRGIZUYK";
                credential.SecretKey = "Cfc214557eafa8b696687a59f96fd490e";
            }

            TahsildarConditions bamboraConditions = new TahsildarConditions();
            bool credentialConditionResult = bamboraConditions.Credential(credential);
            if (!credentialConditionResult) return PaymentGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "Please fill all criteria.", "TNC-02");

            var generalInfo = new GeneralServices().GetPaymentGeneralInfo(BaseSiteUrl: request.BaseSiteUrl, ReservationID: request.ReservationID, ShoppingFileID: request.AuthCode);

            String PurchAmount = (request.Amount).ToString(new CultureInfo("en-US"));

            TahsildarThreeDGatewatRequest tahsildarThreeDGatewatRequest = new TahsildarThreeDGatewatRequest();
            tahsildarThreeDGatewatRequest.public_key = credential.ApiKey;
            tahsildarThreeDGatewatRequest.price = PurchAmount;
            tahsildarThreeDGatewatRequest.currency = request.CurrencyIsoCode.ToString();
            if (request.Installment > 1) tahsildarThreeDGatewatRequest.installment = request.Installment.ToString();
            else tahsildarThreeDGatewatRequest.installment = "0";
            tahsildarThreeDGatewatRequest.order_no = generalInfo.OrderID;
            tahsildarThreeDGatewatRequest.secure3d = "";
            tahsildarThreeDGatewatRequest.ip = "95.9.134.101";
            tahsildarThreeDGatewatRequest.return_url = generalInfo.CallbackUrl;
            tahsildarThreeDGatewatRequest.provision_type = "sales";
            tahsildarThreeDGatewatRequest.card = new Card()
            {
                cvc = request.CardCV2,
                expire_month = request.CardMonth,
                expire_year = request.CardYear,
                holder_name = request.CardHolderName,
                number = request.CardNumber
            };
            string authenticationStr = GetHeaderAuthentication(credential, tahsildarThreeDGatewatRequest.price, tahsildarThreeDGatewatRequest.currency, tahsildarThreeDGatewatRequest.installment, tahsildarThreeDGatewatRequest.order_no);
            tahsildarThreeDGatewatRequest.hash = authenticationStr;
            Models.LogModel.Log(request.ReservationID.ToInt(), "TahsildarPaymentProvider: HostedPaymentPageRequest Model - Start", tahsildarThreeDGatewatRequest, null, request.CustomerCode);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            var restrequest = new RestRequest("/payment-create", Method.POST);
            restrequest.AddHeader("Authorization", $"Basic {authenticationStr}");
            restrequest.AddHeader("Content-Type", $"application/json");
            restrequest.AddHeader("Accept", $"application/json");
            restrequest.AddJsonBody(tahsildarThreeDGatewatRequest);
            var client = new RestClient(credential.ApiUrl);
            var response = client.Execute(restrequest);
            Models.LogModel.Log(request.ReservationID.ToInt(), "TahsildarPaymentProvider: HostedPaymentPageRequest Model - End", tahsildarThreeDGatewatRequest, response.Content, request.CustomerCode);
            TahsildarThreeDGatewayResponse apiResponseData = JsonConvert.DeserializeObject<TahsildarThreeDGatewayResponse>(response.Content);
            if (apiResponseData.success)
            {
                return PaymentGatewayResult.Successed(apiResponseData.redirect_url, apiResponseData.message);
            }
            else
            {
                return PaymentGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, apiResponseData.message, apiResponseData.errCode);
            }
        }

        public HostedPaymentResult HostedPaymentPageRequest(HostedPaymentRequest request)
        {
            //Models.LogModel.Log(request.ReservationID.ToInt(), "TahsildarPaymentProvider: HostedPaymentPageRequest - Start", request, null, request.CustomerCode);
            //PaymentCredentialParameter credential = new PaymentCredentialParameter().GetCredential(request.CustomerCode, request.BankServiceName, request.Market);

            //if (request.Development && credential == null)
            //{
            //    credential = new PaymentCredentialParameter();
            //    credential.ApiUrl = "https://karavancruises.tahsildar.com.tr/rest1/virtual-pos/";
            //    credential.ApiKey = "380PPCNDGFBQBYYMYLHTLGBJQKLLTKJRGIZUYK";
            //    credential.SecretKey = "Cfc214557eafa8b696687a59f96fd490e";
            //}

            //TahsildarConditions bamboraConditions = new TahsildarConditions();
            //bool credentialConditionResult = bamboraConditions.Credential(credential);
            //if (!credentialConditionResult) return PaymentGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "Please fill all criteria.", "TNC-02");

            //var generalInfo = new GeneralServices().GetPaymentGeneralInfo(BaseSiteUrl: request.BaseSiteUrl, ReservationID: request.ReservationID, ShoppingFileID: request.AuthCode);

            //String PurchAmount = (request.Amount).ToString(new CultureInfo("en-US")).Replace(".", "");

            //TahsildarThreeDGatewatRequest tahsildarThreeDGatewatRequest = new TahsildarThreeDGatewatRequest();
            //tahsildarThreeDGatewatRequest.public_key = credential.ApiKey;
            //tahsildarThreeDGatewatRequest.price = PurchAmount;
            //tahsildarThreeDGatewatRequest.currency = request.CurrencyIsoCode.ToString();
            //if (request.Installment > 1) tahsildarThreeDGatewatRequest.installment = request.Installment.ToString();
            //else tahsildarThreeDGatewatRequest.installment = "0";
            //tahsildarThreeDGatewatRequest.order_no = generalInfo.OrderID;
            //tahsildarThreeDGatewatRequest.secure3d = "";
            //tahsildarThreeDGatewatRequest.ip = "95.9.134.101";
            //tahsildarThreeDGatewatRequest.return_url = generalInfo.CallbackUrl;
            //tahsildarThreeDGatewatRequest.provision_type = "sales";
            ////tahsildarThreeDGatewatRequest.card = new Card()
            ////{
            ////    cvc = request.CardCV2,
            ////    expire_month = request.CardMonth,
            ////    expire_year = request.CardYear,
            ////    holder_name = request.CardHolderName,
            ////    number = request.CardNumber
            ////};
            //string authenticationStr = GetHeaderAuthentication(credential, tahsildarThreeDGatewatRequest.price, tahsildarThreeDGatewatRequest.currency, tahsildarThreeDGatewatRequest.installment, tahsildarThreeDGatewatRequest.order_no);
            //tahsildarThreeDGatewatRequest.hash = authenticationStr;
            //Models.LogModel.Log(request.ReservationID.ToInt(), "TahsildarPaymentProvider: HostedPaymentPageRequest Model - Start", tahsildarThreeDGatewatRequest, null, request.CustomerCode);
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            //var restrequest = new RestRequest("/payment-create", Method.POST);
            //restrequest.AddHeader("Authorization", $"Basic {authenticationStr}");
            //restrequest.AddHeader("Content-Type", $"application/json");
            //restrequest.AddHeader("Accept", $"application/json");
            //restrequest.AddJsonBody(tahsildarThreeDGatewatRequest);
            //var client = new RestClient(credential.ApiUrl);
            //var response = client.Execute(restrequest);
            //Models.LogModel.Log(request.ReservationID.ToInt(), "TahsildarPaymentProvider: HostedPaymentPageRequest Model - End", tahsildarThreeDGatewatRequest, response.Content, request.CustomerCode);
            //TahsildarThreeDGatewayResponse apiResponseData = JsonConvert.DeserializeObject<TahsildarThreeDGatewayResponse>(response.Content);
            //if (apiResponseData.success)
            //{
            //    return PaymentGatewayResult.Successed(apiResponseData.redirect_url, apiResponseData.message);
            //}
            //else
            //{
            //    return PaymentGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, apiResponseData.message, apiResponseData.errCode);
            //}
            return HostedPaymentResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "Bu tip Desteklenmiyor.", "TNP-002");
        }

        public VerifyGatewayResult VerifyGateway(VerifyGatewayRequest request, System.Web.HttpRequest httpRequest)
        {
            string result = httpRequest.Form.Get("result");
            string success = httpRequest.Form.Get("success");
            string message = httpRequest.Form.Get("message");
            string errCode = httpRequest.Form.Get("errCode");
            string order_no = httpRequest.Form.Get("request[order_no]");
            if (success == "false")
                return VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, message, order_no, errCode);
            else
                return VerifyGatewayResult.Successed(request.ReservationID, request.AuthCode, order_no);
        }

        public CancelPaymentResult CancelRequest(CancelPaymentRequest request)
        {
            throw new NotImplementedException();
        }

        public PaymentDetailResult PaymentDetailRequest(PaymentDetailRequest request)
        {
            throw new NotImplementedException();
        }

        public RefundPaymentResult RefundRequest(RefundPaymentRequest request)
        {
            throw new NotImplementedException();
        }


        public string GetHeaderAuthentication(PaymentCredentialParameter credential, string price, string currency, string installment, string order_no)
        {
            string hashStr = (price + currency + installment + order_no + credential.SecretKey);
            string hash = GetSHA1(hashStr).ToUpper();
            return hash;
        }
        private string GetSHA1(string text)
        {
            var provider = CodePagesEncodingProvider.Instance;
            Encoding.RegisterProvider(provider);

            var cryptoServiceProvider = new SHA1CryptoServiceProvider();
            var inputbytes = cryptoServiceProvider.ComputeHash(Encoding.GetEncoding("ISO-8859-9").GetBytes(text));

            var builder = new StringBuilder();
            for (int i = 0; i < inputbytes.Length; i++)
            {
                builder.Append(string.Format("{0,2:x}", inputbytes[i]).Replace(" ", "0"));
            }

            return builder.ToString().ToUpper();
        }
        public Dictionary<string, string> TestParameters => new Dictionary<string, string> { };
    }
}
