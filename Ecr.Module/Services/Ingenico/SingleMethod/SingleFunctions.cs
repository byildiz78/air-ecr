using Ecr.Module.Services.Ingenico.GmpIngenico;
using Ecr.Module.Services.Ingenico.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ecr.Module.Services.Ingenico.SingleMethod
{
    public class SingleFunctions
    {
        public static GmpPrintReceiptDto EftPosGetTicket(bool isTransactionItems = false)
        {
            GmpPrintReceiptDto LastTicket = new GmpPrintReceiptDto();

            try
            {
                LastTicket = GetTicket.FiscalPrinterGetTicket(isTransactionItems);
                //LogManager.Append($"FiscalPrinterGetTicket-> Fiş son bilgisi okundu {Newtonsoft.Json.JsonConvert.SerializeObject(LastTicket.TicketInfo)}");
            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, $"EftPosGetTicket");
                LastTicket.ReturnCodeMessage = $"PrintReceiptGmpProvider.EftPosGetTicket ->{ex.Message} ";
            }

            return LastTicket;
        }
        public static GmpPrintReceiptDto EftPosTicket(bool isTransactionItems = false)
        {
            GmpPrintReceiptDto LastTicket = new GmpPrintReceiptDto();

            try
            {
                LastTicket = GetTicket.FiscalGetTicket(isTransactionItems);
            }
            catch (Exception ex)
            {
                LastTicket.ReturnCodeMessage = $"PrintReceiptGmpProvider.EftPosGetTicket ->{ex.Message} ";
            }

            return LastTicket;
        }
        public static List<GmpCommand> EftPosVoidGmpBatchCommand(FiscalOrder order)
        {
            List<GmpCommand> gmpCommandList = new List<GmpCommand>();
            List<GmpPrepareDto> _gmpPrepareDtoList = new List<GmpPrepareDto>();
            try
            {
                //STEP 5 
                GmpPrintReceiptDto lastTicketResult = EftPosGetTicket();

                if ((lastTicketResult.TicketInfo.TransactionFlags & (uint)ETransactionFlags.FLG_XTRANS_GMP3) != 0)
                {
                    //Eğer yazarkasada yarım işlem var ise sadece iptal komutu oluşturuluyor...
                    _gmpPrepareDtoList = ReceiptReturnCommand.VoidAllBatchCommand(order, true);
                }
                else
                {
                    //Eğer fiş yazılmamış ise ürünler yazdırılırıp,fiş iptal komutu oluşturuluyor...
                    _gmpPrepareDtoList = ReceiptReturnCommand.VoidAllBatchCommand(order, false);
                }

                if (_gmpPrepareDtoList.Any())
                {
                    //LogManager.Append("EftPosVoidGmpBatchCommand tanımlanıyor...Command Count : " + _gmpPrepareDtoList.Count());

                    gmpCommandList = _gmpPrepareDtoList.Select(x => x.gmpCommand).ToList();
                }
            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, $"EftPosVoidGmpBatchCommand");
            }

            return gmpCommandList;
        }

        
    }
}
