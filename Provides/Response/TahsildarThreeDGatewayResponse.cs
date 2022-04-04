using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.Payment.Provides.Response
{
    public class TahsildarRequest
    {
        public string price { get; set; }
        public string currency { get; set; }
        public string installment { get; set; }
        public string order_no { get; set; }
        public string secure3d { get; set; }
        public string ip { get; set; }
        public string return_url { get; set; }
        public string provision_type { get; set; }
    }
    public class TahsildarThreeDGatewayResponse
    {
        public string result { get; set; }
        public bool success { get; set; }
        public string message { get; set; }
        public string summary { get; set; }
        public string pagingData { get; set; }
        public string errCode { get; set; }
        public string date { get; set; }
        public TahsildarRequest request { get; set; }
        public string transaction_no { get; set; }
        public string redirect_url { get; set; }
        public string hash { get; set; }
    }
}
