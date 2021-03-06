using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using Lib.Payment.Models;
using Lib.Payment.Models.ProviderConditions;
using Lib.Payment.Requests;
using Lib.Payment.Results;
using Lib.Payment.Services;
using Lib.Extension.v2;

namespace Lib.Payment.Providers
{
    public class VakifbankPaymentProvider : IPaymentProvider
    {
        public PaymentGatewayResult ThreeDGatewayRequest(PaymentGatewayRequest request)
        {
            try
            {
                Models.LogModel.Log(request.ReservationID.ToInt(), "VakifbankPaymentProvider: ThreeDGatewayRequest - Start", request, null, request.CustomerCode);
                var credential = new PaymentCredentialParameter().GetCredential(request.CustomerCode, request.BankServiceName, request.Market);
                if (!request.Development)
                    CardInformation.Mask(request.ReservationID.ToInt(), request.AuthCode, request.CustomerCode);

                VakifbankConditions netsConditions = new VakifbankConditions();
                bool credentialConditionResult = netsConditions.Credential(credential);
                if (!credentialConditionResult) return PaymentGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "Please fill all criteria.", "TNC-02");

                var generalInfo = new GeneralServices().GetPaymentGeneralInfo(request.BaseSiteUrl, request.ReservationID, request.CardNumber, request.AuthCode);

                string merchantId = credential.MerchantID;
                string merchantPassword = credential.MerchantPass;
                string enrollmentUrl = credential.ApiUrl;

                string installment = request.Installment.ToString();
                if (request.Installment < 2) installment = string.Empty;

                string data = "Pan=" + request.CardNumber +
                    "&ExpiryDate=" + request.CardYear.Substring(2, 2) + "" + string.Format("{0:00}", request.CardMonth) +
                    "&PurchaseAmount=" + request.Amount.ToString("N2", new CultureInfo("en-US")).Replace(",", "") +
                    "&Currency=" + request.CurrencyIsoCode.GetHashCode().ToString() +
                    "&BrandName=" + CardTypes[generalInfo.CardType] +
                    "&VerifyEnrollmentRequestId=" + generalInfo.OrderID +
                    "&SessionInfo=&MerchantId=" + merchantId +
                    "&MerchantPassword=" + merchantPassword +
                    "&SuccessUrl=" + generalInfo.CallbackUrl +
                    "&FailureUrl=" + generalInfo.CallbackUrl +
                    "&SessionInfo=" + request.ReservationID + "#" + request.AuthCode +
                    "&InstallmentCount="+ installment;

                byte[] dataStream = Encoding.UTF8.GetBytes(data);
                HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(credential.ApiUrl); //Mpi Enrollment Adresi
                webRequest.Method = "POST";
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.ContentLength = dataStream.Length;
                webRequest.KeepAlive = false;
                string responseFromServer = "";

                using (Stream newStream = webRequest.GetRequestStream())
                {
                    newStream.Write(dataStream, 0, dataStream.Length);
                    newStream.Close();
                }

                using (WebResponse webResponse = webRequest.GetResponse())
                {
                    using (StreamReader reader = new StreamReader(webResponse.GetResponseStream()))
                    {
                        responseFromServer = reader.ReadToEnd();
                        reader.Close();
                    }

                    webResponse.Close();
                }

                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(responseFromServer);
                var statusNode = xmlDocument.SelectSingleNode("IPaySecure/Message/VERes/Status");
                if (statusNode.InnerText != "Y")
                {
                    var messageErrorNode = xmlDocument.SelectSingleNode("IPaySecure/ErrorMessage");
                    var messageErrorCodeNode = xmlDocument.SelectSingleNode("IPaySecure/MessageErrorCode");

                    return PaymentGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, messageErrorNode.InnerText, messageErrorCodeNode?.InnerText);
                }

                var pareqNode = xmlDocument.SelectSingleNode("IPaySecure/Message/VERes/PaReq");
                var termUrlNode = xmlDocument.SelectSingleNode("IPaySecure/Message/VERes/TermUrl");
                var mdNode = xmlDocument.SelectSingleNode("IPaySecure/Message/VERes/MD");
                var acsUrlNode = xmlDocument.SelectSingleNode("IPaySecure/Message/VERes/ACSUrl");

