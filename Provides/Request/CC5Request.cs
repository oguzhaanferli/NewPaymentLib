using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.Payment.Provides.Request
{
    public class CC5Request
    {
        public string Name { get; set; }
        public string Password { get; set; }
        public string ClientId { get; set; }
        public string OrderId { get; set; }
        public string IPAddress { get; set; }
        public string Type { get; set; }
        public string Number { get; set; }
        public string Total { get; set; }
        public string Currency { get; set; }
        public string Taksit { get; set; }
        public string PayerTxnId { get; set; }
        public string PayerSecurityLevel { get; set; }
        public string PayerAuthenticationCode { get; set; }
    }
}
