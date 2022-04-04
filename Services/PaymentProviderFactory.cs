using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lib.Payment.Providers;

namespace Lib.Payment
{
    public class PaymentProviderFactory : IPaymentProviderFactory
    {
        private static readonly Dictionary<BankNames, Type> _providerTypes = new Dictionary<BankNames, Type>
        {
            //NestPay(AkBank, IsBankasi, HalkBank, ZiraatBankasi, TurkEkonomiBankasi, IngBank, TurkiyeFinans, AnadoluBank, HSBC, SekerBank)
            { BankNames.Akbank, typeof(NestPayPaymentProvider) },
            { BankNames.IsBankasi, typeof(NestPayPaymentProvider) },
            { BankNames.HalkBank, typeof(NestPayPaymentProvider) },
            { BankNames.ZiraatBankasi, typeof(NestPayPaymentProvider) },
            { BankNames.TurkEkonomiBankasi, typeof(NestPayPaymentProvider) },
            { BankNames.HSBC, typeof(NestPayPaymentProvider) },
            { BankNames.SekerBank, typeof(NestPayPaymentProvider) },
            { BankNames.IngBank, typeof(NestPayPaymentProvider) },
            { BankNames.TurkiyeFinans, typeof(NestPayPaymentProvider) },
            { BankNames.AnadoluBank, typeof(NestPayPaymentProvider) },
            { BankNames.AlternatifBank, typeof(NestPayPaymentProvider) },

            //Denizbank(InterVpos)
            { BankNames.DenizBank, typeof(DenizbankPaymentProvider) },

            //Finansbank(PayFor)
            { BankNames.CardFinans, typeof(FinansbankPaymentProvider) },

            //Garanti(GVP)
            { BankNames.GarantiBankasi, typeof(GarantiPaymentProvider) },

            //KuveytTurk
            //{ BankNames.KuveytTurk, typeof(KuveytTurkPaymentProvider) },

            //Vakıfbank(GET 7/24)
            { BankNames.VakifBank, typeof(VakifbankPaymentProvider) },

            //POSNET(Yapıkredi, AlbarakaTurk)
            { BankNames.YapiKredi, typeof(PosnetPaymentProvider) },
            { BankNames.Albaraka, typeof(PosnetPaymentProvider) },

            //Bambora 
            { BankNames.Bambora, typeof(BamboraPaymentProvider) },

            //Tahsildar 
            { BankNames.Tahsildar, typeof(TahsildarPaymentProvider) },

            //NetsEasy 
            { BankNames.NetsEasy, typeof(NetsEasyPaymentProvider) },
        };

        private readonly IServiceProvider _serviceProvider;

        public IPaymentProvider Create(string bankServiceName)
        {
            BankNames bankName = (BankNames)Enum.Parse(typeof(BankNames), bankServiceName);
            if (!_providerTypes.ContainsKey(bankName)) throw new NotSupportedException("Bank not supported");

            Type type = _providerTypes[bankName];
            return ActivatorUtilities.CreateInstance(_serviceProvider, type) as IPaymentProvider;
        }

        public string CreatePaymentFormHtml(IDictionary<string, object> parameters, string actionUrl, bool appendSubmitScript = true)
        {
            if (parameters == null || !parameters.Any())
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (actionUrl == null)
            {
                throw new ArgumentNullException(nameof(actionUrl));
            }

            string formId = "PaymentForm";
            StringBuilder formBuilder = new StringBuilder();
            formBuilder.Append($"<form id=\"{formId}\" name=\"{formId}\" target=\"hidden_iframe\" action=\"{actionUrl}\" method=\"POST\">");

            foreach (KeyValuePair<string, object> parameter in parameters)
            {
                formBuilder.Append($"<input type=\"hidden\" name=\"{parameter.Key}\" value=\"{parameter.Value}\">");
            }

            formBuilder.Append("</form>");

            if (appendSubmitScript)
            {
                StringBuilder scriptBuilder = new StringBuilder();
                scriptBuilder.Append("<script>");
                scriptBuilder.Append($"document.{formId}.submit();");
                scriptBuilder.Append("</script>");
                formBuilder.Append(scriptBuilder.ToString());
            }

            return formBuilder.ToString();
        }
    }
}
