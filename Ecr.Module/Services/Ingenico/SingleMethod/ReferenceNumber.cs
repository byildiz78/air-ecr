using Ecr.Module.Services.Ingenico.GmpIngenico;
using Ecr.Module.Services.Ingenico.Models;
using System;

namespace Ecr.Module.Services.Ingenico.SingleMethod
{
    public class ReferenceNumber
    {
        public static GmpPrintReceiptDto EftPosReferenceNumber(FiscalOrder order, bool tryAgain = false , GmpPrintReceiptDto printResult = null)
        {
            GmpPrintReceiptDto receiptDto = new GmpPrintReceiptDto();
            if (printResult != null)
            {
                receiptDto = printResult;
            }

            ushort FNo = 0;
            ushort ZNo = 0;
            ushort EkuNo = 0;

            try
            {
                if (tryAgain)
                {
                    receiptDto.ReturnCode = GMPSmartDLL.FP3_GetCurrentFiscalCounters(DataStore.CurrentInterface, ref ZNo, ref FNo, ref EkuNo);
                }
                else
                {
                    receiptDto.ReturnCode = GMPSmartDLL.FP3_GetCurrentFiscalCounters(DataStore.CurrentInterface, ref ZNo, ref FNo, ref EkuNo);
                    FNo--;
                }
                receiptDto.ReturnCodeMessage = ErrorClass.DisplayErrorCodeMessage(receiptDto.ReturnCode);
                receiptDto.ReturnStringMessage = ErrorClass.DisplayErrorMessage(receiptDto.ReturnCode);
                receiptDto.GmpCommandName = "GMPSmartDLL.FP3_GetCurrentFiscalCounters";
                receiptDto.FiscalGmpUniqueKey = $"{order.OrderDateTime.Value:ddMMyyyyHHmmss}_{order.OrderID.Value}_{DataStore.gmpResult.GmpInfo.Versions.EcrSerialNumber}_{EkuNo}_{ZNo}_{FNo}_{order.AmountDue.Value.ToString().Replace(",", "").Replace(".", "")}_{order.fiscalLines.Count}";
                receiptDto.FiscalUniqueKey = $"{order.OrderDateTime.Value:ddMMyyyyHHmmss}00{order.OrderID.Value}{FNo.ToString().PadLeft(4, '0')}";
            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "EftPosReferenceNumber");
            }
            return receiptDto;
        }

    }
}
