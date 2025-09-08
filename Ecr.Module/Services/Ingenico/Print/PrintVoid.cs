using Ecr.Module.Services.Ingenico.GmpIngenico;
using Ecr.Module.Services.Ingenico.Helper;
using Ecr.Module.Services.Ingenico.Models;
using Ecr.Module.Services.Ingenico.SingleMethod;
using System;

namespace Ecr.Module.Services.Ingenico.Print
{
    public static class PrintVoid
    {
        public static GmpPrintReceiptDto EftPosVoidPrintOrder()
        {
            GmpPrintReceiptDto printResult = new GmpPrintReceiptDto();

            try
            {
                #region STEP 1 : �ptal komutu �al��t�r�l�yor...SINGLE MODE

                printResult = PrinterVoidAll.FiscalPrinterVoidAll();

                #endregion

                #region STEP 2 : Yazarkasadan fi�in bilgisini al

                if (printResult.TicketInfo.FNo == 0)
                {
                    //LogManager.Append($"EftPosVoidPrintOrder.STEP2.EftPosGetTicket yazd�r�lan fi�in bilgileri al�nacak", "EftPosVoidPrintOrder");
                    GmpPrintReceiptDto lastTicketResult = SingleFunctions.EftPosGetTicket();
                    if (lastTicketResult.ReturnCode != Defines.TRAN_RESULT_OK)
                    {
                        //LogManager.Append($"EftPosVoidPrintOrder.STEP2.EftPosGetTicket metodunda hata olu�tu.{ErrorClass.DisplayErrorCodeMessage(lastTicketResult.ReturnCode)}", "EftPosVoidPrintOrder");
                    }
                    if (lastTicketResult.TicketInfo?.FNo > 0)
                    {
                        printResult.TicketInfo = lastTicketResult.TicketInfo;
                    }
                }

                //Metottan d�nen ticket bilgisi order,payment tablolar�nda kaydedilmek �zere d�zenleniyor...
                printResult.PrintReceiptInfo = CommandHelperGmpProvider.ReceiptJsonDataHeader(printResult.TicketInfo, true, null);

                #endregion

                #region STEP 3 : Fi�i Kapat

                if (printResult.ReturnCode == Defines.TRAN_RESULT_OK)
                {
                    GmpPrintReceiptDto closeResult = PrinterClose.EftPosReceiptClose();

                    printResult.ReturnCode = closeResult.ReturnCode;
                    printResult.ReturnCodeMessage = ErrorClass.DisplayErrorCodeMessage(closeResult.ReturnCode);
                    printResult.ReturnStringMessage = ErrorClass.DisplayErrorMessage(closeResult.ReturnCode);

                    if (printResult.ReturnCode == Defines.TRAN_RESULT_OK)
                    {
                        //new DatabaseProvider.DataEngine().OrderFiscalGmpKeyUpdate("iptal", StaticValues.OrderKey);
                    }
                }

                //LogManager.Append($"EftPosVoidPrintOrder() metodu {(printResult.ReturnCode == Defines.TRAN_RESULT_OK ? "BA�ARILI" : "BA�ARISIZ")} oldu...ReturnCode : {printResult.ReturnCode} ,\n ReturnMessage : {printResult.ReturnCodeMessage} ,\n PrintReceiptInfo : {Newtonsoft.Json.JsonConvert.SerializeObject(printResult)} ,\n ");

                #endregion

            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "PrintReceiptGmpProvider.VoidPrintOrder");
            }
            return printResult;
        }
    }
}
