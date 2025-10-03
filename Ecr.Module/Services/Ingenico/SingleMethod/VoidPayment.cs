using Ecr.Module.Services.Ingenico.GmpIngenico;
using Ecr.Module.Services.Ingenico.Models;
using System;

namespace Ecr.Module.Services.Ingenico.SingleMethod
{
    /// <summary>
    /// Void a specific payment from the current transaction
    /// IMPORTANT: Can only be used BEFORE FP3_PrintTotalsAndPayments
    /// </summary>
    public static class VoidPayment
    {
        /// <summary>
        /// Voids a specific payment by index
        /// </summary>
        /// <param name="paymentIndex">Index of the payment to void (starts from 0)</param>
        /// <returns>Result of void operation with ticket info</returns>
        public static GmpPrintReceiptDto VoidPaymentByIndex(ushort paymentIndex)
        {
            GmpPrintReceiptDto result = new GmpPrintReceiptDto();
            ST_TICKET ticket = new ST_TICKET();

            try
            {
                result.ReturnCode = Json_GMPSmartDLL.FP3_VoidPayment(
                    DataStore.CurrentInterface,
                    DataStore.ActiveTransactionHandle,
                    paymentIndex,
                    ref ticket,
                    Defines.TIMEOUT_CARD_TRANSACTIONS
                );

                result.TicketInfo = ticket;
                result.ReturnCodeMessage = ErrorClass.DisplayErrorCodeMessage(result.ReturnCode);
                result.ReturnStringMessage = ErrorClass.DisplayErrorMessage(result.ReturnCode);

                if (result.ReturnCode == Defines.TRAN_RESULT_OK)
                {
                    // Payment successfully voided
                }
            }
            catch (Exception ex)
            {
                result.ReturnCode = 9999;
                result.ReturnCodeMessage = $"VoidPayment Exception: {ex.Message}";
                result.ReturnStringMessage = ex.Message;
            }

            return result;
        }
    }
}
