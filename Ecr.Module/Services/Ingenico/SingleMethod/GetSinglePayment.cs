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
    public static class GetSinglePayment
    {
        public static ST_PAYMENT_REQUEST Single_Payment(FiscalPaymentLine paymentline)
        {
            ushort currencyOfPayment = 949;
            ST_PAYMENT_REQUEST stPaymentRequest = new ST_PAYMENT_REQUEST();
            switch (paymentline.MethodType)
            {
                case PaymentMethodType.Cash: //nakit
                    {
                        stPaymentRequest.typeOfPayment = (uint)EPaymentTypes.PAYMENT_CASH_TL;
                        stPaymentRequest.subtypeOfPayment = 0;
                        stPaymentRequest.payAmount = (uint)Math.Abs(CommandHelperGmpProvider.DoubleFormat(paymentline.PaymentAmount.Value, 2));
                        stPaymentRequest.payAmountCurrencyCode = currencyOfPayment;

                        break;
                    }
                case PaymentMethodType.Visa:  //kredi kartı
                    {
                        stPaymentRequest.typeOfPayment = (uint)EPaymentTypes.PAYMENT_BANK_CARD;
                        stPaymentRequest.subtypeOfPayment = Defines.PAYMENT_SUBTYPE_PROCESS_ON_POS;
                        stPaymentRequest.payAmount = (uint)Math.Abs(CommandHelperGmpProvider.DoubleFormat(paymentline.PaymentAmount.Value, 2));
                        stPaymentRequest.payAmountCurrencyCode = currencyOfPayment;
                        stPaymentRequest.transactionFlag = 0x00000000;
                        stPaymentRequest.bankBkmId = 0;
                        stPaymentRequest.numberOfinstallments = 0;

                        //banka seçilmiş ise
                        if (!string.IsNullOrEmpty(paymentline.BankCode) && !paymentline.BankCode.Contains("JI"))
                        {
                            stPaymentRequest.bankBkmId = (ushort)Convert.ToInt32(paymentline.BankCode);
                        }
                        //taksitli satış kullanılıyorsa
                        if (paymentline.UseBankInstallment.Value)
                        {
                            stPaymentRequest.numberOfinstallments = (ushort)paymentline.BankInstallmentCount;
                        }
                        stPaymentRequest.rawData = Encoding.Default.GetBytes("RawData from external application for the payment application");
                        stPaymentRequest.rawDataLen = (ushort)stPaymentRequest.rawData.Length;

                        break;
                    }
                case PaymentMethodType.GiftCard: //sokkart
                    {
                        stPaymentRequest.typeOfPayment = (uint)EPaymentTypes.PAYMENT_PUAN;
                        stPaymentRequest.subtypeOfPayment = 0;
                        stPaymentRequest.payAmount = (uint)Math.Abs(CommandHelperGmpProvider.DoubleFormat(paymentline.PaymentAmount.Value, 2));
                        stPaymentRequest.payAmountCurrencyCode = currencyOfPayment;
                        stPaymentRequest.bankBkmId = 0;
                        stPaymentRequest.numberOfinstallments = 0;
                        stPaymentRequest.paymentName = paymentline.PaymentName;

                        break;
                    }

                case PaymentMethodType.Ibb:
                    {
                        //stPaymentRequest.typeOfPayment = (uint)EPaymentTypes.PAYMENT_CEK;
                        //stPaymentRequest.bankBkmId = 52619;//CD8B
                        //stPaymentRequest.numberOfinstallments = 0;
                        //stPaymentRequest.subtypeOfPayment = 0;
                        //stPaymentRequest.payAmount = (uint)Math.Abs(CommandHelperGmpProvider.DoubleFormat(paymentline.PaymentAmount.Value, 2));
                        //stPaymentRequest.payAmountCurrencyCode = currencyOfPayment;
                        break;
                    }
                case PaymentMethodType.BankaTransfer:
                    {
                        stPaymentRequest.typeOfPayment = (uint)EPaymentTypes.PAYMENT_BANKA_TRANSFERI;
                        stPaymentRequest.bankBkmId = 0;
                        stPaymentRequest.numberOfinstallments = 0;
                        stPaymentRequest.subtypeOfPayment = 0;
                        stPaymentRequest.payAmount = (uint)Math.Abs(CommandHelperGmpProvider.DoubleFormat(paymentline.PaymentAmount.Value, 2));
                        stPaymentRequest.payAmountCurrencyCode = currencyOfPayment;
                        break;
                    }
                case PaymentMethodType.AcikHesap:
                    {
                        stPaymentRequest.typeOfPayment = (uint)EPaymentTypes.PAYMENT_ACIK_HESAP;
                        stPaymentRequest.bankBkmId = 0;
                        stPaymentRequest.numberOfinstallments = 0;
                        stPaymentRequest.subtypeOfPayment = 0;
                        stPaymentRequest.payAmount = (uint)Math.Abs(CommandHelperGmpProvider.DoubleFormat(paymentline.PaymentAmount.Value, 2));
                        stPaymentRequest.payAmountCurrencyCode = currencyOfPayment;
                        break;
                    }
                case PaymentMethodType.YemekCeki:
                    {
                        stPaymentRequest.typeOfPayment = (uint)EPaymentTypes.PAYMENT_YEMEKCEKI;
                        stPaymentRequest.bankBkmId = 52619;//jenerikyemek ceki;
                        stPaymentRequest.numberOfinstallments = 0;
                        stPaymentRequest.subtypeOfPayment = 0;
                        stPaymentRequest.payAmount = (uint)Math.Abs(CommandHelperGmpProvider.DoubleFormat(paymentline.PaymentAmount.Value, 2));
                        stPaymentRequest.payAmountCurrencyCode = currencyOfPayment;

                        break;
                    }
                case PaymentMethodType.Multinet:
                    {
                        stPaymentRequest.typeOfPayment = (uint)EPaymentTypes.PAYMENT_YEMEKCEKI;
                        stPaymentRequest.bankBkmId = 0xCD85;
                        stPaymentRequest.numberOfinstallments = 0;
                        stPaymentRequest.subtypeOfPayment = 0;
                        stPaymentRequest.payAmount = (uint)Math.Abs(CommandHelperGmpProvider.DoubleFormat(paymentline.PaymentAmount.Value, 2));
                        stPaymentRequest.payAmountCurrencyCode = currencyOfPayment;
                        break;
                    }
                case PaymentMethodType.Ticket:
                    {
                        stPaymentRequest.typeOfPayment = (uint)EPaymentTypes.PAYMENT_YEMEKCEKI;
                        stPaymentRequest.bankBkmId = 0x59BA;
                        stPaymentRequest.numberOfinstallments = 0;
                        stPaymentRequest.subtypeOfPayment = 0;
                        stPaymentRequest.payAmount = (uint)Math.Abs(CommandHelperGmpProvider.DoubleFormat(paymentline.PaymentAmount.Value, 2));
                        stPaymentRequest.payAmountCurrencyCode = currencyOfPayment;
                        break;
                    }
                case PaymentMethodType.Sodexo:
                    {
                        stPaymentRequest.typeOfPayment = (uint)EPaymentTypes.PAYMENT_YEMEKCEKI;
                        stPaymentRequest.bankBkmId = 0x59BD;
                        stPaymentRequest.numberOfinstallments = 0;
                        stPaymentRequest.subtypeOfPayment = 0;
                        stPaymentRequest.payAmount = (uint)Math.Abs(CommandHelperGmpProvider.DoubleFormat(paymentline.PaymentAmount.Value, 2));
                        stPaymentRequest.payAmountCurrencyCode = currencyOfPayment;
                        break;
                    }
                case PaymentMethodType.MetropolCard:
                    {
                        stPaymentRequest.typeOfPayment = (uint)EPaymentTypes.PAYMENT_YEMEKCEKI;
                        stPaymentRequest.bankBkmId = 0xCD9A;
                        stPaymentRequest.numberOfinstallments = 0;
                        stPaymentRequest.subtypeOfPayment = 0;
                        stPaymentRequest.payAmount = (uint)Math.Abs(CommandHelperGmpProvider.DoubleFormat(paymentline.PaymentAmount.Value, 2));
                        stPaymentRequest.payAmountCurrencyCode = currencyOfPayment;
                        break;
                    }
                case PaymentMethodType.Paye:
                    {
                        stPaymentRequest.typeOfPayment = (uint)EPaymentTypes.PAYMENT_YEMEKCEKI;
                        stPaymentRequest.bankBkmId = 52654;
                        stPaymentRequest.numberOfinstallments = 0;
                        stPaymentRequest.subtypeOfPayment = 0;
                        stPaymentRequest.payAmount = (uint)Math.Abs(CommandHelperGmpProvider.DoubleFormat(paymentline.PaymentAmount.Value, 2));
                        stPaymentRequest.payAmountCurrencyCode = (ushort)ECurrency.CURRENCY_TL;
                        break;
                    }
                case PaymentMethodType.Setcard:
                    {
                        stPaymentRequest.typeOfPayment = (uint)EPaymentTypes.PAYMENT_YEMEKCEKI;
                        stPaymentRequest.bankBkmId = 52649;
                        stPaymentRequest.numberOfinstallments = 0;
                        stPaymentRequest.subtypeOfPayment = 0;
                        stPaymentRequest.payAmount = (uint)Math.Abs(CommandHelperGmpProvider.DoubleFormat(paymentline.PaymentAmount.Value, 2));
                        stPaymentRequest.payAmountCurrencyCode = (ushort)ECurrency.CURRENCY_TL;
                        break;
                    }
            }
            return stPaymentRequest;
        }

        public  static GmpPrepareDto PrepareReversePayment(FiscalPaymentLine paymentline)
        {
            GmpPrepareDto _prepare = new GmpPrepareDto();
            UInt16 currencyOfPayment = 949;

            try
            {
                ST_PAYMENT_REQUEST stPaymentRequest = new ST_PAYMENT_REQUEST();

                #region MethodType
                var MethodType = PaymentMethodType.Cash;

                if (paymentline.PaymentBaseTypeID == 2)
                {
                    MethodType = PaymentMethodType.Visa;
                }
                //else if (paymentline.PaymentBaseTypeID == 1 && paymentline.PaymentTypeID == 14)
                //{
                //    MethodType = PaymentMethodType.Ibb;
                //}
                //else if (paymentline.PaymentBaseTypeID == 5 && paymentline.PaymentTypeID == 12)
                //{
                //    MethodType = PaymentMethodType.GiftCard;
                //}
                else if (paymentline.PaymentBaseTypeID == 5)
                {
                    MethodType = PaymentMethodType.GiftCard;
                }
                else if (paymentline.PaymentBaseTypeID == 1)
                {
                    MethodType = PaymentMethodType.Cash;
                }
                #endregion

                switch (MethodType)
                {
                    case PaymentMethodType.Cash: //nakit
                        {
                            stPaymentRequest.typeOfPayment = (uint)EPaymentTypes.REVERSE_PAYMENT_CASH;
                            stPaymentRequest.subtypeOfPayment = 0;
                            stPaymentRequest.payAmount = (uint)Math.Abs(CommandHelperGmpProvider.DoubleFormat(paymentline.PaymentAmount.Value, 2));
                            stPaymentRequest.payAmountCurrencyCode = currencyOfPayment;
                            break;
                        }
                    case PaymentMethodType.Visa:  //kredi kartı
                        {
                            stPaymentRequest.typeOfPayment = (uint)EPaymentTypes.REVERSE_PAYMENT_BANK_CARD_VOID;
                            stPaymentRequest.subtypeOfPayment = Defines.PAYMENT_SUBTYPE_PROCESS_ON_POS;
                            stPaymentRequest.payAmount = (uint)Math.Abs(CommandHelperGmpProvider.DoubleFormat(paymentline.PaymentAmount.Value, 2));
                            stPaymentRequest.payAmountCurrencyCode = currencyOfPayment;
                            stPaymentRequest.transactionFlag = 0x00000000;
                            stPaymentRequest.bankBkmId = 0;
                            stPaymentRequest.numberOfinstallments = 0;

                            //banka seçilmiş ise
                            if (!string.IsNullOrEmpty(paymentline.BankCode))
                            {
                                stPaymentRequest.bankBkmId = (ushort)Convert.ToInt32(paymentline.BankCode);
                            }
                            //taksitli satış kullanılıyorsa
                            if (paymentline.UseBankInstallment.Value)
                            {
                                stPaymentRequest.numberOfinstallments = (ushort)paymentline.BankInstallmentCount;
                            }
                            stPaymentRequest.rawData = Encoding.Default.GetBytes("RawData from external application for the payment application");
                            stPaymentRequest.rawDataLen = (ushort)stPaymentRequest.rawData.Length;

                            break;
                        }


                }

                _prepare.bufferLen = Json_GMPSmartDLL.prepare_ReversePayment(_prepare.buffer, _prepare.buffer.Length, ref stPaymentRequest, 1);
            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "BatchCommadGmpProvider.PrepareReversePayment");
            }
            return _prepare;
        }

        public static GmpPrepareDto PreparePayment(FiscalPaymentLine paymentline)
        {
            GmpPrepareDto _prepare = new GmpPrepareDto();
            UInt16 currencyOfPayment = 949;

            try
            {
                ST_PAYMENT_REQUEST stPaymentRequest = new ST_PAYMENT_REQUEST();
                switch (paymentline.MethodType)
                {
                    case PaymentMethodType.Cash: //nakit
                        {
                            stPaymentRequest.typeOfPayment = (uint)EPaymentTypes.PAYMENT_CASH_TL;
                            stPaymentRequest.subtypeOfPayment = 0;
                            stPaymentRequest.payAmount = (uint)Math.Abs(CommandHelperGmpProvider.DoubleFormat(paymentline.PaymentAmount.Value, 2));
                            stPaymentRequest.payAmountCurrencyCode = currencyOfPayment;
                            break;
                        }
                    case PaymentMethodType.Visa:  //kredi kartı
                        {
                            stPaymentRequest.typeOfPayment = (uint)EPaymentTypes.PAYMENT_BANK_CARD;
                            stPaymentRequest.subtypeOfPayment = Defines.PAYMENT_SUBTYPE_PROCESS_ON_POS;
                            stPaymentRequest.payAmount = (uint)Math.Abs(CommandHelperGmpProvider.DoubleFormat(paymentline.PaymentAmount.Value, 2));
                            stPaymentRequest.payAmountCurrencyCode = currencyOfPayment;
                            stPaymentRequest.transactionFlag = 0x00000000;
                            stPaymentRequest.bankBkmId = 0;
                            stPaymentRequest.numberOfinstallments = 0;

                            //banka seçilmiş ise
                            if (!string.IsNullOrEmpty(paymentline.BankCode) && !paymentline.BankCode.Contains("JI"))
                            {
                                stPaymentRequest.bankBkmId = (ushort)Convert.ToInt32(paymentline.BankCode);
                            }
                            //taksitli satış kullanılıyorsa
                            if (paymentline.UseBankInstallment.Value)
                            {
                                stPaymentRequest.numberOfinstallments = (ushort)paymentline.BankInstallmentCount;
                            }
                            stPaymentRequest.rawData = Encoding.Default.GetBytes("RawData from external application for the payment application");
                            stPaymentRequest.rawDataLen = (ushort)stPaymentRequest.rawData.Length;

                            break;
                        }
                    case PaymentMethodType.GiftCard: //sokkart
                        {
                            stPaymentRequest.typeOfPayment = (uint)EPaymentTypes.PAYMENT_PUAN;
                            stPaymentRequest.subtypeOfPayment = 0;
                            stPaymentRequest.payAmount = (uint)Math.Abs(CommandHelperGmpProvider.DoubleFormat(paymentline.PaymentAmount.Value, 2));
                            stPaymentRequest.payAmountCurrencyCode = currencyOfPayment;
                            stPaymentRequest.bankBkmId = 0;
                            stPaymentRequest.numberOfinstallments = 0;
                            stPaymentRequest.paymentName = paymentline.PaymentName;

                            break;
                        }

                    case PaymentMethodType.Ibb:
                        {
                            //stPaymentRequest.typeOfPayment = (uint)EPaymentTypes.PAYMENT_CEK;
                            //stPaymentRequest.bankBkmId = 52619;//CD8B
                            //stPaymentRequest.numberOfinstallments = 0;
                            //stPaymentRequest.subtypeOfPayment = 0;
                            //stPaymentRequest.payAmount = (uint)Math.Abs(CommandHelperGmpProvider.DoubleFormat(paymentline.PaymentAmount.Value, 2));
                            //stPaymentRequest.payAmountCurrencyCode = currencyOfPayment;
                            break;
                        }
                    case PaymentMethodType.BankaTransfer:
                        {
                            stPaymentRequest.typeOfPayment = (uint)EPaymentTypes.PAYMENT_BANKA_TRANSFERI;
                            stPaymentRequest.bankBkmId = 0;
                            stPaymentRequest.numberOfinstallments = 0;
                            stPaymentRequest.subtypeOfPayment = 0;
                            stPaymentRequest.payAmount = (uint)Math.Abs(CommandHelperGmpProvider.DoubleFormat(paymentline.PaymentAmount.Value, 2));
                            stPaymentRequest.payAmountCurrencyCode = currencyOfPayment;
                            break;
                        }
                    case PaymentMethodType.AcikHesap:
                        {
                            stPaymentRequest.typeOfPayment = (uint)EPaymentTypes.PAYMENT_ACIK_HESAP;
                            stPaymentRequest.bankBkmId = 0;
                            stPaymentRequest.numberOfinstallments = 0;
                            stPaymentRequest.subtypeOfPayment = 0;
                            stPaymentRequest.payAmount = (uint)Math.Abs(CommandHelperGmpProvider.DoubleFormat(paymentline.PaymentAmount.Value, 2));
                            stPaymentRequest.payAmountCurrencyCode = currencyOfPayment;
                            break;
                        }
                    case PaymentMethodType.YemekCeki:
                        {
                            stPaymentRequest.typeOfPayment = (uint)EPaymentTypes.PAYMENT_YEMEKCEKI;
                            stPaymentRequest.bankBkmId = 52619;//jenerikyemek ceki;
                            stPaymentRequest.numberOfinstallments = 0;
                            stPaymentRequest.subtypeOfPayment = 0;
                            stPaymentRequest.payAmount = (uint)Math.Abs(CommandHelperGmpProvider.DoubleFormat(paymentline.PaymentAmount.Value, 2));
                            stPaymentRequest.payAmountCurrencyCode = currencyOfPayment;
                            break;
                        }
                    case PaymentMethodType.Multinet:
                        {
                            stPaymentRequest.typeOfPayment = (uint)EPaymentTypes.PAYMENT_YEMEKCEKI;
                            stPaymentRequest.bankBkmId = 0xCD85;
                            stPaymentRequest.numberOfinstallments = 0;
                            stPaymentRequest.subtypeOfPayment = 0;
                            stPaymentRequest.payAmount = (uint)Math.Abs(CommandHelperGmpProvider.DoubleFormat(paymentline.PaymentAmount.Value, 2));
                            stPaymentRequest.payAmountCurrencyCode = currencyOfPayment;
                            break;
                        }
                    case PaymentMethodType.Ticket:
                        {
                            stPaymentRequest.typeOfPayment = (uint)EPaymentTypes.PAYMENT_YEMEKCEKI;
                            stPaymentRequest.bankBkmId = 0x59BA;
                            stPaymentRequest.numberOfinstallments = 0;
                            stPaymentRequest.subtypeOfPayment = 0;
                            stPaymentRequest.payAmount = (uint)Math.Abs(CommandHelperGmpProvider.DoubleFormat(paymentline.PaymentAmount.Value, 2));
                            stPaymentRequest.payAmountCurrencyCode = currencyOfPayment;
                            break;
                        }
                    case PaymentMethodType.Sodexo:
                        {
                            stPaymentRequest.typeOfPayment = (uint)EPaymentTypes.PAYMENT_YEMEKCEKI;
                            stPaymentRequest.bankBkmId = 0x59BD;
                            stPaymentRequest.numberOfinstallments = 0;
                            stPaymentRequest.subtypeOfPayment = 0;
                            stPaymentRequest.payAmount = (uint)Math.Abs(CommandHelperGmpProvider.DoubleFormat(paymentline.PaymentAmount.Value, 2));
                            stPaymentRequest.payAmountCurrencyCode = currencyOfPayment;
                            break;
                        }
                    case PaymentMethodType.MetropolCard:
                        {
                            stPaymentRequest.typeOfPayment = (uint)EPaymentTypes.PAYMENT_YEMEKCEKI;
                            stPaymentRequest.bankBkmId = 0xCD9A;
                            stPaymentRequest.numberOfinstallments = 0;
                            stPaymentRequest.subtypeOfPayment = 0;
                            stPaymentRequest.payAmount = (uint)Math.Abs(CommandHelperGmpProvider.DoubleFormat(paymentline.PaymentAmount.Value, 2));
                            stPaymentRequest.payAmountCurrencyCode = currencyOfPayment;
                            break;
                        }
                    case PaymentMethodType.Paye:
                        {
                            stPaymentRequest.typeOfPayment = (uint)EPaymentTypes.PAYMENT_YEMEKCEKI;
                            stPaymentRequest.bankBkmId = 52654;
                            stPaymentRequest.numberOfinstallments = 0;
                            stPaymentRequest.subtypeOfPayment = 0;
                            stPaymentRequest.payAmount = (uint)Math.Abs(CommandHelperGmpProvider.DoubleFormat(paymentline.PaymentAmount.Value, 2));
                            stPaymentRequest.payAmountCurrencyCode = (ushort)ECurrency.CURRENCY_TL;
                            break;
                        }
                    case PaymentMethodType.Setcard:
                        {
                            stPaymentRequest.typeOfPayment = (uint)EPaymentTypes.PAYMENT_YEMEKCEKI;
                            stPaymentRequest.bankBkmId = 52649;
                            stPaymentRequest.numberOfinstallments = 0;
                            stPaymentRequest.subtypeOfPayment = 0;
                            stPaymentRequest.payAmount = (uint)Math.Abs(CommandHelperGmpProvider.DoubleFormat(paymentline.PaymentAmount.Value, 2));
                            stPaymentRequest.payAmountCurrencyCode = (ushort)ECurrency.CURRENCY_TL;
                            break;
                        }
                }

                _prepare.bufferLen = Json_GMPSmartDLL.prepare_Payment(_prepare.buffer, _prepare.buffer.Length, ref stPaymentRequest);
            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "BatchCommadGmpProvider.PreparePayment");
            }
            return _prepare;
        }


    }
}
