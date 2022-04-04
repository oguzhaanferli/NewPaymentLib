using System;
using Lib.Payment.Services;

namespace Lib.Payment.Results
{
    public class VerifyGatewayResult
    {
        public bool Success { get; set; }
        public string ResponseCode { get; set; }
        public string ReservationID { get; set; }
        public string AuthCode { get; set; }
        public string OrderNumber { get; set; }
        public int Installment { get; set; }
        public int ExtraInstallment { get; set; }
        public Uri CampaignUrl { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorCode { get; set; }

        public static VerifyGatewayResult Successed(string reservationID, string authCode, string OrderNumber,
            int installment = 0, int extraInstallment = 0,
            string message = null, string responseCode = null,
            string campaignUrl = null)
        {
            return new VerifyGatewayResult
            {
                Success = true,
                ReservationID = reservationID,
                AuthCode = authCode,
                Installment = installment,
                ExtraInstallment = extraInstallment,
                Message = message,
                ResponseCode = responseCode,
                OrderNumber = OrderNumber,
                CampaignUrl = !string.IsNullOrEmpty(campaignUrl) ? new Uri(campaignUrl) : null
            };
        }

        public static VerifyGatewayResult Failed(string ReservationID, string AuthCode, string CustomerCode, string errorMessage, string OrderID, string errorCode = null)
        {
            CreditCardErrorServices.Save(Convert.ToInt32(ReservationID), AuthCode, CustomerCode, errorCode + ": " + errorMessage);
            return new VerifyGatewayResult
            {
                Success = false,
                ReservationID = ReservationID,
                AuthCode = AuthCode,
                ErrorMessage = errorMessage,
                ErrorCode = errorCode,
                OrderNumber = OrderID
            };
        }
    }
}