using Ecr.Module.Services.Ingenico.GmpIngenico;
using Ecr.Module.Services.Ingenico.Helper;
using Ecr.Module.Services.Ingenico.Models;
using Ecr.Module.Services.Ingenico.SingleMethod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecr.Module.Services.Ingenico.Print
{
    public static class PrintUserMessage
    {
        public static IngenicoApiResponse<GmpPrintReceiptDto> PrintMessage(FiscalOrder order,List<GmpCommand> tryCommandList, IngenicoApiResponse<GmpPrintReceiptDto> printResult, int commandCount = 15)
        {

            List<GmpPrepareDto> GmpPrepareDtoList = new List<GmpPrepareDto>();
            var _gmpPrepareDto = new GmpPrepareDto();

            #region prepare_PrintUserMessage

            //A��klama ,mobiliva mesajlar� yazd�r�l�r
            string _messageText = "";

            if (!string.IsNullOrEmpty(order.customText2))
            {

                if (tryCommandList.Count == 0 || !tryCommandList.Where(x => x.Command == "prepare_PrintUserMessage" && x.ReturnValue == "TRAN_RESULT_OK [0]").Any())
                {
                    if (!order.customText2.Contains("Ana çek"))
                    {
                        _messageText = order.customText2.Replace('[', '(').Replace(']', ')');

                        if (!string.IsNullOrEmpty(_messageText))
                        {
                            //LogManager.Append("A��klamalar yazd�r�l�yor...");
                            _messageText = _messageText.Replace('|', '\n');
                            string[] ArrayMessage = _messageText.Split('\n');
                            string str = "";
                            foreach (var item in ArrayMessage)
                            {
                                str = item.Replace("\r", "");

                                str = string.IsNullOrEmpty(str) ? " " : str;
                                _gmpPrepareDto = PrinterStart.PreparePrintUserMessage(str, 4);
                                if (_gmpPrepareDto.bufferLen == 0)
                                {
                                    _gmpPrepareDto.prepareCommand = TPrepareCommand.prepare_PrintUserMessage;
                                    GmpPrepareDtoList.Add(_gmpPrepareDto);
                                }
                                else
                                {
                                    _gmpPrepareDto.prepareCommand = TPrepareCommand.FiscalPrinter_ok;
                                    _gmpPrepareDto.gmpCommand = new GmpCommand();
                                    _gmpPrepareDto.gmpCommand.OrderID = (int)order.OrderID.Value;
                                    _gmpPrepareDto.gmpCommand.OrderKey = order.OrderKey.Value;
                                    _gmpPrepareDto.gmpCommand.TransactionKey = Guid.Empty;
                                    _gmpPrepareDto.gmpCommand.PaymentKey = Guid.Empty;
                                    _gmpPrepareDto.gmpCommand.Command = "prepare_PrintUserMessage";
                                    _gmpPrepareDto.gmpCommand.BufferData = CommandHelperGmpProvider.BufferConvertData(_gmpPrepareDto.buffer, _gmpPrepareDto.bufferLen);
                                    _gmpPrepareDto.gmpCommand.ReturnCode = 0;
                                    _gmpPrepareDto.gmpCommand.ReturnValue = "";
                                    GmpPrepareDtoList.Add(_gmpPrepareDto);
                                }

                            }
                        }
                    }
                }
            }

            _messageText = "";
            var cashGratuity = 0.0;

            try
            {
                if (!string.IsNullOrEmpty(order.customText3))
                {
                    cashGratuity = Convert.ToDouble(order.customText3) / 100.0;
                }
            }
            catch
            {
                cashGratuity = 0;
            }
            if (!string.IsNullOrEmpty(order.customText3) && cashGratuity == 0)
            {
                _messageText = order.customText3.Replace('[', '(').Replace(']', ')');

                if (!string.IsNullOrEmpty(_messageText))
                {
                    if (tryCommandList.Count == 0 || !tryCommandList.Where(x => x.Command == "prepare_PrintUserMessage" && x.ReturnValue == "TRAN_RESULT_OK [0]").Any())
                    {
                        //LogManager.Append("A��klamalar yazd�r�l�yor...");
                        _messageText = _messageText.Replace('|', '\n');
                        string[] ArrayMessage = _messageText.Split('\n');
                        string str = "";
                        foreach (var item in ArrayMessage)
                        {
                            str = item.Replace("\r", "");

                            str = string.IsNullOrEmpty(str) ? " " : str;
                            _gmpPrepareDto = PrinterStart.PreparePrintUserMessage(str, 9);
                            if (_gmpPrepareDto.bufferLen == 0)
                            {
                                _gmpPrepareDto.prepareCommand = TPrepareCommand.prepare_PrintUserMessage;
                                GmpPrepareDtoList.Add(_gmpPrepareDto);
                            }
                            else
                            {
                                _gmpPrepareDto.prepareCommand = TPrepareCommand.FiscalPrinter_ok;
                                _gmpPrepareDto.gmpCommand = new GmpCommand();
                                _gmpPrepareDto.gmpCommand.OrderID = (int)order.OrderID.Value;
                                _gmpPrepareDto.gmpCommand.OrderKey = order.OrderKey.Value;
                                _gmpPrepareDto.gmpCommand.TransactionKey = Guid.Empty;
                                _gmpPrepareDto.gmpCommand.PaymentKey = Guid.Empty;
                                _gmpPrepareDto.gmpCommand.Command = "prepare_PrintUserMessage";
                                _gmpPrepareDto.gmpCommand.BufferData = CommandHelperGmpProvider.BufferConvertData(_gmpPrepareDto.buffer, _gmpPrepareDto.bufferLen);
                                _gmpPrepareDto.gmpCommand.ReturnCode = 0;
                                _gmpPrepareDto.gmpCommand.ReturnValue = "";
                                GmpPrepareDtoList.Add(_gmpPrepareDto);
                            }

                        }
                    }

                }
            }

            //Sat�� barkodu
            if (SettingsValues.fiscalbarcode == "1")
            {

                if (tryCommandList.Count == 0 || !tryCommandList.Where(x => x.Command == "prepare_PrintUserMessage" && x.ReturnValue == "TRAN_RESULT_OK [0]").Any())
                {


                    //LogManager.Append("Sat�� barkodu yazd�r�l�yor...");
                    //Barkod :273
                    _gmpPrepareDto = PrinterStart.PreparePrintUserMessage(order.OrderID.ToString(), 273);
                    if (_gmpPrepareDto.bufferLen == 0)
                    {
                        _gmpPrepareDto.prepareCommand = TPrepareCommand.prepare_PrintUserMessage;
                        GmpPrepareDtoList.Add(_gmpPrepareDto);
                    }
                    else
                    {
                        _gmpPrepareDto.prepareCommand = TPrepareCommand.FiscalPrinter_ok;
                        _gmpPrepareDto.gmpCommand = new GmpCommand();
                        _gmpPrepareDto.gmpCommand.OrderID = (int)order.OrderID.Value;
                        _gmpPrepareDto.gmpCommand.OrderKey = order.OrderKey.Value;
                        _gmpPrepareDto.gmpCommand.TransactionKey = Guid.Empty;
                        _gmpPrepareDto.gmpCommand.PaymentKey = Guid.Empty;
                        _gmpPrepareDto.gmpCommand.Command = "prepare_PrintUserMessage";
                        _gmpPrepareDto.gmpCommand.BufferData = CommandHelperGmpProvider.BufferConvertData(_gmpPrepareDto.buffer, _gmpPrepareDto.bufferLen);
                        _gmpPrepareDto.gmpCommand.ReturnCode = 0;
                        _gmpPrepareDto.gmpCommand.ReturnValue = "";
                        GmpPrepareDtoList.Add(_gmpPrepareDto);
                    }
                }
            }
            if (SettingsValues.fiscalreceiptno == "1")
            {
                if (tryCommandList.Count == 0 || !tryCommandList.Where(x => x.Command == "prepare_PrintUserMessage" && x.ReturnValue == "TRAN_RESULT_OK [0]").Any())
                {
                    //LogManager.Append("Sat�� fi�i yazd�r�l�yor...");
                    _gmpPrepareDto = PrinterStart.PreparePrintUserMessage("SİPARİŞ NO : " + order.fiscalReceiptNo, 9);
                    if (_gmpPrepareDto.bufferLen == 0)
                    {
                        _gmpPrepareDto.prepareCommand = TPrepareCommand.prepare_PrintUserMessage;
                        GmpPrepareDtoList.Add(_gmpPrepareDto);
                    }
                    else
                    {
                        _gmpPrepareDto.prepareCommand = TPrepareCommand.FiscalPrinter_ok;
                        _gmpPrepareDto.gmpCommand = new GmpCommand();
                        _gmpPrepareDto.gmpCommand.OrderID = (int)order.OrderID.Value;
                        _gmpPrepareDto.gmpCommand.OrderKey = order.OrderKey.Value;
                        _gmpPrepareDto.gmpCommand.TransactionKey = Guid.Empty;
                        _gmpPrepareDto.gmpCommand.PaymentKey = Guid.Empty;
                        _gmpPrepareDto.gmpCommand.Command = "prepare_PrintUserMessage";
                        _gmpPrepareDto.gmpCommand.BufferData = CommandHelperGmpProvider.BufferConvertData(_gmpPrepareDto.buffer, _gmpPrepareDto.bufferLen);
                        _gmpPrepareDto.gmpCommand.ReturnCode = 0;
                        _gmpPrepareDto.gmpCommand.ReturnValue = "";
                        GmpPrepareDtoList.Add(_gmpPrepareDto);
                    }
                }
            }
           

            #endregion

            List<GmpCommand> gmpCommandList1 = GmpPrepareDtoList.Select(x => x.gmpCommand).ToList();

            #region STEP 4 : Olu�turulan komutlar� �al��t�r..
            if (gmpCommandList1.Any())
            {
                //LogManager.Append($"STEP4 -> RunCommand komutlar �al��t�r�lacak.Commands : {Newtonsoft.Json.JsonConvert.SerializeObject(gmpCommandList1)}", "EftPosPrintOrder");

                List<BatchCommandResultDto> batchCommandResult11 = StartCommand.RunCommand(gmpCommandList1, commandCount);
                if (!batchCommandResult11.Any())
                {
                    printResult.Data = CommandError.CommandErrorMessage(-1, "STEP4 -> RunCommand.batchCommandResult", "Fiş gmp komutları çalıştırılamadı!");
                    return printResult;
                }

                #endregion

            #region STEP 5 : �al��t�lan komutlar�n sonu�lar�n� kontrol et

                //LogManager.Append($"STEP5 -> BatchCommandResult -> {Newtonsoft.Json.JsonConvert.SerializeObject(batchCommandResult11)}", "EftPosPrintOrder");

                printResult.Data = BatchResult.BatchCommandResult(batchCommandResult11, order.OrderKey.Value.ToString());

                // LogManager.Append($"STEP5 -> BatchCommandResult yaz�m sonu�lar� kontrol edildi. {printResult.ReturnCode}-{ErrorClass.DisplayErrorCodeMessage(printResult.ReturnCode)}", "EftPosPrintOrder");

                if (printResult.Data.ReturnCode != Defines.TRAN_RESULT_OK)
                {
                    return printResult;
                }
            }
            #endregion

            return printResult;
        }
    }
}
