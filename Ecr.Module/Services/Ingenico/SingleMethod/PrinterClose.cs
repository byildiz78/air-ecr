using Ecr.Module.Services.Ingenico.GmpIngenico;
using Ecr.Module.Services.Ingenico.Models;
using System;

namespace Ecr.Module.Services.Ingenico.SingleMethod
{
    public class PrinterClose
    {
        public static GmpPrintReceiptDto FiscalPrinterClose()
        {
            GmpPrintReceiptDto result = new GmpPrintReceiptDto();
            int isCloseCount = 0;
            try
            {
                again:

                result.ReturnCode = GMPSmartDLL.FP3_Close(DataStore.CurrentInterface, DataStore.ActiveTransactionHandle, Defines.TIMEOUT_DEFAULT);
                result.ReturnCodeMessage = ErrorClass.DisplayErrorCodeMessage(result.ReturnCode);
                result.ReturnStringMessage = ErrorClass.DisplayErrorMessage(result.ReturnCode);
               // LogManager.Append($"FP3_Close -> {Newtonsoft.Json.JsonConvert.SerializeObject(result)}", "BatchCommadGmpProvider -> FiscalPrinterClose()");

                if (result.ReturnCode != Defines.TRAN_RESULT_OK)
                {
                    if (isCloseCount < 3)
                    {
                        //ticket bilgisi alınamaz ise ana komutta alınan ticket bilgisine bakalım
                        //LogManager.Append($"FP3_Close -> Fiş yazarkasada kapatılamadı.{isCloseCount}.deneme tekrar close edilmeye calışılıyor..", "BatchCommadGmpProvider -> FiscalPrinterClose()");
                        isCloseCount++;
                        goto again;
                    }
                    else if (result.ReturnCode == Defines.APP_ERR_GMP3_NO_HANDLE)
                    {
                        result.ReturnCode = Defines.TRAN_RESULT_OK;
                        result.ReturnCodeMessage = ErrorClass.DisplayErrorCodeMessage(result.ReturnCode);
                        result.ReturnStringMessage = ErrorClass.DisplayErrorMessage(result.ReturnCode);
                        DataStore.ActiveTransactionHandle = 0;
                    }
                }
                else
                {
                    DataStore.ActiveTransactionHandle = 0;
                }
            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "BatchCommadGmpProvider -> FiscalPrinterClose");
            }
            return result;
        }

        public static GmpPrintReceiptDto EftPosReceiptClose()
        {
            GmpPrintReceiptDto closeResult = new GmpPrintReceiptDto();
            try
            {
                //Fiş kapatma kodu çalıştırılıyor...
                closeResult.ReturnCode = FiscalPrinterClose().ReturnCode;

                //Hata Kodu Mesajı
                closeResult.ReturnCodeMessage = ErrorClass.DisplayErrorCodeMessage(closeResult.ReturnCode);

                //Hata Kodu Kullanıcı Mesajı
                closeResult.ReturnStringMessage = ErrorClass.DisplayErrorMessage(closeResult.ReturnCode);

                //LogManager.Append($"ReceiptEftPosClose-> {closeResult.ReturnCode} - {closeResult.ReturnCodeMessage}");
            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "PrintReceiptGmpProvider.ReceiptEftPosClose");
                closeResult.ReturnCodeMessage = $"ReceiptEftPosClose ->{ex.Message}";
            }
            return closeResult;
        }

        public static GmpPrintReceiptDto EftPosReceiptFreeCloseAll()
        {
            GmpPrintReceiptDto closeResult = new GmpPrintReceiptDto();
            try
            {

                //Fiş kapatma kodu çalıştırılıyor...
                closeResult.ReturnCode = FiscalPrinterFreeCloseAll();

                if (closeResult.ReturnCode != Defines.TRAN_RESULT_OK)
                {
                   // LogManager.Append($"EftPosReceiptFreeCloseAll-> Fiş yazarkasada kapatılamadı. tekrar close edilmeye calışılıyor..");
                }

                //Hata Kodu Mesajı
                closeResult.ReturnCodeMessage = ErrorClass.DisplayErrorCodeMessage(closeResult.ReturnCode);

                //Hata Kodu Kullanıcı Mesajı
                closeResult.ReturnStringMessage = ErrorClass.DisplayErrorMessage(closeResult.ReturnCode);

              //  LogManager.Append($"EftPosReceiptFreeCloseAll -> {closeResult.ReturnCode} - {closeResult.ReturnCodeMessage}");
            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, $"EftPosReceiptFreeCloseAll");
                closeResult.ReturnCodeMessage = $"EftPosReceiptFreeCloseAll ->" + ex.Message;
            }
            return closeResult;
        }

        public static uint FiscalPrinterFreeCloseAll()
        {
            uint returnCode = 0;
            try
            {
                //LogManager.Append("FiscalPrinterFreeCloseAll metodu başlatıldı...");

                returnCode = GMPSmartDLL.FP3_PrintTotalsAndPayments(DataStore.CurrentInterface, DataStore.ActiveTransactionHandle, Defines.TIMEOUT_DEFAULT);
               // LogManager.Append("FP3_PrintTotalsAndPayments : " + ErrorClass.DisplayErrorCodeMessage(returnCode));

                if (returnCode == Defines.APP_ERR_GMP3_INVALID_HANDLE)
                {
                    GetTicket.FiscalPrinterGetTicket();
                }

                returnCode = GMPSmartDLL.FP3_PrintBeforeMF(DataStore.CurrentInterface, DataStore.ActiveTransactionHandle, Defines.TIMEOUT_DEFAULT);
               // LogManager.Append("FP3_PrintBeforeMF : " + ErrorClass.DisplayErrorCodeMessage(returnCode));

                if (returnCode == Defines.APP_ERR_GMP3_INVALID_HANDLE)
                {
                    GetTicket.FiscalPrinterGetTicket();
                }

                returnCode = GMPSmartDLL.FP3_PrintMF(DataStore.CurrentInterface, DataStore.ActiveTransactionHandle, Defines.TIMEOUT_DEFAULT);
              //  LogManager.Append("FP3_PrintMF : " + ErrorClass.DisplayErrorCodeMessage(returnCode));

                if (returnCode == Defines.APP_ERR_GMP3_INVALID_HANDLE)
                {
                    GetTicket.FiscalPrinterGetTicket();
                }

                returnCode = GMPSmartDLL.FP3_Close(DataStore.CurrentInterface, DataStore.ActiveTransactionHandle, Defines.TIMEOUT_DEFAULT);
                if (returnCode != Defines.TRAN_RESULT_OK)
                {
                    //LogManager.Append("FP3_Close : " + ErrorClass.DisplayErrorCodeMessage(returnCode));
                }

              //  LogManager.Append("FiscalPrinterCloseAll metodu sonlandı...");
            }
            catch (Exception ex)
            {
               // LogManager.Append("FiscalPrinterCloseAll : " + ex.Message);
            }

            return returnCode;
        }
    }
}
