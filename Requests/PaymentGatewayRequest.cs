using System;
using System.Collections.Generic;
using Lib.Payment.Models;

namespace Lib.Payment.Requests
{
    public class PaymentGatewayRequest
    {
        public string CardHolderName { get; set; }
        public string CardNumber { get; set; }
        public string CardYear { get; set; }
        public string CardMonth { get; set; }
        public string CardCV2 { get; set; }
        public int Installment { get; set; }
        public double Amount { get; set; }
        public PaymentCurrency CurrencyIsoCode { get; set; } = PaymentCurrency.TRY;
        public PaymentLanguage LanguageIsoCode { get; set; } = PaymentLanguage.tr;
        public string ReservationID { get; set; }
        public string AuthCode { get; set; }
        public string BankServiceName { get; set; }
        public string CustomerCode { get; set; }
        public int Market { get; set; }
        public string BaseSiteUrl { get; set; }
        public bool Development { get; set; }
        public Dictionary<string, string> BankParameters { get; set; } = new Dictionary<string, string>();
    }
}