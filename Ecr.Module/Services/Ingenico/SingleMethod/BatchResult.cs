using Ecr.Module.Services.Ingenico.FiscalLogManager;
using Ecr.Module.Services.Ingenico.GmpIngenico;
using Ecr.Module.Services.Ingenico.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecr.Module.Services.Ingenico.SingleMethod
{
    public static class BatchResult
    {
        public static GmpPrintReceiptDto BatchCommandResult(List<BatchCommandResultDto> batchCommandResult, string orderkey, GmpPrintReceiptDto printres = null)
        {
            GmpPrintReceiptDto printResult = new GmpPrintReceiptDto();
            if (printres != null)
            {
                printResult = printres;
            }
            try
            {
                foreach (var mainCommandList in batchCommandResult)
                {
                    if (mainCommandList.ReturnCode != Defines.TRAN_RESULT_OK)
                    {
                        return CommandError.CommandErrorMessage((int)mainCommandList.ReturnCode, ErrorClass.DisplayErrorCodeMessage(mainCommandList.ReturnCode), ErrorClass.DisplayErrorMessage(mainCommandList.ReturnCode));
                    }

                    if (mainCommandList.ReceiptInfo.FNo > 0)
                    {
                        printResult.TicketInfo = mainCommandList.ReceiptInfo;
                    }

                    List<GmpCommand> SubCommandList = mainCommandList.GmpCommandInfo;

                    foreach (var subCommand in SubCommandList)
                    {
                        printResult.ReturnCode = (uint)subCommand.ReturnCode;
                        printResult.ReturnCodeMessage = ErrorClass.DisplayErrorCodeMessage((uint)subCommand.ReturnCode);
                        printResult.ReturnStringMessage = ErrorClass.DisplayErrorMessage((uint)subCommand.ReturnCode);

                        if (printResult.ReturnCode != Defines.TRAN_RESULT_OK)
                        {
                            printResult.GmpCommandInfo.Add(subCommand);
                            var js = Newtonsoft.Json.JsonConvert.SerializeObject(subCommand);
                            LogManagerOrder.SaveOrder(js, "", orderkey);
                            
                        }
                        else
                        {
                            printResult.GmpCommandInfo.Add(subCommand);
                            var js = Newtonsoft.Json.JsonConvert.SerializeObject(subCommand);
                            LogManagerOrder.SaveOrder(js, "", orderkey);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "PrintReceiptGmpProvider.BatchCommandResult");
            }

            return printResult;
        }

        public static GmpPrintReceiptDto ReturnBatchCommandResult(List<BatchCommandResultDto> batchCommandResult)
        {
            GmpPrintReceiptDto printResult = new GmpPrintReceiptDto();
            try
            {
                BatchCommandResultDto MainCommandError = batchCommandResult.Where(w => w.ReturnCode != Defines.TRAN_RESULT_OK).FirstOrDefault();
                if (MainCommandError != null)
                {
                    //LogManager.Append($"Ana komutta hata oluştu.EftPosReturnCashOrder.ReturnBatchCommandResult.MainCommandError");
                    printResult.TicketInfo = MainCommandError.ReceiptInfo;
                    return CommandError.CommandErrorMessage((int)MainCommandError.ReturnCode, ErrorClass.DisplayErrorCodeMessage((uint)MainCommandError.ReturnCode), ErrorClass.DisplayErrorMessage(MainCommandError.ReturnCode));
                }

                GmpCommand SubCommandError = batchCommandResult.Select(w => w.GmpCommandInfo.Where(x => x.ReturnCode != Defines.TRAN_RESULT_OK).FirstOrDefault()).FirstOrDefault();
                if (SubCommandError != null)
                {
                   // LogManager.Append($"Ana komutta hata oluştu.EftPosReturnCashOrder.ReturnBatchCommandResult.SubCommandError");
                    return CommandError.CommandErrorMessage(SubCommandError.ReturnCode, ErrorClass.DisplayErrorCodeMessage((uint)SubCommandError.ReturnCode), ErrorClass.DisplayErrorMessage((uint)SubCommandError.ReturnCode));
                }

                printResult.ReturnCode = Defines.TRAN_RESULT_OK;
                printResult.ReturnCodeMessage = "";
                printResult.ReturnStringMessage = "";
            }
            catch (Exception ex)
            {
               // LogManager.Exception(ex, "EftPosReturnCashOrder.ReturnBatchCommandResult");
            }
            return printResult;
        }

        public static GmpPrintReceiptDto BatchCommandResult(List<BatchCommandResultDto> batchCommandResult)
        {
            GmpPrintReceiptDto printResult = new GmpPrintReceiptDto();
            try
            {
                foreach (var mainCommandList in batchCommandResult)
                {
                    if (mainCommandList.ReturnCode != Defines.TRAN_RESULT_OK)
                    {
                        return CommandError.CommandErrorMessage((int)mainCommandList.ReturnCode, ErrorClass.DisplayErrorCodeMessage((uint)mainCommandList.ReturnCode), ErrorClass.DisplayErrorMessage(mainCommandList.ReturnCode));
                    }

                    if (mainCommandList.ReceiptInfo.FNo > 0)
                    {
                        printResult.TicketInfo = mainCommandList.ReceiptInfo;
                    }

                    List<GmpCommand> SubCommandList = mainCommandList.GmpCommandInfo;

                    foreach (var subCommand in SubCommandList)
                    {
                        printResult.ReturnCode = (uint)subCommand.ReturnCode;
                        printResult.ReturnCodeMessage = ErrorClass.DisplayErrorCodeMessage((uint)subCommand.ReturnCode);
                        printResult.ReturnStringMessage = ErrorClass.DisplayErrorMessage((uint)subCommand.ReturnCode);

                        if (printResult.ReturnCode != Defines.TRAN_RESULT_OK)
                        {
                            printResult.GmpCommandInfo.Add(subCommand);
                            //dataEngine.GmpCommandInsert(subCommand);
                            return printResult;
                        }
                        else
                        {
                            printResult.GmpCommandInfo.Add(subCommand);
                            //dataEngine.GmpCommandInsert(subCommand);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "PrintReceiptGmpProvider.BatchCommandResult");
            }

            return printResult;
        }



    }
}
