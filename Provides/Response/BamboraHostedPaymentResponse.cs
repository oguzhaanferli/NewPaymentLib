using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.Payment.Provides.Response
{
    public class Message
    {
        public string enduser { get; set; }
        public string merchant { get; set; }
    }

    public class Action
    {
        public string source { get; set; }
        public string code { get; set; }
        public string type { get; set; }
    }

    public class Meta
    {
        public bool result { get; set; }
        public Message message { get; set; }
        public Action action { get; set; }
    }

    public class BamboraHostedPaymentResponse
    {
        public string token { get; set; }
        public string url { get; set; }
        public Meta meta { get; set; }
    }

}
