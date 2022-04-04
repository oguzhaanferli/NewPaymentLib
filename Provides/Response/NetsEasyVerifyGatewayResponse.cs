using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.Payment.Provides.Response
{
    public class Summary
    {
        public int reservedAmount { get; set; }
    }

    public class ShippingAddress
    {
        public string addressLine1 { get; set; }
        public string receiverLine { get; set; }
        public string postalCode { get; set; }
        public string city { get; set; }
        public string country { get; set; }
    }

    public class PhoneNumber
    {
        public string prefix { get; set; }
        public string number { get; set; }
    }

    public class ContactDetails
    {
        public PhoneNumber phoneNumber { get; set; }
    }

    public class Company
    {
        public ContactDetails contactDetails { get; set; }
    }

    public class PrivatePerson
    {
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string email { get; set; }
        public PhoneNumber phoneNumber { get; set; }
    }

    public class BillingAddress
    {
        public string addressLine1 { get; set; }
        public string receiverLine { get; set; }
        public string postalCode { get; set; }
        public string city { get; set; }
        public string country { get; set; }
    }

    public class Consumer
    {
        public ShippingAddress shippingAddress { get; set; }
        public Company company { get; set; }
        public PrivatePerson privatePerson { get; set; }
        public BillingAddress billingAddress { get; set; }
    }

    public class InvoiceDetails
    {
    }

    public class CardDetails
    {
        public string maskedPan { get; set; }
        public string expiryDate { get; set; }
    }

    public class PaymentDetails
    {
        public string paymentType { get; set; }
        public string paymentMethod { get; set; }
        public InvoiceDetails invoiceDetails { get; set; }
        public CardDetails cardDetails { get; set; }
    }

    public class OrderDetails
    {
        public int amount { get; set; }
        public string currency { get; set; }
    }

    public class VerifyGatewayCheckout
    {
        public string url { get; set; }
        public string cancelUrl { get; set; }
    }

    public class Payment
    {
        public string paymentId { get; set; }
        public Summary summary { get; set; }
        public Consumer consumer { get; set; }
        public PaymentDetails paymentDetails { get; set; }
        public OrderDetails orderDetails { get; set; }
        public VerifyGatewayCheckout checkout { get; set; }
        public DateTime created { get; set; }
    }

    public class NetsEasyVerifyGatewayResponse
    {
        public Payment payment { get; set; }
    }


}
