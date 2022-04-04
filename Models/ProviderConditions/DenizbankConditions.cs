using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lib.Payment.Services;

namespace Lib.Payment.Models.ProviderConditions
{
    public class DenizbankConditions
    {
        public bool Credential(PaymentCredentialParameter credential)
        {
            if (credential == null)
            {
                return false;
            }
            if (string.IsNullOrEmpty(credential.ApiUrl) ||
               string.IsNullOrEmpty(credential.GatewayUrl) ||
               string.IsNullOrEmpty(credential.StoreKey) ||
               string.IsNullOrEmpty(credential.MerchantPass) ||
               string.IsNullOrEmpty(credential.UserName) ||
               string.IsNullOrEmpty(credential.Password))
            {
                return false;
            }
            return true;
        }
    }
}
