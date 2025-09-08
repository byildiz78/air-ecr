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
    public static class CreditReturn
    {
        public static GmpPrintReceiptDto EftPosRefundCreditCard(FiscalOrder order)
        {
            GmpPrintReceiptDto printResult = new GmpPrintReceiptDto();

            try
            {
                List<FiscalPaymentLine> CreditCardList = order.paymentLines.Where(w => w.PaymentBaseTypeID == 2 && !w.IsPaid.Value).ToList();

                foreach (var cardPayment in CreditCardList)
                {
                    if (cardPayment.IsVoidMode)
                    {
                        //kredi iptal
                        printResult = EftPosCreditCardVoid(new BankPaymentRefundDto
                        {
                            BankCode = (!string.IsNullOrEmpty(cardPayment.BankCode) ? Convert.ToInt32(cardPayment.BankCode) : 0),
                            BankInstallmentCount = (int)cardPayment.ReturnInstallment.Value,
                            PaidAmount = cardPayment.PaymentAmount.Value,
                            PaymentId = cardPayment.PaymentKey.Value,
                            ReturnAuthCode = cardPayment.ReturnAuthCode,
                            ReturnBatchNo = (!string.IsNullOrEmpty(cardPayment.ReturnBatchNo) ? Convert.ToInt32(cardPayment.ReturnBatchNo) : 0),
                            ReturnStanNo = (!string.IsNullOrEmpty(cardPayment.ReturnStanNo) ? Convert.ToInt32(cardPayment.ReturnStanNo) : 0),
                            ReturnTerminalNo = cardPayment.ReturnTerminalNo,
                            ReturnTranRefCode = cardPayment.ReturnRefRRN,
                            ReturnMerchantID = cardPayment.ReturnMerchantID,
                            ReturnOriginalData = string.Format("{0:YYMMDDHHMM}", cardPayment.ReturnOriginalData),
                            TransAmount = cardPayment.ReturnOrderAmount.Value

                        }, order);
                    }
                    else
                    {
                        //kredi iade
                        printResult = EftPosCreditCardRefund(new BankPaymentRefundDto
                        {
                            BankCode = (!string.IsNullOrEmpty(cardPayment.BankCode) ? Convert.ToInt32(cardPayment.BankCode) : 0),
                            BankInstallmentCount = (int)cardPayment.ReturnInstallment.Value,
                            PaidAmount = cardPayment.PaymentAmount.Value,
                            PaymentId = cardPayment.PaymentKey.Value,
                            ReturnAuthCode = cardPayment.ReturnAuthCode,
                            ReturnBatchNo = (!string.IsNullOrEmpty(cardPayment.ReturnBatchNo) ? Convert.ToInt32(cardPayment.ReturnBatchNo) : 0),
                            ReturnStanNo = (!string.IsNullOrEmpty(cardPayment.ReturnStanNo) ? Convert.ToInt32(cardPayment.ReturnStanNo) : 0),
                            ReturnTerminalNo = cardPayment.ReturnTerminalNo,
                            ReturnMerchantID = cardPayment.ReturnMerchantID,
                            ReturnTranRefCode = cardPayment.ReturnRefRRN,
                            ReturnOriginalData = string.Format("{0:YYMMDDHHMM}", cardPayment.ReturnOriginalData),
                            TransAmount = cardPayment.ReturnOrderAmount.Value
                        }, order);

                    }

                    //LogManager.Append($"EftPosRefundCreditCard() metodu {(printResult.ReturnCode == Defines.TRAN_RESULT_OK ? "BAŞARILI" : "BAŞARISIZ")} oldu...PaymentKey = {cardPayment.PaymentKey.Value} , ReturnCode : {printResult.ReturnCode} ,\n ReturnMessage : {printResult.ReturnCodeMessage} ,\n PrintReceiptInfo : {printResult.JsonSerialize()} ,\n ");

                    if (printResult.ReturnCode != Defines.TRAN_RESULT_OK)
                    {
                        return printResult;
                    }

                }

            }
            catch (Exception ex)
            {
                printResult.ReturnCode = 9999;
                printResult.ReturnStringMessage =
                    "KREDİ KARTI İADE İŞLEMİ İÇİN GİRİLEN PARAMETRELERİ KONTROL EDİNİZ!";
                printResult.ReturnCodeMessage = "";
               // LogManager.Exception(ex, "EftPosRefundCreditCard");
            }

            return printResult;
        }

        //Kredi iptal işlemi-Banka Gün sonu alınmamışşa
        public static GmpPrintReceiptDto EftPosCreditCardVoid(BankPaymentRefundDto paymentRequest, FiscalOrder order)
        {
            GmpPrintReceiptDto printResult = new GmpPrintReceiptDto();

            try
            {
                #region STEP 1 : Kredi kartı banka iade parametreleri

                ST_PAYMENT_REQUEST StPaymentRequest = CreditCardRefundParameter(paymentRequest);
                //LogManager.Append($"Eft-Pos Cihazına gönderilen parametreler : {StPaymentRequest.JsonSerialize()}");

                #endregion

                #region STEP 2 : Yazarkasaya kredi iptali için parametreler gönderiliyor,işlem gerçekleştiriliyor..

                printResult.ReturnCode = Json_GMPSmartDLL.FP3_FunctionBankingRefund(DataStore.gmpResult.GmpInfo.CurrentInterface, ref StPaymentRequest, 300 * 1000);
                printResult.ReturnCodeMessage = ErrorClass.DisplayErrorMessage(printResult.ReturnCode);
                printResult.ReturnStringMessage = ErrorClass.DisplayErrorCodeMessage((uint)printResult.ReturnCode);

                //LogManager.Append(string.Format("EftPosCreditCardRefund.FiscalPrinter_FunctionBankingRefund -> {0} - {1}", printResult.ReturnCode, printResult.ReturnCodeMessage), "EftPosCreditCardRefund.FiscalPrinter_FunctionBankingRefund");

                #endregion

                #region STEP 3 : Yazarkasa dönüşü kontrol ediliyor.ticket oluşturuluyor..

                printResult.TicketInfo = RefundConvertTicket(StPaymentRequest, (int)order.OrderID.Value);
                printResult.PrintReceiptInfo = CommandHelperGmpProvider.ReceiptJsonDataHeader(printResult.TicketInfo, true, order);

                #endregion


            }
            catch (Exception ex)
            {
                printResult.ReturnCode = 9999;
                printResult.ReturnStringMessage =
                    "KREDİ KARTI İPTAL İŞLEMİ İÇİN GİRİLEN PARAMETRELERİ KONTROL EDİNİZ!";
                printResult.ReturnCodeMessage = "";
               // LogManager.Exception(ex, "EftPosCreditCardVoid");
            }
            return printResult;
        }

        //Kredi iade işlemi-Banka Gün sonu alınmışssa
        public static GmpPrintReceiptDto EftPosCreditCardRefund(BankPaymentRefundDto paymentRequest, FiscalOrder order)
        {
            GmpPrintReceiptDto printResult = new GmpPrintReceiptDto();
            //LogManager.Append($"EftPosCreditCardRefund(Order order) metodu başlatıldı...OrderID : {order.OrderID} , Banka Parametreleri : {paymentRequest.JsonSerialize()}", "PrintReceipts.EftPosCreditCardRefund");
            try
            {

                #region STEP 1 : Kredi kartı banka iade parametreleri

                ST_PAYMENT_REQUEST StPaymentRequest = CreditCardRefundParameter(paymentRequest);
                //LogManager.Append($"Eft-Pos Cihazına gönderilen parametreler : {StPaymentRequest.JsonSerialize()}");

                #endregion

                #region STEP 2 : Yazarkasaya kredi iptali için parametreler gönderiliyor,işlem gerçekleştiriliyor..

                printResult.ReturnCode = Json_GMPSmartDLL.FP3_FunctionBankingRefund(DataStore.gmpResult.GmpInfo.CurrentInterface, ref StPaymentRequest, 300 * 1000);
                printResult.ReturnCodeMessage = ErrorClass.DisplayErrorMessage(printResult.ReturnCode);
                printResult.ReturnStringMessage = ErrorClass.DisplayErrorCodeMessage((uint)printResult.ReturnCode);

               // LogManager.Append(string.Format("EftPosCreditCardRefund.FiscalPrinter_FunctionBankingRefund -> {0} - {1}", printResult.ReturnCode, printResult.ReturnCodeMessage), "EftPosCreditCardRefund.FiscalPrinter_FunctionBankingRefund");

                #endregion

                #region STEP 3 : Yazarkasa dönüşü kontrol ediliyor.ticket oluşturuluyor..

                printResult.TicketInfo = RefundConvertTicket(StPaymentRequest, (int)order.OrderID.Value);
                printResult.PrintReceiptInfo = CommandHelperGmpProvider.ReceiptJsonDataHeader(printResult.TicketInfo, true, order);

                #endregion

            }
            catch (Exception ex)
            {
                printResult.ReturnCode = 9999;
                printResult.ReturnStringMessage =
                    "KREDİ KARTI İADE İŞLEMİ İÇİN GİRİLEN PARAMETRELERİ KONTROL EDİNİZ!";
                printResult.ReturnCodeMessage = "";
               // LogManager.Exception(ex, "EftPosCreditCardRefund");
            }

            return printResult;
        }


        private static ST_PAYMENT_REQUEST CreditCardRefundParameter(BankPaymentRefundDto paymentRequest)
        {
            ST_PAYMENT_REQUEST StPaymentRequest = new ST_PAYMENT_REQUEST();
            ST_PAYMENT_APPLICATION_INFO[] StPaymentApplicationInfo = new ST_PAYMENT_APPLICATION_INFO[24];

            try
            {
                double amount = Math.Round(paymentRequest.PaidAmount, 2);
                double CreditTotal = CommandHelperGmpProvider.DoubleFormat(Math.Abs(amount), 2);
                StPaymentRequest.typeOfPayment = (uint)FunctionFlags.GMP_EXT_DEVICE_FUNC_BANKA_IADE;
                StPaymentRequest.subtypeOfPayment = Defines.PAYMENT_SUBTYPE_PROCESS_ON_POS;
                StPaymentRequest.payAmount = (uint)CreditTotal;

                StPaymentRequest.payAmountCurrencyCode = (ushort)ECurrency.CURRENCY_TL;
                //LogManager.Append($"CreditCardVRefundParameter.{amount} TL Kredi Kartı İade işlemi yapılıyor.");

                //"TYPE 1:SALE 2:INSTALMENT"

                if (paymentRequest.TransAmount > 0)
                {
                    double transactionAmount = Math.Round(paymentRequest.TransAmount, 2);
                    double OrderAmount = CommandHelperGmpProvider.DoubleFormat(Math.Abs(transactionAmount), 2);
                    StPaymentRequest.OrgTransData.TransactionAmount = (uint)OrderAmount;
                }
                else
                {
                    StPaymentRequest.OrgTransData.TransactionAmount = (uint)CreditTotal;
                }

                if (!string.IsNullOrEmpty(paymentRequest.ReturnTranRefCode))
                {
                    StPaymentRequest.OrgTransData.referenceCodeOfTransaction = Encoding.Default.GetBytes(paymentRequest.ReturnTranRefCode);
                    StPaymentRequest.OrgTransData.rrn = Encoding.Default.GetBytes(paymentRequest.ReturnTranRefCode);
                    //StPaymentRequest.referenceCodeOfTransaction = Encoding.Default.GetBytes(item.ReturnTranRefCode);
                }


                if (paymentRequest.BankCode > 0)
                {
                    StPaymentRequest.bankBkmId = (ushort)Convert.ToInt32(paymentRequest.BankCode);
                }
                else
                {
                    StPaymentRequest.bankBkmId = 0;
                }

                if (paymentRequest.BankInstallmentCount > 1)
                {
                    StPaymentRequest.OrgTransData.TransactionType = Convert.ToByte(2);
                    StPaymentRequest.OrgTransData.NumberOfinstallments = Convert.ToByte(paymentRequest.BankInstallmentCount);
                }
                else
                {
                    StPaymentRequest.OrgTransData.TransactionType = Convert.ToByte(1);
                    StPaymentRequest.OrgTransData.NumberOfinstallments = 0;
                }

                if (!string.IsNullOrEmpty(paymentRequest.ReturnAuthCode))
                {
                    StPaymentRequest.OrgTransData.AuthorizationCode = Encoding.Default.GetBytes(paymentRequest.ReturnAuthCode);
                }


                if (!string.IsNullOrEmpty(paymentRequest.ReturnOriginalData))
                {
                    StPaymentRequest.OrgTransData.TransactionDate = new byte[5];
                    CommandHelperGmpProvider.ConvertStringToHexArray(string.Format("{0:YYMMDDHHMM}", paymentRequest.ReturnOriginalData), ref StPaymentRequest.OrgTransData.TransactionDate, 5);
                }

                if (!string.IsNullOrEmpty(paymentRequest.ReturnTerminalNo))
                {
                    StPaymentRequest.terminalId = Encoding.Default.GetBytes(paymentRequest.ReturnTerminalNo);
                }

                if (!string.IsNullOrEmpty(paymentRequest.ReturnMerchantID))
                {
                    StPaymentRequest.OrgTransData.MerchantId = Encoding.Default.GetBytes(paymentRequest.ReturnMerchantID);
                }

                StPaymentRequest.OrgTransData.LoyaltyAmount = 0;



                StPaymentRequest.rawData = Encoding.Default.GetBytes("RawData from external application for the payment application");
                StPaymentRequest.rawDataLen = (ushort)StPaymentRequest.rawData.Length;
            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "PrintReceiptGmpProvider.CreditCardRefundParameter");
            }
            return StPaymentRequest;
        }

        public static ST_TICKET RefundConvertTicket(ST_PAYMENT_REQUEST payment, int OrderID)
        {
            ST_TICKET returnTicket = new ST_TICKET();
            try
            {
                #region STEP 1 : Yazarkasadan aktif Z no bilgisi alınıyor,ticket oluşturuluyor...Sadece kredi kartı İadesi ise

                var ZNo = PrinterGetTlvData.FiscalPrinterGetTlvData(Defines.GMP_EXT_DEVICE_TAG_Z_NO, true);

                returnTicket.ZNo = (ushort)ZNo;
                returnTicket.FNo = (ushort)OrderID;
                returnTicket.EJNo = 1;
                returnTicket.TotalReceiptPayment = payment.payAmount;
                returnTicket.TotalReceiptPayment = payment.payAmount;
                returnTicket.uniqueId = new byte[24];
                returnTicket.ticketType = (byte)TTicketType.TRefund;

                ST_PAYMENT[] st_pay = new ST_PAYMENT[1];

                st_pay[0] = new ST_PAYMENT
                {
                    payAmount = payment.payAmount,
                    typeOfPayment = payment.typeOfPayment,
                    orgAmount = payment.payAmount,
                    subtypeOfPayment = (byte)payment.subtypeOfPayment,
                    stBankPayment = new ST_BANK_PAYMENT_INFO
                    {
                        authorizeCode = (payment.OrgTransData.AuthorizationCode != null ? Encoding.Default.GetString(payment.OrgTransData.AuthorizationCode) : ""),
                        balance = payment.OrgTransData.LoyaltyAmount,
                        bankBkmId = payment.bankBkmId,
                        bankName = "İADE/İPTAL",
                        batchNo = payment.batchNo,
                        merchantId = (payment.OrgTransData.MerchantId != null ? Encoding.Default.GetString(payment.OrgTransData.MerchantId) : ""),
                        numberOfbonus = (byte)payment.OrgTransData.LoyaltyAmount,
                        numberOfdiscount = 0,
                        numberOferrorMessage = 0,
                        numberOfInstallments = (byte)payment.numberOfinstallments,
                        numberOfsubPayment = 0,
                        rrn = (payment.OrgTransData.rrn != null ? Encoding.Default.GetString(payment.OrgTransData.rrn) : ""),
                        stan = payment.stanNo,
                        stBankSubPaymentInfo = new ST_BankSubPaymentInfo[1],
                        stCard = new ST_CARD_INFO(),
                        stPaymentErrMessage = new ST_PaymentErrMessage(),
                        terminalId = (payment.terminalId != null ? Encoding.Default.GetString(payment.terminalId) : "")
                    }
                };

                returnTicket.stPayment = new ST_PAYMENT[1];
                returnTicket.stPayment = st_pay;

                #endregion
            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "RefundConvertTicket");
            }
            return returnTicket;
        }

    }
}
