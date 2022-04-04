using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lib.Payment.Services;

namespace Lib.Payment.Models.ProviderConditions
{
    public class GarantiConditions
    {
        public bool Credential(PaymentCredentialParameter credential)
        {
            if (credential == null)
            {
                return false;
            }
            if (string.IsNullOrEmpty(credential.ApiUrl) ||
               string.IsNullOrEmpty(credential.GatewayUrl) ||
               string.IsNullOrEmpty(credential.TerminalID) ||
               string.IsNullOrEmpty(credential.TerminalID1) ||
               string.IsNullOrEmpty(credential.ProvUserID) ||
               string.IsNullOrEmpty(credential.Password) ||
               string.IsNullOrEmpty(credential.UserName) ||
               string.IsNullOrEmpty(credential.MerchantID))
            {
                return false;
            }
            return true;
        }
    }
}
