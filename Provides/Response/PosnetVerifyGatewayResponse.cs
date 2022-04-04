using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Lib.Payment.Provides.Response
{
    [XmlRoot(ElementName = "posnetResponse")]
    public class PosnetVerifyGatewayResponse
    {
        [XmlElement(ElementName = "approved")]
        public int Approved { get; set; }

        [XmlElement(ElementName = "respCode")]
        public string RespCode { get; set; }

        [XmlElement(ElementName = "respText")]
        public string RespText { get; set; }

        [XmlElement(ElementName = "oosResolveMerchantDataResponse")]
        public OosResolveMerchantDataResponse OosResolveMerchantDataResponse { get; set; }
    }

    [XmlRoot(ElementName = "oosResolveMerchantDataResponse")]
    public class OosResolveMerchantDataResponse
    {

        [XmlElement(ElementName = "xid")]
        public string Xid { get; set; }

        [XmlElement(ElementName = "amount")]
        public int Amount { get; set; }

        [XmlElement(ElementName = "currency")]
        public string Currency { get; set; }

        [XmlElement(ElementName = "installment")]
        public int Installment { get; set; }

        [XmlElement(ElementName = "point")]
        public int Point { get; set; }

        [XmlElement(ElementName = "pointAmount")]
        public int PointAmount { get; set; }

        [XmlElement(ElementName = "txStatus")]
        public string TxStatus { get; set; }

        [XmlElement(ElementName = "mdStatus")]
        public int MdStatus { get; set; }

        [XmlElement(ElementName = "mdErrorMessage")]
        public string MdErrorMessage { get; set; }

        [XmlElement(ElementName = "mac")]
        public string Mac { get; set; }
    }
    #region Response Enum
    public enum YKBApproved
    {
        unsuccessful = 0,
        successful = 1,
        pre_approved = 1,
    }
    #endregion

    #region 3D 2. Post 
    [XmlRoot(ElementName = "posnetResponse")]
    public class PosnetVerifyGatewayResponseV1
    {

        [XmlElement(ElementName = "approved")]
        public int Approved { get; set; }

        [XmlElement(ElementName = "respCode")]
        public string RespCode { get; set; }

        [XmlElement(ElementName = "respText")]
        public string RespText { get; set; }

        [XmlElement(ElementName = "mac")]
        public string Mac { get; set; }

        [XmlElement(ElementName = "hostlogkey")]
        public string Hostlogkey { get; set; }

        [XmlElement(ElementName = "authCode")]
        public string AuthCode { get; set; }

        [XmlElement(ElementName = "instInfo")]
        public InstInfo InstInfo { get; set; }

        [XmlElement(ElementName = "pointInfo")]
        public PointInfo PointInfo { get; set; }
    }
    [XmlRoot(ElementName = "instInfo")]
    public class InstInfo
    {

        [XmlElement(ElementName = "inst1")]
        public int Inst1 { get; set; }

        [XmlElement(ElementName = "amnt1")]
        public int Amnt1 { get; set; }
    }

    [XmlRoot(ElementName = "pointInfo")]
    public class PointInfo
    {

        [XmlElement(ElementName = "point")]
        public int Point { get; set; }

        [XmlElement(ElementName = "pointAmount")]
        public int PointAmount { get; set; }

        [XmlElement(ElementName = "totalPoint")]
        public int TotalPoint { get; set; }

        [XmlElement(ElementName = "totalPointAmount")]
        public int TotalPointAmount { get; set; }
    }
    #endregion
}
