using Ecr.Module.Services.Ingenico.GmpIngenico;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Ecr.Module.Services.Ingenico.Models
{
    public class ReceiptData
    {
        public List<headers> headerList { get; set; }

        [JsonConstructor]
        public ReceiptData()
        {
            headerList = new List<headers>();
        }
    }

    public class headers
    {
        public int status { get; set; }
        public byte ecrMode { get; set; }
        public int ZNo { get; set; }
        public int FNo { get; set; }
        public string UniqueID { get; set; }
        public string MergeUniqueID { get; set; }
        public int EJNo { get; set; }
        public string EcrNo { get; set; }
        public DateTime date { get; set; }
        public string ActiveCashier { get; set; }
        public int ActiveCashierNo { get; set; }
        public int TotalAmount { get; set; }
        public int CashBackAmount { get; set; }

        public List<payments> paymentList { get; set; }


        public headers()
        {
            status = 0;
            ecrMode = 0;
            ZNo = 0;
            FNo = 0;
            EJNo = 0;
            EcrNo = "";
            ActiveCashier = "";
            ActiveCashierNo = 0;
            UniqueID = "";
            MergeUniqueID = "";
            date = DateTime.Now;
            TotalAmount = 0;
            CashBackAmount = 0;
            paymentList = new List<payments>();
        }
    }

    public class payments
    {
        //public byte flag { get; set; }
        public int dateOfPayment { get; set; }
        public int typeOfPayment { get; set; }
        public byte subtypeOfPayment { get; set; }
        public int orgAmount { get; set; }
        public int orgAmountCurrencyCode { get; set; }        // as defined in currecyTable from GIB
        public int payAmount { get; set; }                   // always TL with precision 2
        public int payAmountCurrencyCode { get; set; }        // always TL
        public int cashBackAmountInTL { get; set; }           // Para üstü, her zaman TL with precision 2
        public int cashBackAmountInDoviz { get; set; }        // Para Üstü, döviz satış ise döviz karşılığı
        public bool IsVoidMode { get; set; }                                                 //  
        public BANK_PAYMENT_INFO stBankPayment { get; set; }  // Keeps all payment info related with bank

        public payments()
        {
            //flag = 0;
            dateOfPayment = 0;
            typeOfPayment = 0;
            subtypeOfPayment = 0;
            orgAmount = 0;
            orgAmountCurrencyCode = 0;
            payAmount = 0;
            payAmountCurrencyCode = 0;
            cashBackAmountInTL = 0;
            cashBackAmountInDoviz = 0;
            IsVoidMode = false;
            stBankPayment = new BANK_PAYMENT_INFO();
        }

    }

    public class BANK_PAYMENT_INFO
    {
        public int batchNo;
        public int stan;
        public int balance;
        public int bankBkmId;
        public byte numberOfdiscount;
        public byte numberOfbonus;
        public string authorizeCode;
        public string terminalId;
        public string rrn;
        public string merchantId;
        public string bankName;
        public byte numberOfInstallments;
        public byte numberOfsubPayment;
        public byte numberOferrorMessage;
        public ST_BankSubPaymentInfo[] stBankSubPaymentInfo;
        public ST_CARD_INFO stCard;
        public ST_PaymentErrMessage stPaymentErrMessage;

        public BANK_PAYMENT_INFO()
        {
            batchNo = 0;
            stan = 0;
            balance = 0;
            bankBkmId = 0;
            numberOfdiscount = 0;
            numberOfbonus = 0;
            authorizeCode = "";
            terminalId = "";
            rrn = "";
            merchantId = "";
            bankName = "";
            numberOfInstallments = 0;
            numberOfsubPayment = 0;
            numberOferrorMessage = 0;
            stBankSubPaymentInfo = new ST_BankSubPaymentInfo[12];
            stCard = new ST_CARD_INFO();
            stPaymentErrMessage = new ST_PaymentErrMessage();
        }
    }
}
