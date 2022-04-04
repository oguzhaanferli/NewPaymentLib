using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.Payment.Provides.Request
{

    public class NetsEasyHostedPaymentRequest
    {
        public NetsEasyOrder order { get; set; }
        public Checkout checkout { get; set; }
        public string merchantNumber { get; set; }
        public List<PaymentMethods> paymentMethods { get; set; }
    }
    public class NetsEasyOrder
    {
        public List<Item> items { get; set; }
        public int amount { get; set; }
        public string currency { get; set; }
        public string reference { get; set; }
    }

    public class Item
    {
        public string reference { get; set; }
        public string name { get; set; }
        public int quantity { get; set; }
        public string unit { get; set; }
        public int unitPrice { get; set; }
        public int grossTotalAmount { get; set; }
        public int netTotalAmount { get; set; }
    }
    public class Checkout
    {
        public string termsUrl { get; set; }
        public string returnUrl { get; set; }
        public string cancelUrl { get; set; }
        public string integrationType { get; set; }
    }

    public class PaymentMethods
    {
        public string name { get; set; }
        public string fee { get; set; } = String.Empty;
    }
}
