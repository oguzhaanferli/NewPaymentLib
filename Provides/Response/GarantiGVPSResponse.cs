using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Lib.Payment.Provides.Response
{
    [XmlRoot(ElementName = "Terminal")]
    public class GarantiResponseTerminal
    {

        [XmlElement(ElementName = "ProvUserID")]
        public string ProvUserID { get; set; }

        [XmlElement(ElementName = "UserID")]
        public int UserID { get; set; }

        [XmlElement(ElementName = "ID")]
        public int ID { get; set; }

        [XmlElement(ElementName = "MerchantID")]
        public int MerchantID { get; set; }
    }

    [XmlRoot(ElementName = "Customer")]
    public class GarantiResponseCustomer
    {

        [XmlElement(ElementName = "IPAddress")]
        public string IPAddress { get; set; }

        [XmlElement(ElementName = "EmailAddress")]
        public string EmailAddress { get; set; }
    }

    [XmlRoot(ElementName = "Order")]
    public class GarantiResponseOrder
    {

        [XmlElement(ElementName = "OrderID")]
        public double OrderID { get; set; }

        [XmlElement(ElementName = "GroupID")]
        public object GroupID { get; set; }
    }

    [XmlRoot(ElementName = "Response")]
    public class GarantiResponseTransactionResponse
    {

        [XmlElement(ElementName = "Source")]
        public string Source { get; set; }

        [XmlElement(ElementName = "Code")]
        public int Code { get; set; }

        [XmlElement(ElementName = "ReasonCode")]
        public int ReasonCode { get; set; }

        [XmlElement(ElementName = "Message")]
        public string Message { get; set; }

        [XmlElement(ElementName = "ErrorMsg")]
        public string ErrorMsg { get; set; }

        [XmlElement(ElementName = "SysErrMsg")]
        public string SysErrMsg { get; set; }
    }

    [XmlRoot(ElementName = "RewardInqResult")]
    public class GarantiResponseRewardInqResult
    {

        [XmlElement(ElementName = "RewardList")]
        public string RewardList { get; set; }

        [XmlElement(ElementName = "ChequeList")]
        public string ChequeList { get; set; }
    }

    [XmlRoot(ElementName = "Transaction")]
    public class GarantiResponseTransaction
    {

        [XmlElement(ElementName = "Response")]
        public GarantiResponseTransactionResponse Response { get; set; }

        [XmlElement(ElementName = "RetrefNum")]
        public string RetrefNum { get; set; }

        [XmlElement(ElementName = "AuthCode")]
        public string AuthCode { get; set; }

        [XmlElement(ElementName = "BatchNum")]
        public string BatchNum { get; set; }

        [XmlElement(ElementName = "SequenceNum")]
        public string SequenceNum { get; set; }

        [XmlElement(ElementName = "ProvDate")]
        public string ProvDate { get; set; }

        [XmlElement(ElementName = "CardNumberMasked")]
        public string CardNumberMasked { get; set; }

        [XmlElement(ElementName = "CardHolderName")]
        public string CardHolderName { get; set; }

        [XmlElement(ElementName = "CardType")]
        public string CardType { get; set; }

        [XmlElement(ElementName = "HashData")]
        public string HashData { get; set; }

        [XmlElement(ElementName = "HostMsgList")]
        public string HostMsgList { get; set; }

        [XmlElement(ElementName = "RewardInqResult")]
        public GarantiResponseRewardInqResult RewardInqResult { get; set; }
    }

    [XmlRoot(ElementName = "GVPSResponse")]
    public class GarantiGVPSResponse
    {

        [XmlElement(ElementName = "Mode")]
        public object Mode { get; set; }

        [XmlElement(ElementName = "Terminal")]
        public GarantiResponseTerminal Terminal { get; set; }

        [XmlElement(ElementName = "Customer")]
        public GarantiResponseCustomer Customer { get; set; }

        [XmlElement(ElementName = "Order")]
        public GarantiResponseOrder Order { get; set; }

        [XmlElement(ElementName = "Transaction")]
        public GarantiResponseTransaction Transaction { get; set; }
    }
}
