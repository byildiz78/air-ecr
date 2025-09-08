using Ecr.Module.Services.Ingenico.GmpIngenico;
using Ecr.Module.Services.Ingenico.Helper;
using Ecr.Module.Services.Ingenico.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ecr.Module.Services.Ingenico.SingleMethod
{
    public static class  CAfterCommand
    {
        public static List<GmpPrepareDto> CashAfterCommand(FiscalOrder order, TTicketType ticketType)
        {
            GmpPrepareDto _gmpPrepareDto = new GmpPrepareDto();
            List<GmpPrepareDto> GmpPrepareDtoList = new List<GmpPrepareDto>();
            try
            {
                //LogManager.Append("Fiş bilgileri kontrol ediliyor...");
                #region uniq id

                //LogManager.Append("Yazarkasadan uniq id alınıyor...");


                byte[] uniqueId = new byte[24];

                _gmpPrepareDto = PrinterStart.PrepareStart(uniqueId);
                if (_gmpPrepareDto.bufferLen == 0)
                {
                    _gmpPrepareDto.prepareCommand = TPrepareCommand.prepare_Start;
                    GmpPrepareDtoList.Add(_gmpPrepareDto);
                    return GmpPrepareDtoList;
                }
                else
                {
                    _gmpPrepareDto.prepareCommand = TPrepareCommand.FiscalPrinter_ok;
                    _gmpPrepareDto.gmpCommand = new GmpCommand();
                    _gmpPrepareDto.gmpCommand.OrderID = (int)order.OrderID.Value;
                    _gmpPrepareDto.gmpCommand.OrderKey = order.OrderKey.Value;
                    _gmpPrepareDto.gmpCommand.TransactionKey = Guid.Empty;
                    _gmpPrepareDto.gmpCommand.PaymentKey = Guid.Empty;
                    _gmpPrepareDto.gmpCommand.Command = "prepare_Start";
                    _gmpPrepareDto.gmpCommand.BufferData = CommandHelperGmpProvider.BufferConvertData(_gmpPrepareDto.buffer,
                        _gmpPrepareDto.bufferLen);
                    _gmpPrepareDto.gmpCommand.ReturnCode = 0;
                    _gmpPrepareDto.gmpCommand.ReturnValue = "";
                    GmpPrepareDtoList.Add(_gmpPrepareDto);
                }


                #endregion

                #region optionsflags
                _gmpPrepareDto = new GmpPrepareDto();
                _gmpPrepareDto = PrinterStart.PrepareOptionFlags();
                if (_gmpPrepareDto.bufferLen == 0)
                {
                    _gmpPrepareDto.prepareCommand = TPrepareCommand.prepare_OptionFlags;
                    GmpPrepareDtoList.Add(_gmpPrepareDto);
                    return GmpPrepareDtoList;
                }
                else
                {
                    _gmpPrepareDto.prepareCommand = TPrepareCommand.FiscalPrinter_ok;
                    _gmpPrepareDto.gmpCommand = new GmpCommand();
                    _gmpPrepareDto.gmpCommand.OrderID = (int)order.OrderID.Value;
                    _gmpPrepareDto.gmpCommand.OrderKey = order.OrderKey.Value;
                    _gmpPrepareDto.gmpCommand.TransactionKey = Guid.Empty;
                    _gmpPrepareDto.gmpCommand.PaymentKey = Guid.Empty;
                    _gmpPrepareDto.gmpCommand.Command = "prepare_OptionFlags";
                    _gmpPrepareDto.gmpCommand.BufferData = CommandHelperGmpProvider.BufferConvertData(_gmpPrepareDto.buffer, _gmpPrepareDto.bufferLen);
                    _gmpPrepareDto.gmpCommand.ReturnCode = 0;
                    _gmpPrepareDto.gmpCommand.ReturnValue = "";
                    GmpPrepareDtoList.Add(_gmpPrepareDto);
                }
                #endregion

                #region receipt title

                //LogManager.Append("Yazarkasadan fiş başlığı tanımlanıyor..." + ticketType.ToString());

                _gmpPrepareDto = new GmpPrepareDto();
                _gmpPrepareDto = PrinterStart.PrepareTicketHeader(ticketType);
                if (_gmpPrepareDto.bufferLen == 0)
                {
                    _gmpPrepareDto.prepareCommand = TPrepareCommand.prepare_TicketHeader;
                    GmpPrepareDtoList.Add(_gmpPrepareDto);
                    return GmpPrepareDtoList;
                }
                else
                {
                    _gmpPrepareDto.prepareCommand = TPrepareCommand.FiscalPrinter_ok;
                    _gmpPrepareDto.gmpCommand = new GmpCommand();
                    _gmpPrepareDto.gmpCommand.OrderID = (int)order.OrderID.Value;
                    _gmpPrepareDto.gmpCommand.OrderKey = order.OrderKey.Value;
                    _gmpPrepareDto.gmpCommand.TransactionKey = Guid.Empty;
                    _gmpPrepareDto.gmpCommand.PaymentKey = Guid.Empty;
                    _gmpPrepareDto.gmpCommand.Command = "prepare_TicketHeader";
                    _gmpPrepareDto.gmpCommand.BufferData = CommandHelperGmpProvider.BufferConvertData(_gmpPrepareDto.buffer, _gmpPrepareDto.bufferLen);
                    _gmpPrepareDto.gmpCommand.ReturnCode = 0;
                    _gmpPrepareDto.gmpCommand.ReturnValue = "";
                    GmpPrepareDtoList.Add(_gmpPrepareDto);
                }
                #endregion

                #region products 

                if (order.fiscalLines.Any())
                {
                    //LogManager.Append("Ürünler tanımlanıyor...4");

                    foreach (var productline in order.fiscalLines)
                    {
                        string product = "";
                        string productQuantity = "";

                        double Quantity = Math.Abs(productline.SaleQuantity.Value);
                        Quantity = (productline.UnitName == "AD" ? Math.Round(Quantity) : Quantity);
                        if (Quantity > 1 || productline.UnitName != "AD")
                        {
                            productQuantity = string.Format("{0} {1} X {2}", Quantity, productline.UnitName, productline.UnitPrice);
                        }

                        product = (productline.ProductName.Length >= 15 ? productline.ProductName.Substring(0, 15).PadRight(15, ' ') : productline.ProductName.PadRight(15, ' ')) + string.Format("%{0}", Math.Round(productline.TaxPercent.Value)).PadLeft(5, ' ') + string.Format("*{0:N2}", productline.SaleQuantity * productline.UnitPrice).PadLeft(15, ' ');

                        if (!string.IsNullOrEmpty(productQuantity))
                        {
                            _gmpPrepareDto = new GmpPrepareDto();
                            _gmpPrepareDto = PrinterStart.PreparePrintUserMessage(productQuantity, 8192);
                           // LogManager.Append(product);
                            if (_gmpPrepareDto.bufferLen == 0)
                            {
                                _gmpPrepareDto.prepareCommand = TPrepareCommand.prepare_PrintUserMessage;
                                GmpPrepareDtoList.Add(_gmpPrepareDto);
                                return GmpPrepareDtoList;
                            }
                            else
                            {
                                _gmpPrepareDto.prepareCommand = TPrepareCommand.FiscalPrinter_ok;
                                _gmpPrepareDto.gmpCommand = new GmpCommand();
                                _gmpPrepareDto.gmpCommand.OrderID = (int)order.OrderID.Value;
                                _gmpPrepareDto.gmpCommand.OrderKey = order.OrderKey.Value;
                                _gmpPrepareDto.gmpCommand.TransactionKey = productline.OrderLineKey;
                                _gmpPrepareDto.gmpCommand.PaymentKey = Guid.Empty;
                                _gmpPrepareDto.gmpCommand.Command = "prepare_PrintUserMessage";
                                _gmpPrepareDto.gmpCommand.BufferData = CommandHelperGmpProvider.BufferConvertData(_gmpPrepareDto.buffer, _gmpPrepareDto.bufferLen);
                                _gmpPrepareDto.gmpCommand.ReturnCode = 0;
                                _gmpPrepareDto.gmpCommand.ReturnValue = "";
                                GmpPrepareDtoList.Add(_gmpPrepareDto);
                            }
                        }
                        if (!string.IsNullOrEmpty(product))
                        {
                            _gmpPrepareDto = new GmpPrepareDto();
                            _gmpPrepareDto = PrinterStart.PreparePrintUserMessage(product, 8192);
                            //LogManager.Append(product);
                            if (_gmpPrepareDto.bufferLen == 0)
                            {
                                _gmpPrepareDto.prepareCommand = TPrepareCommand.prepare_PrintUserMessage;
                                GmpPrepareDtoList.Add(_gmpPrepareDto);
                                return GmpPrepareDtoList;
                            }
                            else
                            {
                                _gmpPrepareDto.prepareCommand = TPrepareCommand.FiscalPrinter_ok;
                                _gmpPrepareDto.gmpCommand = new GmpCommand();
                                _gmpPrepareDto.gmpCommand.OrderID = (int)order.OrderID.Value;
                                _gmpPrepareDto.gmpCommand.OrderKey = order.OrderKey.Value;
                                _gmpPrepareDto.gmpCommand.TransactionKey = productline.OrderLineKey;
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

                #endregion

            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "FiscalModule.Ingenico.GmpProvider.PrintReceipts.CashAfterCommand");
            }
            return GmpPrepareDtoList;
        }

        public static List<GmpPrepareDto> CashAfterCommand(TTicketType _ticketType)
        {
            GmpPrepareDto _gmpPrepareDto = new GmpPrepareDto();
            List<GmpPrepareDto> GmpPrepareDtoList = new List<GmpPrepareDto>();
            try
            {
                #region uniq id

                byte[] uniqueId = new byte[24];

                _gmpPrepareDto = PrinterStart.PrepareStart(uniqueId);
                if (_gmpPrepareDto.bufferLen == 0)
                {
                    _gmpPrepareDto.prepareCommand = TPrepareCommand.prepare_Start;
                    GmpPrepareDtoList.Add(_gmpPrepareDto);
                    return GmpPrepareDtoList;
                }
                else
                {
                    _gmpPrepareDto.prepareCommand = TPrepareCommand.FiscalPrinter_ok;
                    _gmpPrepareDto.gmpCommand = new GmpCommand();
                    _gmpPrepareDto.gmpCommand.OrderID = 0;
                    _gmpPrepareDto.gmpCommand.TransactionKey = null;
                    _gmpPrepareDto.gmpCommand.PaymentKey = null;
                    _gmpPrepareDto.gmpCommand.Command = "prepare_Start";
                    _gmpPrepareDto.gmpCommand.BufferData = CommandHelperGmpProvider.BufferConvertData(_gmpPrepareDto.buffer, _gmpPrepareDto.bufferLen);
                    _gmpPrepareDto.gmpCommand.ReturnCode = 0;
                    _gmpPrepareDto.gmpCommand.ReturnValue = "";
                    GmpPrepareDtoList.Add(_gmpPrepareDto);
                }

                #endregion

                #region optionsflags
                _gmpPrepareDto = new GmpPrepareDto();
                _gmpPrepareDto = PrinterStart.PrepareOptionFlags();
                if (_gmpPrepareDto.bufferLen == 0)
                {
                    _gmpPrepareDto.prepareCommand = TPrepareCommand.prepare_OptionFlags;
                    GmpPrepareDtoList.Add(_gmpPrepareDto);
                    return GmpPrepareDtoList;
                }
                else
                {
                    _gmpPrepareDto.prepareCommand = TPrepareCommand.FiscalPrinter_ok;
                    _gmpPrepareDto.gmpCommand = new GmpCommand();
                    _gmpPrepareDto.gmpCommand.OrderID = 0;
                    _gmpPrepareDto.gmpCommand.TransactionKey = null;
                    _gmpPrepareDto.gmpCommand.PaymentKey = null;
                    _gmpPrepareDto.gmpCommand.Command = "prepare_OptionFlags";
                    _gmpPrepareDto.gmpCommand.BufferData = CommandHelperGmpProvider.BufferConvertData(_gmpPrepareDto.buffer, _gmpPrepareDto.bufferLen);
                    _gmpPrepareDto.gmpCommand.ReturnCode = 0;
                    _gmpPrepareDto.gmpCommand.ReturnValue = "";
                    GmpPrepareDtoList.Add(_gmpPrepareDto);
                }
                #endregion

                #region receipt title

                _gmpPrepareDto = new GmpPrepareDto();
                _gmpPrepareDto = PrinterStart.PrepareTicketHeader(_ticketType);

                //UiEvents("Yazarkasadan fiş başlığı tanımlanıyor...");

                if (_gmpPrepareDto.bufferLen == 0)
                {
                    //AddGridTitle(true);
                    _gmpPrepareDto.prepareCommand = TPrepareCommand.prepare_TicketHeader;
                    GmpPrepareDtoList.Add(_gmpPrepareDto);
                    return GmpPrepareDtoList;
                }
                else
                {
                    _gmpPrepareDto.prepareCommand = TPrepareCommand.FiscalPrinter_ok;
                    _gmpPrepareDto.gmpCommand = new GmpCommand();
                    _gmpPrepareDto.gmpCommand.OrderID = 0;
                    _gmpPrepareDto.gmpCommand.TransactionKey = null;
                    _gmpPrepareDto.gmpCommand.PaymentKey = null;
                    _gmpPrepareDto.gmpCommand.Command = "prepare_TicketHeader";
                    _gmpPrepareDto.gmpCommand.BufferData = CommandHelperGmpProvider.BufferConvertData(_gmpPrepareDto.buffer, _gmpPrepareDto.bufferLen);
                    _gmpPrepareDto.gmpCommand.ReturnCode = 0;
                    _gmpPrepareDto.gmpCommand.ReturnValue = "";
                    GmpPrepareDtoList.Add(_gmpPrepareDto);
                }
                #endregion

            }
            catch (Exception ex)
            {
               // LogManager.Exception(ex, "PrintReceiptGmpProvider.ReturnCashAfterCommand");
            }
            return GmpPrepareDtoList;
        }
    }
}
