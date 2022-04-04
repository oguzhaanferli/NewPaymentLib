using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Lib.Payment.Provides.Request
{
	[XmlRoot(ElementName = "Terminal")]
	public class GarantiRequestTerminal
	{

		[XmlElement(ElementName = "ProvUserID")]
		public string ProvUserID { get; set; }

		[XmlElement(ElementName = "HashData")]
		public string HashData { get; set; }

		[XmlElement(ElementName = "UserID")]
		public string UserID { get; set; }

		[XmlElement(ElementName = "ID")]
		public string ID { get; set; }

		[XmlElement(ElementName = "MerchantID")]
		public string MerchantID { get; set; }
	}

	[XmlRoot(ElementName = "Customer")]
	public class GarantiRequestCustomer
	{

		[XmlElement(ElementName = "IPAddress")]
		public string IPAddress { get; set; }

		[XmlElement(ElementName = "EmailAddress")]
		public string EmailAddress { get; set; }
	}

	[XmlRoot(ElementName = "Card")]
	public class GarantiRequestCard
	{

		[XmlElement(ElementName = "Number")]
		public string Number { get; set; }

		[XmlElement(ElementName = "ExpireDate")]
		public string ExpireDate { get; set; }

		[XmlElement(ElementName = "CVV2")]
		public string CVV2 { get; set; }
	}

	[XmlRoot(ElementName = "Order")]
	public class GarantiRequestOrder
	{

		[XmlElement(ElementName = "OrderID")]
		public string OrderID { get; set; }

		[XmlElement(ElementName = "GroupID")]
		public string GroupID { get; set; }

		[XmlElement(ElementName = "Description")]
		public string Description { get; set; }
	}

	[XmlRoot(ElementName = "Secure3D")]
	public class GarantiRequestSecure3D
	{

		[XmlElement(ElementName = "AuthenticationCode")]
		public string AuthenticationCode { get; set; }

		[XmlElement(ElementName = "SecurityLevel")]
		public string SecurityLevel { get; set; }

		[XmlElement(ElementName = "TxnID")]
		public string TxnID { get; set; }

		[XmlElement(ElementName = "Md")]
		public string Md { get; set; }
	}

	[XmlRoot(ElementName = "Transaction")]
	public class GarantiRequestTransaction
	{

		[XmlElement(ElementName = "Type")]
		public string Type { get; set; }

		[XmlElement(ElementName = "InstallmentCnt")]
		public string InstallmentCnt { get; set; }

		[XmlElement(ElementName = "Amount")]
		public string Amount { get; set; }

		[XmlElement(ElementName = "CurrencyCode")]
		public string CurrencyCode { get; set; }

		[XmlElement(ElementName = "CardholderPresentCode")]
		public string CardholderPresentCode { get; set; }

		[XmlElement(ElementName = "MotoInd")]
		public string MotoInd { get; set; }

		[XmlElement(ElementName = "Secure3D")]
		public GarantiRequestSecure3D Secure3D { get; set; }

		[XmlElement(ElementName = "Description")]
		public string Description { get; set; }

		[XmlElement(ElementName = "OriginalRetrefNum")]
		public string OriginalRetrefNum { get; set; }
	}

	[XmlRoot(ElementName = "GVPSRequest")]
	public class GarantiGVPSRequest
	{

		[XmlElement(ElementName = "Mode")]
		public string Mode { get; set; }

		[XmlElement(ElementName = "Version")]
		public string Version { get; set; }

		[XmlElement(ElementName = "Terminal")]
		public GarantiRequestTerminal Terminal { get; set; }

		[XmlElement(ElementName = "Customer")]
		public GarantiRequestCustomer Customer { get; set; }

		[XmlElement(ElementName = "Card")]
		public GarantiRequestCard Card { get; set; }

		[XmlElement(ElementName = "Order")]
		public GarantiRequestOrder Order { get; set; }

		[XmlElement(ElementName = "Transaction")]
		public GarantiRequestTransaction Transaction { get; set; }
	}

}
