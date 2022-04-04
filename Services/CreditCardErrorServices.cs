
using System.Linq;
using Lib.Business.v2.Context;
using Lib.DataModel.v2.Entities.BOOKING;

namespace Lib.Payment.Services
{
    public class CreditCardErrorServices
    {
        public static void Save(int reservationID, string authCode, string customerCode, string description)
        {
            ContextBase cb = new ContextBase(customerCode);
            TReservation_Product_Payments reservation_Product_Payments = cb.GetQuery<TReservation_Product_Payments>().Where(x => x.stBankAuthCode == authCode).FirstOrDefault();
            if (reservation_Product_Payments != null)
            {
                TCreditCard_Error creditCard_Error = new TCreditCard_Error();
                creditCard_Error.stDesc = description;
                creditCard_Error.inResID = reservationID;
                creditCard_Error.dtCreatedDate = System.DateTime.Now;
                creditCard_Error.inPaymentID = reservation_Product_Payments.inID;
                cb.Add(creditCard_Error);
            }
        }
    }
}
