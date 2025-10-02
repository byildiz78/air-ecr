using System;
using System.Collections.Generic;

namespace Ecr.Module.Services.Ingenico.Models
{
    public class FiscalOrder
    {

        public string fiscalOrderID { get; set; }
        public string fiscalReceiptNo { get; set; }
        public string customText1 { get; set; }
        public string customText2 { get; set; }
        public string customText3 { get; set; }
        public Guid? OrderKey { get; set; }
        public Guid? ReturnOrderKey { get; set; }
        public long? OrderID { get; set; }
        public double? TotalDiscount { get; set; }
        public bool? IsFiscalOrder { get; set; }
        public string InvoiceNo { get; set; }
        public string TaxOffice { get; set; }
        public string TaxNumber { get; set; }
        public bool? IsEInvoice { get; set; }

        public long? InvoiceTypeID { get; set; }
        public bool? PrintInvoice { get; set; }

        public long? ReturnTypeID { get; set; }
        public bool? IsReturnMode { get; set; }

        public long? FiscalTypeID { get; set; }
        public bool? IsVoidedFiscal { get; set; }
        public bool? OpenCashDrawer { get; set; }
        public string CashierName { get; set; }
        public string BranchID { get; set; }
        public string DatabaseName { get; set; }
        public double? AmountDue { get; set; }
        public DateTime? OrderDateTime { get; set; }
        public bool isGlobal { get; set; }

        public List<FiscalOrderLine> fiscalLines { get; set; }
        public List<FiscalPaymentLine> paymentLines { get; set; }

        public FiscalOrder()
        {
            fiscalLines = new List<FiscalOrderLine>();
            paymentLines = new List<FiscalPaymentLine>();
        }

    }

    public class FiscalOrderLine
    {
        public Guid? OrderLineKey { get; set; }
        public long? itemID { get; set; }
        public long? ArtGroup { get; set; }
        public string ProductGroup { get; set; }
        public string ProductName { get; set; }
        public string Barcode { get; set; }
        public double? UnitPrice { get; set; }
        public double? SaleQuantity { get; set; }
        public string UnitName { get; set; }
        public string CustomField1 { get; set; }

        public long? TaxGroupID { get; set; }
        public double? TaxPercent { get; set; }
        public long? VoidLineID { get; set; }

        public bool BarcodeUsed { get; set; }
        public long? DepartmentID { get; set; }
        public Guid? VoidOrderLineKey { get; set; }

        public long LineOrderID { get; set; } //Satır sıra no
        public bool PromotionUsed { get; set; } //Promosyonda kullanıldı mı ve ise indirimi var demektir.
        public long? PromotionTypeID { get; set; } //PromotionTypeID =0, tutar indirimi
        public double? PromotionAmount { get; set; } //indirim tutarı
        public string PromotionName { get; set; } //adı
        public string PromotionText { get; set; } //açıklama metni
        public string PromotionCode { get; set; } //kodu

        public FiscalOrderLine()
        {
            itemID = 0; ArtGroup = 0; ProductGroup = ""; ProductName = ""; Barcode = "";
            UnitPrice = 0; SaleQuantity = 0; UnitName = ""; TaxGroupID = 0; VoidLineID = 0; CustomField1 = "";
            OrderLineKey = Guid.Empty; BarcodeUsed = false; DepartmentID = 0; TaxPercent = 0; VoidOrderLineKey = Guid.Empty;
            PromotionUsed = false; PromotionTypeID = 0; PromotionAmount = 0; PromotionName = ""; PromotionCode = ""; PromotionText = ""; LineOrderID = 0;
        }

        public FiscalOrderLine(string name, string prodGroup, long id, string unitName, double saleQuantiy, double unitPrice, long taxGroupID)
        {
            ProductName = name;
            ProductGroup = prodGroup;
            itemID = id;
            UnitName = unitName;
            SaleQuantity = saleQuantiy;
            UnitPrice = unitPrice;
            TaxGroupID = taxGroupID;
        }
    }

    public class FiscalPaymentLine
    {
        public long? PaymentTypeID { get; set; }
        public string PaymentName { get; set; }
        public double? PaymentAmount { get; set; }
        public bool? HasChange { get; set; }
        public long? PaymentBaseTypeID { get; set; }
        public Guid? PaymentKey { get; set; }
        public Guid? PaymentMethodKey { get; set; }
        public bool? IsPaid { get; set; }
        public double? PaymentOver { get; set; }

        public bool? UseBankInstallment { get; set; } //taksit var ise true geliyor
        public string BankCode { get; set; }  //u16BKMId banka kodu
        public long? BankInstallmentCount { get; set; } //taksit sayısı


        public string ReturnTerminalNo { get; set; }
        public string ReturnBatchNo { get; set; }
        public string ReturnStanNo { get; set; }
        public long? ReturnInstallment { get; set; }
        public string ReturnMerchantID { get; set; }
        public double? ReturnTranAmount { get; set; }
        public double? ReturnOrderAmount { get; set; }
        public string ReturnTranRefCode { get; set; }
        public string ReturnAuthCode { get; set; }
        public string ReturnRefRRN { get; set; }
        public double? ReturnLoyaltyAmount { get; set; }
        public double? ReturnLoyaltyRefundAmount { get; set; }
        public string ReturnOriginalData { get; set; }
        public string CardNo { get; set; }
        public bool IsVoidMode { get; set; }

        public PaymentMethodType MethodType { get; set; }

        public DateTime PaymentDateTime { get; set; }
        public string DeleteReason { get; set; }
        public FiscalPaymentLine()
        {
            PaymentTypeID = 0; PaymentName = ""; PaymentAmount = 0; HasChange = false; PaymentOver = 0;
            PaymentBaseTypeID = 0; PaymentKey = Guid.Empty; IsPaid = false; UseBankInstallment = false; BankCode = ""; BankInstallmentCount = 1;

            ReturnTerminalNo = ""; ReturnBatchNo = ""; ReturnStanNo = "";
            ReturnMerchantID = ""; ReturnTranAmount = 0; ReturnAuthCode = "";
            ReturnRefRRN = ""; ReturnInstallment = 0; ReturnInstallment = 0; ReturnLoyaltyRefundAmount = 0; CardNo = ""; ReturnOrderAmount = 0;
            IsVoidMode = false;
            PaymentMethodKey = Guid.Empty; MethodType = PaymentMethodType.Cash; PaymentDateTime = DateTime.Now; DeleteReason = "";
        }

        public FiscalPaymentLine(int paymentTypeID, string paymentName, double paymentAmount, bool hasChange, int paymentBaseTypeID)
        {
            PaymentTypeID = paymentTypeID;
            PaymentName = paymentName;
            PaymentAmount = paymentAmount;
            HasChange = hasChange;
            PaymentBaseTypeID = paymentBaseTypeID;
            IsPaid = false;
            PaymentOver = 0;
        }
    }
}
