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
    public static class PrinterStart
    {
        public static GmpPrintReceiptDto FiscalPrinterStart(byte[] UniqueId = null)
        {
            GmpPrintReceiptDto result = new GmpPrintReceiptDto();
            try
            {
                byte[] m_uniqueId = new byte[24];
                byte[] szHashExpireDate = new byte[16];

                if (UniqueId != null)
                {
                    m_uniqueId = UniqueId;
                }
                Array.Clear(m_uniqueId, 0, m_uniqueId.Length);
                ulong TransactionHandle = 0;
                byte isBackground = 0;

                result.ReturnCode = GMPSmartDLL.FP3_StartEx(DataStore.gmpResult.GmpInfo.CurrentInterface, ref TransactionHandle, isBackground, m_uniqueId, m_uniqueId.Length, null, 0, null, 0, szHashExpireDate, 10000);
                if (TransactionHandle > 0)
                {
                    DataStore.ActiveTransactionHandle = TransactionHandle;
                }

                //LogManager.Append($"FP3_Start -> ActiveTransactionHandle : {ActiveTransactionHandle} ");
                //LogManager.Append($"FP3_Start -> {Newtonsoft.Json.JsonConvert.SerializeObject(result)} -> ActiveTransactionHandle : {ActiveTransactionHandle} , TransactionHandle : {TransactionHandle} ", "BatchCommadGmpProvider -> FiscalPrinterStart()");
                result.ReturnCodeMessage = ErrorClass.DisplayErrorCodeMessage(result.ReturnCode);
                result.ReturnStringMessage = ErrorClass.DisplayErrorMessage(result.ReturnCode);
                //LogManager.Append($"FP3_Start -> {Newtonsoft.Json.JsonConvert.SerializeObject(result)}", "BatchCommadGmpProvider -> FiscalPrinterStart()");
            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "BatchCommadGmpProvider -> FiscalPrinterStart()");
            }
            return result;
        }

        public static GmpPrepareDto PrepareStart(byte[] _uniqueId)
        {
            //unique id alınıyor
            GmpPrepareDto _prepare = new GmpPrepareDto();
            try
            {
                Array.Clear(_uniqueId, 0, _uniqueId.Length);

                _prepare.bufferLen = GMPSmartDLL.prepare_Start(_prepare.buffer, _prepare.buffer.Length, _uniqueId,
                    _uniqueId.Length, null, 0, null, 0);
            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "BatchCommadGmpProvider.PrepareStart");
            }
            return _prepare;
        }

        public static GmpPrepareDto PrepareSetInvoice(ST_INVIOCE_INFO stInvoiceInfo)
        {
            GmpPrepareDto _prepare = new GmpPrepareDto();
            try
            {
                _prepare.bufferLen = Json_GMPSmartDLL.prepare_SetInvoice(_prepare.buffer, _prepare.buffer.Length, ref stInvoiceInfo);

                _prepare.InvoiceInfo = stInvoiceInfo;
            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "BatchCommadGmpProvider.PrepareSetInvoice");
            }

            return _prepare;
        }

        public static GmpPrepareDto PrepareTicketHeader(TTicketType titleType)
        {
            //fiş başlığı  
            GmpPrepareDto _prepare = new GmpPrepareDto();
            try
            {
                _prepare.bufferLen = GMPSmartDLL.prepare_TicketHeader(_prepare.buffer, _prepare.buffer.Length, titleType);
            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "BatchCommadGmpProvider.PrepareTicketHeader");
            }
            return _prepare;
        }

        public static GmpPrepareDto PrepareOptionFlags()
        {
            GmpPrepareDto _prepare = new GmpPrepareDto();
            try
            {   //Defines.GMP3_OPTION_ECHO_ITEM_DETAILS : Ürün detayları 
                _prepare.bufferLen = GMPSmartDLL.prepare_OptionFlags(_prepare.buffer, _prepare.buffer.Length, Defines.GMP3_OPTION_ECHO_PRINTER | Defines.GMP3_OPTION_ECHO_PAYMENT_DETAILS | Defines.GMP3_OPTION_NO_RECEIPT_LIMIT_CONTROL_FOR_ITEMS, 0);
            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "BatchCommadGmpProvider.PrepareOptionFlags");
            }
            return _prepare;
        }

        public static GmpPrepareDto PrepareKasaAvans(string CustomerName, string TckNo, string VkNo, double Amount)
        {
            GmpPrepareDto _prepare = new GmpPrepareDto();
            try
            {
                _prepare.bufferLen = GMPSmartDLL.prepare_KasaAvans(_prepare.buffer, _prepare.buffer.Length, (int)CommandHelperGmpProvider.DoubleFormat(Amount, 2), CustomerName, TckNo, VkNo);
            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "BatchCommadGmpProvider.PrepareKasaAvans");
            }

            return _prepare;
        }

        public static GmpPrepareDto PreparePrintUserMessage(string MessageText, uint flag)
        {
            GmpPrepareDto _prepare = new GmpPrepareDto();
            try
            {
                ST_USER_MESSAGE[] stUserMessage = new ST_USER_MESSAGE[1024];
                stUserMessage[0] = new ST_USER_MESSAGE();
                int numberOfUserMessages = 1;
                stUserMessage[0].len = (byte)MessageText.Length;
                stUserMessage[0].message = MessageText;
                stUserMessage[0].flag = flag;

                _prepare.bufferLen = Json_GMPSmartDLL.prepare_PrintUserMessage_Ex(_prepare.buffer, _prepare.buffer.Length, ref stUserMessage, (ushort)numberOfUserMessages);
            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "BatchCommadGmpProvider.PreparePrintUserMessage");
            }

            return _prepare;
        }

        public  static GmpPrepareDto PreparePrintTotalsAndPayments()
        {
            GmpPrepareDto _prepare = new GmpPrepareDto();
            try
            {
                _prepare.bufferLen = GMPSmartDLL.prepare_PrintTotalsAndPayments(_prepare.buffer, _prepare.buffer.Length);
            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "BatchCommadGmpProvider.PreparePrintTotalsAndPayments");
            }
            return _prepare;
        }



    }
}
