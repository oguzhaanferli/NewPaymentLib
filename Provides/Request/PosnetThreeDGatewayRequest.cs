using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Lib.Payment.Provides.Request
{
    [XmlRoot(ElementName = "posnetRequest")]
    public class PosnetThreeDGatewayRequest
    {

        [XmlElement(ElementName = "mid")]
        public string Mid { get; set; }

        [XmlElement(ElementName = "tid")]
        public string Tid { get; set; }

        [XmlElement(ElementName = "oosRequestData")]
        public OosRequestData OosRequestData { get; set; }
    }
    [XmlRoot(ElementName = "oosRequestData")]
    public class OosRequestData
    {

        [XmlElement(ElementName = "posnetid")]
        public string Posnetid { get; set; }

        [XmlElement(ElementName = "XID")]
        public string XID { get; set; }

        [XmlElement(ElementName = "amount")]
        public string Amount { get; set; }

        [XmlElement(ElementName = "currencyCode")]
        public string CurrencyCode { get; set; }

        [XmlElement(ElementName = "installment")]
        public int Installment { get; set; }

        [XmlElement(ElementName = "tranType")]
        public string TranType { get; set; }

        [XmlElement(ElementName = "cardHolderName")]
        public string CardHolderName { get; set; }

        [XmlElement(ElementName = "ccno")]
        public string Ccno { get; set; }

        [XmlElement(ElementName = "expDate")]
        public int ExpDate { get; set; }

        [XmlElement(ElementName = "cvc")]
        public string Cvc { get; set; }
    }
}
