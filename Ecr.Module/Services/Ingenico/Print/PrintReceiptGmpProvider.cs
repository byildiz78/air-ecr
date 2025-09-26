using Ecr.Module.Services.Ingenico.FiscalLogManager;
using Ecr.Module.Services.Ingenico.GmpIngenico;
using Ecr.Module.Services.Ingenico.Helper;
using Ecr.Module.Services.Ingenico.Models;
using Ecr.Module.Services.Ingenico.SingleMethod;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ecr.Module.Services.Ingenico.Print
{
    public static class PrintReceiptGmpProvider
    {
        public static IngenicoApiResponse<GmpPrintReceiptDto> EftPosPrintOrder(FiscalOrder order, int commandCount = 15)
        {
            printRetry:

            var fiscal = Newtonsoft.Json.JsonConvert.SerializeObject(order);
            LogManagerOrder.SaveOrder(fiscal, "", order.OrderKey.Value.ToString() + "_Fiscal");

            var printResult = new IngenicoApiResponse<GmpPrintReceiptDto>();
            bool tryAgain = false;
            var tryCommandList = LogManagerOrder.GetOrderFile(order.OrderKey.Value.ToString());

            if (tryCommandList.Count != 0)
            {
                IngenicoApiResponse<GmpPrintReceiptDto> voidResult = new IngenicoApiResponse<GmpPrintReceiptDto>();
                var andTicketResult = SingleFunctions.EftPosGetTicket(true);

                if (andTicketResult.TicketInfo?.FNo > 0)
                {
                    if (andTicketResult.TicketInfo?.ticketType == (byte)TTicketType.TProcessSale && order.PrintInvoice.Value)
                    {
                        if (andTicketResult.TicketInfo?.TotalReceiptPayment == 0)
                        {
                            voidResult.Data = PrintVoid.EftPosVoidPrintOrder();
                            if (voidResult.Data.ReturnCode == Defines.TRAN_RESULT_OK)
                            {
                                tryCommandList = new List<GmpCommand>();
                            }   
                        }
                        else
                        {
                            //LogManager.Append("Fi� tipi fatura oldu�unda yazarkasadan hata al�n�r ise ve tekrar �deme se�ildi�inde normal fi� tipinin de�i�tirilmesi gerekiyor bu nedenle fi� iptal yap�l�yor..");
                            printResult.Data = CommandError.CommandErrorMessage(-1, "GmpFiscalUniqueControl", "FATURA MODUNDA FİŞ YAZDIRMAK İÇİN EFT-POS CİHAZINDAKİ İŞLEMİ İPTAL ETMELİSİNİZ");
                            return printResult;
                        }

                    }
                    if (andTicketResult.TicketInfo?.ticketType == (byte)TTicketType.TInvoice && !order.PrintInvoice.Value)
                    {
                        if (andTicketResult.TicketInfo?.TotalReceiptPayment == 0)
                        {
                            voidResult.Data = PrintVoid.EftPosVoidPrintOrder();
                            if (voidResult.Data.ReturnCode == Defines.TRAN_RESULT_OK)
                            {

                            }
                        }
                        else
                        {
                            //LogManager.Append("Fi� tipi fatura oldu�unda yazarkasadan hata al�n�r ise ve tekrar �deme se�ildi�inde normal fi� tipinin de�i�tirilmesi gerekiyor bu nedenle fi� iptal yap�l�yor2..");
                            printResult.Data = CommandError.CommandErrorMessage(-1, "GmpFiscalUniqueControl", "NORMAL MODDA FİŞ YAZDIRMAK İÇİNN EFT-POS CİHAZINDAKİ FATURALI İŞLEMİ İPTAL ETMELİSİNİZ");
                            return printResult;
                        }

                    }
                    else if (andTicketResult.TicketInfo?.ticketType != (byte)TTicketType.TProcessSale)
                    {
                        var yemekCeki = order.paymentLines.Any(a => a.PaymentBaseTypeID == 7 || a.PaymentBaseTypeID == 8 || a.PaymentBaseTypeID == 9 || a.PaymentBaseTypeID == 10);
                        var digerOdeme = order.paymentLines.Any(a => a.PaymentBaseTypeID != 7 && a.PaymentBaseTypeID != 8 && a.PaymentBaseTypeID != 9 && a.PaymentBaseTypeID != 10);

                        if (yemekCeki && digerOdeme && andTicketResult.TicketInfo?.TotalReceiptPayment > 0)
                        {
                            //LogManager.Append("Fi� tipi yemek �eki oldu�unda yazarkasadan hata al�n�r ise ve tekrar �deme se�ildi�inde par�al� �deme se�ilmi� ise fi� tipinin de�i�tirilmesi gerekiyor bu nedenle fi� iptal yap�l�yor..");
                            printResult.Data = CommandError.CommandErrorMessage(-1, "GmpFiscalUniqueControl", "YEMEK ÇEKİ İLE PARÇALI ÖDEME ALINMAK İSTENİYOR.FİŞİ TAMAMLAMAK İÇİN,EFT-POS CİHAZINDAKİ İŞLEMİ İPTAL EDİP,TEKRAR YAZDIRINIZ");
                            return printResult;
                        }
                        else if (yemekCeki && digerOdeme && andTicketResult.TicketInfo?.TotalReceiptPayment == 0)
                        {
                            voidResult.Data = PrintVoid.EftPosVoidPrintOrder();
                            if (voidResult.Data.ReturnCode != Defines.TRAN_RESULT_OK)
                            {

                                return EftPosPrintOrder(order);
                            }
                        }
                    }
                    else if (andTicketResult.TicketInfo?.totalNumberOfItems != order.fiscalLines.Count())
                    {
                        //LogManager.Append("Yazd�r�lan �r�n kalemi ile orderdaki �r�n kalemi farkl� ise");
                        var returnCancel = PrintVoid.EftPosVoidPrintOrder();
                        if (returnCancel.ReturnCode == Defines.TRAN_RESULT_OK)
                        {
                            goto printRetry;
                        }
                        printResult.Data = CommandError.CommandErrorMessage(-1, "GmpFiscalUniqueControl", "EFT-POS CİHAZINDA YAZDIRILAN ÜRÜNLER İLE ÇEKTEKİ ÜRÜNLER ARASINDA FARKLILIK VAR.FİŞİ İPTAL EDİP,TEKRAR YAZDIRINIZ");
                        return printResult;
                    }
                }
                else if (andTicketResult.ReturnCode == Defines.TRAN_RESULT_OK && (andTicketResult.TicketInfo?.TransactionFlags & (uint)ETransactionFlags.FLG_XTRANS_GMP3) == 0)
                {
                    tryCommandList = new List<GmpCommand>();
                }

            }

            if (tryCommandList.Count != 0)
            {
                if (tryCommandList[0].Command == "prepare_Start")
                {
                    tryAgain = true;
                }
                else
                {
                    GmpPrintReceiptDto ticketResult = SingleFunctions.EftPosGetTicket();
                    if (ticketResult.ReturnCode == Defines.TRAN_RESULT_OK)
                    {
                        if (ticketResult.TicketInfo?.FNo == 0)
                        {
                            tryAgain = true;
                        }
                    }
                }

                foreach (var item in tryCommandList)
                {
                    switch (item.Command)
                    {
                        case "prepare_Start":
                            tryAgain = true;
                            break;
                        default:
                            break;
                    }
                }

            }

            if (tryCommandList.Where(x => x.Command == "FP3_CloseFiscal" && x.ReturnValue == "TRAN_RESULT_OK [0]").Any())
            {
                GmpPrintReceiptDto closeResult = PrinterClose.EftPosReceiptClose();

                if (closeResult.ReturnCode == Defines.TRAN_RESULT_NOT_ALLOWED)
                {
                    closeResult = PrinterClose.EftPosReceiptFreeCloseAll();
                }
                printResult.Status = true;
                printResult.ErrorCode = "0";
                printResult.Message = "ÖDEME DAHA ÖNCE ALINMIŞ VE BAŞARILI İLE KAPANMIŞ";
                printResult.Data = new GmpPrintReceiptDto();

                var printDetails = LogManagerOrder.GetOrderFileData(order.OrderKey.Value.ToString() + "_Data");
                if (printDetails.Any())
                {
                    printResult.Data = printDetails.FirstOrDefault();
                }
                return printResult;
            }


            //var waitingList = LogManagerOrder.GetOrderFile(order.OrderKey.Value.ToString());
            //if (waitingList.Count != 0)
            //{
            //    if (waitingList.Where(x => x.Command == "prepare_Payment" && x.ReturnValue == "TRAN_RESULT_OK [0]").Any())
            //    {
            //        GmpPrintReceiptDto closeResult = PrinterClose.EftPosReceiptClose();

            //        if (closeResult.ReturnCode == Defines.TRAN_RESULT_NOT_ALLOWED)
            //        {
            //            closeResult = PrinterClose.EftPosReceiptFreeCloseAll();
            //        }
            //        printResult.Status = true;
            //        printResult.ErrorCode = "0";
            //        printResult.Message = "ÖDEME DAHA ÖNCE ALINMIŞ VE BAŞARILI İLE KAPANMIŞ";
            //        printResult.Data = new GmpPrintReceiptDto();

            //        var printDetails = LogManagerOrder.GetOrderFileData(order.OrderKey.Value.ToString() + "_Data");
            //        if (printDetails.Any())
            //        {
            //            printResult.Data = printDetails.FirstOrDefault();
            //        }
            //        return printResult;
            //    }
            //}
            //var CompletedList = LogManagerOrder.GetOrderFileComplated(order.OrderKey.Value.ToString());
            //if (CompletedList.Count != 0)
            //{
            //    if (waitingList.Where(x => x.Command == "prepare_Payment" && x.ReturnValue == "TRAN_RESULT_OK [0]").Any())
            //    {
            //        GmpPrintReceiptDto closeResult = PrinterClose.EftPosReceiptClose();

            //        if (closeResult.ReturnCode == Defines.TRAN_RESULT_NOT_ALLOWED)
            //        {
            //            closeResult = PrinterClose.EftPosReceiptFreeCloseAll();
            //        }
            //        printResult.Status = true;
            //        printResult.ErrorCode = "0";
            //        printResult.Message = "ÖDEME DAHA ÖNCE ALINMIŞ VE BAŞARILI İLE KAPANMIŞ";
            //        printResult.Data = new GmpPrintReceiptDto();

            //        var printDetails = LogManagerOrder.GetOrderFileData(order.OrderKey.Value.ToString() + "_Data");
            //        if (printDetails.Any())
            //        {
            //            printResult.Data = printDetails.FirstOrDefault();
            //        }
            //        return printResult;
            //    }
            //}


            #region STEP 1 : Referans numaras� �ret
            DataStore.CashRegisterStatus = "REFERANS NUMARASI ÜRETİLİYOR";
            printResult.Data = ReferenceNumber.EftPosReferenceNumber(order, tryAgain, printResult.Data);
            // LogManager.Append($"STEP 1 : Referans numaras� �ret : {Newtonsoft.Json.JsonConvert.SerializeObject(printResult)}", "EftPosReferenceNumber");

            if (printResult.Data.ReturnCode != Defines.TRAN_RESULT_OK)
            {
                return printResult;
            }
            //LogManager.Append($"STEP1 -> OK -> EftPosReferenceNumber -> FiscalGmpUniqueKey : {printResult.FiscalGmpUniqueKey} , FiscalUniqueKey : {printResult.FiscalUniqueKey}", "EftPosPrintOrder");

            #endregion

            #region STEP 3 : Yazarkasada yazd�r�lacak olan komutlar olu�turuluyor..

            //LogManager.Append($"STEP3 -> RunCommand yazd�r�lacak komutlar olu�turulacak", "EftPosPrintOrder");
            List<GmpPrepareDto> commandList = BatchCommand.BatchModeCommand(order, tryCommandList);
            if (!commandList.Any())
            {
                if (tryCommandList.Count == 0)
                {
                    printResult.Data = CommandError.CommandErrorMessage(-1, "STEP3 -> BatchModeCommand", "FiŞ komutları oluşturulamadı!", printResult.Data);
                    return printResult;
                }

            }

            List<GmpCommand> gmpCommandList = commandList.Select(x => x.gmpCommand).ToList();
            if (!gmpCommandList.Any())
            {
                if (tryCommandList.Count == 0)
                {
                    printResult.Data = CommandError.CommandErrorMessage(-1, "STEP3 -> BatchModeCommand.gmpCommand", "Fiş gmp komutları oluşturulamadı!", printResult.Data);
                    return printResult;
                }
            }

            #endregion
            DataStore.CashRegisterStatus = "ÜRÜNLER YAZDIRILIYOR";
            #region STEP 4 : Olu�turulan komutlar� �al��t�r..
            List<BatchCommandResultDto> batchCommandResult = new List<BatchCommandResultDto>();
            if (gmpCommandList.Any() && gmpCommandList.Count > 0)
            {
                foreach (var command in gmpCommandList)
                {

                    var js1 = Newtonsoft.Json.JsonConvert.SerializeObject(command);
                    LogManagerOrder.SaveOrder(js1, "", order.OrderKey.ToString());
                }

                batchCommandResult = StartCommand.RunCommand(gmpCommandList, commandCount);
                if (!batchCommandResult.Any())
                {
                    printResult.Data = CommandError.CommandErrorMessage(-1, "STEP4 -> RunCommand.batchCommandResult", "Fiş gmp komutları çalıştırılamadı!", printResult.Data);
                    return printResult;
                }
            }


            #endregion

            #region STEP 5 : �al��t�lan komutlar�n sonu�lar�n� kontrol et

            if (batchCommandResult.Any() && batchCommandResult.Count > 0)
            {
                //LogManager.Append($"STEP5 -> BatchCommandResult -> {Newtonsoft.Json.JsonConvert.SerializeObject(batchCommandResult)}", "EftPosPrintOrder");

                printResult.Data = BatchResult.BatchCommandResult(batchCommandResult, order.OrderKey.Value.ToString(), printResult.Data);

                //LogManager.Append($"STEP5 -> BatchCommandResult yaz�m sonu�lar� kontrol edildi. {printResult.ReturnCode}-{ErrorClass.DisplayErrorCodeMessage(printResult.ReturnCode)}", "EftPosPrintOrder");

                if (printResult.Data.ReturnCode != Defines.TRAN_RESULT_OK)
                {
                    return printResult;
                }
            }

            #endregion

            #region Single Mode ��lemler
            DataStore.CashRegisterStatus = "ÖDEME BEKLENİYOR";
            printResult = SinglePayment.singleModePayment(order, tryCommandList, printResult);
            if (printResult.Data.ReturnCode != Defines.TRAN_RESULT_OK)
            {
                return printResult;
            }
            DataStore.CashRegisterStatus = "ARA TOPLAM YAPILIYOR";
            printResult = SinglePayment.totalsAndPayment(order, tryCommandList, printResult);
            if (printResult.Data.ReturnCode != Defines.TRAN_RESULT_OK)
            {
                return printResult;
            }
            DataStore.CashRegisterStatus = "EKÜ İŞLEMLERİ YAPILIYOR";
            printResult = MF.PrintBeforePayment(tryCommandList, order, printResult);
            if (printResult.Data.ReturnCode != Defines.TRAN_RESULT_OK)
            {
                return printResult;
            }
            DataStore.CashRegisterStatus = "FİŞ ALTI MESAJLAR YAZDIRILIYOR";
            printResult = PrintUserMessage.PrintMessage(order, tryCommandList, printResult, commandCount);
            if (printResult.Data.ReturnCode != Defines.TRAN_RESULT_OK)
            {
                return printResult;
            }
            printResult = MF.PrintMF(tryCommandList, order, printResult);
            if (printResult.Data.ReturnCode != Defines.TRAN_RESULT_OK)
            {
                return printResult;
            }
            #endregion

            #region STEP 6 : Yazarkasadan fi�in bilgisini al

            // LogManager.Append($"STEP6 -> EftPosGetTicket yazd�r�lan fi�in bilgileri al�nacak", "EftPosPrintOrder");
            DataStore.CashRegisterStatus = "FİŞ BİLGİSİ ALINIYOR";
            GmpPrintReceiptDto lastTicketResult = SingleFunctions.EftPosTicket();
            if (lastTicketResult.ReturnCode != Defines.TRAN_RESULT_OK)
            {
                //LogManager.Append($"STEP6 -> EftPosGetTicket metodunda hata olu�tu.{ErrorClass.DisplayErrorCodeMessage(lastTicketResult.ReturnCode)}", "EftPosPrintOrder");
            }
            if (lastTicketResult.TicketInfo?.FNo > 0)
            {
                printResult.Data.TicketInfo = lastTicketResult.TicketInfo;
            }

            //Metottan d�nen ticket bilgisi order,payment tablolar�nda kaydedilmek �zere d�zenleniyor...
            printResult.Data.PrintReceiptInfo = CommandHelperGmpProvider.ReceiptJsonDataHeader(printResult.Data.TicketInfo, true, order);

            //banka hata mesajlar�
            printResult.Data.ReturnStringMessage += BankResponse.EftPosBankErrorMessage(printResult.Data.TicketInfo);
            if (printResult.Data.ReturnCodeMessage == "0x0000 : TRAN_RESULT_OK [0]")
            {
                printResult.Data.GmpCommandName = "GetTicketDetail";
                var js2 = Newtonsoft.Json.JsonConvert.SerializeObject(printResult.Data);
                LogManagerOrder.SaveOrder(js2, "", order.OrderKey.Value.ToString() + "_Data");
            }
            #endregion

            #region STEP 7 : Fi�i Kapat

            DataStore.CashRegisterStatus = "FİŞ KAPATILIYOR";
            var subCommand = new GmpCommand();
            subCommand.OrderID = (int)order.OrderID.Value;
            subCommand.OrderKey = order.OrderKey.Value;
            subCommand.TransactionKey = Guid.Empty;
            subCommand.PaymentKey = Guid.Empty;
            subCommand.Command = "FP3_CloseFiscal";
            subCommand.ReturnCode = (int)0;
            subCommand.ReturnValue = "TRAN_RESULT_OK [0]";
            var js = Newtonsoft.Json.JsonConvert.SerializeObject(subCommand);
            LogManagerOrder.SaveOrder(js, "", order.OrderKey.Value.ToString());

            //LogManager.Append($"STEP7 -> �demeler ba�ar�l� ile al�nd� ise fi� kapat�lacak.Fi�in Son durumu :  {printResult.Data.ReturnCode}-{ErrorClass.DisplayErrorCodeMessage(printResult.ReturnCode)} ,  Toplam Fi� Tutar� (ECR) : {printResult.TicketInfo.TotalReceiptAmount} , Toplam Fi� �denen Tutar� (ECR) : {printResult.TicketInfo.TotalReceiptPayment}", "EftPosPrintOrder");

            if (printResult.Data.ReturnCode == Defines.TRAN_RESULT_OK)
            {
                GmpPrintReceiptDto closeResult = PrinterClose.EftPosReceiptClose();

                if (closeResult.ReturnCode == Defines.TRAN_RESULT_NOT_ALLOWED)
                {
                    closeResult = PrinterClose.EftPosReceiptFreeCloseAll();
                }
                printResult.Data.ReturnCode = closeResult.ReturnCode;
                printResult.Data.ReturnCodeMessage = ErrorClass.DisplayErrorCodeMessage(closeResult.ReturnCode);
                printResult.Data.ReturnStringMessage = ErrorClass.DisplayErrorMessage(closeResult.ReturnCode);
            }

            //LogManager.Append($"Metodu tamamland�...Fi� Yaz�m� : {(printResult.Data.ReturnCode == Defines.TRAN_RESULT_OK ? "BA�ARILI oldu.." : $"BA�ARISIZ oldu..ReturnCode : {printResult.ReturnCode} , ReturnMessage : {printResult.ReturnCodeMessage}")} printResult : {Newtonsoft.Json.JsonConvert.SerializeObject(printResult)} ", "EftPosPrintOrder");

            #endregion

            return printResult;
        }

        public static IngenicoApiResponse<GmpPrintReceiptDto> EftPrintIsCompleted(string orderKey)
        {
            var printResult = new IngenicoApiResponse<GmpPrintReceiptDto>();
            bool tryAgain = false;
            var tryCommandList = LogManagerOrder.GetOrderFile(orderKey);

            if (tryCommandList.Where(x => x.Command == "FP3_CloseFiscal" && x.ReturnValue == "TRAN_RESULT_OK [0]").Any())
            {
                printResult.Status = true;
                printResult.ErrorCode = "0";
                printResult.Message = "ÖDEME DAHA ÖNCE ALINMIŞ VE BAŞARILI İLE KAPANMIŞ";
                printResult.Data = new GmpPrintReceiptDto();

                var printDetails = LogManagerOrder.GetOrderFileData($"{orderKey}_Data");
                if (printDetails.Any())
                {
                    printResult.Data = printDetails.FirstOrDefault();
                }
                return printResult;
            }
            printResult.Status = false;
            printResult.ErrorCode = "9999";
            printResult.Message = "İŞLEM TAMAMLANMAMIŞ";
            printResult.Data = new GmpPrintReceiptDto();
            return printResult;
        }

        public static IngenicoApiResponse<FiscalOrder> GetEftPrintFiscal(string orderKey)
        {
            var printResult = new IngenicoApiResponse<FiscalOrder>();
            bool tryAgain = false;
            var tryCommandList = LogManagerOrder.GetOrderFile(orderKey);

            if (tryCommandList.Where(x => x.Command == "FP3_CloseFiscal" && x.ReturnValue == "TRAN_RESULT_OK [0]").Any())
            {
                printResult.Status = true;
               

                var printDetails = LogManagerOrder.GetOrderFileFiscal($"{orderKey}_Fiscal");
                if (printDetails != null)
                {
                    printResult.Data = printDetails;
                }
                return printResult;
            }
            printResult.Status = false;
            printResult.ErrorCode = "9999";
            printResult.Message = "İŞLEM TAMAMLANMAMIŞ";
            printResult.Data = new FiscalOrder();
            return printResult;
        }

        public static IngenicoApiResponse<FiscalOrder> GetEftFiscal(string orderKey)
        {
            var printResult = new IngenicoApiResponse<FiscalOrder>();
           
                printResult.Status = true;


                var printDetails = LogManagerOrder.GetOrderFileFiscal($"{orderKey}_Fiscal");
                if (printDetails != null)
                {
                    printResult.Data = printDetails;
                }
                return printResult;
        }



        public static IngenicoApiResponse<List<string>> EftPrintWaiting()
        {
            var printResult = new IngenicoApiResponse<List<string>>();
            var tryCommandList = LogManagerOrder.GetLogFileNames();
            if (tryCommandList.Any())
            {
                printResult.Data = tryCommandList;
                printResult.Status = true;
                printResult.ErrorCode = "0";
            }
            else
            {
                printResult.Data = new List<string>();
                printResult.Status = false;
                printResult.ErrorCode = "999";
            }
            return printResult;
        }
    }
}
