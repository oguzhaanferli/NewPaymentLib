using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.Payment.Provides.Response
{
    public class FinansbankVerifyGatewayResponse
    {
        public string ErrMsg { get; set; }
        public string TxnResult { get; set; }
        public string InstallmentCount { get; set; }
        public string ProcReturnCode { get; set; }
        public string OrderId { get; set; }
    }
}
