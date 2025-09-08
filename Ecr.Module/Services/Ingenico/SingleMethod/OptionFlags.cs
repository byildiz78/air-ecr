using Ecr.Module.Services.Ingenico.GmpIngenico;
using Ecr.Module.Services.Ingenico.Models;
using System;

namespace Ecr.Module.Services.Ingenico.SingleMethod
{
    public class OptionFlags
    {
        public static GmpPrintReceiptDto FiscalPrinterOptionFlags(bool isTransactionItems = false)
        {
            GmpPrintReceiptDto result = new GmpPrintReceiptDto();
            try
            {
                ulong activeFlags = 0;
                if (isTransactionItems)
                {
                    result.ReturnCode = GMPSmartDLL.FP3_OptionFlags(DataStore.CurrentInterface, DataStore.ActiveTransactionHandle, ref activeFlags, Defines.GMP3_OPTION_NO_RECEIPT_LIMIT_CONTROL_FOR_ITEMS | Defines.GMP3_OPTION_ECHO_PAYMENT_DETAILS | Defines.GMP3_OPTION_ECHO_ITEM_DETAILS, 0, Defines.TIMEOUT_DEFAULT);
                }
                else
                {
                    result.ReturnCode = GMPSmartDLL.FP3_OptionFlags(DataStore.CurrentInterface, DataStore.ActiveTransactionHandle, ref activeFlags, Defines.GMP3_OPTION_NO_RECEIPT_LIMIT_CONTROL_FOR_ITEMS | Defines.GMP3_OPTION_ECHO_PAYMENT_DETAILS, 0, Defines.TIMEOUT_DEFAULT);
                }
                result.ReturnCodeMessage = ErrorClass.DisplayErrorCodeMessage(result.ReturnCode);
                result.ReturnStringMessage = ErrorClass.DisplayErrorMessage(result.ReturnCode);
               // LogManager.Append($"FP3_OptionFlags -> {Newtonsoft.Json.JsonConvert.SerializeObject(result)}", "BatchCommadGmpProvider -> FiscalPrinterOptionFlags()");
            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "BatchCommadGmpProvider -> FiscalPrinterOptionFlags()");
            }
            return result;
        }
    }
}