                var parameters = new Dictionary<string, object>();
                parameters.Add("PaReq", pareqNode.InnerText);
                parameters.Add("TermUrl", termUrlNode.InnerText);
                parameters.Add("MD", mdNode.InnerText);

                //form post edilecek url xml response i??erisinde bankadan d??n??yor
                Models.LogModel.Log(request.ReservationID.ToInt(), "VakifbankPaymentProvider: ThreeDGatewayRequest - Success", parameters, acsUrlNode, request.CustomerCode);
                return PaymentGatewayResult.Successed(parameters, acsUrlNode.InnerText);
            }
            catch (Exception ex)
            {
                return PaymentGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, ex.Message.ToString());
            }
        }

        public VerifyGatewayResult VerifyGateway(VerifyGatewayRequest request, System.Web.HttpRequest httpRequest)
        {
            Models.LogModel.Log(request.ReservationID.ToInt(), "VakifbankPaymentProvider: ThreeDGatewayRequest - Start", request, null, request.CustomerCode);
            var form = httpRequest.Form;
            if (form == null)
            {
                return VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "Form verisi al??namad??.", form["Xid"]);
            }

            var status = form["Status"].ToString();
            if (string.IsNullOrEmpty(status))
            {
                return VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "????lem sonu?? bilgisi al??namad??.", form["Xid"]);
            }

            if (!status.Equals("Y"))
            {
                string errorMessage = "3D do??rulama ba??ar??s??z";
                if (ErrorCodes.ContainsKey(form["ErrorCode"]))
                    errorMessage = ErrorCodes[form["ErrorCode"]];

                return VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, errorMessage, form["Xid"], form["ErrorCode"]);
            }

            var credential = new PaymentCredentialParameter().GetCredential(request.CustomerCode, request.BankServiceName, request.Market);

            string merchantId = credential.MerchantID;
            string merchantPassword = credential.MerchantPass;
            string terminalNo = credential.TerminalID;

            var xmlBuilder = new StringBuilder();
            xmlBuilder.Append($@"<?xml version=""1.0"" encoding=""utf-8""?>
                                    <VposRequest>
                                        <MerchantId>{merchantId}</MerchantId>
                                        <Password>{merchantPassword}</Password>
                                        <TerminalNo>{terminalNo}</TerminalNo>
                                        <TransactionType>Sale</TransactionType>
                                        <TransactionId>{Guid.NewGuid().ToString("N")}</TransactionId>
                                        <ClientIp>{request.CustomerIpAddress}</ClientIp>
                                        <TransactionDeviceSource>0</TransactionDeviceSource>
                                        <ECI>{form["Eci"]}</ECI>
                                        <CAVV>{form["CAVV"]}</CAVV>
                                        <OrderId>{form["Xid"]}</OrderId>
                                        <MpiTransactionId>{form["VerifyEnrollmentRequestId"]}</MpiTransactionId>");
            if (int.TryParse(form["InstallmentCount"], out int installment) && installment > 1)
            {
                xmlBuilder.Append($@"<NumberOfInstallments>{installment}</NumberOfInstallments>");
            }
            xmlBuilder.Append($@"</VposRequest>");
            var queryString = "?prmstr=" + xmlBuilder.ToString();


            byte[] dataStream = Encoding.UTF8.GetBytes("prmstr=" + xmlBuilder.ToString());
            HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(credential.GatewayUrl);
            webRequest.Method = "POST";
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.ContentLength = dataStream.Length;
            webRequest.KeepAlive = false;
            string responseFromServer = "";

            using (Stream newStream = webRequest.GetRequestStream())
            {
                newStream.Write(dataStream, 0, dataStream.Length);
                newStream.Close();
            }

            using (WebResponse webResponse = webRequest.GetResponse())
            {
                using (StreamReader reader = new StreamReader(webResponse.GetResponseStream()))
                {
                    responseFromServer = reader.ReadToEnd();
                    reader.Close();
                }

                webResponse.Close();
            }


            //var client = new RestClient();
            //client.BaseUrl = new Uri(credential.GatewayUrl);
            //var restRequest = new RestRequest(queryString, RestSharp.Method.POST);
            //restRequest.AddParameter("Content-Type", "application/x-www-form-urlencoded");
            //IRestResponse response = client.Execute(restRequest);

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(responseFromServer);
            var resultCodeNode = xmlDocument.SelectSingleNode("VposResponse/ResultCode");
            var resultDetailNode = xmlDocument.SelectSingleNode("VposResponse/ResultDetail");
            var transactionNode = xmlDocument.SelectSingleNode("VposResponse/TransactionId");

            if (resultCodeNode.InnerText != "0000")
            {
                string errorMessage = resultDetailNode.InnerText;
                if (string.IsNullOrEmpty(errorMessage) && ErrorCodes.ContainsKey(resultCodeNode.InnerText))
                    errorMessage = ErrorCodes[resultCodeNode.InnerText];

                return VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, errorMessage, form["Xid"], resultCodeNode.InnerText);
            }

            int.TryParse(form["InstallmentCount"], out int installmentCount);
            int.TryParse(form["EXTRA.ARTITAKSIT"], out int extraInstallment);

            return VerifyGatewayResult.Successed(request.ReservationID, request.AuthCode, form["Xid"],
                installmentCount, extraInstallment,
                resultDetailNode.InnerText,
                resultCodeNode.InnerText);
        }

        public CancelPaymentResult CancelRequest(CancelPaymentRequest request)
        {
            string merchantId = request.BankParameters["merchantId"];
            string merchantPassword = request.BankParameters["merchantPassword"];

            string requestXml = $@"<VposRequest>
                                    <MerchantId>{merchantId}</MerchantId>
                                    <Password>{merchantPassword}</Password>
                                    <TransactionType>Cancel</TransactionType>
                                    <ReferenceTransactionId>{request.TransactionId}</ReferenceTransactionId>
                                    <ClientIp>{request.CustomerIpAddress}</ClientIp>
                                </VposRequest>";

            var parameters = new Dictionary<string, string>();
            parameters.Add("prmstr", requestXml);

            //TODO: VAk??fbank CancelRequest
            var restclient = new RestClient("");
            var restrequest = new RestRequest("/api/Token/GetToken", Method.POST, DataFormat.Xml);
            restrequest.AddXmlBody(requestXml);
            string responseContent = restclient.Execute<string>(restrequest).Data;

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(responseContent);
            if (xmlDocument.SelectSingleNode("VposResponse/ResultCode") == null ||
                xmlDocument.SelectSingleNode("VposResponse/ResultCode").InnerText != "0000")
            {
                string errorMessage = xmlDocument.SelectSingleNode("VposResponse/ResultDetail")?.InnerText ?? string.Empty;
                if (string.IsNullOrEmpty(errorMessage))
                    errorMessage = "Bankadan hata mesaj?? al??namad??.";

                return CancelPaymentResult.Failed(errorMessage);
            }

            var transactionId = xmlDocument.SelectSingleNode("VposResponse/TransactionId")?.InnerText;
            return CancelPaymentResult.Successed(transactionId, transactionId);
        }

        public RefundPaymentResult RefundRequest(RefundPaymentRequest request)
        {
            string merchantId = request.BankParameters["merchantId"];
            string merchantPassword = request.BankParameters["merchantPassword"];

            string requestXml = $@"<VposRequest>
                                    <MerchantId>{merchantId}</MerchantId>
                                    <Password>{merchantPassword}</Password>
                                    <TransactionType>Refund</TransactionType>
                                    <ReferenceTransactionId>{request.TransactionId}</ReferenceTransactionId>
                                    <ClientIp>{request.CustomerIpAddress}</ClientIp>
                                </VposRequest>";

            var parameters = new Dictionary<string, string>();
            parameters.Add("prmstr", requestXml);

            //TODO: VAk??fbank PaymentDetailRequest
            var restclient = new RestClient("");
            var restrequest = new RestRequest("/api/Token/GetToken", Method.POST, DataFormat.Xml);
            restrequest.AddXmlBody(requestXml);
            string responseContent = restclient.Execute<string>(restrequest).Data;

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(responseContent);
            if (xmlDocument.SelectSingleNode("VposResponse/ResultCode") == null ||
                xmlDocument.SelectSingleNode("VposResponse/ResultCode").InnerText != "0000")
            {
                string errorMessage = xmlDocument.SelectSingleNode("VposResponse/ResultDetail")?.InnerText ?? string.Empty;
                if (string.IsNullOrEmpty(errorMessage))
                    errorMessage = "Bankadan hata mesaj?? al??namad??.";

                return RefundPaymentResult.Failed(errorMessage);
            }

            var transactionId = xmlDocument.SelectSingleNode("VposResponse/TransactionId")?.InnerText;
            return RefundPaymentResult.Successed(transactionId, transactionId);
        }

        public PaymentDetailResult PaymentDetailRequest(PaymentDetailRequest request)
        {
            string merchantId = request.BankParameters["merchantId"];
            string merchantPassword = request.BankParameters["merchantPassword"];
            var startDate = request.PaidDate.AddDays(-1).ToString("yyyy-MM-dd");
            var endDate = request.PaidDate.AddDays(1).ToString("yyyy-MM-dd");

            string requestXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                                        <SearchRequest>
                                           <MerchantCriteria>
                                              <HostMerchantId>{merchantId}</HostMerchantId>
                                              <MerchantPassword>{merchantPassword}</MerchantPassword>
                                           </MerchantCriteria>
                                           <DateCriteria>
                                              <StartDate>{startDate}</StartDate>
                                              <EndDate>{endDate}</EndDate>
                                           </DateCriteria>
                                           <TransactionCriteria>
                                              <TransactionId>{request.TransactionId}</TransactionId>
                                              <OrderId>{request.OrderNumber}</OrderId>
                                              <AuthCode />
                                           </TransactionCriteria>
                                        </SearchRequest>";

            var parameters = new Dictionary<string, string>();
            parameters.Add("prmstr", requestXml);

            //TODO: VAk??fbank PaymentDetailRequest
            var restclient = new RestClient("");
            var restrequest = new RestRequest("/api/Token/GetToken", Method.POST, DataFormat.Xml);
            restrequest.AddXmlBody(requestXml);
            string responseContent = restclient.Execute<string>(restrequest).Data;

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(responseContent);

            var totalItemCount = int.Parse(xmlDocument.SelectSingleNode("SearchResponse/PagedResponseInfo/TotalItemCount").InnerText);
            if (totalItemCount < 1)
            {
                string errorMessage = xmlDocument.SelectSingleNode("SearchResponse/ResponseInfo/ResponseMessage").InnerText;
                if (string.IsNullOrEmpty(errorMessage))
                    errorMessage = "Bankadan hata mesaj?? al??namad??.";

                return PaymentDetailResult.FailedResult(errorMessage);
            }

            var transactionInfoNode = xmlDocument.SelectNodes("SearchResponse/TransactionSearchResultInfo/TransactionSearchResultInfo")
                .Cast<XmlNode>()
                .FirstOrDefault();

            if (transactionInfoNode == null)
            {
                string errorMessage = xmlDocument.SelectSingleNode("SearchResponse/ResponseInfo/ResponseMessage").InnerText;
                if (string.IsNullOrEmpty(errorMessage))
                    errorMessage = "Bankadan hata mesaj?? al??namad??.";

                return PaymentDetailResult.FailedResult(errorMessage);
            }

            string transactionId = transactionInfoNode.SelectSingleNode("TransactionId")?.InnerText;
            string referenceNumber = transactionInfoNode.SelectSingleNode("TransactionId")?.InnerText;
            string cardPrefix = transactionInfoNode.SelectSingleNode("PanMasked")?.InnerText;
            int.TryParse(cardPrefix, out int cardPrefixValue);

            string bankMessage = transactionInfoNode.SelectSingleNode("ResponseMessage")?.InnerText;
            string responseCode = transactionInfoNode.SelectSingleNode("ResultCode")?.InnerText;

            var canceled = bool.Parse(transactionInfoNode.SelectSingleNode("IsCanceled")?.InnerText ?? "false");
            if (canceled)
            {
                return PaymentDetailResult.CanceledResult(transactionId, referenceNumber, bankMessage, responseCode);
            }

            var refunded = bool.Parse(transactionInfoNode.SelectSingleNode("IsRefunded")?.InnerText ?? "false");
            if (refunded)
            {
                return PaymentDetailResult.RefundedResult(transactionId, referenceNumber, bankMessage, responseCode);
            }

            if (responseCode == "0000")
            {
                return PaymentDetailResult.PaidResult(transactionId, referenceNumber, cardPrefixValue.ToString(), bankMessage: bankMessage, responseCode: responseCode);
            }

            if (string.IsNullOrEmpty(bankMessage))
                bankMessage = "Bankadan hata mesaj?? al??namad??.";

            return PaymentDetailResult.FailedResult(errorMessage: bankMessage, errorCode: responseCode);
        }

        public HostedPaymentResult HostedPaymentPageRequest(HostedPaymentRequest request)
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, string> TestParameters => new Dictionary<string, string>
        {
            { "merchantId", "655500056" },
            { "merchantPassword", "123456" },
            { "enrollmentUrl", "https://3dsecuretest.vakifbank.com.tr/MPIAPI/MPI_Enrollment.aspx" },
            { "verifyUrl", "https://onlineodemetest.vakifbank.com.tr:4443/UIService/TransactionSearchOperations.asmx" }
        };

        private static readonly IDictionary<string, string> ErrorCodes = new Dictionary<string, string>
        {
            { "0000", "Ba??ar??l??" },
            { "0001", "Bankan??z?? Aray??n" },
            { "0002", "Bankan??z?? Aray??n" },
            { "0003", "??ye Kodu Hatal??/tan??ms??z" },
            { "0004", "Karta El Koy" },
            { "0005", "I????lem Onaylanmad??" },
            { "0006", "Hatal?? I????lem" },
            { "0007", "Karta El Koy" },
            { "0009", "Tekrar Deneyin" },
            { "0010", "Tekrar Deneyin" },
            { "0011", "Tekrar Deneyin" },
            { "0012", "Ge??ersiz I????lem" },
            { "0013", "Ge??ersiz I????lem Tutar??" },
            { "0014", "Ge??ersiz Kart Numaras??" },
            { "0015", "M????teri Bulunamad??/bin Hatal??" },
            { "0021", "I????lem Onaylanmad??" },
            { "0030", "Mesaj Format?? Hatal?? (??ye I????yeri)" },
            { "0032", "Dosyas??na Ula????lamad??" },
            { "0033", "S??resi Bitmi?? Kart" },
            { "0034", "Sahte Kart" },
            { "0036", "I????lem Onaylanmad??" },
            { "0038", "??ifre Deneme A????m??/karta El Koy" },
            { "0041", "Kay??p Kart - Karta El Koy" },
            { "0043", "??al??nt?? Kart - Karta El Koy" },
            { "0051", "Limit Yetersiz" },
            { "0052", "Hesap Numaras??n?? Kontrol Edin" },
            { "0053", "Hesap Bulunamad??" },
            { "0054", "Ge??ersiz Kart" },
            { "0055", "Hatal?? Kart ??ifresi" },
            { "0056", "Kart Tan??ml?? De??il" },
            { "0057", "Kart??n I??lem Izni Yok" },
            { "0058", "Pos I????lem Tipine Kapal??" },
            { "0059", "Sahtekarl??k ????phesi" },
            { "0061", "Para ??ekme Tutar Limiti A????ld??" },
            { "0062", "Yasaklan??m?? Kart" },
            { "0063", "G??venlik Ihlali" },
            { "0065", "G??nl??k I????lem Adedi Limiti A????ld??" },
            { "0075", "??ifre Deneme Say??s?? A????ld??" },
            { "0077", "??ifre Script Talebi Reddedildi" },
            { "0078", "??ifre G??venilir De??il" },
            { "0089", "I????lem Onaylanmad??" },
            { "0091", "Karti Veren Banka Hi??zmet Di??i" },
            { "0092", "Bankasi Bi??li??nmi??yor" },
            { "0093", "I????lem Onaylanmad??" },
            { "0096", "Bankasinin Si??stemi?? Arizali" },
            { "0312", "Ge??ersi??z Kart" },
            { "0315", "Tekrar Deneyi??ni??z" },
            { "0320", "??nprovi??zyon Kapatilamadi" },
            { "0323", "??nprovi??zyon Kapatilamadi" },
            { "0357", "I????lem Onaylanmad??" },
            { "0358", "Kart Kapal??" },
            { "0381", "Red Karta El Koy" },
            { "0382", "Sahte Kart-karta El Koyunuz" },
            { "0501", "Ge??ersi??z Taksi??t/i????lem Tutari" },
            { "0503", "Kart Numarasi Hatali" },
            { "0504", "I????lem Onaylanmad??" },
            { "0540", "I??ade Edilecek I????lemin Orijinali Bulunamad??" },
            { "0541", "Orj. I????lemin Tamam?? Iade Edildi" },
            { "0542", "I??ade I????lemi?? Ger??ekle??ti??ri??lemez" },
            { "0550", "I????lem Ykb Pos Undan Yapilmali" },
            { "0570", "Yurtdi??i Kart I????lem I??zni?? Yok" },
            { "0571", "I????yeri Amex I????lem I??zni Yok" },
            { "0572", "I????yeri Amex Tan??mlar?? Eksik" },
            { "0574", "??ye I????yeri?? I????lem I??zni?? Yok" },
            { "0575", "I????lem Onaylanmad??" },
            { "0577", "Taksi??tli?? I????lem I??zni?? Yok" },
            { "0580", "Hatali 3d G??venli??k Bi??lgi??si??" },
            { "0581", "Eci Veya Cavv Bilgisi Eksik" },
            { "0582", "Hatali 3d G??venli??k Bi??lgi??si??" },
            { "0583", "Tekrar Deneyi??ni??z" },
            { "0880", "I????lem Onaylanmad??" },
            { "0961", "I????lem Ti??pi?? Ge??ersi??z" },
            { "0962", "Terminalid Tan??m??s??z" },
            { "0963", "??ye I????yeri Tan??ml?? De??il" },
            { "0966", "I????lem Onaylanmad??" },
            { "0971", "E??le??mi?? Bir I??lem Iptal Edilemez" },
            { "0972", "Para Kodu Ge??ersiz" },
            { "0973", "I????lem Onaylanmad??" },
            { "0974", "I????lem Onaylanmad??" },
            { "0975", "??ye I????yeri?? I????lem I??zni?? Yok" },
            { "0976", "I????lem Onaylanmad??" },
            { "0978", "Kartin Taksi??tli?? I????leme I??zni?? Yok" },
            { "0980", "I????lem Onaylanmad??" },
            { "0981", "Eksi??k G??venli??k Bi??lgi??si??" },
            { "0982", "I????lem I??ptal Durumda. I??ade Edi??lemez" },
            { "0983", "I??ade Edilemez,iptal" },
            { "0984", "I??ade Tutar Hatasi" },
            { "0985", "I????lem Onaylanmad??." },
            { "0986", "Gib Taksit Hata" },
            { "0987", "I????lem Onaylanmad??." },
            { "8484", "Birden Fazla Hata Olmas?? Durumunda Geri D??n??l??r. Resultdetail Alan??ndan Detaylar?? Al??nabilir." },
            { "1001", "Sistem Hatas??." },
            { "1006", "Bu Transactionid Ile Daha ??nce Ba??ar??l?? Bir I??lem Ger??ekle??tirilmi??" },
            { "1007", "Referans Transaction Al??namad??" },
            { "1046", "I??ade I??leminde Tutar Hatal??." },
            { "1047", "I????lem Tutar?? Ge??ersizdir." },
            { "1049", "Ge??ersiz Tutar." },
            { "1050", "Cvv Hatal??." },
            { "1051", "Kredi Kart?? Numaras?? Hatal??d??r." },
            { "1052", "Kredi Kart?? Son Kullanma Tarihi Hatal??." },
            { "1054", "I????lem Numaras?? Hatal??d??r." },
            { "1059", "Yeniden Iade Denemesi." },
            { "1060", "Hatal?? Taksit Say??s??." },
            { "2011", "Terminal no bulunamad??." },
            { "2200", "I???? Yerinin I??lem I??in Gerekli Hakk?? Yok." },
            { "2202", "I????lem Iptal Edilemez. ( Batch Kapal?? )" },
            { "5001", "I???? Yeri ??ifresi Yanl????." },
            { "5002", "I???? Yeri Aktif De??il." },
            { "1073", "Terminal ??zerinde Aktif Olarak Bir Batch Bulunamad??" },
            { "1074", "I????lem Hen??z Sonlanmam???? Yada Referans I??lem Hen??z Tamamlanmam????." },
            { "1075", "Sadakat Puan Tutar?? Hatal??" },
            { "1076", "Sadakat Puan Kodu Hatal??" },
            { "1077", "Para Kodu Hatal??" },
            { "1078", "Ge??ersiz Sipari?? Numaras??" },
            { "1079", "Ge??ersiz Sipari?? A????klamas??" },
            { "1080", "Sadakat Tutar?? Ve Para Tutar?? G??nderilmemi??." },
            { "1061", "Ayn?? Sipari?? Numaras??yla (Orderid) Daha ??nceden Ba??ar??l?? I??lem Yap??lm????" },
            { "1065", "??n Provizyon Daha ??nceden Kapat??lm????" },
            { "1082", "Ge??ersiz I??lem Tipi" },
            { "1083", "Referans I??lem Daha ??nceden Iptal Edilmi??." },
            { "1084", "Ge??ersiz Poa?? Kart Numaras??" },
            { "7777", "Banka Taraf??nda G??n Sonu Yap??ld??????ndan I??lem Ger??ekle??tirilemedi" },
            { "1087", "Yabanc?? Para Birimiyle Taksitli Provizyon Kapama I??lemi Yap??lamaz" },
            { "1088", "??nprovizyon Iptal Edilmi??" },
            { "1089", "Referans I??lem Yap??lmak Istenen I??lem I??in Uygun De??il" },
            { "1091", "Recurring I??lemin Toplam Taksit Say??s?? Hatal??" },
            { "1092", "Recurring I??lemin Tekrarlama Aral?????? Hatal??" },
            { "1093", "Sadece Sat???? (Sale) I??lemi Recurring Olarak I??aretlenebilir" },
            { "1095", "L??tfen Ge??erli Bir Email Adresi Giriniz" },
            { "1096", "L??tfen Ge??erli Bir Ip Adresi Giriniz" },
            { "1097", "L??tfen Ge??erli Bir Cavv De??eri Giriniz" },
            { "1098", "L??tfen Ge??erli Bir Eci De??eri Giriniz." },
            { "1099", "L??tfen Ge??erli Bir Kart Sahibi Ismi Giriniz." },
            { "1100", "L??tfen Ge??erli Bir Brand Giri??i Yap??n." },
            { "1105", "??ye I??yeri Ip Si Sistemde Tan??ml?? De??il" },
            { "1102", "Recurring I??lem Aral??k Tipi Hatal?? Bir De??ere Sahip" },
            { "1101", "Referans Transaction Reverse Edilmi??." },
            { "1104", "I??lgili Taksit I??in Tan??m Yok" },
            { "1111", "Bu ??ye I??yeri Non Secure I??lem Yapamaz" },
            { "1122", "Surchargeamount De??eri 0 Dan B??y??k Olmal??d??r." },
            { "6000", "Talep Mesaj?? Okunamad??." },
            { "6001", "I??stek Httppost Y??ntemi Ile Yap??lmal??d??r." },
            { "6003", "Pox Request Adresine Istek Yap??yorsunuz. Mesaj Bo?? Geldi. I??stek Xml Mesaj??n?? Prmstr Parametresi Ile Iletiniz." },
            { "9117", "3dsecure Islemlerde Eci Degeri Bos Olamaz." },
            { "33",   "Kart??n 3d Secure ??ifre Do??rulamas?? Yap??lamad??" },
            { "400",  "3d ??ifre Do??rulamas?? Yap??lamad??." },
            { "1026", "Failureurl Format Hatas??" },
            { "2000", "Acquirer Info Is Empty" },
            { "2005", "Merchant Cannot Be Found For This Bank" },
            { "2006", "Merchant Acquirer Bin Password Required" },
            { "2009", "Brand Not Found" },
            { "2010", "Cardholder Info Is Empty" },
            { "2012", "Devicecategory Must Be Between 0 And 2" },
            { "2013", "Threed Secure Message Can Not Be Found" },
            { "2014", "Pares Message Id Does Not Match Threed Secure Message Id" },
            { "2015", "Signature Verification False" },
            { "2017", "Acquirebin Can Not Be Found" },
            { "2018", "Merchant Acquirer Bin Password Wrong" },
            { "2019", "Bank Not Found" },
            { "2020", "Bank Id Does Not Match Merchant Bank" },
            { "2021", "Invalid Currency Code" },
            { "2022", "Verify Enrollmentrequest Id Cannot Be Empty" },
            { "2023", "Verify Enrollment Request Id Already Exist For This Merchant" },
            { "2024", "Acs Certificate Cannot Be Found In Database" },
            { "2025", "Certificate Could Not Be Found In Certificate Store" },
            { "2026", "Brand Certificate Not Found In Store" },
            { "2027", "Invalid Xml File" },
            { "2028", "Threed Secure Message Is Invalid State" },
            { "2029", "Invalid Pan" },
            { "2030", "Invalid Expire Date" },
            { "1002", "Successurl Format Is Invalid." },
            { "1003", "Brandid Format Is Invalid" },
            { "1004", "Devicecategory Format Is Invalid" },
            { "1005", "Sessioninfo Format Is Invalid" },
            { "1008", "Purchaseamount Format Is Invalid" },
            { "1009", "Expire Date Format Is Invalid" },
            { "1010", "Pan Format Is Invalid" },
            { "1011", "Merchant Acquirer Bin Password Format Is Invalid" },
            { "1012", "Hostmerchant Format Is Invalid" },
            { "1013", "Bankid Format Is Invalid" },
            { "2031", "Verification Failed: No Signature Was Found In The Document" },
            { "2032", "Verification Failed: More That One Signature Was Found For The Document" },
            { "2033", "Actual Brand Can Not Be Found" },
            { "2034", "Invalid Amount" },
            { "1014", "Is Recurring Format Is Invalid" },
            { "1015", "Recurring Frequency Format Is Invalid" },
            { "1016", "Recurring End Date Format Is Invalid" },
            { "2035", "Invalid Recurring Information" },
            { "2036", "Invalid Recurring Frequency" },
            { "2037", "Invalid Reccuring End Date" },
            { "2038", "Recurring End Date Must Be Greater Than Expire Date" },
            { "2039", "Invalid X509 Certificate Data" },
            { "2040", "Invalid Installment" },
            { "1017", "Installment Count Format Is Invalid" },
            { "3000", "Bank Not Found" },
            { "3001", "Country Not Found" },
            { "3002", "Invalid Failurl" },
            { "3003", "Hostmerchantnumber Cannot  Be Empty" },
            { "3004", "Merchantbrandacquirerbin Cannot Be Empty" },
            { "3005", "Merchantname Cannot Be Empty" },
            { "3006", "Merchantpassword Cannot Be Empty" },
            { "3007", "Invalid Sucessurl" },
            { "3008", "Invalid Merchantsiteurl" },
            { "3009", "Invalid Acquirerbin Length" },
            { "3010", "Brand Cannot Be Null" },
            { "3011", "Invalid Acquirerbinpassword Length" },
            { "3012", "Invalid Hostmerchantnumber Length" },
            { "2041", "Pares Exponent Value Does Not Match Pareq Exponent" },
            { "2042", "Pares Acquirer Bin Value Does Not Match Pareq Acqiurer Bin" },
            { "2043", "Pares Merchant Id Does Not Match Pareq Merchant Id" },
            { "2044", "Pares Xid Does Not Match Pareq Xid" },
            { "2045", "Pares Purchase Amount Does Not Match Pareq Purchase Amount" },
            { "2046", "Pares Currency Does Not Match Pareq Currency" },
            { "2047", "Veres Xsd Validation Error" },
            { "2048", "Pares Xsd Validation Exception" },
            { "2049", "Invalid Request" },
            { "2050", "File Is Empty" },
            { "2051", "Custom Error" },
            { "2052", "Bank Brand Bin Already Exist" },
            { "3013", "End Date Must Be Greater Than Start" },
            { "3014", "Start Date Must Be Greater Than Datetime Minval" },
            { "3015", "End Date Must Be Greater Than Datetime Minval" },
            { "3016", "Invalid Search Period" },
            { "3017", "Bin Cannot Be Empty" },
            { "3018", "Card Type Cannot Be Empty" },
            { "3019", "Bank Brand Bin Not Found" },
            { "3020", "Bin Length Must Be Six" },
            { "2053", "Directory Server Communication Error" },
            { "2054", "Acs Hata Bildirdi" },
            { "5037", "Successurl Alan?? Hatal??d??r." }
        };
        private static readonly IDictionary<string, string> CardTypes = new Dictionary<string, string>
        {
            { "1", "100" },//Visa
            { "2", "200" },//Master Card
            { "3", "300" },//American Express
        };
    }
}