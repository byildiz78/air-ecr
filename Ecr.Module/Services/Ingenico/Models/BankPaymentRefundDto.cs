using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecr.Module.Services.Ingenico.Models
{
    public class BankPaymentRefundDto
    {
        public Guid PaymentId { get; set; }
        public double PaidAmount { get; set; }
        public string ReturnTerminalNo { get; set; }
        public int ReturnBatchNo { get; set; }
        public int ReturnStanNo { get; set; }
        public int BankCode { get; set; }
        public int BankInstallmentCount { get; set; }
        public string ReturnMerchantID { get; set; }

        public string ReturnTranRefCode { get; set; }

        public string ReturnAuthCode { get; set; }

        public string ReturnOriginalData { get; set; }

        public double TransAmount { get; set; }
        public BankPaymentRefundDto()
        {

            PaymentId = Guid.Empty;
            PaidAmount = 0;
            ReturnTerminalNo = "";
            ReturnBatchNo = 0;
            ReturnStanNo = 0;
            BankCode = 0;
            BankInstallmentCount = 0;
            ReturnMerchantID = "";
            ReturnAuthCode = "";
            ReturnTranRefCode = "";
            ReturnOriginalData = "";
            TransAmount = 0;
        }

    }
}
