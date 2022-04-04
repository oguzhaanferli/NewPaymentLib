using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
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
    public class PosnetPaymentProvider : IPaymentProvider
    {
        public PaymentGatewayResult ThreeDGatewayRequest(PaymentGatewayRequest request)
        {
            Models.LogModel.Log(request.ReservationID.ToInt(), "PosnetPaymentProvider: ThreeDGatewayRequest - Start", request, null, request.CustomerCode);
            try
            {
                var credential = new PaymentCredentialParameter().GetCredential(request.CustomerCode, request.BankServiceName, request.Market);
                if (!request.Development)
                    CardInformation.Mask(request.ReservationID.ToInt(), request.AuthCode, request.CustomerCode);

                PosnetConditions netsConditions = new PosnetConditions();
                bool credentialConditionResult = netsConditions.Credential(credential);
                if (!credentialConditionResult)
                {
                    return PaymentGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "Please fill all criteria.", "TNC-02");
                }

                var generalInfo = new GeneralServices().GetPaymentGeneralInfo(request.BaseSiteUrl, request.ReservationID, request.CardNumber, request.AuthCode);


                int tmpInstallment = 0;
                if (request.Installment > 1) tmpInstallment = request.Installment;

                PosnetThreeDGatewayRequest posnetThreeDGatewayRequest = new PosnetThreeDGatewayRequest();
                string xid = generalInfo.OrderID;
                posnetThreeDGatewayRequest.Mid = credential.MerchantID;
                posnetThreeDGatewayRequest.Tid = credential.TerminalID;
                string currency = CurrencyCodes[request.CurrencyIsoCode];

                posnetThreeDGatewayRequest.OosRequestData = new OosRequestData()
                {
                    Amount = (request.Amount.ToDecimal() * 100m).ToString(new CultureInfo("en-US")).Replace(".", ""),
                    CardHolderName = request.CardHolderName,
                    Ccno = request.CardNumber,
                    CurrencyCode = currency,
                    Cvc = request.CardCV2,
                    ExpDate = Convert.ToInt32(request.CardYear.ToString().Substring(2, 2) + (request.CardMonth.ToString().Length == 1 ? "0" + request.CardMonth.ToString() : request.CardMonth.ToString())),
                    Installment = tmpInstallment,
                    Posnetid = credential.PosnetID,
                    TranType = "Sale",
                    XID = xid
                };
                string xmlstr = HttpUtility.UrlEncode(posnetThreeDGatewayRequest.ToSeriliazeXMLString(), Encoding.UTF8);
                var client = new RestClient();
                client.BaseUrl = new Uri(credential.ApiUrl);
                var restRequest = new RestRequest("?xmldata=" + xmlstr, RestSharp.Method.POST);
                restRequest.AddParameter("Content-Type", "application/xml");
                restRequest.RequestFormat = DataFormat.Xml;
                IRestResponse response = client.Execute(restRequest);

                PosnetThreeDGatewayResponse result = null;
                XmlSerializer serializer = new XmlSerializer(typeof(PosnetThreeDGatewayResponse));
                using (TextReader reader = new StringReader(response.Content)) result = (PosnetThreeDGatewayResponse)serializer.Deserialize(reader);
                Models.LogModel.Log(request.ReservationID.ToInt(), "PosnetPaymentProvider: ThreeDGatewayRequest - Api Response", xmlstr, response.Content, request.CustomerCode);

                if (result.OosRequestDataResponse != null)
                {
                    var parameters = new Dictionary<string, object>();
                    parameters.Add("mid", credential.MerchantID);
                    parameters.Add("posnetID", credential.PosnetID);
                    parameters.Add("posnetData", result.OosRequestDataResponse.Data1);
                    parameters.Add("posnetData2", result.OosRequestDataResponse.Data2);
                    parameters.Add("digest", result.OosRequestDataResponse.Sign);
                    //parameters.Add("vftCode", yapikredi.custName);
                    //parameters.Add("useJokerVadaa", yapikredi.custName);
                    parameters.Add("merchantReturnURL", generalInfo.CallbackUrl);
                    parameters.Add("lang", request.LanguageIsoCode.ToString());
                    parameters.Add("url", HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Authority);

                    Models.LogModel.Log(request.ReservationID.ToInt(), "PosnetPaymentProvider: ThreeDGatewayRequest - Success", parameters, null, request.CustomerCode);
                    return PaymentGatewayResult.Successed(parameters, credential.GatewayUrl);
                }
                else
                {
                    return PaymentGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, result.RespText, result.RespCode);
                }
            }
            catch (Exception ex)
            {
                return PaymentGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, ex.Message.ToString());
            }
        }

        public VerifyGatewayResult VerifyGateway(VerifyGatewayRequest request, System.Web.HttpRequest httpRequest)
        {
            var credential = new PaymentCredentialParameter().GetCredential(request.CustomerCode, request.BankServiceName, request.Market);

            PosnetConditions netsConditions = new PosnetConditions();
            bool credentialConditionResult = netsConditions.Credential(credential);
            if (!credentialConditionResult) return VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode,"Please fill all criteria.", httpRequest.Form.Get("Xid"));

            String MerchantPacket = httpRequest.Form.Get("MerchantPacket");
            String BankPacket = httpRequest.Form.Get("BankPacket");
            String Sign = httpRequest.Form.Get("Sign");
            String CCPrefix = httpRequest.Form.Get("CCPrefix");
            String TranType = httpRequest.Form.Get("TranType");
            String Amount = httpRequest.Form.Get("Amount");
            String Xid = httpRequest.Form.Get("Xid");
            String MerchantId = httpRequest.Form.Get("MerchantId");

            string firstHash = YKBHASH(credential.EncKey + ';' + credential.TerminalID);
            string MAC = YKBHASH(Xid + ';' + Amount + ';' + CurrencyCodes[request.CurrencyIsoCode] + ';' + credential.MerchantID + ';' + firstHash);
            PosnetVerifyGatewayRequest posnetVerifyGatewayRequest = new PosnetVerifyGatewayRequest();
            posnetVerifyGatewayRequest.Mid = credential.MerchantID;
            posnetVerifyGatewayRequest.Tid = credential.TerminalID;
            posnetVerifyGatewayRequest.OosResolveMerchantData = new OosResolveMerchantData()
            {
                BankData = BankPacket,
                MerchantData = MerchantPacket,
                Sign = Sign,
                Mac = MAC
            };

            string xmlstr = HttpUtility.UrlEncode(posnetVerifyGatewayRequest.ToSeriliazeXMLString(), Encoding.UTF8);
            var client = new RestClient();
            client.BaseUrl = new Uri(credential.ApiUrl);
            var apiRequest = new RestRequest("?xmldata=" + xmlstr, Method.POST);
            apiRequest.AddParameter("Content-Type", "application/xml");
            apiRequest.RequestFormat = DataFormat.Xml;
            IRestResponse response = client.Execute(apiRequest);

            PosnetVerifyGatewayResponse result = null;
            XmlSerializer serializer = new XmlSerializer(typeof(PosnetVerifyGatewayResponse));
            using (TextReader reader = new StringReader(response.Content)) result = (PosnetVerifyGatewayResponse)serializer.Deserialize(reader);
            Models.LogModel.Log(request.ReservationID.ToInt(), "PosnetPaymentProvider: VerifyGateway - PosnetVerifyGatewayResponse", xmlstr, result, request.CustomerCode);

            if (result != null)
            {
                if (result.Approved == (int)YKBApproved.successful)
                {
                    string MAC1 = YKBHASH(result.OosResolveMerchantDataResponse.MdStatus + ";" + Xid + ';' + Amount + ';' + CurrencyCodes[request.CurrencyIsoCode] + ';' + credential.MerchantID + ';' + firstHash);
                    if (MAC1 == result.OosResolveMerchantDataResponse.Mac)
                    {
                        PosnetVerifyGatewayRequestV1 posnetVerifyGatewayRequest1 = new PosnetVerifyGatewayRequestV1();
                        posnetVerifyGatewayRequest1.Mid = credential.MerchantID;
                        posnetVerifyGatewayRequest1.Tid = credential.TerminalID;
                        posnetVerifyGatewayRequest1.OosTranData = new OosTranData()
                        {
                            BankData = BankPacket,
                            WpAmount = 0,
                            Mac = MAC
                        };

                        string xmlstr1 = HttpUtility.UrlEncode(posnetVerifyGatewayRequest1.ToSeriliazeXMLString(), Encoding.UTF8);
                        var client1 = new RestClient();
                        client1.BaseUrl = new Uri("https://posnet.yapikredi.com.tr");
                        var request1 = new RestRequest("/PosnetWebService/XML?xmldata=" + xmlstr1, Method.POST);
                        request1.AddParameter("Content-Type", "application/xml");
                        request1.RequestFormat = DataFormat.Xml;
                        IRestResponse response1 = client1.Execute(request1);

                        PosnetVerifyGatewayResponseV1 posnetVerifyGatewayResponseV1 = null;
                        XmlSerializer serializer1 = new XmlSerializer(typeof(PosnetVerifyGatewayResponseV1));
                        using (TextReader reader = new StringReader(response1.Content)) posnetVerifyGatewayResponseV1 = (PosnetVerifyGatewayResponseV1)serializer1.Deserialize(reader);
                        //Methods.SaveLog(xmlstr1, result1, "YKB Dönüş 2. Post - End", RezID);
                        Models.LogModel.Log(request.ReservationID.ToInt(), "PosnetPaymentProvider: VerifyGateway - PosnetVerifyGatewayResponseV1", xmlstr1, posnetVerifyGatewayResponseV1, request.CustomerCode);

                        if (posnetVerifyGatewayResponseV1.Approved == (int)YKBApproved.successful)
                        {
                            Models.LogModel.Log(request.ReservationID.ToInt(), "PosnetPaymentProvider: VerifyGateway - Success", xmlstr1, posnetVerifyGatewayResponseV1, request.CustomerCode);
                            return (VerifyGatewayResult.Successed(request.ReservationID, request.AuthCode, Xid, message: posnetVerifyGatewayResponseV1.RespText, responseCode: posnetVerifyGatewayResponseV1.RespCode,
                                installment: posnetVerifyGatewayResponseV1.InstInfo.Inst1));
                        }
                        else if (posnetVerifyGatewayResponseV1.Approved == (int)YKBApproved.unsuccessful)
                        {
                            Models.LogModel.Log(request.ReservationID.ToInt(), "PosnetPaymentProvider: VerifyGateway - Error", xmlstr1, posnetVerifyGatewayResponseV1, request.CustomerCode);
                            return VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, posnetVerifyGatewayResponseV1.RespText, Xid, posnetVerifyGatewayResponseV1.RespCode);
                        }
                        else if (posnetVerifyGatewayResponseV1.Approved == (int)YKBApproved.pre_approved)
                        {
                            Models.LogModel.Log(request.ReservationID.ToInt(), "PosnetPaymentProvider: VerifyGateway - Daha önceden onaylanmış", xmlstr1, posnetVerifyGatewayResponseV1, request.CustomerCode);
                            return VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "Daha önceden onaylanmış", Xid);
                        }
                        else
                        {
                            Models.LogModel.Log(request.ReservationID.ToInt(), "PosnetPaymentProvider: VerifyGateway - Bankadan dönen sonuç uygun değil", xmlstr1, posnetVerifyGatewayResponseV1, request.CustomerCode);
                            return VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "Bankadan dönen sonuç uygun değil", Xid);
                        }
                    }
                    else
                    {
                        Models.LogModel.Log(request.ReservationID.ToInt(), "PosnetPaymentProvider: VerifyGateway - MAC Eşleşmiyor", xmlstr, result, request.CustomerCode);
                        return VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "MAC Eşleşmiyor", Xid);
                    }
                }
                else
                {
                    Models.LogModel.Log(request.ReservationID.ToInt(), "PosnetPaymentProvider: VerifyGateway - YKB Result Error", xmlstr, result, request.CustomerCode);
                    return VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "YKB Result:" + result.RespText, Xid, result.RespCode);
                }
            }
            else
            {
                Models.LogModel.Log(request.ReservationID.ToInt(), "PosnetPaymentProvider: VerifyGateway - YKB Result Null", xmlstr, null, request.CustomerCode);
                return VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "YKB Result Null.", Xid);
            }
        }

        public CancelPaymentResult CancelRequest(CancelPaymentRequest request)
        {
            string merchantId = request.BankParameters["merchantId"];
            string terminalId = request.BankParameters["terminalId"];

            var xmlBuilder = new StringBuilder();
            xmlBuilder.Append($@"<?xml version=""1.0"" encoding=""utf-8""?>
                                     <posnetRequest>
                                         <mid>{merchantId}</mid>
                                         <tid>{terminalId}</tid>
                                         <reverse>
                                             <transaction>sale</transaction>
                                             <hostLogKey>{request.ReferenceNumber.Split('-').First().Trim()}</hostLogKey>");

            //taksitli işlemde 6 haneli auth kodu isteniyor
            if (request.Installment > 1)
                xmlBuilder.Append($"<authCode>{request.ReferenceNumber.Split('-').Last().Trim()}</authCode>");

            xmlBuilder.Append(@"</reverse>
                                </posnetRequest>");

            var httpParameters = new Dictionary<string, string>();
            httpParameters.Add("xmldata", xmlBuilder.ToString());

            //TODO: VAkıfbank CancelRequest
            var restclient = new RestClient("");
            var restrequest = new RestRequest("/api/Token/GetToken", Method.POST, DataFormat.Xml);
            restrequest.AddXmlBody(httpParameters);
            string responseContent = restclient.Execute<string>(restrequest).Data;

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(responseContent);

            if (xmlDocument.SelectSingleNode("posnetResponse/approved") == null ||
                xmlDocument.SelectSingleNode("posnetResponse/approved").InnerText != "1")
            {
                string errorMessage = xmlDocument.SelectSingleNode("posnetResponse/respText")?.InnerText ?? string.Empty;
                if (string.IsNullOrEmpty(errorMessage))
                    errorMessage = "Bankadan hata mesajı alınamadı.";

                return CancelPaymentResult.Failed(errorMessage);
            }

            var transactionId = xmlDocument.SelectSingleNode("posnetResponse/hostlogkey")?.InnerText;
            return CancelPaymentResult.Successed(transactionId, transactionId);
        }

        public RefundPaymentResult RefundRequest(RefundPaymentRequest request)
        {
            //string merchantId = request.BankParameters["merchantId"];
            //string terminalId = request.BankParameters["terminalId"];

            ////yapıkredi bankasında tutar bilgisinde nokta, virgül gibi değerler istenmiyor. 1.10 TL'lik işlem 110 olarak gönderilmeli. Yani tutarı 100 ile çarpabiliriz.
            //string amount = (request.TotalAmount * 100m).ToString("N");//virgülden sonraki sıfırlara gerek yok

            //string requestXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
            //                            <posnetRequest>
            //                                <mid>{merchantId}</mid>
            //                                <tid>{terminalId}</tid>
            //                                <tranDateRequired>1</tranDateRequired>
            //                                <return>
            //                                    <amount>{amount}</amount>
            //                                    <currencyCode>{CurrencyCodes[request.CurrencyIsoCode]}</currencyCode>
            //                                    <hostLogKey>{request.ReferenceNumber.Split('-').First().Trim()}</hostLogKey>
            //                                </return>
            //                            </posnetRequest>";

            //var httpParameters = new Dictionary<string, string>();
            //httpParameters.Add("xmldata", requestXml);

            ////TODO: VAkıfbank CancelRequest
            //var restclient = new RestClient("");
            //var restrequest = new RestRequest("/api/Token/GetToken", Method.POST, DataFormat.Xml);
            //restrequest.AddXmlBody(requestXml);
            //string responseContent = restclient.Execute<string>(restrequest).Data;

            //var xmlDocument = new XmlDocument();
            //xmlDocument.LoadXml(responseContent);

            //if (xmlDocument.SelectSingleNode("posnetResponse/approved") == null ||
            //    xmlDocument.SelectSingleNode("posnetResponse/approved").InnerText != "1")
            //{
            //    string errorMessage = xmlDocument.SelectSingleNode("posnetResponse/respText")?.InnerText ?? string.Empty;
            //    if (string.IsNullOrEmpty(errorMessage))
            //        errorMessage = "Bankadan hata mesajı alınamadı.";

            //    return RefundPaymentResult.Failed(errorMessage);
            //}

            //var transactionId = xmlDocument.SelectSingleNode("posnetResponse/hostlogkey")?.InnerText;
            //return RefundPaymentResult.Successed(transactionId, transactionId);
            return RefundPaymentResult.Failed("Şu anda desteklenmiyor");
        }

        public PaymentDetailResult PaymentDetailRequest(PaymentDetailRequest request)
        {
            string merchantId = request.BankParameters["merchantId"];
            string terminalId = request.BankParameters["terminalId"];

            string requestXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                                        <posnetRequest>
                                            <mid>{merchantId}</mid>
                                            <tid>{terminalId}</tid>
                                            <agreement>
                                                <orderID>TDSC{request.OrderNumber}</orderID>
                                            </agreement>
                                        </posnetRequest>";

            var httpParameters = new Dictionary<string, string>();
            httpParameters.Add("xmldata", requestXml);

            //TODO: VAkıfbank CancelRequest
            var restclient = new RestClient("");
            var restrequest = new RestRequest("/api/Token/GetToken", Method.POST, DataFormat.Xml);
            restrequest.AddXmlBody(requestXml);
            string responseContent = restclient.Execute<string>(restrequest).Data;

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(responseContent);

            string bankMessage = xmlDocument.SelectSingleNode("posnetResponse/respText")?.InnerText;
            string responseCode = xmlDocument.SelectSingleNode("posnetResponse/respCode")?.InnerText;
            string approved = xmlDocument.SelectSingleNode("posnetResponse/approved")?.InnerText ?? string.Empty;

            if (!approved.Equals("1"))
            {
                if (string.IsNullOrEmpty(bankMessage))
                    bankMessage = "Bankadan hata mesajı alınamadı.";

                return PaymentDetailResult.FailedResult(errorMessage: bankMessage, errorCode: responseCode);
            }

            string finalStatus = xmlDocument.SelectSingleNode("posnetResponse/transactions/transaction/state")?.InnerText ?? string.Empty;
            if (!finalStatus.Equals("SALE", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(bankMessage))
                    bankMessage = "Bankadan hata mesajı alınamadı.";

                return PaymentDetailResult.FailedResult(errorMessage: bankMessage, errorCode: responseCode);
            }

            string transactionId = xmlDocument.SelectSingleNode("posnetResponse/transactions/transaction/hostLogKey")?.InnerText;
            string referenceNumber = xmlDocument.SelectSingleNode("posnetResponse/transactions/transaction/hostLogKey")?.InnerText;
            string authCode = xmlDocument.SelectSingleNode("posnetResponse/transactions/transaction/authCode")?.InnerText;
            string cardPrefix = xmlDocument.SelectSingleNode("posnetResponse/transactions/transaction/ccno")?.InnerText;
            int.TryParse(cardPrefix, out int cardPrefixValue);

            var refNumber = $"{referenceNumber}-{authCode}";
            return PaymentDetailResult.PaidResult(transactionId, refNumber, cardPrefixValue.ToString(), bankMessage: bankMessage, responseCode: responseCode);
        }

        public HostedPaymentResult HostedPaymentPageRequest(HostedPaymentRequest request)
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, string> TestParameters => new Dictionary<string, string>
        {
            { "merchantId", "" },
            { "terminalId", "" },
            { "posnetId", "" },
            { "verifyUrl", "https://posnettest.yapikredi.com.tr/PosnetWebService/XML" },
            { "gatewayUrl", "https://posnettest.yapikredi.com.tr/PosnetWebService/XML" }
        };

        private static readonly Dictionary<PaymentCurrency, string> CurrencyCodes = new Dictionary<PaymentCurrency, string>
        {
            { PaymentCurrency.TRY, "TL" },
            { PaymentCurrency.USD, "US" },
            { PaymentCurrency.EUR, "EU" },
        };
        private string YKBHASH(string originalString)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(originalString));
                return Convert.ToBase64String(bytes);
            }
        }
    }
}