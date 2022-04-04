using System;
using System.Linq;
using Lib.Payment.Models;
using Lib.Business.v2.Context;
using Lib.DataModel.v2.Entities.PARAMETERS;

namespace Lib.Payment.Services
{
    public class PaymentCredentialParameter
    {
        public string ApiUrl { get; set; }
        public string GatewayUrl { get; set; }
        public string ClientID { get; set; }
        public string StoreKey { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string MerchantID { get; set; }
        public string MerchantPass { get; set; }
        public string ApiKey { get; set; }
        public string SecretKey { get; set; }
        public string AccessKey { get; set; }
        public string MD5Key { get; set; }
        public string PosnetID { get; set; }
        public string ProvUserID { get; set; }
        public string TerminalID { get; set; }
        public string TerminalID1 { get; set; }
        public string EncKey { get; set; }
        public string StoreType { get; set; } = "3D";
        public string TxnType { get; set; } = "Auth"; // Ön Satış: PreAuth -  Satış: Auth

        public PaymentCredentialParameter GetCredential(string CustomerCode, string BankName, int Market)
        {
            ContextBase cb = new ContextBase(CustomerCode);
            TBank bank = cb.GetQuery<TBank>().Where(x => x.stServiceName == BankName && x.inMarket == Market && x.stVURL != null && x.stVURL != "").FirstOrDefault();
            if (bank != null)
            {
                return new PaymentCredentialParameter
                {
                    ApiUrl = bank.stVURL,
                    GatewayUrl = bank.st3DURL,
                    UserName = bank.stVUserName,
                    Password = bank.stVPassword,
                    StoreKey = bank.stStoreKey,
                    ClientID = bank.stVClintID,
                    MerchantID = bank.stMerchantID,
                    MerchantPass = bank.stMerchantPass,
                    AccessKey = bank.stAccessKey,
                    ApiKey = bank.stApiKey,
                    SecretKey = bank.stSecretKey,
                    MD5Key = bank.stMD5Key,
                    PosnetID = bank.st3DPosnetID,
                    TerminalID = bank.stVTerminalID ?? bank.st3DTerminalID,
                    ProvUserID = bank.stProviderUserID,
                    EncKey = bank.stEncKey,
                    TerminalID1 = bank.stTerminalID1

                };
            }
            else return null;
        }
    }
}
