using Microsoft.Extensions.Primitives;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Lib.Payment.Models;
using Lib.Payment.Provides.Response;
using Lib.Payment.Requests;
using Lib.Payment.Results;
using Lib.Payment.Services;
using Lib.Extension.v2;

namespace Lib.Payment.Providers
{
    public class FinansbankPaymentProvider : IPaymentProvider
    {
        public PaymentGatewayResult ThreeDGatewayRequest(PaymentGatewayRequest request)
        {
            try
            {
                Models.LogModel.Log(request.ReservationID.ToInt(), "FinansbankPaymentProvider: ThreeDGatewayRequest - Start", request, null, request.CustomerCode);
                var credential = new PaymentCredentialParameter().GetCredential(request.CustomerCode, request.BankServiceName, request.Market);
                if (!request.Development)
                {
                    CardInformation.Mask(request.ReservationID.ToInt(), request.AuthCode, request.CustomerCode);
                }
                var generalInfo = new GeneralServices().GetPaymentGeneralInfo(request.BaseSiteUrl, request.ReservationID, request.CardNumber, request.AuthCode);


                string random = DateTime.Now.ToString();
                string installment = request.Installment.ToString();
                if (request.Installment < 2) installment = "0";//0 veya 1 olması durumunda taksit bilgisini 0 gönderiyoruz
                string totalAmount = request.Amount.ToString(new CultureInfo("en-US"));

                String MbrId = "5";//Kurum Kodu
                String MerchantID = credential.MerchantID;//Language_MerchantID
                String MerchantPass = credential.MerchantPass;//Language_MerchantPass
                String UserCode = credential.UserName;//Kullanici Kodu
                String UserPass = credential.Password;//Kullanici Sifre
                String TxnType = credential.TxnType;//Islem Tipi
                String OkUrl = generalInfo.CallbackUrl;//Language_OkUrl
                String FailUrl = generalInfo.CallbackUrl;//Language_FailUrl
                String PurchAmount = request.Amount.ToString(new CultureInfo("en-US"));

                String str = MbrId + generalInfo.OrderID + PurchAmount + OkUrl + FailUrl + TxnType + installment + random + MerchantPass;
                System.Security.Cryptography.SHA1 sha = new System.Security.Cryptography.SHA1CryptoServiceProvider();
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(str);
                byte[] hashingbytes = sha.ComputeHash(bytes);
                String hash = Convert.ToBase64String(hashingbytes);
                Models.LogModel.Log(request.ReservationID.ToInt(), "FinansbankPaymentProvider: ThreeDGatewayRequest - Hash", str, hash, request.CustomerCode);

                var parameters = new Dictionary<string, object>();
                parameters.Add("Pan", request.CardNumber);
                parameters.Add("Cvv2", request.CardCV2);
                parameters.Add("Expiry", request.CardMonth + Convert.ToString(request.CardYear).Substring(2, 2));
                parameters.Add("MbrId", MbrId);
                parameters.Add("MerchantID", MerchantID);
                parameters.Add("UserCode", UserCode);
                parameters.Add("UserPass", UserPass);
                parameters.Add("SecureType", "3DModel");
                parameters.Add("TxnType", TxnType);
                parameters.Add("InstallmentCount", installment);
                parameters.Add("Currency", request.CurrencyIsoCode.GetHashCode().ToString());
                parameters.Add("OkUrl", OkUrl);
                parameters.Add("FailUrl", FailUrl);
                parameters.Add("OrderId", generalInfo.OrderID);
                //İade ve İptal ve ÖnProvizyonKapama işlemleri için gönderilmesi gereken OrderId numarasıdır.
                //Satış(Auth) işlemlerinde OrgOrderId ya boş gönderilmeli ya da hiç gönderilmemelidir.
                parameters.Add("OrgOrderId", "");
                parameters.Add("PurchAmount", PurchAmount);
                parameters.Add("Lang", request.LanguageIsoCode.ToString().ToUpper());
                parameters.Add("Rnd", random);
                parameters.Add("Hash", hash);



                Models.LogModel.Log(request.ReservationID.ToInt(), "FinansbankPaymentProvider: ThreeDGatewayRequest - Success", parameters, null, request.CustomerCode);
                return PaymentGatewayResult.Successed(parameters, credential.GatewayUrl);
            }
            catch (Exception ex)
            {
                return (PaymentGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, ex.Message.ToString()));
            }
        }

