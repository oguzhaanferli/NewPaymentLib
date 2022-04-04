using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lib.Payment.Services;

namespace Lib.Payment.Models.ProviderConditions
{
    public class BamboraConditions
    {
        public bool Credential(PaymentCredentialParameter credential)
        {
            if (credential == null)
            {
                return false;
            }
            if (string.IsNullOrEmpty(credential.AccessKey) ||
               string.IsNullOrEmpty(credential.MerchantID) ||
               string.IsNullOrEmpty(credential.SecretKey) ||
               string.IsNullOrEmpty(credential.GatewayUrl) ||
               string.IsNullOrEmpty(credential.MD5Key) ||
               string.IsNullOrEmpty(credential.ApiUrl))
            {
                return false;
            }
            return true;
        }
    }
}
