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
    public class BatchCommand
    {
        public static List<GmpPrepareDto> BatchModeCommand(FiscalOrder order, List<GmpCommand> tryCommandList)
        {
            GmpPrepareDto _gmpPrepareDto = new GmpPrepareDto();
            List<GmpPrepareDto> GmpPrepareDtoList = new List<GmpPrepareDto>();
            var _ticketType = TTicketType.TProcessSale;
            var PrintType = OrderType.Normal;
            double cashGratuity = 0;

            try
            {
                //LogManager.Append("Fiş bilgileri kontrol ediliyor...");
                if (order.FiscalTypeID == 71)
                {
                    _ticketType = TTicketType.TAvans;
                    PrintType = OrderType.GiftCard;
                }
                else if (order.IsFiscalOrder == false && order.paymentLines.Any(a => a.PaymentBaseTypeID == 3))
                {
                    PrintType = OrderType.Normal;
                    if (order.paymentLines.Any(a => a.PaymentBaseTypeID != 3))
                        _ticketType = TTicketType.TProcessSale;
                    else
                        _ticketType = TTicketType.TYemekceki;
                }
                else if (order.IsFiscalOrder == true && order.paymentLines.Any(a => a.PaymentBaseTypeID == 7 || a.PaymentBaseTypeID == 8 || a.PaymentBaseTypeID == 9 || a.PaymentBaseTypeID == 10) && order.paymentLines.Any(a => a.PaymentBaseTypeID != 7 && a.PaymentBaseTypeID != 8 && a.PaymentBaseTypeID != 9 && a.PaymentBaseTypeID != 10))
                {
                    PrintType = OrderType.Normal;
                    _ticketType = TTicketType.TProcessSale;
                }
                else if (order.IsFiscalOrder == true && order.paymentLines.Any(a => a.PaymentBaseTypeID == 7 || a.PaymentBaseTypeID == 8 || a.PaymentBaseTypeID == 9 || a.PaymentBaseTypeID == 10))
                {
                    PrintType = OrderType.Normal;
                    _ticketType = TTicketType.TYemekceki;
                }
                else if (order.IsFiscalOrder == true && order.paymentLines.Any(a => a.PaymentBaseTypeID == 6) && order.paymentLines.Any(a => a.PaymentName.Contains("MOBIL")))
                {
                    //mobil ödemler için bu işlemi yaptık.Harici akbank,ykb pos var mı?
                    PrintType = OrderType.Normal;
                    _ticketType = TTicketType.TInfo;
                }
                else if (order.IsFiscalOrder == false && order.PrintInvoice == true)
                {
                    PrintType = OrderType.Invoice;
                }
                else
                {
                    _ticketType = TTicketType.TProcessSale;
                    PrintType = OrderType.Normal;
                }


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


                #region uniq id


                if (tryCommandList.Count == 0 || !tryCommandList.Where(x => x.Command == "prepare_Start" && x.ReturnValue == "TRAN_RESULT_OK [0]").Any())
                {
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
                }




                #endregion

                #region kağıt fatura / e-fatura / e-arşiv

                if (PrintType == OrderType.Invoice)
                {
                    if (tryCommandList.Count == 0 || !tryCommandList.Where(x => x.Command == "FiscalPrinter_SetInvoice" && x.ReturnValue == "TRAN_RESULT_OK [0]").Any())
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

                        stInvoiceInfo.source = (byte)source;
                        stInvoiceInfo.amount = 0;
                        stInvoiceInfo.currency = 949; //TL

                        stInvoiceInfo.no = new byte[25];
                        CommandHelperGmpProvider.ConvertAscToBcdArray(!string.IsNullOrEmpty(order.InvoiceNo) ? order.InvoiceNo : "0", ref stInvoiceInfo.no, stInvoiceInfo.no.Length);
                        stInvoiceInfo.tck_no = new byte[12];
                        CommandHelperGmpProvider.ConvertAscToBcdArray("", ref stInvoiceInfo.tck_no, stInvoiceInfo.tck_no.Length);
                        stInvoiceInfo.vk_no = new byte[12];
                        CommandHelperGmpProvider.ConvertAscToBcdArray(!string.IsNullOrEmpty(order.TaxNumber.Trim().Replace(" ", string.Empty)) ? order.TaxNumber.Trim().Replace(" ", string.Empty) : "0", ref stInvoiceInfo.vk_no, stInvoiceInfo.vk_no.Length);

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



                }

                #endregion

                #region receipt title

                if (tryCommandList.Count == 0 || !tryCommandList.Where(x => x.Command == "prepare_TicketHeader" && x.ReturnValue == "TRAN_RESULT_OK [0]").Any())
                {
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
                }

                #endregion

                #region optionsflags

                if (tryCommandList.Count == 0 || !tryCommandList.Where(x => x.Command == "prepare_OptionFlags" && x.ReturnValue == "TRAN_RESULT_OK [0]").Any())
                {

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
                }
                #endregion

                if (PrintType == OrderType.GiftCard)
                {
                    if (tryCommandList.Count == 0 || !tryCommandList.Where(x => x.Command == "prepare_KasaAvans" && x.ReturnValue == "TRAN_RESULT_OK [0]").Any())
                    {
                        #region PrepareKasaAvans

                        //LogManager.Append("GiftCard müşteri avans tanımlanıyor..");
                        _gmpPrepareDto = new GmpPrepareDto();
                        var totalAmount = order.paymentLines.Where(w => !w.IsPaid.Value).Sum(s => s.PaymentAmount - s.PaymentOver);
                        _gmpPrepareDto = PrinterStart.PrepareKasaAvans(order.customText1, order.customText2, order.customText3, totalAmount.Value);
                        if (_gmpPrepareDto.bufferLen == 0)
                        {
                            //LogManager.Append("müşteri avans");
                            _gmpPrepareDto.prepareCommand = TPrepareCommand.prepare_KasaAvans;
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
                            _gmpPrepareDto.gmpCommand.Command = "prepare_KasaAvans";
                            _gmpPrepareDto.gmpCommand.BufferData = CommandHelperGmpProvider.BufferConvertData(_gmpPrepareDto.buffer, _gmpPrepareDto.bufferLen);
                            _gmpPrepareDto.gmpCommand.ReturnCode = 0;
                            _gmpPrepareDto.gmpCommand.ReturnValue = "";
                            GmpPrepareDtoList.Add(_gmpPrepareDto);
                        }
                        #endregion
                    }


                    #region products 

                    if (order.fiscalLines.Any())
                    {
                        //LogManager.Append("Ürünler tanımlanıyor...1");

                        foreach (var productline in order.fiscalLines)
                        {

                            if (tryCommandList.Count == 0 || !tryCommandList.Where(x => x.TransactionKey == productline.OrderLineKey && x.ReturnValue == "TRAN_RESULT_OK [0]").Any())
                            {
                                string product = "";
                                string productQuantity = "";

                                double Quantity = Math.Abs(productline.SaleQuantity.Value);
                                Quantity = productline.UnitName == "AD" ? Math.Round(Quantity) : Quantity;
                                if (Quantity > 1 || productline.UnitName != "AD")
                                {
                                    productQuantity = string.Format("{0} {1} X {2}", Quantity, productline.UnitName, productline.UnitPrice);
                                }




                                product = (productline.ProductName.Length >= 15 ? productline.ProductName.Substring(0, 15).PadRight(15, ' ') : productline.ProductName.PadRight(15, ' ')) + string.Format("%{0}", Math.Round(productline.TaxPercent.Value)).PadLeft(5, ' ') + string.Format("*{0:N2}", productline.SaleQuantity * productline.UnitPrice).PadLeft(15, ' ');



                                if (!string.IsNullOrEmpty(productQuantity))
                                {
                                    _gmpPrepareDto = new GmpPrepareDto();
                                    _gmpPrepareDto = PrinterStart.PreparePrintUserMessage(productQuantity, 8192);
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
                    }

                    #endregion
                }
                else
                {
                    #region products 

                    if (order.fiscalLines.Any())
                    {
                        //LogManager.Append("Ürünler tanımlanıyor...2");
                        int sortNumber = 1;

                        foreach (var productline in order.fiscalLines.Where(x => x.PromotionText != "COMBO"))
                        {

                            if (tryCommandList.Count == 0 || !tryCommandList.Where(x => x.TransactionKey == productline.OrderLineKey && x.ReturnValue == "TRAN_RESULT_OK [0]").Any())
                            {
                                string product = "";
                                if (SettingsValues.receiptWeightNotDetails == "1")
                                    product = sortNumber + ". " + productline.ProductName + " %" + productline.TaxPercent + "   *" + string.Format("{0:N2}", productline.SaleQuantity * productline.UnitPrice);
                                else
                                    product = sortNumber + ". " + productline.SaleQuantity + " " + productline.UnitName + " X " + productline.UnitPrice + " " + productline.ProductName + " %" + productline.TaxPercent + "   *" + string.Format("{0:N2}", productline.SaleQuantity * productline.UnitPrice);

                                if (productline.SaleQuantity > 0)
                                {
                                    if (productline.SaleQuantity * productline.UnitPrice > 0)
                                    {
                                        _gmpPrepareDto = new GmpPrepareDto();

                                        _gmpPrepareDto = PrinterItem.PrepareItemSale(productline);

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

                                if (!string.IsNullOrEmpty(productline.PromotionText))
                                {
                                    string promotionText = productline.PromotionText.Replace('[', '(').Replace(']', ')');

                                    if (!string.IsNullOrEmpty(promotionText))
                                    {
                                       // LogManager.Append("Açıklamalar yazdırılıyor...");
                                        promotionText = promotionText.Replace('|', '\n');
                                        string[] ArrayMessage = promotionText.Split('\n');
                                        string str = "";
                                        foreach (var item in ArrayMessage)
                                        {
                                            str = item.Replace("\r", "");
                                            _gmpPrepareDto = PrinterStart.PreparePrintUserMessage(str, 8192);
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

                                if (!string.IsNullOrEmpty(productline.CustomField1))
                                {
                                    string customField1 = productline.CustomField1.Replace('[', '(').Replace(']', ')');

                                    if (!string.IsNullOrEmpty(customField1))
                                    {
                                       // LogManager.Append("Açıklamalar yazdırılıyor...");
                                        customField1 = customField1.Replace('|', '\n');
                                        string[] ArrayMessage = customField1.Split('\n');
                                        string str = "";
                                        foreach (var item in ArrayMessage)
                                        {
                                            str = item.Replace("\r", "");
                                            _gmpPrepareDto = PrinterStart.PreparePrintUserMessage(str, 8192);
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



                                sortNumber++;
                            }

                        }

                        #region product discount

                        sortNumber = 1;
                        foreach (var productline in order.fiscalLines)
                        {
                            if (tryCommandList.Count == 0 || !tryCommandList.Where(x => x.Command == "prepare_Minus" && x.TransactionKey == Guid.Empty && x.ReturnValue == "TRAN_RESULT_OK [0]").Any())
                            {
                                string product = "";
                                if (SettingsValues.receiptWeightNotDetails == "1")
                                    product = sortNumber + ". " + productline.ProductName + " %" + productline.TaxPercent + "   *" + string.Format("{0:N2}", productline.SaleQuantity * productline.UnitPrice);
                                else
                                    product = sortNumber + ". " + productline.SaleQuantity + " " + productline.UnitName + " X " + productline.UnitPrice + " " + productline.ProductName + " %" + productline.TaxPercent + "   *" + string.Format("{0:N2}", productline.SaleQuantity * productline.UnitPrice);

                                //LogManager.Append(product);
                                if (productline.SaleQuantity > 0)
                                {
                                    //ürün indirimi ---indirim gönderirken promotionCode alanı boş gönderilmeli
                                    if (productline.PromotionAmount > 0)
                                    {
                                        _gmpPrepareDto = new GmpPrepareDto();
                                        _gmpPrepareDto = PrinterItem.PrepareMinus(productline.PromotionAmount.Value, (int)productline.LineOrderID - 1);
                                        if (_gmpPrepareDto.bufferLen == 0)
                                        {
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
                                            _gmpPrepareDto.gmpCommand.TransactionKey = productline.OrderLineKey;
                                            _gmpPrepareDto.gmpCommand.PaymentKey = Guid.Empty;
                                            _gmpPrepareDto.gmpCommand.Command = "prepare_Minus";
                                            _gmpPrepareDto.gmpCommand.BufferData = CommandHelperGmpProvider.BufferConvertData(_gmpPrepareDto.buffer, _gmpPrepareDto.bufferLen);
                                            _gmpPrepareDto.gmpCommand.ReturnCode = 0;
                                            _gmpPrepareDto.gmpCommand.ReturnValue = "";
                                            GmpPrepareDtoList.Add(_gmpPrepareDto);
                                        }
                                    }
                                }

                                sortNumber++;
                            }


                        }
                        #endregion

                    }

                    #endregion
                }

                if (cashGratuity > 0)
                {

                    if (tryCommandList.Count == 0 || !tryCommandList.Where(x => x.Command == "prepare_ItemSale" && x.ReturnValue == "TRAN_RESULT_OK [0]" && x.TransactionKey == Guid.Empty).Any())
                    {
                        //var taxRate = new DataEngine().GetTaxGroupID(20);

                        _gmpPrepareDto = new GmpPrepareDto();
                        _gmpPrepareDto = PrinterItem.PrepareItemSale(new FiscalOrderLine
                        {
                            ProductName = "Garsoniye Bedeli",
                            TaxGroupID = 3,//(taxRate == null ? 3 : taxRate.TaxGroupID),
                            UnitPrice = cashGratuity,
                            SaleQuantity = 1,
                            UnitName = "ADET",
                            Barcode = ""
                        });
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
                            _gmpPrepareDto.gmpCommand.TransactionKey = Guid.Empty;
                            _gmpPrepareDto.gmpCommand.PaymentKey = Guid.Empty;
                            _gmpPrepareDto.gmpCommand.Command = "prepare_ItemSale";
                            _gmpPrepareDto.gmpCommand.BufferData = CommandHelperGmpProvider.BufferConvertData(_gmpPrepareDto.buffer, _gmpPrepareDto.bufferLen);
                            _gmpPrepareDto.gmpCommand.ReturnCode = 0;
                            _gmpPrepareDto.gmpCommand.ReturnValue = "";
                            GmpPrepareDtoList.Add(_gmpPrepareDto);
                        }
                    }


                }

                #region total discount
                if (order.TotalDiscount > 0)
                {
                    if (tryCommandList.Count == 0 || !tryCommandList.Where(x => x.Command == "prepare_Minus" && x.ReturnValue == "TRAN_RESULT_OK [0]").Any())
                    {
                        _gmpPrepareDto = new GmpPrepareDto();
                        _gmpPrepareDto = PrinterItem.PrepareMinus(order.TotalDiscount.Value);
                        if (_gmpPrepareDto.bufferLen == 0)
                        {
                           // LogManager.Append("İndirim tanımlanıyor...İndirim Tutarı :" + order.TotalDiscount.Value);
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

                }
                #endregion
            }
            catch (Exception ex)
            {
               // LogManager.Exception(ex, "FiscalModule.Ingenico.GmpProvider.PrintReceipts.BatchModeCommand");
            }
            return GmpPrepareDtoList;
        }
    }
}
