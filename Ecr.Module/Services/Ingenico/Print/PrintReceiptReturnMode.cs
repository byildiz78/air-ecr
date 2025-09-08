using Ecr.Module.Services.Ingenico.FiscalLogManager;
using Ecr.Module.Services.Ingenico.GmpIngenico;
using Ecr.Module.Services.Ingenico.Models;
using Ecr.Module.Services.Ingenico.SingleMethod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecr.Module.Services.Ingenico.Print
{
    public class PrintReceiptReturnMode
    {
        public static IngenicoApiResponse<GmpPrintReceiptDto> ReturnPrintOrder(FiscalOrder order)
        {
            IngenicoApiResponse<GmpPrintReceiptDto> printResult = new IngenicoApiResponse<GmpPrintReceiptDto>();
            try
            {
                var logName = $"iade_{order.OrderKey}_{DateTime.Now.Day}:{DateTime.Now.Month}:{DateTime.Now.Second}";
                LogManagerOrder.RenameLog(order.OrderKey.ToString(), logName);
                LogManagerOrder.MoveLogFile(logName, LogFolderType.Return);


                FiscalReturnType returnType = GetFiscalReturnType(order.paymentLines);

                int creditCardCount = order.paymentLines.Count(c => c.PaymentBaseTypeID == 2 && !c.IsPaid.Value);
                if (creditCardCount > 1)
                {
                    printResult.Data = CommandError.CommandErrorMessage(-1, "KREDİ KARTI İADE / İPTAL İŞLEMİ", "AYNI ANDA TEK KREDİ KARTI İPTAL/İADESİ ALABİLİRSİNİZ");
                    return printResult;
                }

                switch (returnType)
                {
                    case FiscalReturnType.Cash:
                        {
                            printResult.Data = CashReturn.EftPosReturnCashOrder(order);
                            break;
                        }

                    case FiscalReturnType.CreditCard:
                        {
                            printResult.Data = CreditReturn.EftPosRefundCreditCard(order);
                            break;
                        }

                    case FiscalReturnType.CashAndCreditCard:
                        {
                            printResult.Data = CreditReturn.EftPosRefundCreditCard(order);

                            if (printResult.Data.ReturnCode == Defines.TRAN_RESULT_OK)
                            {
                                printResult.Data = CashReturn.EftPosReturnCashOrder(order);
                            }
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "ReturnPrintOrder");
            }
            return printResult;

        }
        public static FiscalReturnType GetFiscalReturnType(List<FiscalPaymentLine> PaymentLineList)
        {
            FiscalReturnType returnType = FiscalReturnType.Cash;

            int paymentCount = PaymentLineList.Count(w => !w.IsPaid.Value);
            int paymentCashCount = PaymentLineList.Count(w => w.PaymentBaseTypeID == 1 && !w.IsPaid.Value);
            int paymentCreditCount = PaymentLineList.Count(w => w.PaymentBaseTypeID == 2 && !w.IsPaid.Value);

            if (paymentCount == paymentCashCount)
            {
                returnType = FiscalReturnType.Cash;
            }
            else if (paymentCount == paymentCreditCount)
            {
                returnType = FiscalReturnType.CreditCard;
            }
            else if (paymentCount == paymentCreditCount + paymentCashCount)
            {
                returnType = FiscalReturnType.CashAndCreditCard;
            }

            //LogManager.Append($"GetFiscalReturnType -> Return Type : {returnType.ToString()}", "GetFiscalReturnType");

            return returnType;
        }
    }
}
