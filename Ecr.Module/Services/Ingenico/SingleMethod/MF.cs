using Ecr.Module.Services.Ingenico.FiscalLogManager;
using Ecr.Module.Services.Ingenico.GmpIngenico;
using Ecr.Module.Services.Ingenico.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ecr.Module.Services.Ingenico.SingleMethod
{
    public static class MF
    {
        public static GmpPrepareDto PreparePrintBeforeMF()
        {
            GmpPrepareDto _prepare = new GmpPrepareDto();
            try
            {
                _prepare.bufferLen = GMPSmartDLL.prepare_PrintBeforeMF(_prepare.buffer, _prepare.buffer.Length);
            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "BatchCommadGmpProvider.PreparePrintBeforeMF");
            }
            return _prepare;
        }
        public static GmpPrepareDto PreparePrintMF()
        {
            GmpPrepareDto _prepare = new GmpPrepareDto();
            try
            {
                _prepare.bufferLen = GMPSmartDLL.prepare_PrintMF(_prepare.buffer, _prepare.buffer.Length);
            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "BatchCommadGmpProvider.PreparePrintMF");
            }

            return _prepare;
        }

        public static IngenicoApiResponse<GmpPrintReceiptDto> PrintBeforePayment(List<GmpCommand> tryCommandList, FiscalOrder order, IngenicoApiResponse<GmpPrintReceiptDto> printResult)
        {

            #region FP3_PrintBeforeMF Ek�ye kaydet
            try
            {
                if (tryCommandList.Count == 0 || !tryCommandList.Where(x => x.Command == "FP3_PrintBeforeMF" && x.ReturnValue == "TRAN_RESULT_OK [0]").Any())
                {
                    var retcode = GMPSmartDLL.FP3_PrintBeforeMF(DataStore.CurrentInterface, DataStore.ActiveTransactionHandle, DataStore.TIMEOUT_CARD_TRANSACTIONS);
                    if (retcode != Defines.TRAN_RESULT_OK && retcode != Defines.APP_ERR_ALREADY_DONE)
                    {
                        var rep = CommandError.CommandErrorMessage((int)retcode, ErrorClass.DisplayErrorCodeMessage(retcode), ErrorClass.DisplayErrorMessage(retcode));
                        //LogManager.Append($"---------{Newtonsoft.Json.JsonConvert.SerializeObject(rep)} : error FP3_PrintBeforeMF");
                        var subCommand = new GmpCommand();
                        subCommand.OrderID = (int)order.OrderID.Value;
                        subCommand.OrderKey = order.OrderKey.Value;
                        subCommand.TransactionKey = Guid.Empty;
                        subCommand.PaymentKey = Guid.Empty;
                        subCommand.Command = "FP3_PrintBeforeMF";
                        subCommand.ReturnCode = (int)retcode;
                        subCommand.ReturnValue = rep.ReturnCodeMessage;

                        printResult.Data.GmpCommandInfo.Add(subCommand);
                        var js = Newtonsoft.Json.JsonConvert.SerializeObject(subCommand);
                        LogManagerOrder.SaveOrder(js, "", order.OrderKey.Value.ToString());
                        printResult.Data = rep;
                        return printResult;

                    }
                    else
                    {
                        var rep = CommandError.CommandErrorMessage((int)retcode, ErrorClass.DisplayErrorCodeMessage(retcode), ErrorClass.DisplayErrorMessage(retcode));
                        //LogManager.Append($"---------{Newtonsoft.Json.JsonConvert.SerializeObject(rep)} : OK FP3_PrintBeforeMF");
                        var subCommand = new GmpCommand();
                        subCommand.OrderID = (int)order.OrderID.Value;
                        subCommand.OrderKey = order.OrderKey.Value;
                        subCommand.TransactionKey = Guid.Empty;
                        subCommand.PaymentKey = Guid.Empty;
                        subCommand.Command = "FP3_PrintBeforeMF";
                        subCommand.ReturnCode = (int)retcode;
                        subCommand.ReturnValue = "TRAN_RESULT_OK [0]";

                        printResult.Data.GmpCommandInfo.Add(subCommand);
                        var js = Newtonsoft.Json.JsonConvert.SerializeObject(subCommand);
                        LogManagerOrder.SaveOrder(js, "", order.OrderKey.Value.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                //LogManager.Append($"---------{Newtonsoft.Json.JsonConvert.SerializeObject(ex)} : FP3_PrintBeforeMF");

                throw;
            }
            return printResult;

            #endregion
        }

        public static IngenicoApiResponse<GmpPrintReceiptDto> PrintMF(List<GmpCommand> tryCommandList, FiscalOrder order, IngenicoApiResponse<GmpPrintReceiptDto> printResult)
        {

            #region FP3_PrintMF Ek�ye kaydet
            try
            {
                var retcode = GMPSmartDLL.FP3_PrintMF(DataStore.CurrentInterface, DataStore.ActiveTransactionHandle, DataStore.TIMEOUT_CARD_TRANSACTIONS);
                if (retcode != Defines.TRAN_RESULT_OK && retcode != Defines.APP_ERR_ALREADY_DONE)
                {
                    var rep = CommandError.CommandErrorMessage((int)retcode, ErrorClass.DisplayErrorCodeMessage(retcode), ErrorClass.DisplayErrorMessage(retcode));
                    // LogManager.Append($"---------{Newtonsoft.Json.JsonConvert.SerializeObject(rep)} : error FP3_PrintMF");
                    var subCommand = new GmpCommand();
                    subCommand.OrderID = (int)order.OrderID.Value;
                    subCommand.OrderKey = order.OrderKey.Value;
                    subCommand.TransactionKey = Guid.Empty;
                    subCommand.PaymentKey = Guid.Empty;
                    subCommand.Command = "FP3_PrintMF";
                    subCommand.ReturnCode = (int)retcode;
                    subCommand.ReturnValue = rep.ReturnCodeMessage;
                    printResult.Data = rep;
                    printResult.Data.GmpCommandInfo.Add(subCommand);
                    var js = Newtonsoft.Json.JsonConvert.SerializeObject(subCommand);
                    LogManagerOrder.SaveOrder(js, "", order.OrderKey.Value.ToString());

                    return printResult;

                }
                else
                {
                    var rep = CommandError.CommandErrorMessage((int)retcode, ErrorClass.DisplayErrorCodeMessage(retcode), ErrorClass.DisplayErrorMessage(retcode));
                    // LogManager.Append($"---------{Newtonsoft.Json.JsonConvert.SerializeObject(rep)} : OK FP3_PrintMF");
                    var subCommand = new GmpCommand();
                    subCommand.OrderID = (int)order.OrderID.Value;
                    subCommand.OrderKey = order.OrderKey.Value;
                    subCommand.TransactionKey = Guid.Empty;
                    subCommand.PaymentKey = Guid.Empty;
                    subCommand.Command = "FP3_PrintMF";
                    subCommand.ReturnCode = (int)retcode;
                    subCommand.ReturnValue = "TRAN_RESULT_OK [0]";

                    printResult.Data.GmpCommandInfo.Add(subCommand);
                    var js = Newtonsoft.Json.JsonConvert.SerializeObject(subCommand);
                    LogManagerOrder.SaveOrder(js, "", order.OrderKey.Value.ToString());
                }
            }
            catch (Exception ex)
            {
                // LogManager.Append($"---------{Newtonsoft.Json.JsonConvert.SerializeObject(ex)} : FP3_PrintMF");

                throw;
            }

            return printResult;

            #endregion
        }
    }
}
