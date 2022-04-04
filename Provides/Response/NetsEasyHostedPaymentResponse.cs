using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.Payment.Provides.Response
{
    public class NetsEasyHostedPaymentResponse
    {
        public string hostedPaymentPageUrl { get; set; }
        public string paymentId { get; set; }
    }

    public class Errors
    {
        public List<string> property1 { get; set; }
        public List<string> property2 { get; set; }
    }

    public class NetsEasyHostedPaymentErrorResponse
    {
        public Errors errors { get; set; }
        public string message { get; set; }
        public string code { get; set; }
        public string source { get; set; }
    }

}
