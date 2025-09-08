using Ecr.Module.Services.Ingenico.GmpIngenico;
using Ecr.Module.Services.Ingenico.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecr.Module.Services.Ingenico.Helper
{
    public static class CommandHelperGmpProvider
    {
        public static string BufferConvertData(byte[] buffer, int bufferLen)
        {
            string _bufferData;
            try
            {
                int commandType = Defines.GMP3_FISCAL_PRINTER_MODE_REQ;
                byte[] dataPtr = new byte[bufferLen + 6];
                byte[] type = new byte[4];
                int typeLen = 0;
                StringToByteArray(commandType.ToString("X2"), type, ref typeLen);
                Buffer.BlockCopy(type, 0, dataPtr, 0, 4);

                byte[] bufLen = new byte[2];
                int bufLenLen = 0;
                StringToByteArray(bufferLen.ToString("X2"), bufLen, ref bufLenLen);
                Buffer.BlockCopy(bufLen, 0, dataPtr, 4, 2);

                Buffer.BlockCopy(buffer, 0, dataPtr, 6, bufferLen);

                _bufferData = ByteArrayToString(dataPtr, bufferLen + 6);

            }
            catch (Exception ex)
            {
                _bufferData = "";
                //LogManager.Exception(ex, "FiscalModule.Ingenico.GmpProvider.PrintReceipts.BufferConvertData");
            }
            return _bufferData;
        }

        public static void StringToByteArray(string s, byte[] Out_byteArr, ref int Out_byteArrLen)
        {
            byte[] ba = new byte[s.Length / 2];
            for (int i = 0; i < ba.Length; i++)
            {
                string temp = s.Substring(i * 2, 2);
                ba[ba.Length - 1 - i] = Convert.ToByte(temp, 16);
            }
            Out_byteArrLen = ba.Length;
            Array.Copy(ba, 0, Out_byteArr, 0, ba.Length);
        }

        public static string ByteArrayToString(byte[] buffer, int bufferLen)
        {
            string str = "";
            for (int i = 0; i < bufferLen; i++)
            {
                str += buffer[i].ToString("X2");
            }
            return str;
        }
        public static void StringToByteArray_Rev(string s, byte[] Out_byteArr, ref int Out_byteArrLen)
        {
            byte[] ba = new byte[s.Length / 2];
            for (int i = 0; i < ba.Length; i++)
            {
                string temp = s.Substring(i * 2, 2);
                ba[i] = Convert.ToByte(temp, 16);
            }
            Out_byteArrLen = ba.Length;
            Array.Copy(ba, 0, Out_byteArr, 0, ba.Length);
        }

        public static void ConvertAscToBcdArray(string str, ref byte[] arr, int arrLen)
        {
            arrLen = str.Length;
            Array.Copy(Encoding.Default.GetBytes(str), 0, arr, 0, str.Length);
        }

        public static void ConvertStringToHexArray(string s, ref byte[] Out_byteArr, int byteArrLen)
        {

            byte[] ba = new byte[s.Length / 2];
            for (int i = 0; i < ba.Length; i++)
            {
                string temp = s.Substring(i * 2, 2);
                ba[i] = Convert.ToByte(temp, 16);
            }
            byteArrLen = ba.Length;
            Array.Copy(ba, 0, Out_byteArr, 0, ba.Length);
        }

        public static string ConvertByteArrayToString(byte[] byteArray, int byteArrayLen)
        {
            string str = "";

            try
            {
                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < byteArrayLen; i++)
                {
                    sb.Append(byteArray[i].ToString("X2"));
                }
                return str = sb.ToString();
            }
            catch
            {
                return "0";
            }
        }

        public static double DoubleFormat(double number, int digit)
        {
            return Math.Round(number * Math.Pow(10, digit), digit);
        }

        public static IEnumerable<List<T>> InSetsOf<T>(IEnumerable<T> source, int max)
        {
            List<T> toReturn = new List<T>(max);
            foreach (var item in source)
            {
                toReturn.Add(item);
                if (toReturn.Count == max)
                {
                    yield return toReturn;
                    toReturn = new List<T>(max);
                }
            }
            if (toReturn.Any())
            {
                yield return toReturn;
            }
        }

        public static ReceiptData ReceiptJsonDataHeader(ST_TICKET ticket, bool IsOrder, FiscalOrder order = null)
        {

            //LogManager.Append("ReceiptJsonDataHeader çalıştırılıyor..");

            ReceiptData data = new ReceiptData();

            data.headerList = new List<headers>();

            try
            {
                headers header = new headers();
                header.ActiveCashierNo = DataStore.gmpResult.GmpInfo.ActiveCashierNo;
                header.ActiveCashier = DataStore.gmpResult.GmpInfo.ActiveCashier;
                header.date = order != null ? order.OrderDateTime.Value : DateTime.Now;
                header.EcrNo = DataStore.gmpResult.GmpInfo.EcrSerialNumber.Trim();
                header.EJNo = ticket.EJNo;
                header.ZNo = ticket.ZNo;
                header.FNo = ticket.FNo;
                header.status = DataStore.gmpResult.GmpInfo.EcrStatus;
                header.ecrMode = DataStore.gmpResult.GmpInfo.ecrMode;

                string str_uniqueID = "";
                try
                {
                    for (int m = 0; m < 24; m++)
                    {
                        str_uniqueID += ticket.uniqueId[m].ToString("X2");
                    }
                }
                catch
                {
                    str_uniqueID = "";
                }

                header.UniqueID = str_uniqueID;

                DataStore.MergeUniqueID = string.Format("{0:ddMMyyyyHHmmss}", order != null ? order.OrderDateTime.Value : DateTime.Now) + "00" +
                                                 DataStore.OrderID.ToString() + "" +
                                                 ticket.FNo.ToString().PadLeft(4, '0');


                header.MergeUniqueID = DataStore.MergeUniqueID;
                header.TotalAmount = (int)(ticket.TotalReceiptAmount + ticket.KasaAvansAmount + ticket.KatkiPayiAmount);
                header.CashBackAmount = (int)ticket.CashBackAmount;

                if (IsOrder == false)
                {
                    if (ticket.stPayment != null)
                    {
                        foreach (var item in ticket.stPayment)
                        {
                            if (item != null)
                            {
                                if (item.payAmount > 0)
                                {
                                    payments payment = new payments();
                                    payment.IsVoidMode = false;
                                    payment.cashBackAmountInDoviz = (int)item.cashBackAmountInDoviz;
                                    payment.cashBackAmountInTL = (int)item.cashBackAmountInTL;
                                    payment.dateOfPayment = (int)item.dateOfPayment;
                                    payment.orgAmount = (int)item.orgAmount;
                                    payment.orgAmountCurrencyCode = item.orgAmountCurrencyCode;
                                    payment.payAmount = (int)item.payAmount;
                                    payment.payAmountCurrencyCode = item.payAmountCurrencyCode;
                                    payment.subtypeOfPayment = item.subtypeOfPayment;
                                    payment.typeOfPayment = (int)item.typeOfPayment;

                                    payment.stBankPayment = new BANK_PAYMENT_INFO
                                    {
                                        authorizeCode = item.stBankPayment.authorizeCode,
                                        balance = (int)item.stBankPayment.balance,
                                        bankBkmId = item.stBankPayment.bankBkmId,
                                        bankName = item.stBankPayment.bankName,
                                        batchNo = (int)item.stBankPayment.batchNo,
                                        merchantId = item.stBankPayment.merchantId,
                                        numberOfbonus = item.stBankPayment.numberOfbonus,
                                        numberOfdiscount = item.stBankPayment.numberOfdiscount,
                                        numberOferrorMessage = item.stBankPayment.numberOferrorMessage,
                                        numberOfInstallments = item.stBankPayment.numberOfInstallments,
                                        numberOfsubPayment = item.stBankPayment.numberOfsubPayment,
                                        rrn = item.stBankPayment.rrn,
                                        stan = (int)item.stBankPayment.stan,
                                        stBankSubPaymentInfo = item.stBankPayment.stBankSubPaymentInfo,
                                        stCard = item.stBankPayment.stCard,
                                        stPaymentErrMessage = item.stBankPayment.stPaymentErrMessage,
                                        terminalId = item.stBankPayment.terminalId
                                    };

                                    header.paymentList.Add(payment);
                                }
                            }
                        }
                    }
                }
                data.headerList.Add(header);


            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "ReceiptJsonDataHeader");
            }

            return data;
        }

    }
}
