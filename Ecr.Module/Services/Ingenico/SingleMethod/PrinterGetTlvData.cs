using Ecr.Module.Services.Ingenico.GmpIngenico;
using Ecr.Module.Services.Ingenico.Models;
using System;

namespace Ecr.Module.Services.Ingenico.SingleMethod
{
    public static class PrinterGetTlvData
    {
        public static int FiscalPrinterGetTlvData(int tag, bool isLast = true)
        {
            GmpPrintReceiptDto result = new GmpPrintReceiptDto();
            int returnValue = 0;
            int againCount = 0;
            try
            {
                FP3_GetTlvData:

                byte[] arr = new byte[100];
                short len = 0;
                result.ReturnCode = GMPSmartDLL.FP3_GetTlvData(DataStore.gmpResult.GmpInfo.CurrentInterface, tag, arr, (short)arr.Length, ref len);
                result.ReturnCodeMessage = ErrorClass.DisplayErrorCodeMessage(result.ReturnCode);
                result.ReturnStringMessage = ErrorClass.DisplayErrorMessage(result.ReturnCode);
                //LogManager.Append($"FP3_GetTlvData -> {result.JsonSerialize()}", "BatchCommadGmpProvider -> FiscalPrinterGetTlvData()");

                string TempFNo = "0";
                for (int i = 0; i < len; i++)
                {
                    TempFNo += arr[i].ToString("X2");
                }

                returnValue = (isLast == true ? Convert.ToInt32(TempFNo) : Convert.ToInt32(TempFNo) - 1);

                if (returnValue == 0)
                {
                    if (againCount < 3)
                    {
                        againCount++;
                       // LogManager.Append($"FP3_GetTlvData -> {againCount}.deneme -> Yazarkasadan deger tekrar okunuyor...", "BatchCommadGmpProvider -> FiscalPrinterGetTlvData()");
                        goto FP3_GetTlvData;
                    }
                }

            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "BatchCommadGmpProvider -> FiscalPrinterGetTlvData()");
            }
            return returnValue;
        }
    }
}
