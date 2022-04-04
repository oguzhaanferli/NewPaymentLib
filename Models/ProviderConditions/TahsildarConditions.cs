using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lib.Payment.Services;

namespace Lib.Payment.Models.ProviderConditions
{
    public class TahsildarConditions
    {
        public bool Credential(PaymentCredentialParameter credential)
        {
            if (credential == null)
            {
                return false;
            }
            if (string.IsNullOrEmpty(credential.ApiUrl) ||
               string.IsNullOrEmpty(credential.ApiKey) ||
               string.IsNullOrEmpty(credential.SecretKey))
            {
                return false;
            }
            return true;
        }
    }
}
