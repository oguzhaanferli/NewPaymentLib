using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lib.Payment.Models;

namespace Lib.Payment.Requests
{
    public class HostedPaymentRequest
    {
        public string CustomerCode { get; set; }
        public string ReservationID { get; set; }
        public string VoucherNo { get; set; } = String.Empty;
        public string BankServiceName { get; set; }
        public string AuthCode { get; set; }
        public double Amount { get; set; }
        public int Market { get; set; }
        public int Installment { get; set; }
        public string BaseSiteUrl { get; set; }
        public PaymentCurrency CurrencyIsoCode { get; set; } = PaymentCurrency.TRY;
        public PaymentLanguage LanguageIsoCode { get; set; } = PaymentLanguage.en;
        public bool Development { get; set; } = false;
    }
}
