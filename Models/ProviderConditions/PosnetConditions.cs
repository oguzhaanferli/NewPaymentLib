using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lib.Payment.Services;

namespace Lib.Payment.Models.ProviderConditions
{
    public class PosnetConditions
    {
        public bool Credential(PaymentCredentialParameter credential)
        {
            if (credential == null)
            {
                return false;
            }
            if (string.IsNullOrEmpty(credential.MerchantID) ||
               string.IsNullOrEmpty(credential.PosnetID) ||
               string.IsNullOrEmpty(credential.ApiUrl) ||
               string.IsNullOrEmpty(credential.GatewayUrl) ||
               string.IsNullOrEmpty(credential.TerminalID) ||
               string.IsNullOrEmpty(credential.EncKey))
            {
                return false;
            }
            return true;
        }
    }
}
