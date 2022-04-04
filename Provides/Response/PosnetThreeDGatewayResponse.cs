using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Lib.Payment.Provides.Response
{
    [XmlRoot(ElementName = "posnetResponse")]
    public class PosnetThreeDGatewayResponse
    {

        [XmlElement(ElementName = "approved")]
        public int Approved { get; set; }

        [XmlElement(ElementName = "respCode")]
        public string RespCode { get; set; }

        [XmlElement(ElementName = "respText")]
        public string RespText { get; set; }

        [XmlElement(ElementName = "oosRequestDataResponse")]
        public OosRequestDataResponse OosRequestDataResponse { get; set; }
    }

    [XmlRoot(ElementName = "oosRequestDataResponse")]
    public class OosRequestDataResponse
    {

        [XmlElement(ElementName = "data1")]
        public string Data1 { get; set; }

        [XmlElement(ElementName = "data2")]
        public string Data2 { get; set; }

        [XmlElement(ElementName = "sign")]
        public string Sign { get; set; }
    }
}
