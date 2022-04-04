using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lib.Business.v2.Context;
using Lib.DataModel.v2.Entities.BOOKING;

namespace Lib.Payment.Models
{
    public class CardInformation
    {
        public string CardNumber { get; set; }
        public string CardYear { get; set; }
        public string CardMonth { get; set; }
        public string CardCv2 { get; set; }

        public static CardInformation GetCreditCardInfo(int reservationID, string authCode, string customerCode)
        {
            ContextBase cb = new ContextBase(customerCode);
            TReservation_Product_Payments reservation_Product_Payments = cb.GetQuery<TReservation_Product_Payments>().Where(x => x.inResID == reservationID && x.stBankAuthCode == authCode).FirstOrDefault();
            return new CardInformation
            {
                CardCv2 = reservation_Product_Payments.stCVV,
                CardMonth = (reservation_Product_Payments.inCardMonth.ToString().Length == 1 ? "0" + reservation_Product_Payments.inCardMonth.ToString() : reservation_Product_Payments.inCardMonth.ToString()),
                CardYear = reservation_Product_Payments.inCardYear.ToString().Substring(2, 2),
                CardNumber = reservation_Product_Payments.stCardNo
            };
        }
        public static void Mask(int reservationID, string authCode, string customerCode)
        {
            ContextBase cb = new ContextBase(customerCode);
            TReservation_Product_Payments reservation_Product_Payments = cb.GetQuery<TReservation_Product_Payments>().Where(x => x.inResID == reservationID && x.stBankAuthCode == authCode).FirstOrDefault();
            string maskcardno = reservation_Product_Payments.stCardNo.Substring(0, 4) + " **** **** " + reservation_Product_Payments.stCardNo.Substring(12);
            reservation_Product_Payments.stCardNo = maskcardno;
            reservation_Product_Payments.stCVV = "***";
            reservation_Product_Payments.inCardMonth = null;
            reservation_Product_Payments.inCardYear = null;
            cb.Update(reservation_Product_Payments);
        }
    }
}
