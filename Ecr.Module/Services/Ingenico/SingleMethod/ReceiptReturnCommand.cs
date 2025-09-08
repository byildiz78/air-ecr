using Ecr.Module.Services.Ingenico.GmpIngenico;
using Ecr.Module.Services.Ingenico.Helper;
using Ecr.Module.Services.Ingenico.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ecr.Module.Services.Ingenico.SingleMethod
{
    public static class ReceiptReturnCommand
    {
        public static List<GmpPrepareDto> VoidAllBatchCommand(FiscalOrder order, bool IsVoidAll)
        {
            GmpPrepareDto _gmpPrepareDto = new GmpPrepareDto();
            List<GmpPrepareDto> GmpPrepareDtoList = new List<GmpPrepareDto>();
            TTicketType _ticketType = TTicketType.TProcessSale;
            OrderType PrintType = OrderType.Normal;
            try
            {
               // LogManager.Append("Fiş iptal ediliyor...");
                if (IsVoidAll)
                {
                    //var ticket = _batchCommand.FiscalPrinterGetTicket();
                    //if (ticket?.TicketInfo?.TotalReceiptPayment > 0)
                    //{
                    //    #region VoidAllPayment

                    //    if (order.paymentLines.Any())
                    //    {
                    //        int i = 0;
                    //        foreach (var voidpayment in order.paymentLines)
                    //        {
                    //            if (voidpayment.IsPaid == false)
                    //            {
                    //                _gmpPrepareDto = new GmpPrepareDto();
                    //                _gmpPrepareDto = _batchCommand.PrepareVoidPayment(i);
                    //                if (_gmpPrepareDto.bufferLen == 0)
                    //                {
                    //                    _gmpPrepareDto.prepareCommand = TPrepareCommand.prepare_VoidPayment;
                    //                    GmpPrepareDtoList.Add(_gmpPrepareDto);
                    //                    return GmpPrepareDtoList;
                    //                }
                    //                else
                    //                {
                    //                    _gmpPrepareDto.prepareCommand = TPrepareCommand.FiscalPrinter_ok;
                    //                    _gmpPrepareDto.gmpCommand = new GmpCommand();
                    //                    _gmpPrepareDto.gmpCommand.OrderID = (int)order.OrderID.Value;
                    //                    _gmpPrepareDto.gmpCommand.OrderKey = order.OrderKey.Value;
                    //                    _gmpPrepareDto.gmpCommand.TransactionKey = Guid.Empty;
                    //                    _gmpPrepareDto.gmpCommand.PaymentKey = Guid.Empty;
                    //                    _gmpPrepareDto.gmpCommand.Command = "prepare_VoidPayment";
                    //                    _gmpPrepareDto.gmpCommand.BufferData =
                    //                        CommandHelperGmpProvider.BufferConvertData(_gmpPrepareDto.buffer, _gmpPrepareDto.bufferLen);
                    //                    _gmpPrepareDto.gmpCommand.ReturnCode = 0;
                    //                    _gmpPrepareDto.gmpCommand.ReturnValue = "";
                    //                    GmpPrepareDtoList.Add(_gmpPrepareDto);
                    //                }
                    //                i++;
                    //            }
                    //        }

                    //    }

                    //    #endregion
                    //}

                    #region VoidAll
                    _gmpPrepareDto = new GmpPrepareDto();
                    _gmpPrepareDto = PrepareVoidAll();
                    if (_gmpPrepareDto.bufferLen == 0)
                    {
                        _gmpPrepareDto.prepareCommand = TPrepareCommand.prepare_VoidAll;
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
                        _gmpPrepareDto.gmpCommand.Command = "prepare_VoidAll";
                        _gmpPrepareDto.gmpCommand.BufferData = CommandHelperGmpProvider.BufferConvertData(_gmpPrepareDto.buffer, _gmpPrepareDto.bufferLen);
                        _gmpPrepareDto.gmpCommand.ReturnCode = 0;
                        _gmpPrepareDto.gmpCommand.ReturnValue = "";
                        GmpPrepareDtoList.Add(_gmpPrepareDto);
                    }
                    #endregion
                }
                else
                {
                    #region uniq id

                   // LogManager.Append("Yazarkasadan uniq id alınıyor...");

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

                    #region kağıt fatura / e-fatura / e-arşiv

                    if (order.IsFiscalOrder == false && order.PrintInvoice == true)
                    {
                        PrintType = OrderType.Invoice;
                    }

                    if (PrintType == OrderType.Invoice)
                    {
                        //LogManager.Append("Yazarkasadan fatura tanımlanıyor...");
                        _ticketType = TTicketType.TInvoice;
                        ST_INVIOCE_INFO stInvoiceInfo = new ST_INVIOCE_INFO();
                        int source = 0;
                        switch (order.InvoiceTypeID)
                        {
                            case 0:
                                {
                                    //Kağıt Fatura
                                    source = 0;
                                    break;
                                }

                            case 1:
                                {
                                    // "E-FATURA";
                                    source = 1;
                                    stInvoiceInfo.flag |= (uint)EInvoiceFlags.INVOICE_FLAG_IRSALIYE;
                                    break;
                                }
                            case 2:
                                {
                                    // "E-ARŞİV";
                                    source = 2;
                                    stInvoiceInfo.flag |= (uint)EInvoiceFlags.INVOICE_FLAG_IRSALIYE;
                                    break;
                                }
                        }

                        if (order.InvoiceNo.Length > 10)
                        {
                            order.InvoiceNo = "0";
                        }

                        stInvoiceInfo.source = (byte)source;
                        stInvoiceInfo.amount = 0;
                        stInvoiceInfo.currency = 949; //TL

                        stInvoiceInfo.no = new byte[25];
                        CommandHelperGmpProvider.ConvertAscToBcdArray((!string.IsNullOrEmpty(order.InvoiceNo) ? order.InvoiceNo : "0"), ref stInvoiceInfo.no, stInvoiceInfo.no.Length);
                        stInvoiceInfo.tck_no = new byte[12];
                        CommandHelperGmpProvider.ConvertAscToBcdArray("", ref stInvoiceInfo.tck_no, stInvoiceInfo.tck_no.Length);
                        stInvoiceInfo.vk_no = new byte[12];
                        CommandHelperGmpProvider.ConvertAscToBcdArray((!string.IsNullOrEmpty(order.TaxNumber) ? order.TaxNumber : "0"), ref stInvoiceInfo.vk_no, stInvoiceInfo.vk_no.Length);

                        stInvoiceInfo.date = new byte[3];
                        string dateStr = DateTime.Now.Date.Day.ToString().PadLeft(2, '0') + DateTime.Now.Date.Month.ToString().PadLeft(2, '0') + DateTime.Now.Date.Year.ToString().Substring(2, 2).PadLeft(2, '0');

                        CommandHelperGmpProvider.ConvertStringToHexArray(dateStr, ref stInvoiceInfo.date, 3);
                        Array.Reverse(stInvoiceInfo.date);

                        _gmpPrepareDto = PrinterStart.PrepareSetInvoice(stInvoiceInfo);
                        if (_gmpPrepareDto.bufferLen == 0)
                        {
                            _gmpPrepareDto.prepareCommand = TPrepareCommand.FiscalPrinter_SetInvoice;
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
                            _gmpPrepareDto.gmpCommand.Command = "FiscalPrinter_SetInvoice";
                            _gmpPrepareDto.gmpCommand.BufferData = CommandHelperGmpProvider.BufferConvertData(_gmpPrepareDto.buffer, _gmpPrepareDto.bufferLen);
                            _gmpPrepareDto.gmpCommand.ReturnCode = 0;
                            _gmpPrepareDto.gmpCommand.ReturnValue = "";
                            GmpPrepareDtoList.Add(_gmpPrepareDto);
                        }

                    }
                    #endregion

                    #region receipt title

                    //LogManager.Append("Yazarkasadan fiş başlığı tanımlanıyor..." + _ticketType.ToString());

                    _gmpPrepareDto = new GmpPrepareDto();
                    _gmpPrepareDto = PrinterStart.PrepareTicketHeader(_ticketType);
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

                    #region products 

                    if (order.fiscalLines.Any())
                    {
                        //LogManager.Append("Ürünler tanımlanıyor...3");
                        int sortNumber = 1;
                        foreach (var productline in order.fiscalLines)
                        {
                            string product = "";
                            if (SettingsValues.receiptWeightNotDetails == "1")
                                product = sortNumber + ". " + productline.ProductName + " %" + productline.TaxPercent + "   *" + string.Format("{0:N2}", productline.SaleQuantity * productline.UnitPrice);
                            else
                                product = sortNumber + ". " + productline.SaleQuantity + " " + productline.UnitName + " X " + productline.UnitPrice + " " + productline.ProductName + " %" + productline.TaxPercent + "   *" + string.Format("{0:N2}", productline.SaleQuantity * productline.UnitPrice);
                            if (productline.SaleQuantity > 0)
                            {
                                _gmpPrepareDto = new GmpPrepareDto();
                                _gmpPrepareDto = PrinterItem.PrepareItemSale(productline);
                                //LogManager.Append(product);
                                if (_gmpPrepareDto.bufferLen == 0)
                                {
                                    _gmpPrepareDto.prepareCommand = TPrepareCommand.prepare_ItemSale;
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
                                    _gmpPrepareDto.gmpCommand.Command = "prepare_ItemSale";
                                    _gmpPrepareDto.gmpCommand.BufferData = CommandHelperGmpProvider.BufferConvertData(_gmpPrepareDto.buffer, _gmpPrepareDto.bufferLen);
                                    _gmpPrepareDto.gmpCommand.ReturnCode = 0;
                                    _gmpPrepareDto.gmpCommand.ReturnValue = "";
                                    GmpPrepareDtoList.Add(_gmpPrepareDto);
                                }
                            }
                            else
                            {
                                _gmpPrepareDto = new GmpPrepareDto();
                                _gmpPrepareDto = PrinterItem.PrepareVoidItem(productline);
                                //LogManager.Append(product);
                                if (_gmpPrepareDto.bufferLen == 0)
                                {
                                    _gmpPrepareDto.prepareCommand = TPrepareCommand.prepare_VoidItem;
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
                                    _gmpPrepareDto.gmpCommand.Command = "prepare_VoidItem";
                                    _gmpPrepareDto.gmpCommand.BufferData = CommandHelperGmpProvider.BufferConvertData(_gmpPrepareDto.buffer, _gmpPrepareDto.bufferLen);
                                    _gmpPrepareDto.gmpCommand.ReturnCode = 0;
                                    _gmpPrepareDto.gmpCommand.ReturnValue = "";
                                    GmpPrepareDtoList.Add(_gmpPrepareDto);
                                }
                            }
                            sortNumber++;
                        }
                    }

                    #endregion

                    #region total discount
                    if (order.TotalDiscount > 0)
                    {
                        _gmpPrepareDto = new GmpPrepareDto();
                        _gmpPrepareDto = PrinterItem.PrepareMinus(order.TotalDiscount.Value);
                        if (_gmpPrepareDto.bufferLen == 0)
                        {
                            //LogManager.Append("İndirim tanımlanıyor...İndirim Tutarı :" + order.TotalDiscount.Value);
                            _gmpPrepareDto.prepareCommand = TPrepareCommand.prepare_Minus;
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
                            _gmpPrepareDto.gmpCommand.Command = "prepare_Minus";
                            _gmpPrepareDto.gmpCommand.BufferData = CommandHelperGmpProvider.BufferConvertData(_gmpPrepareDto.buffer, _gmpPrepareDto.bufferLen);
                            _gmpPrepareDto.gmpCommand.ReturnCode = 0;
                            _gmpPrepareDto.gmpCommand.ReturnValue = "";
                            GmpPrepareDtoList.Add(_gmpPrepareDto);
                        }
                    }
                    #endregion

                    #region VoidAll
                    _gmpPrepareDto = PrepareVoidAll();
                    if (_gmpPrepareDto.bufferLen == 0)
                    {
                        _gmpPrepareDto.prepareCommand = TPrepareCommand.prepare_VoidAll;
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
                        _gmpPrepareDto.gmpCommand.Command = "prepare_VoidAll";
                        _gmpPrepareDto.gmpCommand.BufferData = CommandHelperGmpProvider.BufferConvertData(_gmpPrepareDto.buffer,
                            _gmpPrepareDto.bufferLen);
                        _gmpPrepareDto.gmpCommand.ReturnCode = 0;
                        _gmpPrepareDto.gmpCommand.ReturnValue = "";
                        GmpPrepareDtoList.Add(_gmpPrepareDto);
                    }

                    #endregion
                }



            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "FiscalModule.Ingenico.GmpProvider.PrintReceipts.VoidAllBatchCommand");
            }

            return GmpPrepareDtoList;
        }

        public static GmpPrepareDto PrepareVoidAll()
        {
            GmpPrepareDto _prepare = new GmpPrepareDto();
            try
            {
                _prepare.bufferLen = GMPSmartDLL.prepare_VoidAll(_prepare.buffer, _prepare.buffer.Length);
            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "BatchCommadGmpProvider.PrepareVoidAll");
            }

            return _prepare;
        }
    }
}
