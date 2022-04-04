using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Lib.Payment.Provides.Request
{
    [XmlRoot(ElementName = "posnetRequest")]
    public class PosnetVerifyGatewayRequest
    {
        [XmlElement(ElementName = "mid")]
        public string Mid { get; set; }

        [XmlElement(ElementName = "tid")]
        public string Tid { get; set; }

        [XmlElement(ElementName = "oosResolveMerchantData")]
        public OosResolveMerchantData OosResolveMerchantData { get; set; }
    }

    //Banka Dönüşü 3D Kontrol
    [XmlRoot(ElementName = "oosResolveMerchantData")]
    public class OosResolveMerchantData
    {

        [XmlElement(ElementName = "bankData")]
        public string BankData { get; set; }

        [XmlElement(ElementName = "merchantData")]
        public string MerchantData { get; set; }

        [XmlElement(ElementName = "sign")]
        public string Sign { get; set; }

        [XmlElement(ElementName = "mac")]
        public string Mac { get; set; }
    }

    #region 3D 2. Post 
    [XmlRoot(ElementName = "posnetRequest")]
    public class PosnetVerifyGatewayRequestV1
    {
        [XmlElement(ElementName = "mid")]
        public string Mid { get; set; }

        [XmlElement(ElementName = "tid")]
        public string Tid { get; set; }

        [XmlElement(ElementName = "oosTranData")]
        public OosTranData OosTranData { get; set; }
    }
    public class OosTranData
    {

        [XmlElement(ElementName = "bankData")]
        public string BankData { get; set; }

        [XmlElement(ElementName = "wpAmount")]
        public int WpAmount { get; set; }

        [XmlElement(ElementName = "mac")]
        public string Mac { get; set; }
    }
    #endregion
}
