using System;
using System.Collections.Generic;

namespace Lib.Payment
{
    public interface IPaymentProviderFactory
    {
        IPaymentProvider Create(string bankServiceName);
        string CreatePaymentFormHtml(IDictionary<string, object> parameters, string actionUrl, bool appendFormSubmitScript = true);
    }
}
