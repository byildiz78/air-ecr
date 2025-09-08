using Ecr.Module.Services.Ingenico.GmpIngenico;
using Ecr.Module.Services.Ingenico.Helper;
using Ecr.Module.Services.Ingenico.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ecr.Module.Services.Ingenico.SingleMethod
{
    public static class CashReturn
    {
        public static GmpPrintReceiptDto EftPosReturnCashOrder(FiscalOrder order)
        {
            ST_TICKET ticket = new ST_TICKET();
            GmpPrintReceiptDto printResult = new GmpPrintReceiptDto();

            try
            {
                #region STEP 1 : Yazarkasada yarım kalan nakit iade fişi bulunuyor mu? Eğer var ise fişi iptal et.

                GmpPrintReceiptDto lastTicketResult = SingleFunctions.EftPosGetTicket();
                if (lastTicketResult.ReturnCode == Defines.DLL_RETCODE_ACK_NOT_RECEIVED)
                {
                    return CommandError.CommandErrorMessage(-1, "EftPosReturnCashOrder.CashAfterCommand", "EFT-POS CİHAZINA ERİŞİLEMİYOR.CİHAZI YENİDEN BAŞLATINIZ");
                }
                if (lastTicketResult.TicketInfo?.KasaPaymentAmount > 0)
                {
                   // LogManager.Append($"Yazarkasada yarım kalan işlem var.Bu işlem iptal edilecek.Kasa Ödeme Tutarı :  {lastTicketResult.TicketInfo?.KasaPaymentAmount}");

                    GmpPrintReceiptDto voidResult = ReceiptVoid.EftPosVoidPrintOrder(order);
                    if (voidResult.ReturnCode != Defines.TRAN_RESULT_OK)
                    {
                        //LogManager.Append($"Yazarkasada yarım işlem iptal edilemedi.EftPosReturnCashAmount.EftPosVoidPrintOrder -> {voidResult.ReturnCode} - {ErrorClass.DisplayErrorCodeMessage((uint)voidResult.ReturnCode)}-{ErrorClass.DisplayErrorMessage(voidResult.ReturnCode)}");
                        return CommandError.CommandErrorMessage((int)voidResult.ReturnCode, ErrorClass.DisplayErrorCodeMessage((uint)voidResult.ReturnCode), ErrorClass.DisplayErrorMessage(voidResult.ReturnCode));
                    }
                }
                #endregion

                #region STEP 2 : Yazdırılacak komutlar oluşturuyor..AFTER

                //Fiş yazımı için After Batch komutları hazırlanıyor..
                List<GmpPrepareDto> commandAfterList = CAfterCommand.CashAfterCommand(order, TTicketType.TPayment);

                if (!commandAfterList.Any())
                {
                    return CommandError.CommandErrorMessage(-1, "EftPosReturnCashOrder.CashAfterCommand", "Nakit iade komutları oluşturulurken hata oluştu!");
                }

                //LogManager.Append($"EftPosReturnCashOrder.ReturnCashAfterCommand tanımlanıyor...Command Count : {commandAfterList.Count()}");

                List<GmpCommand> CommandList = commandAfterList.Select(x => x.gmpCommand).ToList();

                #endregion

                #region STEP 3 : RUN GMP

                List<BatchCommandResultDto> batchCommandResultList = StartCommand.RunCommand(CommandList);

                #endregion

                #region STEP 4 : Yazarkasa sonuçları kontrol ediliyor

                printResult = BatchResult.ReturnBatchCommandResult(batchCommandResultList);

               // LogManager.Append($"EftPosReturnCashOrder.CashAfterCommand komutları başarı ile yazdırıldı");

                #endregion

                #region STEP 5 : Kasa Ödeme kısmı

                double paymentAmount = order.paymentLines.Where(w => w.PaymentBaseTypeID == 1 && !w.IsPaid.Value).Sum(s => Math.Round(s.PaymentAmount.Value - s.PaymentOver.Value, 2));
                var cashAmount = (int)Math.Abs(CommandHelperGmpProvider.DoubleFormat(paymentAmount, 2));

                //LogManager.Append($"EftPosReturnCashOrder.Kasa Ödeme işlemi yapılıyor...Nakit İade Tutarı : {cashAmount}");

                printResult.ReturnCode = Json_GMPSmartDLL.FP3_KasaPayment(DataStore.gmpResult.GmpInfo.CurrentInterface, DataStore.ActiveTransactionHandle, cashAmount, ref ticket, Defines.TIMEOUT_DEFAULT);
                printResult.ReturnCodeMessage = ErrorClass.DisplayErrorCodeMessage(printResult.ReturnCode);
                printResult.ReturnStringMessage = ErrorClass.DisplayErrorMessage(printResult.ReturnCode);
                printResult.TicketInfo = ticket;

                if (printResult.ReturnCode != Defines.TRAN_RESULT_OK)
                {
                    //LogManager.Append($"Yazarkasada yarım işlem iptal edilemedi.EftPosReturnCashOrder.FiscalPrinter_KasaPayment -> {printResult.ReturnCode} - {printResult.ReturnCode}-{printResult.ReturnCode}");
                    return CommandError.CommandErrorMessage((int)printResult.ReturnCode, printResult.ReturnCodeMessage, printResult.ReturnStringMessage);
                }

                #endregion

                #region STEP 6 : Yazarkasa ödeme komutları oluşturuluyor..BEFORE

                //Fiş yazımı için After Batch komutları hazırlanıyor..
                List<GmpPrepareDto> commandBeforeList = CBeforeCommand.CashBeforeCommand(order, 0);

                if (!commandBeforeList.Any())
                {
                    return CommandError.CommandErrorMessage(-1, "EftPosReturnCashOrder.commandBeforeList", "Nakit iade ödeme komutları oluşturulurken hata oluştu!");
                }

                //LogManager.Append($"EftPosReturnCashOrder.commandBeforeList tanımlanıyor...Command Count : {commandBeforeList.Count()}");

                List<GmpCommand> GmpCommandBeforeList = commandBeforeList.Select(x => x.gmpCommand).ToList();

                #endregion

                #region STEP 7 : RUN GMP

                batchCommandResultList = StartCommand.RunCommand(GmpCommandBeforeList);

                #endregion

                #region STEP 8 : Yazarkasa sonuçları kontrol ediliyor

                printResult = BatchResult.ReturnBatchCommandResult(batchCommandResultList);

                //LogManager.Append($"EftPosReturnCashOrder.CashBeforeCommand.ReturnBatchCommandResult => {printResult.ReturnCode} - {printResult.ReturnCodeMessage}");

                #endregion

                #region STEP 9 : Yazarkasadan fişin bilgisini al

                GmpPrintReceiptDto TicketResult = SingleFunctions.EftPosGetTicket();
                if (lastTicketResult.ReturnCode != Defines.TRAN_RESULT_OK)
                {
                    //LogManager.Append($"EftPosGetTicket metodunda hata oluştu.{ErrorClass.DisplayErrorCodeMessage((uint)lastTicketResult.ReturnCode)}");
                }
                if (TicketResult.TicketInfo?.FNo > 0)
                {
                    printResult.TicketInfo = TicketResult.TicketInfo;
                }

                //Metottan dönen ticket bilgisi order,payment tablolarında kaydedilmek üzere düzenleniyor...
                printResult.PrintReceiptInfo = CommandHelperGmpProvider.ReceiptJsonDataHeader(printResult.TicketInfo, true, order);

                #endregion

                #region STEP 7 : Fişi Kapat

                if (printResult.ReturnCode == Defines.TRAN_RESULT_OK)
                {
                    if ((printResult.TicketInfo.TransactionFlags & (uint)ETransactionFlags.FLG_XTRANS_TICKET_FOOTER_MF_PRINTED) != 0)
                    {
                        GmpPrintReceiptDto closeResult = PrinterClose.EftPosReceiptClose();

                        printResult.ReturnCode = closeResult.ReturnCode;
                        printResult.ReturnCodeMessage = ErrorClass.DisplayErrorCodeMessage((uint)closeResult.ReturnCode);
                        printResult.ReturnStringMessage = ErrorClass.DisplayErrorMessage(closeResult.ReturnCode);
                    }
                }

               //LogManager.Append($"EftPosReturnCashOrder() metodu {(printResult.ReturnCode == Defines.TRAN_RESULT_OK ? "BAŞARILI" : "BAŞARISIZ")} oldu...ReturnCode : {printResult.ReturnCode} ,\n ReturnMessage : {printResult.ReturnCodeMessage} ,\n PrintReceiptInfo : {printResult.JsonSerialize()} ,\n ");
                #endregion

            }
            catch (Exception ex)
            {
                printResult.ReturnCode = 9999;
                printResult.ReturnStringMessage =
                    "NAKİT İADE İŞLEMİ GERÇEKLEŞTİRİLEMEDİ!";
                printResult.ReturnCodeMessage = "";
                //LogManager.Exception(ex, "PrintReceiptGmpProvider.EftPosReturnCashOrder");
            }

            return printResult;
        }
        public static GmpPrintReceiptDto EftPosReturnCashAmount(double amount)
        {
            ST_TICKET ticket = new ST_TICKET();
            GmpPrintReceiptDto printResult = new GmpPrintReceiptDto();
            //LogManager.Append($"EftPosReturnCashAmount(double amount) metodu başlatıldı...Kasa ödeme tutarı : {amount}", "PrintReceipts.EftPosReturnCashAmount");

            try
            {
                #region STEP 1 : Yazarkasada yarım kalan nakit iade fişi bulunuyor mu? Eğer var ise fişi iptal et.

                GmpPrintReceiptDto lastTicketResult = SingleFunctions.EftPosGetTicket();
                if (lastTicketResult.TicketInfo?.KasaPaymentAmount > 0)
                {
                    //LogManager.Append($"Yazarkasada yarım kalan işlem var.Bu işlem iptal edilecek.Kasa Ödeme Tutarı :  {lastTicketResult.TicketInfo?.KasaPaymentAmount}");

                    GmpPrintReceiptDto voidResult = ReceiptVoid.EftPosVoidPrintOrder();
                    if (voidResult.ReturnCode != Defines.TRAN_RESULT_OK)
                    {
                        //LogManager.Append($"Yazarkasada yarım işlem iptal edilemedi.EftPosReturnCashAmount.EftPosVoidPrintOrder -> {voidResult.ReturnCode} - {ErrorClass.DisplayErrorCodeMessage((uint)voidResult.ReturnCode)}-{ErrorClass.DisplayErrorMessage(voidResult.ReturnCode)}");
                        return CommandError.CommandErrorMessage((int)voidResult.ReturnCode, ErrorClass.DisplayErrorCodeMessage((uint)voidResult.ReturnCode), ErrorClass.DisplayErrorMessage(voidResult.ReturnCode));
                    }
                }
                #endregion

                #region STEP 2 : Yazdırılacak komutlar oluşturuyor..AFTER

                //Fiş yazımı için After Batch komutları hazırlanıyor..
                List<GmpPrepareDto> commandAfterList = CAfterCommand.CashAfterCommand(TTicketType.TPayment);

                if (!commandAfterList.Any())
                {
                    return CommandError.CommandErrorMessage(-1, "EftPosReturnCashAmount.CashAfterCommand", "Nakit iade komutları oluşturulurken hata oluştu!");
                }

               // LogManager.Append($"EftPosReturnCashAmount.ReturnCashAfterCommand tanımlanıyor...Command Count : {commandAfterList.Count()}");

                List<GmpCommand> CommandList = commandAfterList.Select(x => x.gmpCommand).ToList();

                #endregion

                #region STEP 3 : RUN GMP

                List<BatchCommandResultDto> batchCommandResultList = StartCommand.RunCommand(CommandList);

                #endregion

                #region STEP 4 : Yazarkasa sonuçları kontrol ediliyor

                printResult = BatchResult.ReturnBatchCommandResult(batchCommandResultList);

                //LogManager.Append($"EftPosReturnCashAmount.CashAfterCommand komutları başarı ile yazdırıldı");

                #endregion

                #region STEP 5 : Kasa Ödeme kısmı

                int cashAmount = (int)Math.Abs(CommandHelperGmpProvider.DoubleFormat(amount, 2));

                //LogManager.Append($"EftPosReturnCashAmount.Kasa Ödeme işlemi yapılıyor...Kasa ödeme Tutarı : {cashAmount}");

                printResult.ReturnCode = Json_GMPSmartDLL.FP3_KasaPayment(DataStore.gmpResult.GmpInfo.CurrentInterface, DataStore.ActiveTransactionHandle, cashAmount, ref ticket, Defines.TIMEOUT_DEFAULT);
                printResult.ReturnCodeMessage = ErrorClass.DisplayErrorCodeMessage((uint)printResult.ReturnCode);
                printResult.ReturnStringMessage = ErrorClass.DisplayErrorMessage(printResult.ReturnCode);
                printResult.TicketInfo = ticket;

                if (printResult.ReturnCode != Defines.TRAN_RESULT_OK)
                {
                    //LogManager.Append($"Yazarkasada yarım işlem iptal edilemedi.EftPosReturnCashAmount.FiscalPrinter_KasaPayment -> {printResult.ReturnCode} - {printResult.ReturnCode}-{printResult.ReturnCode}");
                    return CommandError.CommandErrorMessage((int)printResult.ReturnCode, printResult.ReturnCodeMessage, printResult.ReturnStringMessage);
                }

                #endregion

                #region STEP 6 : Yazarkasa ödeme komutları oluşturuluyor..BEFORE

                //Fiş yazımı için After Batch komutları hazırlanıyor..
                List<GmpPrepareDto> commandBeforeList = CBeforeCommand.CashBeforeCommand(amount, 0);

                if (!commandBeforeList.Any())
                {
                    return CommandError.CommandErrorMessage(-1, "EftPosReturnCashAmount.commandBeforeList", "Nakit iade ödeme komutları oluşturulurken hata oluştu!");
                }

                //LogManager.Append($"EftPosReturnCashAmount.commandBeforeList tanımlanıyor...Command Count : {commandBeforeList.Count()}");

                List<GmpCommand> GmpCommandBeforeList = commandBeforeList.Select(x => x.gmpCommand).ToList();

                #endregion

                #region STEP 7 : RUN GMP

                batchCommandResultList = StartCommand.RunCommand(GmpCommandBeforeList);

                #endregion

                #region STEP 8 : Yazarkasa sonuçları kontrol ediliyor

                printResult = BatchResult.ReturnBatchCommandResult(batchCommandResultList);

                //LogManager.Append($"EftPosReturnCashAmount.CashBeforeCommand komutları başarı ile yazdırıldı");

                #endregion

                #region STEP 9 : Yazarkasadan fişin bilgisini al

                GmpPrintReceiptDto TicketResult = SingleFunctions.EftPosGetTicket();
                if (lastTicketResult.ReturnCode != Defines.TRAN_RESULT_OK)
                {
                    //LogManager.Append($"EftPosReturnCashAmount.EftPosGetTicket metodunda hata oluştu.{ErrorClass.DisplayErrorCodeMessage((uint)lastTicketResult.ReturnCode)}");
                }
                if (TicketResult.TicketInfo?.FNo > 0)
                {
                    printResult.TicketInfo = TicketResult.TicketInfo;
                }

                #endregion

                #region STEP 7 : Fişi Kapat

                if (printResult.ReturnCode == Defines.TRAN_RESULT_OK)
                {
                    if ((printResult.TicketInfo.TransactionFlags & (uint)ETransactionFlags.FLG_XTRANS_TICKET_FOOTER_MF_PRINTED) != 0)
                    {
                        GmpPrintReceiptDto closeResult = PrinterClose.EftPosReceiptClose();

                        printResult.ReturnCode = closeResult.ReturnCode;
                        printResult.ReturnCodeMessage = ErrorClass.DisplayErrorCodeMessage((uint)closeResult.ReturnCode);
                        printResult.ReturnStringMessage = ErrorClass.DisplayErrorMessage(closeResult.ReturnCode);
                    }
                }

               // LogManager.Append($"EftPosReturnCashAmount() metodu {(printResult.ReturnCode == Defines.TRAN_RESULT_OK ? "BAŞARILI" : "BAŞARISIZ")} oldu...ReturnCode : {printResult.ReturnCode} ,\n ReturnMessage : {printResult.ReturnCodeMessage} ,\n PrintReceiptInfo : {printResult.JsonSerialize()} ,\n ");
                #endregion

            }
            catch (Exception ex)
            {
                printResult.ReturnCode = 9999;
                printResult.ReturnStringMessage =
                    "NAKİT İADE İŞLEMİ GERÇEKLEŞTİRİLEMEDİ!";
                printResult.ReturnCodeMessage = "";
                //LogManager.Exception(ex, "PrintReceiptGmpProvider.EftPosReturnCashOrder");
            }
            return printResult;
        }
    }
}
