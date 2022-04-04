using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Serialization;
using Lib.Payment.Requests;
using Lib.Payment.Results;

namespace Lib.Payment.Providers
{
    public class KuveytTurkPaymentProvider : IPaymentProvider
    {
        private readonly HttpClient client;

        public PaymentGatewayResult ThreeDGatewayRequest(PaymentGatewayRequest request)
        {
            try
            {
                ////Total amount (100 = 1TL)
                //var amount = Convert.ToInt32(request.TotalAmount * 100m).ToString();

                //var merchantOrderId = request.OrderNumber;
                //var merchantId = request.BankParameters["merchantId"];
                //var customerId = request.BankParameters["customerNumber"];
                //var userName = request.BankParameters["userName"];
                //var password = request.BankParameters["password"];

                //string installment = request.Installment.ToString();
                //if (request.Installment < 2)
                //    installment = string.Empty;//0 veya 1 olması durumunda taksit bilgisini boş gönderiyoruz

                ////merchantId, merchantOrderId, amount, okUrl, failUrl, userName and password
                //var hashData = CreateHash(merchantId, merchantOrderId, amount, request.CallbackUrl.ToString(), request.CallbackUrl.ToString(), userName, password);

                //var requestXml = $@"<KuveytTurkVPosMessage
                //    xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
                //    xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
                //        <APIVersion>1.0.0</APIVersion>
                //        <OkUrl>{request.CallbackUrl}</OkUrl>
                //        <FailUrl>{request.CallbackUrl}</FailUrl>
                //        <HashData>{hashData}</HashData>
                //        <MerchantId>{merchantId}</MerchantId>
                //        <CustomerId>{customerId}</CustomerId>
                //        <UserName>{userName}</UserName>
                //        <CardNumber>{request.CardNumber}</CardNumber>
                //        <CardExpireDateYear>{string.Format("{0:00}", request.ExpireYear)}</CardExpireDateYear>
                //        <CardExpireDateMonth>{string.Format("{0:00}", request.ExpireMonth)}</CardExpireDateMonth>
                //        <CardCVV2>{request.CvvCode}</CardCVV2>
                //        <CardHolderName>{request.CardHolderName}</CardHolderName>
                //        <CardType></CardType>
                //        <BatchID>0</BatchID>
                //        <TransactionType>Sale</TransactionType>
                //        <InstallmentCount>{installment}</InstallmentCount>
                //        <Amount>{amount}</Amount>
                //        <DisplayAmount>{amount}</DisplayAmount>
                //        <CurrencyCode>{string.Format("{0:0000}", int.Parse(request.CurrencyIsoCode))}</CurrencyCode>
                //        <MerchantOrderId>{merchantOrderId}</MerchantOrderId>
                //        <TransactionSecurity>3</TransactionSecurity>
                //        </KuveytTurkVPosMessage>";

                ////send request
                //var response = await client.PostAsync(request.BankParameters["gatewayUrl"], new StringContent(requestXml, Encoding.UTF8, "text/xml"));
                //string responseContent = await response.Content.ReadAsStringAsync();

                ////failed
                //if (string.IsNullOrWhiteSpace(responseContent))
                //    return PaymentGatewayResult.Failed("Ödeme sırasında bir hata oluştu.");

                ////successed
                //return PaymentGatewayResult.Successed(responseContent, request.BankParameters["gatewayUrl"]);
            }
            catch (Exception ex)
            {
                return PaymentGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, ex.Message.ToString());
            }
            return PaymentGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "Banka Aktif Değil");
        }

        public VerifyGatewayResult VerifyGateway(VerifyGatewayRequest request, System.Web.HttpRequest httpRequest)
        {
            var form = httpRequest.Form;
            if (form == null)
                return VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "Form verisi alınamadı.", request.AuthCode);

            var authenticationResponse = form["AuthenticationResponse"].ToString();
            if (string.IsNullOrEmpty(authenticationResponse))
                return VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "Form verisi alınamadı.", request.AuthCode);

            authenticationResponse = HttpUtility.UrlDecode(authenticationResponse);
            var serializer = new XmlSerializer(typeof(VPosTransactionResponseContract));

            var model = new VPosTransactionResponseContract();
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(authenticationResponse)))
            {
                model = serializer.Deserialize(ms) as VPosTransactionResponseContract;
            }

            if (model.ResponseCode != "00")
            {
                return VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, model.ResponseMessage, request.AuthCode, model.ResponseCode);
            }

            var merchantOrderId = model.MerchantOrderId;
            var amount = model.VPosMessage.Amount;
            var mD = model.MD;
            var merchantId = request.BankParameters["merchantId"];
            var customerId = request.BankParameters["customerNumber"];
            var userName = request.BankParameters["userName"];
            var password = request.BankParameters["password"];

            //Hash some data in one string result
            var cryptoServiceProvider = new SHA1CryptoServiceProvider();
            var hashedPassword = Convert.ToBase64String(cryptoServiceProvider.ComputeHash(Encoding.UTF8.GetBytes(password)));

            //merchantId, merchantOrderId, amount, userName, hashedPassword
            var hashstr = $"{merchantId}{merchantOrderId}{amount}{userName}{hashedPassword}";
            var hashbytes = Encoding.GetEncoding("ISO-8859-9").GetBytes(hashstr);
            var inputbytes = cryptoServiceProvider.ComputeHash(hashbytes);
            var hashData = Convert.ToBase64String(inputbytes);

            var requestXml = $@"<KuveytTurkVPosMessage
                    xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
                    xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
                        <APIVersion>1.0.0</APIVersion>
                        <HashData>{hashData}</HashData>
                        <MerchantId>{merchantId}</MerchantId>
                        <CustomerId>{customerId}</CustomerId>
                        <UserName>{userName}</UserName>
                        <CurrencyCode>0949</CurrencyCode>
                        <TransactionType>Sale</TransactionType>
                        <InstallmentCount>0</InstallmentCount>
                        <Amount>{amount}</Amount>
                        <MerchantOrderId>{merchantOrderId}</MerchantOrderId>
                        <TransactionSecurity>3</TransactionSecurity>
                        <KuveytTurkVPosAdditionalData>
                        <AdditionalData>
                        <Key>MD</Key>
                        <Data>{mD}</Data>
                        </AdditionalData>
                        </KuveytTurkVPosAdditionalData>
                        </KuveytTurkVPosMessage>";
            //TODO: 3D dönüş Kodlanacak
            //send request
            //var response = await client.PostAsync(request.BankParameters["verifyUrl"], new StringContent(requestXml, Encoding.UTF8, "text/xml"));
            //string responseContent = await response.Content.ReadAsStringAsync();
            //responseContent = HttpUtility.UrlDecode(responseContent);

            //using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(responseContent)))
            //{
            //    model = serializer.Deserialize(ms) as VPosTransactionResponseContract;
            //}

            //if (model.ResponseCode == "00")
            //{
            //    return VerifyGatewayResult.Successed(model.OrderId.ToString(), model.OrderId.ToString(),
            //        0, 0, model.ResponseMessage,
            //        model.ResponseCode);
            //}

            //return VerifyGatewayResult.Failed(model.ResponseMessage, model.ResponseCode);
            return VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "Banka aktif değil", request.AuthCode);
        }

        public CancelPaymentResult CancelRequest(CancelPaymentRequest request)
        {
            //TODO: CancelRequest KuveytTurkPaymentProvider
            throw new NotImplementedException();
        }

        public RefundPaymentResult RefundRequest(RefundPaymentRequest request)
        {
            //TODO: RefundRequest KuveytTurkPaymentProvider
            throw new NotImplementedException();
        }

        public PaymentDetailResult PaymentDetailRequest(PaymentDetailRequest request)
        {
            //TODO: PaymentDetailRequest KuveytTurkPaymentProvider
            throw new NotImplementedException();
        }

        public Dictionary<string, string> TestParameters => new Dictionary<string, string>
        {
            { "merchantId", "496" },
            { "customerNumber", "400235" },
            { "gatewayUrl", "https://boatest.kuveytturk.com.tr/boa.virtualpos.services/Home/ThreeDModelPayGate" },
            { "userName", "apitest" },
            { "password", "api123" },
            { "verifyUrl", "https://boatest.kuveytturk.com.tr/boa.virtualpos.services/Home/ThreeDModelProvisionGate" }
        };

        private string CreateHash(string merchantId, string merchantOrderId, string amount, string okUrl, string failUrl, string userName, string password)
        {
            var provider = CodePagesEncodingProvider.Instance;
            Encoding.RegisterProvider(provider);

            var cryptoServiceProvider = new SHA1CryptoServiceProvider();
            var inputbytes = cryptoServiceProvider.ComputeHash(Encoding.UTF8.GetBytes(password));
            var hashedPassword = Convert.ToBase64String(inputbytes);

            var hashstr = $"{merchantId}{merchantOrderId}{amount}{okUrl}{failUrl}{userName}{hashedPassword}";
            var hashbytes = Encoding.GetEncoding("ISO-8859-9").GetBytes(hashstr);

            return Convert.ToBase64String(cryptoServiceProvider.ComputeHash(hashbytes));
        }

        public HostedPaymentResult HostedPaymentPageRequest(HostedPaymentRequest request)
        {
            throw new NotImplementedException();
        }

        private class VPosTransactionResponseContract
        {
            public string ACSURL { get; set; }
            public string AuthenticationPacket { get; set; }
            public string HashData { get; set; }
            public bool IsEnrolled { get; set; }
            public bool IsSuccess { get; }
            public bool IsVirtual { get; set; }
            public string MD { get; set; }
            public string MerchantOrderId { get; set; }
            public int OrderId { get; set; }
            public string PareqHtmlFormString { get; set; }
            public string Password { get; set; }
            public string ProvisionNumber { get; set; }
            public string ResponseCode { get; set; }
            public string ResponseMessage { get; set; }
            public string RRN { get; set; }
            public string SafeKey { get; set; }
            public string Stan { get; set; }
            public DateTime TransactionTime { get; set; }
            public string TransactionType { get; set; }
            public KuveytTurkVPosMessage VPosMessage { get; set; }
        }

        public class KuveytTurkVPosMessage
        {
            public decimal Amount { get; set; }
        }
    }
}