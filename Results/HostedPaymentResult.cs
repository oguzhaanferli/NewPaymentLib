using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lib.Payment.Services;

namespace Lib.Payment.Results
{
    public class HostedPaymentResult
    {
        public bool Success { get; set; }
        public string RedirectUrl { get; set; }
        public string Token { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
        public string PaymentId { get; set; }

        public static HostedPaymentResult Successed(string redirectUrl, string code, string token = "", string message = null, string paymentId = "")
        {
            return new HostedPaymentResult
            {
                Success = true,
                RedirectUrl = redirectUrl,
                Token = token,
                Code = code,
                Message = message,
                PaymentId = paymentId
            };
        }

        public static HostedPaymentResult Failed(string ReservationID, string AuthCode, string CustomerCode, string errorMessage, string errorCode = null)
        {
            CreditCardErrorServices.Save(Convert.ToInt32(ReservationID), AuthCode, CustomerCode, errorCode + ": " + errorMessage);
            return new HostedPaymentResult
            {
                Success = false,
                Message = errorMessage,
                Code = errorCode
            };
        }
    }
}
