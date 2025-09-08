using Ecr.Module.Services.Ingenico.GmpIngenico;
using Ecr.Module.Services.Ingenico.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecr.Module.Services.Ingenico.SingleMethod
{
    public class GetTicket
    {
        public static GmpPrintReceiptDto FiscalPrinterGetTicket(bool isTransactionItems = false)
        {
            GmpPrintReceiptDto returnTicket = new GmpPrintReceiptDto();
            ST_TICKET ticket = new ST_TICKET();
            ulong activeFlags = 3;
            int isTicketCount = 0;
            try
            {
                //LogManager.Append($"FiscalPrinterGetTicket -> metodu başladı..", "BatchCommadGmpProvider -> FiscalPrinterGetTicket()");

                byte[] m_uniqueId = new byte[24] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

                GmpPrintReceiptDto handleResult = PrinterStart.FiscalPrinterStart(m_uniqueId);

                //LogManager.Append($"FiscalPrinter_Start-> {handleResult.ReturnCode} - {ErrorClass.DisplayErrorCodeMessage(handleResult.ReturnCode)}");

                if (handleResult.ReturnCode == Defines.APP_ERR_ALREADY_DONE)
                {

                    GmpPrintReceiptDto optionResult = OptionFlags.FiscalPrinterOptionFlags(isTransactionItems);

                    //LogManager.Append($"FP3_OptionFlags -> {Newtonsoft.Json.JsonConvert.SerializeObject(optionResult)}", "BatchCommadGmpProvider -> FiscalPrinterGetTicket()");

                    again:

                    returnTicket.ReturnCode = Json_GMPSmartDLL.FP3_GetTicket(DataStore.CurrentInterface, DataStore.ActiveTransactionHandle, ref ticket, Defines.TIMEOUT_DEFAULT);

                    returnTicket.ReturnCodeMessage = ErrorClass.DisplayErrorCodeMessage(returnTicket.ReturnCode);
                    returnTicket.ReturnStringMessage = ErrorClass.DisplayErrorMessage(returnTicket.ReturnCode);

                    //Ticket
                    returnTicket.TicketInfo = ticket;

                    //LogManager.Append($"FP3_GetTicket -> {Newtonsoft.Json.JsonConvert.SerializeObject(returnTicket)}", "CommadGmpProvider -> FiscalPrinterGetTicket()");

                    if (returnTicket.ReturnCode != Defines.TRAN_RESULT_OK)
                    {
                        if (isTicketCount < 3)
                        {
                            //LogManager.Append($" FP3_GetTicket -> Fiş son bilgisi okunamadı.{isTicketCount}.deneme tekrar ticket bilgileri alınmaya calışılıyor..");
                            isTicketCount++;
                            goto again;
                        }
                        else
                        {
                            return returnTicket;
                        }
                    }

                    if ((returnTicket.TicketInfo?.TransactionFlags & (uint)ETransactionFlags.FLG_XTRANS_GMP3) != 0 && (returnTicket.TicketInfo?.TransactionFlags & (uint)ETransactionFlags.FLG_XTRANS_TICKET_HEADER_PRINTED) == 0 && (returnTicket.TicketInfo?.TransactionFlags & (uint)ETransactionFlags.FLG_XTRANS_INVOICE_PARAMETERS_SET) == 0 && (returnTicket.TicketInfo?.TransactionFlags & (uint)ETransactionFlags.FLG_XTRANS_NONEY_COLLECTION_EXISTS) == 0)
                    {
                        uint? ReciptAmount = returnTicket.TicketInfo?.TotalReceiptAmount + returnTicket.TicketInfo?.KatkiPayiAmount + returnTicket.TicketInfo?.KasaAvansAmount;

                        if (ReciptAmount == 0)
                        {
                            //LogManager.Append("FiscalPrinterGetTicket ->FiscalPrinterClose 1 ");
                            //Fiş kapatma kodu çalıştırılıyor...
                            PrinterClose.FiscalPrinterClose();
                        }
                    }
                }
                else if (handleResult.ReturnCode == Defines.TRAN_RESULT_OK)
                {
                    GmpPrintReceiptDto closeResult = PrinterClose.FiscalPrinterClose();
                    returnTicket.ReturnCode = closeResult.ReturnCode;
                    returnTicket.ReturnCodeMessage = ErrorClass.DisplayErrorCodeMessage(returnTicket.ReturnCode);
                    returnTicket.ReturnStringMessage = ErrorClass.DisplayErrorMessage(returnTicket.ReturnCode);
                }

                //LogManager.Append($"FiscalPrinterGetTicket -> metodu bitti..", "BatchCommadGmpProvider -> FiscalPrinterGetTicket()");
            }
            catch (Exception ex)
            {
                //LogManager.Append("BatchCommadGmpProvider.FiscalPrinterGetTicket " + ex.Message);
            }
            return returnTicket;
        }

        public static GmpPrintReceiptDto FiscalGetTicket(bool isTransactionItems = false)
        {
            GmpPrintReceiptDto returnTicket = new GmpPrintReceiptDto();
            ST_TICKET ticket = new ST_TICKET();
            int isTicketCount = 0;

            again:

            returnTicket.ReturnCode = Json_GMPSmartDLL.FP3_GetTicket(DataStore.CurrentInterface, DataStore.ActiveTransactionHandle, ref ticket, Defines.TIMEOUT_DEFAULT);

            returnTicket.ReturnCodeMessage = ErrorClass.DisplayErrorCodeMessage(returnTicket.ReturnCode);
            returnTicket.ReturnStringMessage = ErrorClass.DisplayErrorMessage(returnTicket.ReturnCode);

            //Ticket
            returnTicket.TicketInfo = ticket;


            if (returnTicket.ReturnCode != Defines.TRAN_RESULT_OK)
            {
                if (isTicketCount < 3)
                {
                    isTicketCount++;
                    goto again;
                }
                else
                {
                    return returnTicket;
                }
            }
            return returnTicket;
        }
    }
}
