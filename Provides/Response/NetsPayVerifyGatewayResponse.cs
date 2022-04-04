using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.Payment.Provides.Response
{
    public class NetsPayVerifyGatewayResponse
    {
        public string Response { get; set; }
        public string Orderid { get; set; }
        public string AuthCode { get; set; }
        public string ProcReturnCode { get; set; }
        public string HostRefNum { get; set; }
        public string TransId { get; set; }
        public string ErrMsg { get; set; }
    }
}
