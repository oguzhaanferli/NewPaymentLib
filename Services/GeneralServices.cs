using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Serialization;
using Lib.Payment.Models;
using Lib.Business.v2.Context;
using Lib.DataModel.v2.Entities.BOOKING;

namespace Lib.Payment.Services
{
    internal class GeneralServices
    {
        public string CardType { get; set; }
        public string OrderID { get; set; }
        public string CallbackUrl { get; set; }

        public GeneralServices GetPaymentGeneralInfo(string BaseSiteUrl = "", string ReservationID = "", string CardNumber = "", string ShoppingFileID = "")
        {
            string OrderID = (!string.IsNullOrEmpty(ReservationID) ? GetOrderID(ReservationID) : "");
            string CardType = (!string.IsNullOrEmpty(CardNumber) ? GetCardType(CardNumber) : "");
            string RedirectUrl = CreateUrl(BaseSiteUrl, ReservationID, ShoppingFileID);

            return new GeneralServices
            {
                OrderID = OrderID,
                CardType = CardType,
                CallbackUrl = RedirectUrl,
            };
        }
        public string GetOrderID(string ReservationNo)
        {
            string dateString = DateTime.Now.ToString("yyyyMMddHHmmss");
            string TmpOrderID = dateString + ReservationNo.ToString();
            if (TmpOrderID.Length < 20)
            {
                int eklenecek = 20 - TmpOrderID.Length;

                for (int i = 0; i < eklenecek; i++)
                {
                    TmpOrderID = TmpOrderID + i.ToString();
                }

            }
            if (TmpOrderID.Length > 20)
            {
                TmpOrderID = TmpOrderID.Substring(0, 20);
            }
            return TmpOrderID;
        }
        public string GetCardType(string CardNumber)
        {
            string CardType = "1";
            if ((Convert.ToInt32(CardNumber.Substring(0, 2)) >= 50) && (Convert.ToInt32(CardNumber.Substring(0, 2)) <= 55)) CardType = "2";
            return CardType;
        }
        public string CreateUrl(string Url, string ReservationID, string ShoppingFileID = "")
        {
            string urlLastCharacter = Url.Substring((Url.Length - 1), 1);
            string seperatorCharacter = "";
            if (urlLastCharacter != "/") seperatorCharacter = "/";
            string bankredirectUrl = "BankRedirectV1.aspx";
            if (Url.Contains("BankRedirect")) bankredirectUrl = "";
            return Url + seperatorCharacter + bankredirectUrl + "?ShoppingFileID=" + ShoppingFileID + "&ReservationID=" + ReservationID;
        }
        public string ToSerialize<T>(T serializeObject)
        {
            try
            {
                if (serializeObject == null)
                    return "";
                var xmlSerializer = new XmlSerializer(serializeObject.GetType());
                var textWriter = new StringWriter();
                xmlSerializer.Serialize(textWriter, serializeObject);
                return textWriter.ToString();
            }
            catch (Exception exception)
            {
                return string.Format("Object cannot serializable. Exception Message : {0} ,Inner Exception : {1}",
                    exception.Message, exception.InnerException);
            }
        }
    }
}
