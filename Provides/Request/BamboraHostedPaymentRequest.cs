using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.Payment.Provides.Request
{
    public class BamboraHostedPaymentRequest
    {
        public Order order { get; set; }
        public Url url { get; set; }
        public Paymentwindow paymentwindow { get; set; }
    }

    public class Order
    {
        public string id { get; set; }
        public string amount { get; set; }
        public string currency { get; set; }
    }

    public class Url
    {
        public string accept { get; set; }
        public string cancel { get; set; }
    }
    public class Paymentwindow
    {
        public string language { get; set; }
    }
}
