using Ecr.Module.Services.Ingenico.FiscalLogManager;
using Ecr.Module.Services.Ingenico.GmpIngenico;
using Ecr.Module.Services.Ingenico.Models;
using Ecr.Module.Services.Ingenico.SingleMethod;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ecr.Module.Services.Ingenico.Print
{
    public static class SinglePayment
    {
        public static IngenicoApiResponse<GmpPrintReceiptDto> singleModePayment(FiscalOrder order, List<GmpCommand> tryCommandList , IngenicoApiResponse<GmpPrintReceiptDto> printResult)
        {
            #region payment

            if (order.paymentLines.Any())
            {

                #region MethodType 


                foreach (var sortpay in order.paymentLines)
                {


                    if (sortpay.PaymentBaseTypeID == 2)
                    {
                        sortpay.MethodType = PaymentMethodType.Visa;
                    }
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

                #region �denen tutarlar-Not :�denen tutarlar�n komutlar� olu�turuluyor.�lk komut d�ng�s�nde adl�nm�� �demeleri en ba�a set ediyoruz.Yoksa s�rada kaymalar oluyor ve bu yanl�� �ekimlere neden oluyor

                var paymentCompletedList = order.paymentLines.Where(w => w.IsPaid.Value).OrderBy(x => x.MethodType).ThenBy(x => x.PaymentDateTime).ToList();
                if (paymentCompletedList.Any())
                {
                    //LogManager.Append("�denen �deme tan�mlan�yor...");
                    //LogManager.Append($"{Newtonsoft.Json.JsonConvert.SerializeObject(paymentCompletedList)}", "paymentCompletedList");

                }

                #endregion

                var paymentList = order.paymentLines.Where(w => !w.IsPaid.Value).OrderBy(x => x.MethodType).ThenBy(x => x.PaymentDateTime).ToList();
                //LogManager.Append("�deme tan�mlan�yor...");
                //LogManager.Append("Olu�turan �deme Sat�rlar� ->" + Newtonsoft.Json.JsonConvert.SerializeObject(paymentList));
                foreach (var pay in paymentList)
                {
                    if (pay.IsPaid == false)
                    {
                        if (tryCommandList.Count == 0 || !tryCommandList.Where(x => x.paymentType == pay.MethodType && x.PaymentKey == pay.PaymentKey && x.ReturnValue == "TRAN_RESULT_OK [0]").Any())
                        {
                            // LogManager.Append(pay.PaymentName + "   *" + pay.PaymentAmount.Value + " ispaid : " + pay.IsPaid.Value);

                            ST_TICKET ticket = new ST_TICKET();
                            var payment = GetSinglePayment.Single_Payment(pay);
                            int retvalue = (int)Json_GMPSmartDLL.FP3_Payment(DataStore.CurrentInterface, DataStore.ActiveTransactionHandle, ref payment, ref ticket, DataStore.TIMEOUT_CARD_TRANSACTIONS);
                            if (retvalue != Defines.TRAN_RESULT_OK)
                            {
                                // LogManager.Append($"---------{pay.PaymentName} : error FP3_Payment");
                                var rep = CommandError.CommandErrorMessage(retvalue, ErrorClass.DisplayErrorCodeMessage((uint)retvalue), ErrorClass.DisplayErrorMessage((uint)retvalue));
                                //LogManager.Append($"---------{Newtonsoft.Json.JsonConvert.SerializeObject(rep)} : error FP3_Payment");

                                var subCommand = new GmpCommand();

                                subCommand.OrderID = (int)order.OrderID.Value;
                                subCommand.OrderKey = order.OrderKey.Value;
                                subCommand.TransactionKey = Guid.Empty;
                                subCommand.PaymentKey = pay.PaymentKey;
                                subCommand.Command = "prepare_Payment";
                                subCommand.paymentType = pay.MethodType;
                                subCommand.PaymentMethodKey = pay.PaymentMethodKey;
                                subCommand.ReturnCode = retvalue;
                                subCommand.ReturnValue = rep.ReturnCodeMessage;

                                var js = Newtonsoft.Json.JsonConvert.SerializeObject(subCommand);
                                LogManagerOrder.SaveOrder(js, "", order.OrderKey.Value.ToString());
                                printResult.Data = rep;
                                printResult.Data.GmpCommandInfo.Add(subCommand);
                                return printResult;
                            }
                            else
                            {

                                var rep = CommandError.CommandErrorMessage(retvalue, ErrorClass.DisplayErrorCodeMessage((uint)retvalue), ErrorClass.DisplayErrorMessage((uint)retvalue));

                                var subCommand = new GmpCommand();

                                subCommand.OrderID = (int)order.OrderID.Value;
                                subCommand.OrderKey = order.OrderKey.Value;
                                subCommand.TransactionKey = Guid.Empty;
                                subCommand.PaymentKey = pay.PaymentKey;
                                subCommand.Command = "prepare_Payment";
                                subCommand.paymentType = pay.MethodType;
                                subCommand.PaymentMethodKey = pay.PaymentMethodKey;

                                subCommand.ReturnCode = retvalue;
                                subCommand.ReturnValue = "TRAN_RESULT_OK [0]";



                                printResult.Data.GmpCommandInfo.Add(subCommand);
                                var js = Newtonsoft.Json.JsonConvert.SerializeObject(subCommand);
                                LogManagerOrder.SaveOrder(js, "", order.OrderKey.Value.ToString());

                                //LogManager.Append($"---------{pay.PaymentName} : OK FP3_Payment");
                                //LogManager.Append($"---------{Newtonsoft.Json.JsonConvert.SerializeObject(rep)} : OK FP3_Payment");
                            }
                        }


                    }
                }
            }

            #endregion

            return printResult;
        }
   
        public static IngenicoApiResponse<GmpPrintReceiptDto> totalsAndPayment(FiscalOrder order, List<GmpCommand> tryCommandList, IngenicoApiResponse<GmpPrintReceiptDto> printResult)
        {

            #region prepare_PrintTotalsAndPayments ara toplam

            try
            {
                if (tryCommandList.Count == 0 || !tryCommandList.Where(x => x.Command == "prepare_PrintTotalsAndPayments" && x.ReturnValue == "TRAN_RESULT_OK [0]").Any())
                {
                    var retcode = GMPSmartDLL.FP3_PrintTotalsAndPayments(DataStore.CurrentInterface, DataStore.ActiveTransactionHandle, DataStore.TIMEOUT_CARD_TRANSACTIONS);
                    if (retcode != Defines.TRAN_RESULT_OK && retcode != Defines.APP_ERR_ALREADY_DONE)
                    {

                        var rep = CommandError.CommandErrorMessage((int)retcode, ErrorClass.DisplayErrorCodeMessage(retcode), ErrorClass.DisplayErrorMessage(retcode));
                        //LogManager.Append($"---------{Newtonsoft.Json.JsonConvert.SerializeObject(rep)} : error FP3_PrintTotalsAndPayments");
                        var subCommand = new GmpCommand();
                        subCommand.OrderID = (int)order.OrderID.Value;
                        subCommand.OrderKey = order.OrderKey.Value;
                        subCommand.TransactionKey = Guid.Empty;
                        subCommand.PaymentKey = Guid.Empty;
                        subCommand.Command = "prepare_PrintTotalsAndPayments";
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
                        // LogManager.Append($"---------{Newtonsoft.Json.JsonConvert.SerializeObject(rep)} : OK FP3_PrintTotalsAndPayments");
                        var subCommand = new GmpCommand();
                        subCommand.OrderID = (int)order.OrderID.Value;
                        subCommand.OrderKey = order.OrderKey.Value;
                        subCommand.TransactionKey = Guid.Empty;
                        subCommand.PaymentKey = Guid.Empty;
                        subCommand.Command = "prepare_PrintTotalsAndPayments";
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
                // LogManager.Append($"---------{Newtonsoft.Json.JsonConvert.SerializeObject(ex)} : FP3_PrintTotalsAndPayments");
                return printResult;
            }

            return printResult;

            #endregion
        }
    }
}
