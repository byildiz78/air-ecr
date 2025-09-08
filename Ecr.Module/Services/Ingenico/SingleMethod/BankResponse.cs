using Ecr.Module.Services.Ingenico.GmpIngenico;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ecr.Module.Services.Ingenico.SingleMethod
{
    public class BankResponse
    {
        public static string EftPosBankErrorMessage(ST_TICKET ticket)
        {
            string stringBankMessage = "";
            try
            {
                if (ticket != null)
                {
                    if (ticket.stPayment != null)
                    {
                        List<ST_PAYMENT> isPaymentNull = ticket.stPayment.Where(w => w != null).ToList();

                        if (isPaymentNull.Any())
                        {
                            List<ST_PAYMENT> BankMessageList = ticket.stPayment.Where(w => w != null && (w.typeOfPayment == (uint)EPaymentTypes.PAYMENT_BANK_CARD || w.typeOfPayment == (uint)EPaymentTypes.PAYMENT_MOBILE || w.typeOfPayment == (uint)EPaymentTypes.PAYMENT_YEMEKCEKI) && (!string.IsNullOrEmpty(w.stBankPayment.stPaymentErrMessage.AppErrorCode) || !string.IsNullOrEmpty(w.stBankPayment.stPaymentErrMessage.ErrorCode))).ToList();

                            if (BankMessageList.Any())
                            {
                                foreach (var message in BankMessageList)
                                {
                                    if (message != null)
                                    {
                                        string authorizeCode = message.stBankPayment?.authorizeCode;

                                        stringBankMessage += Environment.NewLine +
                                            $"Banka Uygulama Hata : {message.stBankPayment.stPaymentErrMessage.AppErrorCode}-{message.stBankPayment.stPaymentErrMessage.AppErrorMsg}" +
                                            Environment.NewLine +
                                            $"Banka Hata : {message.stBankPayment.stPaymentErrMessage.ErrorCode}-{message.stBankPayment.stPaymentErrMessage.ErrorMsg}" +
                                            Environment.NewLine +
                                            (!string.IsNullOrEmpty(authorizeCode) ? $"Onay Kodu : {authorizeCode}" : "Onay Kodu  : ALINAMADI");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, $"PrintReceiptGmpProvider.BankErrorMessage");
            }

            return stringBankMessage;
        }
    }
}
