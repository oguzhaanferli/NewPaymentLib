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
using System.Xml;
using Lib.Payment.Models;
using Lib.Payment.Models.ProviderConditions;
using Lib.Payment.Provides.Request;
using Lib.Payment.Provides.Response;
using Lib.Payment.Requests;
using Lib.Payment.Results;
using Lib.Payment.Services;
using Lib.DataModel.v2.Entities.BOOKING;
using Lib.Extension.v2;

namespace Lib.Payment.Providers
{
    public class NestPayPaymentProvider : IPaymentProvider
    {
        public PaymentGatewayResult ThreeDGatewayRequest(PaymentGatewayRequest request)
        {
            try
            {
                Models.LogModel.Log(request.ReservationID.ToInt(), "NestPayPaymentProvider: ThreeDGatewayRequest - Start", request, null, request.CustomerCode);
                var credential = new PaymentCredentialParameter().GetCredential(request.CustomerCode, request.BankServiceName, request.Market);
                if (!request.Development)
                    CardInformation.Mask(request.ReservationID.ToInt(), request.AuthCode, request.CustomerCode);

                NetsConditions netsConditions = new NetsConditions();
                bool credentialConditionResult = netsConditions.Credential(credential);
                if (!credentialConditionResult) return PaymentGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "Please fill all criteria.");

                var generalInfo = new GeneralServices().GetPaymentGeneralInfo(request.BaseSiteUrl, request.ReservationID, request.CardNumber, request.AuthCode);
                string random = DateTime.Now.ToString();
                string totalAmount = request.Amount.ToString(new CultureInfo("en-US"));
                string installment = request.Installment.ToString();
                if (request.Installment < 2) installment = string.Empty;//0 veya 1 olması durumunda taksit bilgisini boş gönderiyoruz
                var hashBuilder = new StringBuilder();
                hashBuilder.Append(credential.ClientID);
                hashBuilder.Append(generalInfo.OrderID);
                hashBuilder.Append(totalAmount);
                hashBuilder.Append(generalInfo.CallbackUrl);
                hashBuilder.Append(generalInfo.CallbackUrl);
                hashBuilder.Append(credential.StoreType);
                hashBuilder.Append(installment);
                hashBuilder.Append(random);
                hashBuilder.Append(credential.StoreKey);
                var hashData = GetSHA1(hashBuilder.ToString());
                Models.LogModel.Log(request.ReservationID.ToInt(), "NestPayPaymentProvider: ThreeDGatewayRequest - Hash", hashBuilder, hashData, request.CustomerCode);

                var parameters = new Dictionary<string, object>();
                parameters.Add("clientid", credential.ClientID);
                parameters.Add("pan", request.CardNumber);
                parameters.Add("cardHolderName", request.CardHolderName);
                parameters.Add("Ecom_Payment_Card_ExpDate_Month", request.CardMonth);//kart bitiş ay'ı
                parameters.Add("Ecom_Payment_Card_ExpDate_Year", Convert.ToString(request.CardYear).Substring(2, 2));//kart bitiş yıl'ı
                parameters.Add("cv2", request.CardCV2);//kart güvenlik kodu
                parameters.Add("rnd", random);//rastgele bir sayı üretilmesi isteniyor
                parameters.Add("currency", request.CurrencyIsoCode.GetHashCode().ToString());//ISO code TL 949 | EURO 978 | Dolar 840
                parameters.Add("lang", request.LanguageIsoCode.ToString());//iki haneli dil iso kodu
                parameters.Add("taksit", installment);//taksit sayısı | 1 veya boş tek çekim olur
                parameters.Add("amount", totalAmount);
                parameters.Add("islemtipi", credential.StoreType);//direk satış
                parameters.Add("cardType", generalInfo.CardType);//kart tipi visa 1 | master 2 | amex 3
                parameters.Add("storetype", credential.StoreType);
                parameters.Add("oid", generalInfo.OrderID);//sipariş numarası
                parameters.Add("okUrl", generalInfo.CallbackUrl);//başarılı dönüş adresi
                parameters.Add("failUrl", generalInfo.CallbackUrl);//hatalı dönüş adresi
                parameters.Add("hash", hashData);//hash data

                Models.LogModel.Log(request.ReservationID.ToInt(), "NestPayPaymentProvider: ThreeDGatewayRequest - Success", parameters, null, request.CustomerCode);
                return PaymentGatewayResult.Successed(parameters, credential.GatewayUrl);
            }
            catch (Exception ex)
            {
                return PaymentGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, ex.Message.ToString());
            }
        }

        public VerifyGatewayResult VerifyGateway(VerifyGatewayRequest request, System.Web.HttpRequest httpRequest)
        {
            var form = httpRequest.Form;
            Models.LogModel.Log(request.ReservationID.ToInt(), "NestPayPaymentProvider: VerifyGateway - Start", request, form, request.CustomerCode);
            var credential = new PaymentCredentialParameter().GetCredential(request.CustomerCode, request.BankServiceName, request.Market);
            if (form == null) return VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "Form verisi alınamadı.", form["oid"]);

            var mdStatus = form["mdStatus"].ToString();
            if (string.IsNullOrEmpty(mdStatus)) return (VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, form["mdErrorMsg"], form["oid"], form["ProcReturnCode"]));

            //mdstatus 1,2,3 veya 4 olursa 3D doğrulama geçildi anlamına geliyor
            if (!mdStatusCodes.Contains(mdStatus))
                return (VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, $"{form["mdErrorMsg"]}", form["oid"], form["ProcReturnCode"]));

            //TODO: bankadan dönen hash doğrulaması yapılacak
            //string response = form["Response"];

            //if (string.IsNullOrEmpty(response) || !response.Equals("Approved"))
            //    return (VerifyGatewayResult.Failed($"{response} - {form["ErrMsg"]}", form["ProcReturnCode"]));

            //var hashBuilder = new StringBuilder();
            //hashBuilder.Append(credential.ClientID);
            //hashBuilder.Append(form["oid"].FirstOrDefault());
            //hashBuilder.Append(form["AuthCode"].FirstOrDefault());
            //hashBuilder.Append(form["ProcReturnCode"].FirstOrDefault());
            //hashBuilder.Append(form["Response"].FirstOrDefault());
            //hashBuilder.Append(form["mdStatus"].FirstOrDefault());
            //hashBuilder.Append(form["cavv"].FirstOrDefault());
            //hashBuilder.Append(form["eci"].FirstOrDefault());
            //hashBuilder.Append(form["md"].FirstOrDefault());
            //hashBuilder.Append(form["rnd"].FirstOrDefault());
            //hashBuilder.Append(credential.StoreKey);

            //var hashData = GetSHA1(hashBuilder.ToString());
            //if (!form["HASH"].Equals(hashData))
            //{
            //    return (VerifyGatewayResult.Failed("Güvenlik imza doğrulaması geçersiz."));
            //}

            int.TryParse(form["taksit"], out int installment);
            int.TryParse(form["EXTRA.HOSTMSG"], out int extraInstallment);


            if (request.CustomerIpAddress == "::1") request.CustomerIpAddress = "127.0.0.1";

            var xmldata = new CC5Request()
            {
                Name = credential.UserName,
                Password = credential.Password,
                ClientId = credential.ClientID,
                OrderId = form["oid"],
                IPAddress = request.CustomerIpAddress,
                Type = "Auth",
                Number = form["md"],
                Total = form["amount"],
                Currency = request.CurrencyIsoCode.GetHashCode().ToString(),
                Taksit = form["taksit"],
                PayerTxnId = form["xid"],
                PayerSecurityLevel = form["eci"],
                PayerAuthenticationCode = form["cavv"],
            };
            Models.LogModel.Log(request.ReservationID.ToInt(), "NestPayPaymentProvider: CC5Request - Start", xmldata, form, request.CustomerCode);
            var restclient = new RestClient(credential.ApiUrl);
            var restrequest = new RestRequest("", Method.POST);
            restrequest.AddXmlBody(xmldata);
            var dasda = restclient.Execute<NetsPayVerifyGatewayResponse>(restrequest);
            NetsPayVerifyGatewayResponse responseContent = dasda.Data;
            Models.LogModel.Log(request.ReservationID.ToInt(), "NestPayPaymentProvider: CC5Request - End", xmldata, responseContent, request.CustomerCode);
            if (responseContent.Response == "Approved")
            {
                return (VerifyGatewayResult.Successed(request.ReservationID, request.AuthCode, form["xid"], installment, extraInstallment, responseContent.Response, responseContent.ProcReturnCode));
            }
            return (VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, responseContent.ErrMsg, form["oid"], responseContent.ProcReturnCode));
        }

        public CancelPaymentResult CancelRequest(CancelPaymentRequest request)
        {
            string clientId = "";
            string userName = "";
            string password = "";

            string requestXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                                    <CC5Request>
                                      <Name>{userName}</Name>
                                      <Password>{password}</Password>
                                      <ClientId>{clientId}</ClientId>
                                      <Type>Void</Type>
                                      <OrderId>{request.OrderNumber}</OrderId>
                                    </CC5Request>";

            //TODO: Netspay CancelRequest
            var restclient = new RestClient("");
            var restrequest = new RestRequest("/api/Token/GetToken", Method.POST, DataFormat.Xml);
            restrequest.AddXmlBody(requestXml);
            string responseContent = restclient.Execute<string>(restrequest).Data;

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(responseContent);

            if (xmlDocument.SelectSingleNode("CC5Response/Response") == null ||
                xmlDocument.SelectSingleNode("CC5Response/Response").InnerText != "Approved")
            {
                var errorMessage = xmlDocument.SelectSingleNode("CC5Response/ErrMsg")?.InnerText ?? string.Empty;
                if (string.IsNullOrEmpty(errorMessage))
                    errorMessage = "Bankadan hata mesajı alınamadı.";

                return CancelPaymentResult.Failed(errorMessage);
            }

            if (xmlDocument.SelectSingleNode("CC5Response/ProcReturnCode") == null ||
                xmlDocument.SelectSingleNode("CC5Response/ProcReturnCode").InnerText != "00")
            {
                var errorMessage = xmlDocument.SelectSingleNode("CC5Response/ErrMsg")?.InnerText ?? string.Empty;
                if (string.IsNullOrEmpty(errorMessage))
                    errorMessage = "Bankadan hata mesajı alınamadı.";

                return CancelPaymentResult.Failed(errorMessage);
            }

            var transactionId = xmlDocument.SelectSingleNode("CC5Response/TransId")?.InnerText ?? string.Empty;
            return CancelPaymentResult.Successed(transactionId, transactionId);
        }

        public RefundPaymentResult RefundRequest(RefundPaymentRequest request)
        {
            string clientId = request.BankParameters["clientId"];
            string userName = request.BankParameters["refundUsername"];
            string password = request.BankParameters["refundUserPassword"];

            string requestXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                                    <CC5Request>
                                      <Name>{userName}</Name>
                                      <Password>{password}</Password>
                                      <ClientId>{clientId}</ClientId>
                                      <Type>Credit</Type>
                                      <OrderId>{request.OrderNumber}</OrderId>
                                    </CC5Request>";

            //TODO: Netspay RefundRequest
            var restclient = new RestClient("");
            var restrequest = new RestRequest("/api/Token/GetToken", Method.POST, DataFormat.Xml);
            restrequest.AddXmlBody(requestXml);
            string responseContent = restclient.Execute<string>(restrequest).Data;

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(responseContent);

            if (xmlDocument.SelectSingleNode("CC5Response/Response") == null ||
                xmlDocument.SelectSingleNode("CC5Response/Response").InnerText != "Approved")
            {
                var errorMessage = xmlDocument.SelectSingleNode("CC5Response/ErrMsg")?.InnerText ?? string.Empty;
                if (string.IsNullOrEmpty(errorMessage))
                    errorMessage = "Bankadan hata mesajı alınamadı.";

                return RefundPaymentResult.Failed(errorMessage);
            }

            if (xmlDocument.SelectSingleNode("CC5Response/ProcReturnCode") == null ||
                xmlDocument.SelectSingleNode("CC5Response/ProcReturnCode").InnerText != "00")
            {
                var errorMessage = xmlDocument.SelectSingleNode("CC5Response/ErrMsg")?.InnerText ?? string.Empty;
                if (string.IsNullOrEmpty(errorMessage))
                    errorMessage = "Bankadan hata mesajı alınamadı.";

                return RefundPaymentResult.Failed(errorMessage);
            }

            var transactionId = xmlDocument.SelectSingleNode("CC5Response/TransId")?.InnerText ?? string.Empty;
            return RefundPaymentResult.Successed(transactionId, transactionId);
        }

        public PaymentDetailResult PaymentDetailRequest(PaymentDetailRequest request)
        {
            string clientId = "";
            string userName = "";
            string password = "";

            string requestXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                                    <CC5Request>
                                        <Name>{userName}</Name>
                                        <Password>{password}</Password>
                                        <ClientId>{clientId}</ClientId>
                                        <OrderId>{request.OrderNumber}</OrderId>
                                        <Extra>
                                            <ORDERDETAIL>QUERY</ORDERDETAIL>
                                        </Extra>
                                    </CC5Request>";

            //TODO: Netspay PaymentDetailRequest
            var restclient = new RestClient("");
            var restrequest = new RestRequest("/api/Token/GetToken", Method.POST, DataFormat.Xml);
            restrequest.AddXmlBody(requestXml);
            string responseContent = restclient.Execute<string>(restrequest).Data;

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(responseContent);

            string finalStatus = xmlDocument.SelectSingleNode("CC5Response/Extra/ORDER_FINAL_STATUS")?.InnerText ?? string.Empty;
            string transactionId = xmlDocument.SelectSingleNode("CC5Response/Extra/TRX_1_TRAN_UID")?.InnerText;
            string referenceNumber = xmlDocument.SelectSingleNode("CC5Response/Extra/TRX_1_TRAN_UID")?.InnerText;
            string cardPrefix = xmlDocument.SelectSingleNode("CC5Response/Extra/TRX_1_CARDBIN")?.InnerText;
            int.TryParse(cardPrefix, out int cardPrefixValue);

            string installment = xmlDocument.SelectSingleNode("CC5Response/Extra/TRX_1_INSTALMENT")?.InnerText ?? "0";
            string bankMessage = xmlDocument.SelectSingleNode("CC5Response/Response")?.InnerText;
            string responseCode = xmlDocument.SelectSingleNode("CC5Response/ProcReturnCode")?.InnerText;

            if (finalStatus.Equals("SALE", StringComparison.OrdinalIgnoreCase))
            {
                int.TryParse(installment, out int installmentValue);
                return PaymentDetailResult.PaidResult(transactionId, referenceNumber, cardPrefixValue.ToString(), installmentValue, 0, bankMessage, responseCode);
            }
            else if (finalStatus.Equals("VOID", StringComparison.OrdinalIgnoreCase))
            {
                return PaymentDetailResult.CanceledResult(transactionId, referenceNumber, bankMessage, responseCode);
            }
            else if (finalStatus.Equals("REFUND", StringComparison.OrdinalIgnoreCase))
            {
                return PaymentDetailResult.RefundedResult(transactionId, referenceNumber, bankMessage, responseCode);
            }

            var errorMessage = xmlDocument.SelectSingleNode("CC5Response/ErrMsg")?.InnerText ?? string.Empty;
            if (string.IsNullOrEmpty(errorMessage))
                errorMessage = "Bankadan hata mesajı alınamadı.";

            return PaymentDetailResult.FailedResult(errorMessage: errorMessage);
        }

        public Dictionary<string, string> TestParameters => new Dictionary<string, string>
        {
            { "clientId", "700655000200" },
            { "processType", "Auth" },
            { "storeKey", "TRPS0200" },
            { "storeType", "3D_PAY" },
            { "gatewayUrl", "https://entegrasyon.asseco-see.com.tr/fim/est3Dgate" },
            { "userName", "ISBANKAPI" },
            { "password", "ISBANK07" },
            { "verifyUrl", "https://entegrasyon.asseco-see.com.tr/fim/api" }
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

        private static readonly string[] mdStatusCodes = new[] { "1", "2", "3", "4" };
    }
}