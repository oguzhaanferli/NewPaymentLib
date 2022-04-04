using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lib.Payment.Requests;
using Lib.Payment.Results;

namespace Lib.Payment
{
    public interface IPaymentProvider
    {
        PaymentGatewayResult ThreeDGatewayRequest(PaymentGatewayRequest request);
        VerifyGatewayResult VerifyGateway(VerifyGatewayRequest request, System.Web.HttpRequest httpRequest);
        CancelPaymentResult CancelRequest(CancelPaymentRequest request);
        RefundPaymentResult RefundRequest(RefundPaymentRequest request);
        PaymentDetailResult PaymentDetailRequest(PaymentDetailRequest request);
        HostedPaymentResult HostedPaymentPageRequest(HostedPaymentRequest request);
        Dictionary<string, string> TestParameters { get; }
    }
}
