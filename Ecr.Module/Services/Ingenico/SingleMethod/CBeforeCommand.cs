using Ecr.Module.Services.Ingenico.Helper;
using Ecr.Module.Services.Ingenico.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecr.Module.Services.Ingenico.SingleMethod
{
    public static class CBeforeCommand
    {
        public static List<GmpPrepareDto> CashBeforeCommand(FiscalOrder order, int PaymentType)
        {
            GmpPrepareDto _gmpPrepareDto = new GmpPrepareDto();
            List<GmpPrepareDto> GmpPrepareDtoList = new List<GmpPrepareDto>();
            try
            {
                //LogManager.Append("Nakit İade Fiş bilgileri kontrol ediliyor...");

                #region payment

                if (order.paymentLines.Any())
                {
                    //payment sort number methodId
                    #region MethodType 

                    foreach (var sortpay in order.paymentLines)
                    {
                        if (sortpay.PaymentBaseTypeID == 2)
                        {
                            sortpay.MethodType = PaymentMethodType.Visa;
                        }
                        //else if (sortpay.PaymentBaseTypeID == 1 && sortpay.PaymentTypeID == 14)
                        //{
                        //    sortpay.MethodType = PaymentMethodType.Ibb;
                        //}
                        else if (sortpay.PaymentBaseTypeID == 5)
                        {
                            sortpay.MethodType = PaymentMethodType.GiftCard;
                        }
                        else if (sortpay.PaymentBaseTypeID == 3)
                        {
                            sortpay.MethodType = PaymentMethodType.YemekCeki;
                        }
                        else if (sortpay.PaymentBaseTypeID == 7)
                        {
                            sortpay.MethodType = PaymentMethodType.Multinet;
                        }
                        else if (sortpay.PaymentBaseTypeID == 8)
                        {
                            sortpay.MethodType = PaymentMethodType.Ticket;
                        }
                        else if (sortpay.PaymentBaseTypeID == 9)
                        {
                            sortpay.MethodType = PaymentMethodType.Sodexo;
                        }
                        else if (sortpay.PaymentBaseTypeID == 10)
                        {
                            sortpay.MethodType = PaymentMethodType.MetropolCard;
                        }
                        else if (sortpay.PaymentBaseTypeID == 6)
                        {
                            sortpay.MethodType = PaymentMethodType.BankaTransfer;
                        }
                        else if (sortpay.PaymentBaseTypeID == 4)
                        {
                            sortpay.MethodType = PaymentMethodType.AcikHesap;
                        }
                        else if (sortpay.PaymentBaseTypeID == 1)
                        {
                            sortpay.MethodType = PaymentMethodType.Cash;
                        }
                        else if (sortpay.PaymentBaseTypeID == 11)
                        {
                            sortpay.MethodType = PaymentMethodType.Setcard;
                        }
                        else if (sortpay.PaymentBaseTypeID == 12)
                        {
                            sortpay.MethodType = PaymentMethodType.Paye;
                        }

                    }

                    #endregion

                    var paymentList = order.paymentLines.Where(w => w.PaymentBaseTypeID == 1 && !w.IsPaid.Value).OrderBy(x => x.MethodType).ToList();
                    //LogManager.Append("Ödeme tanımlanıyor...");
                    foreach (var pay in paymentList)
                    {
                        if (pay.IsPaid == false)
                        {
                            pay.PaymentAmount = Math.Abs(Math.Round(pay.PaymentAmount.Value - pay.PaymentOver.Value, 2));
                            //LogManager.Append(pay.PaymentName + "   *" + pay.PaymentAmount.Value);
                            _gmpPrepareDto = new GmpPrepareDto();
                            if (PaymentType == 0)
                            {
                                //kasa ödeme
                                _gmpPrepareDto = GetSinglePayment.PrepareReversePayment(pay);
                            }
                            else
                            {
                                //kasa avans
                                _gmpPrepareDto = GetSinglePayment.PreparePayment(pay);
                            }
                            if (_gmpPrepareDto.bufferLen == 0)
                            {
                                _gmpPrepareDto.prepareCommand = TPrepareCommand.prepare_Payment;
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
                                _gmpPrepareDto.gmpCommand.PaymentKey = pay.PaymentKey;
                                _gmpPrepareDto.gmpCommand.paymentType = pay.MethodType;
                                _gmpPrepareDto.gmpCommand.Command = "prepare_Payment";
                                _gmpPrepareDto.gmpCommand.BufferData =
                                    CommandHelperGmpProvider.BufferConvertData(_gmpPrepareDto.buffer, _gmpPrepareDto.bufferLen);
                                _gmpPrepareDto.gmpCommand.ReturnCode = 0;
                                _gmpPrepareDto.gmpCommand.ReturnValue = "";
                                GmpPrepareDtoList.Add(_gmpPrepareDto);
                            }
                        }
                    }
                }
                #endregion

                #region prepare_PrintTotalsAndPayments
                _gmpPrepareDto = new GmpPrepareDto();
                _gmpPrepareDto = PrinterStart.PreparePrintTotalsAndPayments();
                if (_gmpPrepareDto.bufferLen == 0)
                {
                    _gmpPrepareDto.prepareCommand = TPrepareCommand.prepare_PrintTotalsAndPayments;
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
                    _gmpPrepareDto.gmpCommand.Command = "prepare_PrintTotalsAndPayments";
                    _gmpPrepareDto.gmpCommand.BufferData = CommandHelperGmpProvider.BufferConvertData(_gmpPrepareDto.buffer, _gmpPrepareDto.bufferLen);
                    _gmpPrepareDto.gmpCommand.ReturnCode = 0;
                    _gmpPrepareDto.gmpCommand.ReturnValue = "";
                    GmpPrepareDtoList.Add(_gmpPrepareDto);
                }
                #endregion

                #region prepare_PrintBeforeMF
                _gmpPrepareDto = new GmpPrepareDto();
                _gmpPrepareDto = MF.PreparePrintBeforeMF();
                if (_gmpPrepareDto.bufferLen == 0)
                {
                    _gmpPrepareDto.prepareCommand = TPrepareCommand.prepare_PrintBeforeMF;
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
                    _gmpPrepareDto.gmpCommand.Command = "prepare_PrintBeforeMF";
                    _gmpPrepareDto.gmpCommand.BufferData = CommandHelperGmpProvider.BufferConvertData(_gmpPrepareDto.buffer, _gmpPrepareDto.bufferLen);
                    _gmpPrepareDto.gmpCommand.ReturnCode = 0;
                    _gmpPrepareDto.gmpCommand.ReturnValue = "";
                    GmpPrepareDtoList.Add(_gmpPrepareDto);
                }
                #endregion

                #region prepare_PrintUserMessage
                string _messageText = "";
                //E-Arşiv ise
                if (order.InvoiceTypeID == 2)
                {
                    //_messageText = string.Format("BU BELGE İÇİN OLUŞTURULAN \n E-ARŞİV FATURASINA, \n earsiv.sokmarket.com.tr \n ADRESİ ÜZERİNDEN \n BU BELGE ÜZERİNDEKİ \n REFERANS NO VE VKN/TCKN NUMARASI \n GİRİLEREK ALIŞVERİŞ TARİHİNİ TAKİP\n EDEN GÜNDEN İTİBAREN ULAŞILABİLİR \n\n BU BİLDİRİM TTK NEZDİNDE \n TEBLİĞ HÜKMÜNDEDİR \n\n REFERANS NO \n\n {0}", StaticValues.MergeUniqueID);
                    //if (!string.IsNullOrEmpty(_messageText))
                    //{
                    //    LogManager.Append("E-Arşiv bilgi yazdırılıyor...");
                    //    _messageText = " \n \n" + _messageText.Replace('|', '\n') + " \n \n";
                    //    string[] ArrayMessage = _messageText.Split('\n');
                    //    string str = "";
                    //    foreach (var item in ArrayMessage)
                    //    {
                    //        str = item.Replace("\r", "");
                    //        _gmpPrepareDto = batchCommand.PreparePrintUserMessage(str, 8208);
                    //        if (_gmpPrepareDto.bufferLen == 0)
                    //        {
                    //            _gmpPrepareDto.prepareCommand = TPrepareCommand.prepare_PrintUserMessage;
                    //            GmpPrepareDtoList.Add(_gmpPrepareDto);
                    //            return GmpPrepareDtoList;
                    //        }
                    //        else
                    //        {
                    //            _gmpPrepareDto.prepareCommand = TPrepareCommand.FiscalPrinter_ok;
                    //            _gmpPrepareDto.gmpCommand = new GmpCommand();
                    //            _gmpPrepareDto.gmpCommand.OrderID = (int)order.OrderID.Value;
                    //            _gmpPrepareDto.gmpCommand.OrderKey = order.OrderKey.Value;
                    //            _gmpPrepareDto.gmpCommand.TransactionKey = Guid.Empty;
                    //            _gmpPrepareDto.gmpCommand.PaymentKey = Guid.Empty;
                    //            _gmpPrepareDto.gmpCommand.Command = "prepare_PrintUserMessage";
                    //            _gmpPrepareDto.gmpCommand.BufferData = CommandHelperGmpProvider.BufferConvertData(_gmpPrepareDto.buffer, _gmpPrepareDto.bufferLen);
                    //            _gmpPrepareDto.gmpCommand.ReturnCode = 0;
                    //            _gmpPrepareDto.gmpCommand.ReturnValue = "";
                    //            GmpPrepareDtoList.Add(_gmpPrepareDto);
                    //        }
                    //    }
                    //}
                }

                //Satış barkodu
                if (SettingsValues.fiscalbarcode == "1")
                {
                    //LogManager.Append("Satış barkodu yazdırılıyor...");
                    //Barkod :273
                    _gmpPrepareDto = PrinterStart.PreparePrintUserMessage(order.OrderID.ToString(), 273);
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

                //Satış fiş no
                if (SettingsValues.fiscalreceiptno == "1")
                {
                    //LogManager.Append("Satış fiş no yazdırılıyor...");
                    _gmpPrepareDto = PrinterStart.PreparePrintUserMessage("SİPARİŞ NO : " + order.fiscalReceiptNo, 9);
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
                #endregion

                #region prepare_PrintMF
                _gmpPrepareDto = new GmpPrepareDto();
                _gmpPrepareDto = MF.PreparePrintMF();
                if (_gmpPrepareDto.bufferLen == 0)
                {
                    _gmpPrepareDto.prepareCommand = TPrepareCommand.prepare_PrintMF;
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
                    _gmpPrepareDto.gmpCommand.Command = "prepare_PrintMF";
                    _gmpPrepareDto.gmpCommand.BufferData = CommandHelperGmpProvider.BufferConvertData(_gmpPrepareDto.buffer, _gmpPrepareDto.bufferLen);
                    _gmpPrepareDto.gmpCommand.ReturnCode = 0;
                    _gmpPrepareDto.gmpCommand.ReturnValue = "";
                    GmpPrepareDtoList.Add(_gmpPrepareDto);
                }
                #endregion

            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "FiscalModule.Ingenico.GmpProvider.PrintReceipts.CashBeforeCommand");
            }
            return GmpPrepareDtoList;
        }
        public static List<GmpPrepareDto> CashBeforeCommand(double amount, int paymentType)
        {
            GmpPrepareDto _gmpPrepareDto = new GmpPrepareDto();
            List<GmpPrepareDto> GmpPrepareDtoList = new List<GmpPrepareDto>();
            try
            {
                #region payment

                _gmpPrepareDto = new GmpPrepareDto();
                if (paymentType == 0)
                {
                    //kasa ödeme
                    _gmpPrepareDto = GetSinglePayment.PrepareReversePayment(new FiscalPaymentLine { PaymentAmount = amount, MethodType = PaymentMethodType.Cash });
                }
                else
                {
                    //kasa avans
                    _gmpPrepareDto = GetSinglePayment.PreparePayment(new FiscalPaymentLine { PaymentAmount = amount, MethodType = PaymentMethodType.Cash });
                }


                if (_gmpPrepareDto.bufferLen == 0)
                {
                    _gmpPrepareDto.prepareCommand = TPrepareCommand.prepare_Payment;
                    GmpPrepareDtoList.Add(_gmpPrepareDto);
                    return GmpPrepareDtoList;
                }
                else
                {
                    _gmpPrepareDto.prepareCommand = TPrepareCommand.FiscalPrinter_ok;
                    _gmpPrepareDto.gmpCommand = new GmpCommand();
                    _gmpPrepareDto.gmpCommand.OrderID = 0;
                    _gmpPrepareDto.gmpCommand.TransactionKey = null;
                    _gmpPrepareDto.gmpCommand.PaymentKey = Guid.Empty;
                    _gmpPrepareDto.gmpCommand.Command = "prepare_Payment";
                    _gmpPrepareDto.gmpCommand.BufferData = CommandHelperGmpProvider.BufferConvertData(_gmpPrepareDto.buffer, _gmpPrepareDto.bufferLen);
                    _gmpPrepareDto.gmpCommand.ReturnCode = 0;
                    _gmpPrepareDto.gmpCommand.ReturnValue = "";
                    GmpPrepareDtoList.Add(_gmpPrepareDto);
                }



                #endregion

                #region prepare_PrintTotalsAndPayments
                _gmpPrepareDto = new GmpPrepareDto();
                _gmpPrepareDto = PrinterStart.PreparePrintTotalsAndPayments();
                if (_gmpPrepareDto.bufferLen == 0)
                {
                    _gmpPrepareDto.prepareCommand = TPrepareCommand.prepare_PrintTotalsAndPayments;
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
                    _gmpPrepareDto.gmpCommand.Command = "prepare_PrintTotalsAndPayments";
                    _gmpPrepareDto.gmpCommand.BufferData = CommandHelperGmpProvider.BufferConvertData(_gmpPrepareDto.buffer, _gmpPrepareDto.bufferLen);
                    _gmpPrepareDto.gmpCommand.ReturnCode = 0;
                    _gmpPrepareDto.gmpCommand.ReturnValue = "";
                    GmpPrepareDtoList.Add(_gmpPrepareDto);
                }
                #endregion

                #region prepare_PrintBeforeMF
                _gmpPrepareDto = new GmpPrepareDto();
                _gmpPrepareDto = MF.PreparePrintBeforeMF();
                if (_gmpPrepareDto.bufferLen == 0)
                {
                    _gmpPrepareDto.prepareCommand = TPrepareCommand.prepare_PrintBeforeMF;
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
                    _gmpPrepareDto.gmpCommand.Command = "prepare_PrintBeforeMF";
                    _gmpPrepareDto.gmpCommand.BufferData = CommandHelperGmpProvider.BufferConvertData(_gmpPrepareDto.buffer, _gmpPrepareDto.bufferLen);
                    _gmpPrepareDto.gmpCommand.ReturnCode = 0;
                    _gmpPrepareDto.gmpCommand.ReturnValue = "";
                    GmpPrepareDtoList.Add(_gmpPrepareDto);
                }
                #endregion

                #region prepare_PrintMF
                _gmpPrepareDto = new GmpPrepareDto();
                _gmpPrepareDto = MF.PreparePrintMF();
                if (_gmpPrepareDto.bufferLen == 0)
                {
                    _gmpPrepareDto.prepareCommand = TPrepareCommand.prepare_PrintMF;
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
                    _gmpPrepareDto.gmpCommand.Command = "prepare_PrintMF";
                    _gmpPrepareDto.gmpCommand.BufferData = CommandHelperGmpProvider.BufferConvertData(_gmpPrepareDto.buffer, _gmpPrepareDto.bufferLen);
                    _gmpPrepareDto.gmpCommand.ReturnCode = 0;
                    _gmpPrepareDto.gmpCommand.ReturnValue = "";
                    GmpPrepareDtoList.Add(_gmpPrepareDto);
                }
                #endregion

            }
            catch (Exception ex)
            {
               // LogManager.Exception(ex, "PrintReceiptGmpProvider.ReturnCashBeforeCommand");
            }
            return GmpPrepareDtoList;
        }

    }
}
