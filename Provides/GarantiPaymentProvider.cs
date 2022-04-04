using Microsoft.Extensions.Primitives;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
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
    public class GarantiPaymentProvider : IPaymentProvider
    {
        public PaymentGatewayResult ThreeDGatewayRequest(PaymentGatewayRequest request)
        {
            try
            {
                Models.LogModel.Log(request.ReservationID.ToInt(), "GarantiPaymentProvider: ThreeDGatewayRequest - Start", request, null, request.CustomerCode);
                var credential = new PaymentCredentialParameter().GetCredential(request.CustomerCode, request.BankServiceName, request.Market);

                GarantiConditions netsConditions = new GarantiConditions();
                bool credentialConditionResult = netsConditions.Credential(credential);
                if (!credentialConditionResult) return PaymentGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "Please fill all criteria.");

                var generalInfo = new GeneralServices().GetPaymentGeneralInfo(request.BaseSiteUrl, request.ReservationID, request.CardNumber, request.AuthCode);

                string terminalId = credential.TerminalID;
                string terminalUserId = credential.UserName;
                string terminalMerchantId = credential.MerchantID;
                string terminalProvUserId = credential.ProvUserID;
                string terminalProvPassword = credential.Password;
                string storeKey = credential.StoreKey;
                string type = "sales";
                string amount = (request.Amount.ToDecimal() * 100m).ToString("0.##", new CultureInfo("en-US"));//virgülden sonraki sıfırlara gerek yok
                string installment = request.Installment.ToString();
                if (request.Installment < 2) installment = string.Empty;

                string securityData = GetSHA1($"{terminalProvPassword}{credential.TerminalID1}");

                var hashBuilder = new StringBuilder();
                hashBuilder.Append(terminalId);
                hashBuilder.Append(generalInfo.OrderID);
                hashBuilder.Append(amount);
                hashBuilder.Append(generalInfo.CallbackUrl);
                hashBuilder.Append(generalInfo.CallbackUrl);
                hashBuilder.Append(type);
                hashBuilder.Append(installment);
                hashBuilder.Append(storeKey);
                hashBuilder.Append(securityData);
                var hashData = GetSHA1(hashBuilder.ToString());

                var parameters = new Dictionary<string, object>();
                parameters.Add("mode", "PROD");
                parameters.Add("apiversion", "v0.01");
                parameters.Add("secure3dsecuritylevel", "3D");//SMS onaylı ödeme modeli 3DPay olarak adlandırılıyor.
                parameters.Add("terminalprovuserid", terminalProvUserId);
                parameters.Add("terminaluserid", terminalProvUserId);
                parameters.Add("terminalmerchantid", terminalMerchantId);
                parameters.Add("txntype", type);//direk satış
                parameters.Add("txnamount", amount);
                parameters.Add("txncurrencycode", request.CurrencyIsoCode.GetHashCode().ToString());//TL ISO code | EURO 978 | Dolar 840
                parameters.Add("txninstallmentcount", installment);//taksit sayısı | boş tek çekim olur
                parameters.Add("orderid", generalInfo.OrderID);//sipariş numarası
                parameters.Add("terminalid", terminalId);
                parameters.Add("successurl", generalInfo.CallbackUrl);//başarılı dönüş adresi
                parameters.Add("errorurl", generalInfo.CallbackUrl);//hatalı dönüş adresi
                parameters.Add("customeremailaddress", "eticaret@garanti.com.tr");
                parameters.Add("customeripaddress", "95.9.134.101");
                parameters.Add("secure3dhash", hashData);
                parameters.Add("cardnumber", request.CardNumber);
                parameters.Add("cardexpiredatemonth", request.CardMonth);//kart bitiş ay'ı
                parameters.Add("cardexpiredateyear", request.CardYear.Substring(2, 2));//kart bitiş yıl'ı
                parameters.Add("cardcvv2", request.CardCV2);//kart güvenlik kodu
                parameters.Add("lang", request.LanguageIsoCode.ToString());

                Models.LogModel.Log(request.ReservationID.ToInt(), "GarantiPaymentProvider: ThreeDGatewayRequest - Return", parameters, null, request.CustomerCode);
                return (PaymentGatewayResult.Successed(parameters, credential.GatewayUrl));
            }
            catch (Exception ex)
            {
                return (PaymentGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, ex.Message.ToString()));
            }
        }

        public VerifyGatewayResult VerifyGateway(VerifyGatewayRequest request, System.Web.HttpRequest httpRequest)
        {
            Models.LogModel.Log(request.ReservationID.ToInt(), "GarantiPaymentProvider: VerifyGateway - Start", request, httpRequest.Form, request.CustomerCode);
            var form = httpRequest.Form;
            if (form == null)
            {
                return (VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "Form verisi alınamadı.", form["oid"]));
            }

            var mdStatus = form["mdstatus"].ToString();
            if (string.IsNullOrEmpty(mdStatus))
            {
                return (VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, form["mderrormessage"], form["oid"], form["procreturncode"]));
            }

            var response = form["response"];
            //mdstatus 1,2,3 veya 4 olursa 3D doğrulama geçildi anlamına geliyor
            if (!mdStatusCodes.Contains(mdStatus))
            {
                return (VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, $"{response} - {form["mderrormessage"]}", form["oid"], form["procreturncode"]));
            }

            Models.LogModel.Log(request.ReservationID.ToInt(), "GarantiPaymentProvider: ThreeDGatewayRequest - Start", request, null, request.CustomerCode);
            var credential = new PaymentCredentialParameter().GetCredential(request.CustomerCode, request.BankServiceName, request.Market);
            var cardInformation = CardInformation.GetCreditCardInfo(request.ReservationID.ToInt(), request.AuthCode, request.CustomerCode);

            string securityData = GetSHA1($"{credential.Password}{credential.TerminalID1}");
            string HashData = (GetSHA1(form["oid"] + credential.TerminalID + cardInformation.CardNumber + form["txnamount"] + securityData)).ToUpper();

            int.TryParse(form["txninstallmentcount"], out int installment);

            GarantiGVPSRequest garantiGVPSRequest = new GarantiGVPSRequest();
            garantiGVPSRequest.Version = "v0.01";
            garantiGVPSRequest.Mode = "PROD";
            garantiGVPSRequest.Card = new GarantiRequestCard
            {
                CVV2 = cardInformation.CardCv2,
                ExpireDate = cardInformation.CardMonth + cardInformation.CardYear,
                Number = cardInformation.CardNumber
            };
            garantiGVPSRequest.Terminal = new GarantiRequestTerminal()
            {
                HashData = HashData,
                ID = credential.TerminalID,
                MerchantID = credential.MerchantID,
                ProvUserID = credential.ProvUserID,
                UserID = credential.UserName
            };
            garantiGVPSRequest.Customer = new GarantiRequestCustomer
            {
                EmailAddress = "eticaret@garanti.com.tr",
                IPAddress = (request.CustomerIpAddress == "::1" ? "95.9.134.101" : request.CustomerIpAddress)
            };
            garantiGVPSRequest.Order = new GarantiRequestOrder
            {
                Description = "",
                GroupID = "",
                OrderID = form["oid"]
            };
            GarantiRequestSecure3D secure3D = new GarantiRequestSecure3D();
            secure3D.AuthenticationCode = form["cavv"];
            secure3D.Md = form["md"];
            secure3D.SecurityLevel = form["eci"];
            secure3D.TxnID = form["xid"];
            garantiGVPSRequest.Transaction = new GarantiRequestTransaction
            {
                Amount = form["txnamount"],
                CardholderPresentCode = "0",
                CurrencyCode = request.CurrencyIsoCode.GetHashCode().ToString(),
                Description = "",
                InstallmentCnt = form["txninstallmentcount"],
                MotoInd = "N",
                OriginalRetrefNum = "",
                Secure3D = secure3D,
                Type = "sales",
            };

            Models.LogModel.Log(request.ReservationID.ToInt(), "GarantiPaymentProvider: VerifyGateway - Api Start", garantiGVPSRequest, null, request.CustomerCode);
            string xml = garantiGVPSRequest.ToSeriliazeXMLString();

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            byte[] parameters = System.Text.Encoding.UTF8.GetBytes("data=" + xml);
            System.Net.HttpWebRequest apiRequest = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(credential.ApiUrl);
            apiRequest.Method = "POST";
            apiRequest.ContentType = "application/x-www-form-urlencoded";
            apiRequest.ContentLength = parameters.Length;
            System.IO.Stream requeststream = apiRequest.GetRequestStream();
            requeststream.Write(parameters, 0, parameters.Length);
            requeststream.Close();
            Models.LogModel.Log(request.ReservationID.ToInt(), "GarantiPaymentProvider: ApiRequest - Request Close ", null, null, request.CustomerCode);

            System.Net.HttpWebResponse resp = (System.Net.HttpWebResponse)apiRequest.GetResponse();
            System.IO.StreamReader responsereader = new System.IO.StreamReader(resp.GetResponseStream(), System.Text.Encoding.UTF8);

            String responseStr = responsereader.ReadToEnd();
            Models.LogModel.Log(request.ReservationID.ToInt(), "GarantiPaymentProvider: VerifyGateway - Api End", garantiGVPSRequest, responseStr, request.CustomerCode);

            GarantiGVPSResponse garantiGVPSResponse = new GarantiGVPSResponse();
            XmlSerializer serializer = new XmlSerializer(typeof(GarantiGVPSResponse));
            using (StringReader reader = new StringReader(responseStr))
            {
                garantiGVPSResponse = (GarantiGVPSResponse)serializer.Deserialize(reader);
            }

            if (!request.Development)
                CardInformation.Mask(request.ReservationID.ToInt(), request.AuthCode, request.CustomerCode);

            if (garantiGVPSResponse.Transaction.Response.Message == "Declined")
            {
                Models.LogModel.Log(request.ReservationID.ToInt(), "GarantiPaymentProvider: VerifyGateway - Api Declined", garantiGVPSRequest, garantiGVPSResponse, request.CustomerCode);
                return (VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, $"{garantiGVPSResponse.Transaction.Response.Code} - {garantiGVPSResponse.Transaction.Response.SysErrMsg} - {garantiGVPSResponse.Transaction.Response.ErrorMsg}", form["oid"], garantiGVPSResponse.Transaction.Response.ReasonCode.ToString()));
            }
            else
            {
                Models.LogModel.Log(request.ReservationID.ToInt(), "GarantiPaymentProvider: VerifyGateway - Api Success", garantiGVPSRequest, garantiGVPSResponse, request.CustomerCode);
                return (VerifyGatewayResult.Successed(request.ReservationID, request.AuthCode, form["oid"], installment, 0, response, form["procreturncode"], form["campaignchooselink"]));
            }
        }

        public CancelPaymentResult CancelRequest(CancelPaymentRequest request)
        {
            string terminalUserId = request.BankParameters["terminalUserId"];
            string terminalId = request.BankParameters["terminalId"];
            string terminalMerchantId = request.BankParameters["terminalMerchantId"];
            string cancelUserId = request.BankParameters["cancelUserId"];
            string cancelUserPassword = request.BankParameters["cancelUserPassword"];
            string mode = request.BankParameters["mode"];//PROD | TEST

            //garanti tarafından terminal numarasını 9 haneye tamamlamak için başına sıfır eklenmesi isteniyor.
            string _terminalid = string.Format("{0:000000000}", int.Parse(terminalId));

            //garanti bankasında tutar bilgisinde nokta, virgül gibi değerler istenmiyor. 1.10 TL'lik işlem 110 olarak gönderilmeli. Yani tutarı 100 ile çarpabiliriz.
            string amount = (request.TotalAmount * 100m).ToString("0.##", new CultureInfo("en-US"));//virgülden sonraki sıfırlara gerek yok

            //provizyon şifresi ve 9 haneli terminal numarasının birleşimi ile bir hash oluşturuluyor
            string securityData = GetSHA1($"{cancelUserPassword}{_terminalid}");

            //ilgili veriler birleştirilip hash oluşturuluyor
            string hashstr = GetSHA1($"{request.OrderNumber}{terminalId}{amount}{securityData}");

            string installment = request.Installment.ToString();
            if (request.Installment < 2)
                installment = string.Empty;//0 veya 1 olması durumunda taksit bilgisini boş gönderiyoruz

            string requestXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                                        <GVPSRequest>
                                            <Mode>{mode}</Mode>
                                            <Version>v0.01</Version>
                                            <ChannelCode></ChannelCode>
                                            <Terminal>
                                                <ProvUserID>{cancelUserId}</ProvUserID>
                                                <HashData>{hashstr}</HashData>
                                                <UserID>{terminalUserId}</UserID>
                                                <ID>{terminalId}</ID>
                                                <MerchantID>{terminalMerchantId}</MerchantID>
                                            </Terminal>
                                            <Customer>
                                                <IPAddress>{request.CustomerIpAddress}</IPAddress>
                                                <EmailAddress></EmailAddress>
                                            </Customer>
                                            <Order>
                                                <OrderID>{request.OrderNumber}</OrderID>
                                                <GroupID></GroupID>
                                            </Order>
                                            <Transaction>
                                                <Type>void</Type>
                                                <InstallmentCnt>{installment}</InstallmentCnt>
                                                <Amount>{amount}</Amount>
                                                <CurrencyCode>{request.CurrencyIsoCode}</CurrencyCode>
                                                <CardholderPresentCode>0</CardholderPresentCode>
                                                <MotoInd>N</MotoInd>
                                                <OriginalRetrefNum>{request.ReferenceNumber}</OriginalRetrefNum>
                                            </Transaction>
                                        </GVPSRequest>";

            //TODO: GARANTI CancelRequest
            var restclient = new RestClient("");
            var restrequest = new RestRequest("/api/Token/GetToken", Method.POST, DataFormat.Xml);
            restrequest.AddXmlBody(requestXml);
            string responseContent = restclient.Execute<string>(restrequest).Data;

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(responseContent);

            if (xmlDocument.SelectSingleNode("GVPSResponse/Transaction/Response/ReasonCode")?.InnerText != "00")
            {
                string errorMessage = xmlDocument.SelectSingleNode("GVPSResponse/Transaction/Response/ErrorMsg")?.InnerText;
                if (string.IsNullOrEmpty(errorMessage))
                    errorMessage = xmlDocument.SelectSingleNode("GVPSResponse/Transaction/Response/SysErrMsg")?.InnerText;

                if (string.IsNullOrEmpty(errorMessage))
                    errorMessage = "Bankadan hata mesajı alınamadı.";

                return CancelPaymentResult.Failed(errorMessage);
            }

            string transactionId = xmlDocument.SelectSingleNode("GVPSResponse/Transaction/RetrefNum")?.InnerText;
            return CancelPaymentResult.Successed(transactionId, transactionId);
        }

        public RefundPaymentResult RefundRequest(RefundPaymentRequest request)
        {
            string terminalUserId = request.BankParameters["terminalUserId"];
            string terminalId = request.BankParameters["terminalId"];
            string terminalMerchantId = request.BankParameters["terminalMerchantId"];
            string refundUserId = request.BankParameters["refundUserId"];
            string refundUserPassword = request.BankParameters["refundUserPassword"];
            string mode = request.BankParameters["mode"];//PROD | TEST

            //garanti terminal numarasını 9 haneye tamamlamak için başına sıfır eklenmesini istiyor.
            string _terminalid = string.Format("{0:000000000}", int.Parse(terminalId));

            //garanti bankasında tutar bilgisinde nokta, virgül gibi değerler istenmiyor. 1.10 TL'lik işlem 110 olarak gönderilmeli. Yani tutarı 100 ile çarpabiliriz.
            string amount = (request.TotalAmount * 100m).ToString("0.##", new CultureInfo("en-US"));//virgülden sonraki sıfırlara gerek yok

            //provizyon şifresi ve 9 haneli terminal numarasının birleşimi ile bir hash oluşturuluyor
            string securityData = GetSHA1($"{refundUserPassword}{_terminalid}");

            //ilgili veriler birleştirilip hash oluşturuluyor
            string hashstr = GetSHA1($"{request.OrderNumber}{terminalId}{amount}{securityData}");

            string installment = request.Installment.ToString();
            if (request.Installment < 2)
                installment = string.Empty;//0 veya 1 olması durumunda taksit bilgisini boş gönderiyoruz

            string requestXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                                        <GVPSRequest>
                                            <Mode>{mode}</Mode>
                                            <Version>v0.01</Version>
                                            <ChannelCode></ChannelCode>
                                            <Terminal>
                                                <ProvUserID>{refundUserId}</ProvUserID>
                                                <HashData>{hashstr}</HashData>
                                                <UserID>{terminalUserId}</UserID>
                                                <ID>{terminalId}</ID>
                                                <MerchantID>{terminalMerchantId}</MerchantID>
                                            </Terminal>
                                            <Customer>
                                                <IPAddress>{request.CustomerIpAddress}</IPAddress>
                                                <EmailAddress></EmailAddress>
                                            </Customer>
                                            <Order>
                                                <OrderID>{request.OrderNumber}</OrderID>
                                                <GroupID></GroupID>
                                            </Order>
                                            <Transaction>
                                                <Type>refund</Type>
                                                <InstallmentCnt>{installment}</InstallmentCnt>
                                                <Amount>{amount}</Amount>
                                                <CurrencyCode>{request.CurrencyIsoCode}</CurrencyCode>
                                                <CardholderPresentCode>0</CardholderPresentCode>
                                                <MotoInd>N</MotoInd>
                                                <OriginalRetrefNum>{request.ReferenceNumber}</OriginalRetrefNum>
                                            </Transaction>
                                        </GVPSRequest>";
            //TODO: GARANTI RefundRequest
            var restclient = new RestClient("");
            var restrequest = new RestRequest("/api/Token/GetToken", Method.POST, DataFormat.Xml);
            restrequest.AddXmlBody(requestXml);
            string responseContent = restclient.Execute<string>(restrequest).Data;

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(responseContent);

            if (xmlDocument.SelectSingleNode("GVPSResponse/Transaction/Response/ReasonCode")?.InnerText != "00")
            {
                string errorMessage = xmlDocument.SelectSingleNode("GVPSResponse/Transaction/Response/ErrorMsg")?.InnerText;
                if (string.IsNullOrEmpty(errorMessage))
                    errorMessage = xmlDocument.SelectSingleNode("GVPSResponse/Transaction/Response/SysErrMsg")?.InnerText;

                if (string.IsNullOrEmpty(errorMessage))
                    errorMessage = "Bankadan hata mesajı alınamadı.";

                return RefundPaymentResult.Failed(errorMessage);
            }

            string transactionId = xmlDocument.SelectSingleNode("GVPSResponse/Transaction/RetrefNum")?.InnerText;
            return RefundPaymentResult.Successed(transactionId, transactionId);
        }

        public PaymentDetailResult PaymentDetailRequest(PaymentDetailRequest request)
        {
            string terminalUserId = request.BankParameters["terminalUserId"];
            string terminalId = request.BankParameters["terminalId"];
            string terminalMerchantId = request.BankParameters["terminalMerchantId"];
            string terminalProvUserId = request.BankParameters["terminalProvUserId"];
            string terminalProvPassword = request.BankParameters["terminalProvPassword"];
            string mode = request.BankParameters["mode"];//PROD | TEST

            //garanti terminal numarasını 9 haneye tamamlamak için başına sıfır eklenmesini istiyor.
            string _terminalid = string.Format("{0:000000000}", int.Parse(terminalId));

            //provizyon şifresi ve 9 haneli terminal numarasının birleşimi ile bir hash oluşturuluyor
            string securityData = GetSHA1($"{terminalProvPassword}{_terminalid}");

            string amount = "100";//sabit 100 gönderin dediler. Yani 1 TL.

            //ilgili veriler birleştirilip hash oluşturuluyor
            string hashstr = GetSHA1($"{request.OrderNumber}{terminalId}{amount}{securityData}");

            string requestXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                                        <GVPSRequest>
                                           <Mode>{mode}</Mode>
                                           <Version>v0.01</Version>
                                           <ChannelCode />
                                           <Terminal>
                                              <ProvUserID>{terminalProvUserId}</ProvUserID>
                                              <HashData>{hashstr}</HashData>
                                              <UserID>{terminalUserId}</UserID>
                                              <ID>{terminalId}</ID>
                                              <MerchantID>{terminalMerchantId}</MerchantID>
                                           </Terminal>
                                           <Customer>
                                              <IPAddress>{request.CustomerIpAddress}</IPAddress>
                                              <EmailAddress></EmailAddress>
                                           </Customer>
                                           <Card>
                                              <Number />
                                              <ExpireDate />
                                              <CVV2 />
                                           </Card>
                                           <Order>
                                              <OrderID>{request.OrderNumber}</OrderID>
                                              <GroupID />
                                           </Order>
                                           <Transaction>
                                              <Type>orderinq</Type>
                                              <InstallmentCnt />
                                              <Amount>{amount}</Amount>
                                              <CurrencyCode>{request.CurrencyIsoCode}</CurrencyCode>
                                              <CardholderPresentCode>0</CardholderPresentCode>
                                              <MotoInd>N</MotoInd>
                                           </Transaction>
                                        </GVPSRequest>";

            //TODO: GARANTI PaymentDetailRequest
            var restclient = new RestClient("");
            var restrequest = new RestRequest("/api/Token/GetToken", Method.POST, DataFormat.Xml);
            restrequest.AddXmlBody(requestXml);
            string responseContent = restclient.Execute<string>(restrequest).Data;

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(responseContent);

            string finalStatus = xmlDocument.SelectSingleNode("GVPSResponse/Order/OrderInqResult/Status")?.InnerText ?? string.Empty;
            string transactionId = xmlDocument.SelectSingleNode("GVPSResponse/Transaction/RetrefNum")?.InnerText;
            string referenceNumber = xmlDocument.SelectSingleNode("GVPSResponse/Transaction/RetrefNum")?.InnerText;
            string cardPrefix = xmlDocument.SelectSingleNode("GVPSResponse/Order/OrderInqResult/CardNumberMasked")?.InnerText;
            int.TryParse(cardPrefix, out int cardPrefixValue);

            string installment = xmlDocument.SelectSingleNode("GVPSResponse/Order/OrderInqResult/InstallmentCnt")?.InnerText ?? "0";
            string bankMessage = xmlDocument.SelectSingleNode("GVPSResponse/Transaction/Response/Message")?.InnerText;
            string responseCode = xmlDocument.SelectSingleNode("GVPSResponse/Transaction/Response/ReasonCode")?.InnerText;

            if (finalStatus.Equals("APPROVED", StringComparison.OrdinalIgnoreCase))
            {
                return PaymentDetailResult.PaidResult(transactionId, referenceNumber, cardPrefixValue.ToString(), int.Parse(installment), 0, bankMessage, responseCode);
            }
            else if (finalStatus.Equals("VOID", StringComparison.OrdinalIgnoreCase))
            {
                return PaymentDetailResult.CanceledResult(transactionId, referenceNumber, bankMessage, responseCode);
            }
            else if (finalStatus.Equals("REFUNDED", StringComparison.OrdinalIgnoreCase))
            {
                return PaymentDetailResult.RefundedResult(transactionId, referenceNumber, bankMessage, responseCode);
            }

            var bankErrorMessage = xmlDocument.SelectSingleNode("GVPSResponse/Transaction/Response/SysErrMsg")?.InnerText ?? string.Empty;
            var errorMessage = xmlDocument.SelectSingleNode("GVPSResponse/Transaction/Response/ErrorMsg")?.InnerText ?? string.Empty;
            if (string.IsNullOrEmpty(errorMessage))
                errorMessage = "Bankadan hata mesajı alınamadı.";

            return PaymentDetailResult.FailedResult(bankErrorMessage, responseCode, errorMessage);
        }

        public Dictionary<string, string> TestParameters => new Dictionary<string, string>
        {
            { "terminalUserId", "1" },
            { "terminalId", "1" },
            { "terminalMerchantId", "1" },
            { "terminalProvUserId", "1" },
            { "terminalProvPassword", "1" },
            { "storeKey", "1" },
            { "mode", "TEST" },
            { "gatewayUrl", "https://sanalposprov.garanti.com.tr/VPServlet" },
            { "verifyUrl", "https://sanalposprov.garanti.com.tr/VPServlet" }
        };

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

        public HostedPaymentResult HostedPaymentPageRequest(HostedPaymentRequest request)
        {
            throw new NotImplementedException();
        }

        private static readonly string[] mdStatusCodes = new[] { "1", "2", "3", "4" };
    }
}