        public VerifyGatewayResult VerifyGateway(VerifyGatewayRequest request, System.Web.HttpRequest httpRequest)
        {
            Models.LogModel.Log(request.ReservationID.ToInt(), "FinansbankPaymentProvider: VerifyGateway - Start", request, httpRequest.Form, request.CustomerCode);
            var credential = new PaymentCredentialParameter().GetCredential(request.CustomerCode, request.BankServiceName, request.Market);
            var form = httpRequest.Form;
            if (form == null)
            {
                return (VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "Form verisi alınamadı.", ""));
            }

            StringBuilder str = new StringBuilder();
            String format = "{0}={1}&";

            String mdstatus = httpRequest.Form.Get("3DStatus");
            String orderId = httpRequest.Form.Get("OrderId");
            if (mdstatus == "1")
            {
                Models.LogModel.Log(request.ReservationID.ToInt(), "FinansbankPaymentProvider: VerifyGateway - mdstatus == 1", mdstatus, orderId, request.CustomerCode);
                String payersecuritylevelval = httpRequest.Form.Get("Eci");
                String payertxnidval = httpRequest.Form.Get("PayerTxnId");
                String payerauthenticationcodeval = httpRequest.Form.Get("PayerAuthenticationCode");
                String merchantId = httpRequest.Form.Get("MerchantID");
                String requestGuid = httpRequest.Form.Get("RequestGuid");

                str.AppendFormat(format, "UserCode", credential.UserName);
                str.AppendFormat(format, "UserPass", credential.Password);
                str.AppendFormat(format, "OrderId", orderId);
                str.AppendFormat(format, "SecureType", "3DModelPayment");
                str.AppendFormat(format, "RequestGuid", requestGuid);
                Models.LogModel.Log(request.ReservationID.ToInt(), "FinansbankPaymentProvider: VerifyGateway - Api Request Start", str, null, request.CustomerCode);
                FinansbankVerifyGatewayResponse finansbankVerifyGatewayResponse = ApiRequest(credential, str, request.CustomerCode, request.ReservationID.ToInt());
                Models.LogModel.Log(request.ReservationID.ToInt(), "FinansbankPaymentProvider: VerifyGateway - Api Request End", str, finansbankVerifyGatewayResponse, request.CustomerCode);
                if (finansbankVerifyGatewayResponse.TxnResult == "Failed")
                {
                    return (VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, finansbankVerifyGatewayResponse.ErrMsg, orderId, finansbankVerifyGatewayResponse.ProcReturnCode));
                }
                else
                {
                    return VerifyGatewayResult.Successed(request.ReservationID, request.AuthCode, orderId, responseCode: finansbankVerifyGatewayResponse.ProcReturnCode, installment: finansbankVerifyGatewayResponse.InstallmentCount.ToInt(), message: finansbankVerifyGatewayResponse.ErrMsg);
                }
            }
            Models.LogModel.Log(request.ReservationID.ToInt(), "FinansbankPaymentProvider: VerifyGateway - mdstatus != 1 ", request, null, request.CustomerCode);
            return (VerifyGatewayResult.Failed(request.ReservationID, request.AuthCode, request.CustomerCode, "3D Kullanici Dogrulama Hatali", orderId, mdstatus));
        }

        public CancelPaymentResult CancelRequest(CancelPaymentRequest request)
        {
            string mbrId = request.BankParameters["mbrId"];//Mağaza numarası
            string merchantId = request.BankParameters["merchantId"];//Mağaza numarası
            string userCode = request.BankParameters["userCode"];//
            string userPass = request.BankParameters["userPass"];//Mağaza anahtarı
            string txnType = request.BankParameters["txnType"];//İşlem tipi
            string secureType = request.BankParameters["secureType"];

            string requestXml = $@"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
                                    <PayforIptal>
                                        <MbrId>{mbrId}</MbrId>
                                        <MerchantID>{merchantId}</MerchantID>
                                        <UserCode>{userCode}</UserCode>
                                        <UserPass>{userPass}</UserPass>
                                        <OrgOrderId></OrgOrderId>
                                        <SecureType>NonSecure</SecureType>
                                        <TxnType>Void</TxnType>
                                        <Currency>{request.CurrencyIsoCode}</Currency>
                                        <Lang>{request.LanguageIsoCode.ToUpper()}</Lang>
                                    </PayforIptal>";

            //TODO: Finansbank CancelRequest
            var restclient = new RestClient("");
            var restrequest = new RestRequest("/api/Token/GetToken", Method.POST, DataFormat.Xml);
            restrequest.AddXmlBody(requestXml);
            string responseContent = restclient.Execute<string>(restrequest).Data;

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(responseContent);

            //TODO Finansbank response
            //if (xmlDocument.SelectSingleNode("VposResponse/ResultCode") == null ||
            //    xmlDocument.SelectSingleNode("VposResponse/ResultCode").InnerText != "0000")
            //{
            //    string errorMessage = xmlDocument.SelectSingleNode("VposResponse/ResultDetail")?.InnerText ?? string.Empty;
            //    if (string.IsNullOrEmpty(errorMessage))
            //        errorMessage = "Bankadan hata mesajı alınamadı.";

            //    return CancelPaymentResult.Failed(errorMessage);
            //}

            var transactionId = xmlDocument.SelectSingleNode("VposResponse/TransactionId")?.InnerText;
            return CancelPaymentResult.Successed(transactionId, transactionId);
        }

        public RefundPaymentResult RefundRequest(RefundPaymentRequest request)
        {
            string mbrId = request.BankParameters["mbrId"];//Mağaza numarası
            string merchantId = request.BankParameters["merchantId"];//Mağaza numarası
            string userCode = request.BankParameters["userCode"];//
            string userPass = request.BankParameters["userPass"];//Mağaza anahtarı
            string txnType = request.BankParameters["txnType"];//İşlem tipi
            string secureType = request.BankParameters["secureType"];
            string totalAmount = request.TotalAmount.ToString(new CultureInfo("en-US"));

            string requestXml = $@"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
                                    <PayforIade>
                                        <MbrId>{mbrId}</MbrId>
                                        <MerchantID>{merchantId}</MerchantID>
                                        <UserCode>{userCode}</UserCode>
                                        <UserPass>{userPass}</UserPass>
                                        <OrgOrderId></OrgOrderId>
                                        <SecureType>NonSecure</SecureType>
                                        <TxnType>Refund</TxnType>
                                        <PurchAmount>{totalAmount}</PurchAmount>
                                        <Currency>{request.CurrencyIsoCode}</Currency>
                                        <Lang>{request.LanguageIsoCode.ToUpper()}</Lang>
                                    </PayforIade>";

            //TODO: Finansbank RefundRequest
            var restclient = new RestClient("");
            var restrequest = new RestRequest("/api/Token/GetToken", Method.POST, DataFormat.Xml);
            restrequest.AddXmlBody(requestXml);
            string responseContent = restclient.Execute<string>(restrequest).Data;

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(responseContent);

            //TODO Finansbank response
            //if (xmlDocument.SelectSingleNode("VposResponse/ResultCode") == null ||
            //    xmlDocument.SelectSingleNode("VposResponse/ResultCode").InnerText != "0000")
            //{
            //    string errorMessage = xmlDocument.SelectSingleNode("VposResponse/ResultDetail")?.InnerText ?? string.Empty;
            //    if (string.IsNullOrEmpty(errorMessage))
            //        errorMessage = "Bankadan hata mesajı alınamadı.";

            //    return RefundPaymentResult.Failed(errorMessage);
            //}

            var transactionId = xmlDocument.SelectSingleNode("VposResponse/TransactionId")?.InnerText;
            return RefundPaymentResult.Successed(transactionId, transactionId);
        }

        public PaymentDetailResult PaymentDetailRequest(PaymentDetailRequest request)
        {
            //TODO: Finansbank PaymentDetailRequest
            throw new NotImplementedException();
        }

        public HostedPaymentResult HostedPaymentPageRequest(HostedPaymentRequest request)
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, string> TestParameters => new Dictionary<string, string>
        {
            { "mbrId", "" },
            { "merchantId", "" },
            { "userCode", "" },
            { "userPass", "" },
            { "txnType", "" },
            { "secureType", "" },
            { "gatewayUrl", "" },
            { "verifyUrl", "" }
        };

        public FinansbankVerifyGatewayResponse ApiRequest(PaymentCredentialParameter credential, StringBuilder parameter, string CustomerCode, int ReservationID)
        {
            FinansbankVerifyGatewayResponse finansbankVerifyGatewayResponse = new FinansbankVerifyGatewayResponse();
            Models.LogModel.Log(ReservationID, "FinansbankPaymentProvider: ApiRequest - Start ", credential, parameter, CustomerCode);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(credential.ApiUrl);

            byte[] parameters = System.Text.Encoding.UTF8.GetBytes(parameter.ToString());
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = parameters.Length;
            System.IO.Stream requeststream = request.GetRequestStream();
            requeststream.Write(parameters, 0, parameters.Length);
            requeststream.Close();
            Models.LogModel.Log(ReservationID, "FinansbankPaymentProvider: ApiRequest - Request Close ", null, null, CustomerCode);

            System.Net.HttpWebResponse resp = (System.Net.HttpWebResponse)request.GetResponse();
            System.IO.StreamReader responsereader = new System.IO.StreamReader(resp.GetResponseStream(), System.Text.Encoding.UTF8);

            String responseStr = responsereader.ReadToEnd();
            string Onay = "";

            if (responseStr != null)
            {
                string[] paramArr = responseStr.Split(new char[] { ';', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string p in paramArr)
                {
                    string[] nameValue = p.Split('=');
                    if (p.Contains("ErrMsg"))
                    {
                        if (nameValue.Count() > 1)
                        {
                            finansbankVerifyGatewayResponse.ErrMsg = nameValue[1];
                        }
                    }
                    else if (p.Contains("TxnResult"))
                    {
                        if (nameValue.Count() > 1)
                        {
                            finansbankVerifyGatewayResponse.TxnResult = nameValue[1];
                        }
                    }
                    else if (p.Contains("InstallmentCount"))
                    {
                        if (nameValue.Count() > 1)
                        {
                            finansbankVerifyGatewayResponse.InstallmentCount = nameValue[1];
                        }
                    }
                    else if (p.Contains("ProcReturnCode"))
                    {
                        if (nameValue.Count() > 1)
                        {
                            finansbankVerifyGatewayResponse.ProcReturnCode = nameValue[1];
                        }
                    }
                    else if (p.Contains("OrderId"))
                    {
                        if (nameValue.Count() > 1)
                        {
                            finansbankVerifyGatewayResponse.OrderId = nameValue[1];
                        }
                    }
                }
            }
            Models.LogModel.Log(ReservationID, "FinansbankPaymentProvider: ApiRequest - Return ", null, null, CustomerCode);
            return finansbankVerifyGatewayResponse;

        }
    }
}