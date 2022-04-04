using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.Payment.Provides.Response
{
    public class Available
    {
        public int capture { get; set; }
        public int credit { get; set; }
    }

    public class Currency
    {
        public string code { get; set; }
        public bool historic { get; set; }
        public object localname { get; set; }
        public int minorunits { get; set; }
        public string name { get; set; }
        public string number { get; set; }
    }

    public class Acquirer
    {
        public string name { get; set; }
    }

    public class Eci
    {
        public string value { get; set; }
    }

    public class Paymenttype
    {
        public string displayname { get; set; }
        public int groupid { get; set; }
        public int id { get; set; }
    }

    public class Primaryaccountnumber
    {
        public string number { get; set; }
    }

    public class Information
    {
        public List<Acquirer> acquirers { get; set; }
        public List<Eci> ecis { get; set; }
        public List<Paymenttype> paymenttypes { get; set; }
        public List<Primaryaccountnumber> primaryaccountnumbers { get; set; }
        public List<object> wallets { get; set; }
    }

    public class Links
    {
        public string transactionoperations { get; set; }
    }

    public class Total
    {
        public int authorized { get; set; }
        public int balance { get; set; }
        public int captured { get; set; }
        public int credited { get; set; }
        public int declined { get; set; }
        public int feeamount { get; set; }
    }

    public class Transaction
    {
        public Available available { get; set; }
        public bool candelete { get; set; }
        public DateTime createddate { get; set; }
        public Currency currency { get; set; }
        public string id { get; set; }
        public Information information { get; set; }
        public Links links { get; set; }
        public string merchantnumber { get; set; }
        public string orderid { get; set; }
        public string reference { get; set; }
        public string status { get; set; }
        public object subscription { get; set; }
        public Total total { get; set; }
    }

    public class BamboraHostedPaymentGatewayResponse
    {
        public Meta meta { get; set; }
        public Transaction transaction { get; set; }
    }

}
