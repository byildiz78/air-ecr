using Ecr.Module.Services.Ingenico.GmpIngenico;
using Ecr.Module.Services.Ingenico.Helper;
using Ecr.Module.Services.Ingenico.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecr.Module.Services.Ingenico.SingleMethod
{
    public static class ReceiptVoid
    {
        public static GmpPrintReceiptDto EftPosVoidPrintOrder(FiscalOrder order, int CommandCount = 15)
        {
            GmpPrintReceiptDto printResult = new GmpPrintReceiptDto();
            //LogManager.Append($"EftPosVoidPrintOrder(Order order) metodu başlatıldı...OrderID : {order.OrderID}", "printFiscal -> EftPosVoidPrintOrder");

            try
            {
                #region STEP 1 : Referans numarası üret

                printResult = ReferenceNumber.EftPosReferenceNumber(order);

                if (printResult.ReturnCode != Defines.TRAN_RESULT_OK)
                {
                    return printResult;
                }

                //LogManager.Append($"STEP1 -> OK -> EftPosReferenceNumber -> FiscalGmpUniqueKey : {printResult.FiscalGmpUniqueKey} , FiscalUniqueKey : {printResult.FiscalUniqueKey}", "EftPosPrintOrder");

                #endregion

                DataStore.MergeUniqueID = printResult.FiscalUniqueKey;

                #region STEP 2 : Yazarkasadan yazdırılacak komutlar oluşturuluyor

                //LogManager.Append($"EftPosVoidPrintOrder.STEP2.EftPosVoidGmpBatchCommand EftPosVoidGmpBatchCommandiptal komutları oluşturulacak", "printFiscal -> EftPosVoidPrintOrder");
                List<GmpCommand> CommandList = SingleFunctions.EftPosVoidGmpBatchCommand(order);

                #endregion

                if (CommandList.Any())
                {
                    #region RUN GMP
                    //LogManager.Append($"EftPosVoidPrintOrder.RUNGMP.RunCommand iptal komutları çalıştırılacak", "printFiscal -> EftPosVoidPrintOrder");
                    List<BatchCommandResultDto> batchCommandResultList = StartCommand.RunCommand(CommandList);

                    #endregion

                    #region STEP 3 : Yazarkasa sonuçları kontrol ediliyor

                    printResult = BatchResult.BatchCommandResult(batchCommandResultList);
                    //LogManager.Append($"EftPosVoidPrintOrder.STEP3.BatchCommandResult iptal komutları çalıştırıldı.{ErrorClass.DisplayErrorCodeMessage((uint)printResult.ReturnCode)}", "printFiscal -> EftPosVoidPrintOrder");
                    #endregion

                    #region STEP 4  : Yazarkasadan fiş bilgisi alınıyor

                   // LogManager.Append($"EftPosVoidPrintOrder.STEP4.EftPosGetTicket yazarkasadan fiş bilgisi alınıyor..", "printFiscal -> EftPosVoidPrintOrder");
                    GmpPrintReceiptDto lastTicketResult = SingleFunctions.EftPosGetTicket();
                    if (lastTicketResult.ReturnCode != Defines.TRAN_RESULT_OK)
                    {
                        //LogManager.Append($"EftPosGetTicket.STEP4 metodunda hata oluştu.{ErrorClass.DisplayErrorCodeMessage((uint)lastTicketResult.ReturnCode)}", "printFiscal -> EftPosVoidPrintOrder");
                    }
                    if (lastTicketResult.TicketInfo?.FNo > 0)
                    {
                        printResult.TicketInfo = lastTicketResult.TicketInfo;
                    }

                    //Metottan dönen ticket bilgisi order,payment tablolarında kaydedilmek üzere düzenleniyor...
                    printResult.PrintReceiptInfo = CommandHelperGmpProvider.ReceiptJsonDataHeader(printResult.TicketInfo, true, order);

                    #endregion

                    #region STEP 5 : Fişi Kapat
                   // LogManager.Append($"EftPosVoidPrintOrder.STEP5.EftPosReceiptClose fiş başarılı ise kapatılacak..{ErrorClass.DisplayErrorCodeMessage((uint)lastTicketResult.ReturnCode)}", "printFiscal -> EftPosVoidPrintOrder");
                    if (printResult.ReturnCode == Defines.TRAN_RESULT_OK)
                    {
                        GmpPrintReceiptDto closeResult = PrinterClose.EftPosReceiptClose();

                        printResult.ReturnCode = closeResult.ReturnCode;
                        printResult.ReturnCodeMessage = ErrorClass.DisplayErrorCodeMessage((uint)closeResult.ReturnCode);
                        printResult.ReturnStringMessage = ErrorClass.DisplayErrorMessage(closeResult.ReturnCode);
                    }

                    //LogManager.Append($"EftPosVoidPrintOrder(Order order) metodu {(printResult.ReturnCode == Defines.TRAN_RESULT_OK ? "BAŞARILI" : "BAŞARISIZ")} oldu...ReturnCode : {printResult.ReturnCode} ,\n ReturnMessage : {printResult.ReturnCodeMessage} ,\n PrintReceiptInfo : {printResult.JsonSerialize()} ,\n ", "printFiscal -> EftPosVoidPrintOrder");

                    if (printResult.ReturnCode == Defines.TRAN_RESULT_OK)
                    {
                        DataStore.MergeUniqueID = "";
                    }

                    #endregion
                }

            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "PrintReceiptGmpProvider.EftPosVoidPrintOrder");
                printResult.ReturnCodeMessage = $"EftPosVoidPrintOrder -> {ex.Message}";
            }

            return printResult;
        }

        public static GmpPrintReceiptDto EftPosVoidPrintOrder()
        {
            GmpPrintReceiptDto printResult = new GmpPrintReceiptDto();

            try
            {
                #region STEP 1 : İptal komutu çalıştırılıyor...SINGLE MODE

                printResult = PrinterVoidAll.FiscalPrinterVoidAll();

                #endregion

                #region STEP 2 : Yazarkasadan fişin bilgisini al

                if (printResult.TicketInfo.FNo == 0)
                {
                   // LogManager.Append($"EftPosVoidPrintOrder.STEP2.EftPosGetTicket yazdırılan fişin bilgileri alınacak", "EftPosVoidPrintOrder");
                    GmpPrintReceiptDto lastTicketResult = SingleFunctions.EftPosGetTicket();
                    if (lastTicketResult.ReturnCode != Defines.TRAN_RESULT_OK)
                    {
                       // LogManager.Append($"EftPosVoidPrintOrder.STEP2.EftPosGetTicket metodunda hata oluştu.{ErrorClass.DisplayErrorCodeMessage((uint)lastTicketResult.ReturnCode)}", "EftPosVoidPrintOrder");
                    }
                    if (lastTicketResult.TicketInfo?.FNo > 0)
                    {
                        printResult.TicketInfo = lastTicketResult.TicketInfo;
                    }
                }

                //Metottan dönen ticket bilgisi order,payment tablolarında kaydedilmek üzere düzenleniyor...
                printResult.PrintReceiptInfo = CommandHelperGmpProvider.ReceiptJsonDataHeader(printResult.TicketInfo, true, null);

                #endregion

                #region STEP 3 : Fişi Kapat

                if (printResult.ReturnCode == Defines.TRAN_RESULT_OK)
                {
                    GmpPrintReceiptDto closeResult = PrinterClose.EftPosReceiptClose();

                    printResult.ReturnCode = closeResult.ReturnCode;
                    printResult.ReturnCodeMessage = ErrorClass.DisplayErrorCodeMessage((uint)closeResult.ReturnCode);
                    printResult.ReturnStringMessage = ErrorClass.DisplayErrorMessage(closeResult.ReturnCode);

                    if (printResult.ReturnCode == Defines.TRAN_RESULT_OK)
                    {
                       // new DatabaseProvider.DataEngine().OrderFiscalGmpKeyUpdate("iptal", StaticValues.OrderKey);
                    }
                }

                //LogManager.Append($"EftPosVoidPrintOrder() metodu {(printResult.ReturnCode == Defines.TRAN_RESULT_OK ? "BAŞARILI" : "BAŞARISIZ")} oldu...ReturnCode : {printResult.ReturnCode} ,\n ReturnMessage : {printResult.ReturnCodeMessage} ,\n PrintReceiptInfo : {printResult.JsonSerialize()} ,\n ");

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
