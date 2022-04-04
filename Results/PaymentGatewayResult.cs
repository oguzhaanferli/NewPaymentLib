using System;
using System.Collections.Generic;
using Lib.Payment.Services;

namespace Lib.Payment.Results
{
    public class PaymentGatewayResult
    {
        public Uri GatewayUrl { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorCode { get; set; }
        public IDictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public string HtmlFormContent { get; set; }
        public bool HtmlContent => !string.IsNullOrEmpty(HtmlFormContent);

        public static PaymentGatewayResult Successed(string redirectUrl,
            string message = null)
        {
            return new PaymentGatewayResult
            {
                Success = true,
                GatewayUrl = new Uri(redirectUrl),
                Message = message
            };
        }

        public static PaymentGatewayResult Successed(IDictionary<string, object> parameters,
            string gatewayUrl,
            string message = null)
        {
            IPaymentProviderFactory _paymentProviderFactory = new PaymentProviderFactory();
            string form = _paymentProviderFactory.CreatePaymentFormHtml(parameters, gatewayUrl, false);
            return new PaymentGatewayResult
            {
                Success = true,
                Parameters = parameters,
                HtmlFormContent = form,
                GatewayUrl = new Uri(gatewayUrl),
                Message = message
            };
        }

        public static PaymentGatewayResult Failed(string ReservationID, string AuthCode, string CustomerCode, string errorMessage, string errorCode = null)
        {
            CreditCardErrorServices.Save(Convert.ToInt32(ReservationID), AuthCode, CustomerCode, errorCode + ": " + errorMessage);
            return new PaymentGatewayResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                ErrorCode = errorCode
            };
        }
    }
}