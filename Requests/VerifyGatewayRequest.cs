using System.Collections.Generic;
using Lib.Payment.Models;

namespace Lib.Payment
{
    public class VerifyGatewayRequest
    {
        public string CustomerIpAddress { get; set; }
        public string CustomerCode { get; set; }
        public string BankServiceName { get; set; }
        public string AuthCode { get; set; }
        public string PaymentId { get; set; }
        public int Market { get; set; }
        public PaymentCurrency CurrencyIsoCode { get; set; } = PaymentCurrency.TRY;
        public bool Development { get; set; } = false;
        public Dictionary<string, string> BankParameters { get; set; } = new Dictionary<string, string>();
        public string ReservationID { get; set; }
    }
}