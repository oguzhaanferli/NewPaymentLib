using Microsoft.Extensions.Primitives;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Lib.Payment;
using Lib.Payment.Models;
using Lib.Payment.Models.ProviderConditions;
using Lib.Payment.Requests;
using Lib.Payment.Results;
using Lib.Payment.Services;
using Lib.Extension.v2;

namespace Lib.Payment.Providers
{
    public class DenizbankPaymentProvider : IPaymentProvider
    {
        private readonly HttpClient client;

        public PaymentGatewayResult ThreeDGatewayRequest(PaymentGatewayRequest request)
        {
            try
            {
                Models.LogModel.Log(request.ReservationID.ToInt(), "DenizbankPaymentProvider: ThreeDGatewayRequest - Start", request, null, request.CustomerCode);
                var credential = new PaymentCredentialParameter().GetCredential(request.CustomerCode, request.BankServiceName, request.Market);
                if (!request.Development)
                    CardInformation.Mask(request.ReservationID.ToInt(), request.AuthCode, request.CustomerCode);
                if (request.Development && credential == null)
                {
                    credential = new PaymentCredentialParameter();
                    credential.ApiUrl = "https://spos.denizbank.com/mpi/3DHost.aspx";
                    credential.GatewayUrl = "https://spos.denizbank.com/mpi/Default.aspx";
                    credential.UserName = "InterTestApi";
                    credential.Password = "3";
                    credential.StoreKey = "3123";
                    credential.MerchantPass = "gDg1N";
                }

                DenizbankConditions denizConditions = new DenizbankConditions();
                bool credentialConditionResult = denizConditions.Credential(credential);
                if (!credentialConditionResult) return PaymentGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "Please fill all criteria.");

                var generalInfo = new GeneralServices().GetPaymentGeneralInfo(request.BaseSiteUrl, request.ReservationID, request.CardNumber, request.AuthCode);
                string random = DateTime.Now.ToString();
                string totalAmount = request.Amount.ToString(new CultureInfo("en-US"));
                string installment = request.Installment.ToString();
                if (request.Installment < 2) installment = string.Empty;//0 veya 1 olması durumunda taksit bilgisini boş gönderiyoruz
                var hashBuilder = new StringBuilder();
                hashBuilder.Append(credential.StoreKey);
                hashBuilder.Append(generalInfo.OrderID);
                hashBuilder.Append(totalAmount);
                hashBuilder.Append(generalInfo.CallbackUrl);
                hashBuilder.Append(generalInfo.CallbackUrl);
                hashBuilder.Append(credential.TxnType);
                hashBuilder.Append(installment);
                hashBuilder.Append(random);
                hashBuilder.Append(credential.MerchantPass);

                var hashData = GetSHA1(hashBuilder.ToString());
                Models.LogModel.Log(request.ReservationID.ToInt(), "DenizbankPaymentProvider: ThreeDGatewayRequest - Hash", hashBuilder, hashData, request.CustomerCode);


                var parameters = new Dictionary<string, object>();
                parameters.Add("ShopCode", credential.StoreKey);
                parameters.Add("OrderId", generalInfo.OrderID);//sipariş numarası
                parameters.Add("OkUrl", generalInfo.CallbackUrl);//başarılı dönüş adresi
                parameters.Add("FailUrl", generalInfo.CallbackUrl);//hatalı dönüş adresi
                parameters.Add("TxnType", credential.TxnType);//direk satış
                parameters.Add("Rnd", random);//rastgele bir sayı üretilmesi isteniyor
                parameters.Add("Currency", request.CurrencyIsoCode.GetHashCode());//TL ISO code | EURO 978 | Dolar 840
                parameters.Add("Pan", request.CardNumber);
                parameters.Add("Expiry", $"{request.CardMonth}{request.CardYear.ToString().Substring(2, 2)}");//kart bitiş ay-yıl birleşik
                parameters.Add("Cvv2", request.CardCV2);//kart güvenlik kodu
                parameters.Add("CartType", generalInfo.CardType);//kart tipi visa 1 | master 2 | amex 3
                parameters.Add("SecureType", "3DPay");
                parameters.Add("Lang", request.LanguageIsoCode.ToString().ToUpper());//iki haneli dil iso kodu
                parameters.Add("PurchAmount", totalAmount);
                parameters.Add("InstallmentCount", installment);//taksit sayısı | 1 veya boş tek çekim olur
                parameters.Add("taksitsayisi", installment);//taksit sayısı | 1 veya boş tek çekim olur
                parameters.Add("Hash", hashData);//hash data

                return (PaymentGatewayResult.Successed(parameters, credential.GatewayUrl));
            }
            catch (Exception ex)
            {
                return (PaymentGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, ex.Message.ToString()));
            }
        }


        public VerifyGatewayResult VerifyGateway(VerifyGatewayRequest request, System.Web.HttpRequest httpRequest)
        {
            var form = httpRequest.Form;
            Models.LogModel.Log(request.ReservationID.ToInt(), "NestPayPaymentProvider: VerifyGateway - Start", request, form, request.CustomerCode);
            var credential = new PaymentCredentialParameter().GetCredential(request.CustomerCode, request.BankServiceName, request.Market);
            if (form == null)
            {
                return (VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "Form verisi alınamadı.", form["OrderId"]));
            }

            var mdStatus = form["mdStatus"];
            if (StringValues.IsNullOrEmpty(mdStatus))
            {
                return (VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, form["mdErrorMsg"], form["OrderId"], form["ProcReturnCode"]));
            }

            var response = form["Response"];
            //mdstatus 1,2,3 veya 4 olursa 3D doğrulama geçildi anlamına geliyor
            if (!mdStatus.Equals("1") || !mdStatus.Equals("2") || !mdStatus.Equals("3") || !mdStatus.Equals("4"))
            {
                return (VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, $"{response} - {form["mdErrorMsg"]}", form["OrderId"], form["ProcReturnCode"]));
            }

            if (StringValues.IsNullOrEmpty(response) || !response.Equals("Approved"))
            {
                return (VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, $"{response} - {form["ErrorMessage"]}", form["OrderId"], form["ProcReturnCode"]));
            }

            var hashBuilder = new StringBuilder();
            hashBuilder.Append(credential.StoreKey);
            hashBuilder.Append(form["Version"].FirstOrDefault());
            hashBuilder.Append(form["PurchAmount"].FirstOrDefault());
            hashBuilder.Append(form["Exponent"].FirstOrDefault());
            hashBuilder.Append(form["Currency"].FirstOrDefault());
            hashBuilder.Append(form["OkUrl"].FirstOrDefault());
            hashBuilder.Append(form["FailUrl"].FirstOrDefault());
            hashBuilder.Append(form["MD"].FirstOrDefault());
            hashBuilder.Append(form["OrderId"].FirstOrDefault());
            hashBuilder.Append(form["ProcReturnCode"].FirstOrDefault());
            hashBuilder.Append(form["Response"].FirstOrDefault());
            hashBuilder.Append(form["mdStatus"].FirstOrDefault());
            hashBuilder.Append(credential.MerchantPass);

            var hashData = GetSHA1(hashBuilder.ToString());
            if (!form["HASH"].Equals(hashData))
            {
                return (VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "Güvenlik imza doğrulaması geçersiz.", form["OrderId"]));
            }

            int.TryParse(form["taksitsayisi"], out int taksitSayisi);
            int.TryParse(form["EXTRA.ARTITAKSIT"], out int extraTaksitSayisi);

            return (VerifyGatewayResult.Successed(request.ReservationID, request.AuthCode, form["OrderId"]));
        }

        public CancelPaymentResult CancelRequest(CancelPaymentRequest request)
        {
            string shopCode = request.BankParameters["shopCode"];
            string userCode = request.BankParameters["cancelUserCode"];
            string userPass = request.BankParameters["cancelUserPass"];

            var formBuilder = new StringBuilder();
            formBuilder.AppendFormat("ShopCode={0}&", shopCode);
            formBuilder.AppendFormat("PurchAmount={0}&", request.TotalAmount.ToString(new CultureInfo("en-US")));
            formBuilder.AppendFormat("Currency={0}&", request.CurrencyIsoCode);
            formBuilder.Append("OrderId=&");
            formBuilder.Append("TxnType=Void&");
            formBuilder.AppendFormat("orgOrderId={0}&", request.OrderNumber);
            formBuilder.AppendFormat("UserCode={0}&", userCode);
            formBuilder.AppendFormat("UserPass={0}&", userPass);
            formBuilder.Append("SecureType=NonSecure&");
            formBuilder.AppendFormat("Lang={0}&", request.LanguageIsoCode.ToUpper());
            formBuilder.Append("MOTO=0");

            //TODO: Denizbank CancelRequest
            var restclient = new RestClient("");
            var restrequest = new RestRequest("/api/Token/GetToken", Method.POST);
            restrequest.AddBody(formBuilder);
            string responseContent = restclient.Execute<string>(restrequest).Data;

            if (string.IsNullOrEmpty(responseContent))
                return CancelPaymentResult.Failed("İptal işlemi başarısız.");

            responseContent = responseContent.Replace(";;", ";").Replace(";", "&");
            var responseParams = HttpUtility.ParseQueryString(responseContent);

            if (responseParams["ProcReturnCode"] != "00")
                return CancelPaymentResult.Failed(responseParams["ErrorMessage"]);

            return CancelPaymentResult.Successed(responseParams["TransId"], responseParams["TransId"]);
        }

        public RefundPaymentResult RefundRequest(RefundPaymentRequest request)
        {
            string shopCode = request.BankParameters["shopCode"];
            string userCode = request.BankParameters["refundUserCode"];
            string userPass = request.BankParameters["refundUserPass"];

            var formBuilder = new StringBuilder();
            formBuilder.AppendFormat("ShopCode={0}&", shopCode);
            formBuilder.AppendFormat("PurchAmount={0}&", request.TotalAmount.ToString(new CultureInfo("en-US")));
            formBuilder.AppendFormat("Currency={0}&", request.CurrencyIsoCode);
            formBuilder.Append("OrderId=&");
            formBuilder.Append("TxnType=Refund&");
            formBuilder.AppendFormat("orgOrderId={0}&", request.OrderNumber);
            formBuilder.AppendFormat("UserCode={0}&", userCode);
            formBuilder.AppendFormat("UserPass={0}&", userPass);
            formBuilder.Append("SecureType=NonSecure&");
            formBuilder.AppendFormat("Lang={0}&", request.LanguageIsoCode.ToUpper());
            formBuilder.Append("MOTO=0");

            //TODO: Denizbank RefundRequest
            var restclient = new RestClient("");
            var restrequest = new RestRequest("/api/Token/GetToken", Method.POST);
            restrequest.AddBody(formBuilder);
            string responseContent = restclient.Execute<string>(restrequest).Data;

            if (string.IsNullOrEmpty(responseContent))
                return RefundPaymentResult.Failed("İade işlemi başarısız.");

            responseContent = responseContent.Replace(";;", ";").Replace(";", "&");
            var responseParams = HttpUtility.ParseQueryString(responseContent);

            if (responseParams["ProcReturnCode"] != "00")
                return RefundPaymentResult.Failed(responseParams["ErrorMessage"]);

            return RefundPaymentResult.Successed(responseParams["TransId"], responseParams["TransId"]);
        }

        public PaymentDetailResult PaymentDetailRequest(PaymentDetailRequest request)
        {
            string shopCode = request.BankParameters["shopCode"];
            string userCode = request.BankParameters["userCode"];
            string userPass = request.BankParameters["userPass"];

            var formBuilder = new StringBuilder();
            formBuilder.AppendFormat("ShopCode={0}&", shopCode);
            formBuilder.AppendFormat("Currency={0}&", request.CurrencyIsoCode);
            formBuilder.Append("TxnType=StatusHistory&");
            formBuilder.AppendFormat("orgOrderId={0}&", request.OrderNumber);
            formBuilder.AppendFormat("UserCode={0}&", userCode);
            formBuilder.AppendFormat("UserPass={0}&", userPass);
            formBuilder.Append("SecureType=NonSecure&");
            formBuilder.AppendFormat("Lang={0}&", request.LanguageIsoCode.ToUpper());

            //TODO: Denizbank PaymentDetailRequest
            var restclient = new RestClient("");
            var restrequest = new RestRequest("/api/Token/GetToken", Method.POST);
            restrequest.AddBody(formBuilder);
            string responseContent = restclient.Execute<string>(restrequest).Data;

            if (string.IsNullOrEmpty(responseContent))
                return PaymentDetailResult.FailedResult(errorMessage: "İade işlemi başarısız.");

            responseContent = responseContent.Replace(";;", ";").Replace(";", "&");
            var responseParams = HttpUtility.ParseQueryString(responseContent);

            if (responseParams["ProcReturnCode"] != "00")
                return PaymentDetailResult.FailedResult(errorMessage: responseParams["ErrorMessage"], errorCode: responseParams["ErrorCode"]);

            return PaymentDetailResult.PaidResult(responseParams["TransId"], responseParams["TransId"]);
        }

        public Dictionary<string, string> TestParameters => new Dictionary<string, string>
        {
            { "shopCode", "" },
            { "txnType", "" },
            { "storeKey", "" },
            { "secureType", "" },
            { "gatewayUrl", "https://spos.denizbank.com/mpi/Default.aspx" },
            { "userCode", "" },
            { "userPass", "" },
            { "verifyUrl", "https://spos.denizbank.com/mpi/Default.aspx" }
        };

        private string GetSHA1(string text)
        {
            var cryptoServiceProvider = new SHA1CryptoServiceProvider();
            var inputbytes = cryptoServiceProvider.ComputeHash(Encoding.UTF8.GetBytes(text));
            var hashData = Convert.ToBase64String(inputbytes);

            return hashData;
        }

        public HostedPaymentResult HostedPaymentPageRequest(HostedPaymentRequest request)
        {
            throw new NotImplementedException();
        }
    }
}