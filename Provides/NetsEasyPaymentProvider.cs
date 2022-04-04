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
    public class NetsEasyPaymentProvider : IPaymentProvider
    {
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
        public PaymentGatewayResult ThreeDGatewayRequest(PaymentGatewayRequest request)
        {
            return PaymentGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "this type is not supported");
        }

        public HostedPaymentResult HostedPaymentPageRequest(HostedPaymentRequest request)
        {
            Models.LogModel.Log(request.ReservationID.ToInt(), "NetsEasyPaymentProvider: HostedPaymentPageRequest - Start", request, null, request.CustomerCode);
            PaymentCredentialParameter credential = new PaymentCredentialParameter().GetCredential(request.CustomerCode, request.BankServiceName, request.Market);

            if (request.Development && credential == null)
            {
                credential = new PaymentCredentialParameter();
                credential.ApiUrl = "https://api.dibspayment.eu/";
                credential.ApiKey = "live-checkout-key-ff30aa47ca884c54a4f432c26ac1ddbc";
                credential.SecretKey = "live-secret-key-c51c29dd96804439be6bbe76273cee3c";
                credential.MerchantID = "100032212";
            }

            NetsEasyConditions netseasyConditions = new NetsEasyConditions();
            bool credentialConditionResult = netseasyConditions.Credential(credential);
            if (!credentialConditionResult) return HostedPaymentResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "Please fill all criteria.", "TNC-02");

            var generalInfo = new GeneralServices().GetPaymentGeneralInfo(BaseSiteUrl: request.BaseSiteUrl, ReservationID: request.ReservationID, ShoppingFileID: request.AuthCode);

            String PurchAmount = (Math.Ceiling(request.Amount)).ToString().Replace(",", "").Replace(".", "") + "00";
            Console.WriteLine(PurchAmount);

            NetsEasyHostedPaymentRequest netsEasyHostedPaymentRequest = new NetsEasyHostedPaymentRequest();
            netsEasyHostedPaymentRequest.checkout = new Checkout()
            {
                cancelUrl = generalInfo.CallbackUrl,
                integrationType = "HostedPaymentPage",
                returnUrl = generalInfo.CallbackUrl,
                termsUrl = "https://www.mixxtravel.se/",
            };
            netsEasyHostedPaymentRequest.order = new NetsEasyOrder()
            {
                amount = Convert.ToInt32(PurchAmount),
                currency = request.CurrencyIsoCode.ToString(),
                reference = request.VoucherNo,
                items = new List<Item>() { new Item() {
                grossTotalAmount = Convert.ToInt32(PurchAmount),
                name = "Package",
                netTotalAmount = Convert.ToInt32(PurchAmount),
                quantity = 1,
                reference = "1",
                unit = "pcs",
                unitPrice = Convert.ToInt32(PurchAmount),
                }}
            };
            netsEasyHostedPaymentRequest.merchantNumber = credential.MerchantID;
            Models.LogModel.Log(request.ReservationID.ToInt(), "NetsEasyPaymentProvider: HostedPaymentPageRequest - Post Start", netsEasyHostedPaymentRequest, null, request.CustomerCode);
            var client = new RestClient(credential.ApiUrl + "v1/payments");
            var apiRequest = new RestRequest(Method.POST);
            apiRequest.AddHeader("content-type", "application/*+json");
            apiRequest.AddHeader("Authorization", credential.SecretKey);
            apiRequest.AddJsonBody(netsEasyHostedPaymentRequest);
            IRestResponse response = client.Execute(apiRequest);
            Models.LogModel.Log(request.ReservationID.ToInt(), "NetsEasyPaymentProvider: HostedPaymentPageRequest - Post End", response.StatusCode, response.Content, request.CustomerCode);
            if (response.StatusCode == HttpStatusCode.Created)
            {
                NetsEasyHostedPaymentResponse apiResponseData = JsonConvert.DeserializeObject<NetsEasyHostedPaymentResponse>(response.Content);
                apiResponseData.hostedPaymentPageUrl = apiResponseData.hostedPaymentPageUrl + "&language=" + LanguageCodes[request.LanguageIsoCode];
                return HostedPaymentResult.Successed(apiResponseData.hostedPaymentPageUrl, "", paymentId: apiResponseData.paymentId);
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return HostedPaymentResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "Unauthorized", "401");
            }
            else
            {
                NetsEasyHostedPaymentErrorResponse apiResponseData = JsonConvert.DeserializeObject<NetsEasyHostedPaymentErrorResponse>(response.Content);
                return HostedPaymentResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, apiResponseData.message, response.StatusCode.GetHashCode().ToString());
            }
        }

        public VerifyGatewayResult VerifyGateway(VerifyGatewayRequest request, System.Web.HttpRequest httpRequest)
        {
            Models.LogModel.Log(request.ReservationID.ToInt(), "NetsEasyPaymentProvider: HostedPaymentPageRequest - Start", request, null, request.CustomerCode);
            PaymentCredentialParameter credential = new PaymentCredentialParameter().GetCredential(request.CustomerCode, request.BankServiceName, request.Market);

            string paymentid = request.PaymentId;

            if (request.Development && credential == null)
            {
                credential = new PaymentCredentialParameter();
                credential.ApiUrl = "https://api.dibspayment.eu/";
                credential.ApiKey = "live-checkout-key-ff30aa47ca884c54a4f432c26ac1ddbc";
                credential.SecretKey = "live-secret-key-c51c29dd96804439be6bbe76273cee3c";
                credential.MerchantID = "100032212";
            }

            var client = new RestClient(credential.ApiUrl + "v1/payments/" + paymentid);
            var apiRequest = new RestRequest(Method.GET);
            apiRequest.AddHeader("content-type", "application/*+json");
            apiRequest.AddHeader("Authorization", credential.SecretKey);
            IRestResponse response = client.Execute(apiRequest);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                NetsEasyVerifyGatewayResponse apiResponseData = JsonConvert.DeserializeObject<NetsEasyVerifyGatewayResponse>(response.Content);
                if (apiResponseData.payment.summary != null && apiResponseData.payment.summary.reservedAmount > 0)
                {
                    return VerifyGatewayResult.Successed(request.ReservationID, request.AuthCode, paymentid);
                }
                else
                {
                    return VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "Payment failed", paymentid);
                }
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "Unauthorized", paymentid);
            }
            else
            {
                NetsEasyHostedPaymentErrorResponse apiResponseData = JsonConvert.DeserializeObject<NetsEasyHostedPaymentErrorResponse>(response.Content);
                return VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, apiResponseData.message, paymentid);
            }
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

            return hashStr;
        }

        public Dictionary<string, string> TestParameters => new Dictionary<string, string> { };
    }
}
