using System;

namespace Ecr.Module.Services.Ingenico.Models
{
    public class GmpCommand
    {
        public int AutoID { get; set; }
        public int OrderID { get; set; }

        public Guid OrderKey { get; set; }

        public string Command { get; set; }

        public int SubCommand { get; set; }

        public int SubCommandIndex { get; set; }

        public string BufferData { get; set; }

        public int ReturnCode { get; set; }

        public string ReturnValue { get; set; }

        public Guid? PaymentKey { get; set; }

        public Guid? TransactionKey { get; set; }

        public PaymentMethodType paymentType { get; set; }

        public Guid? PaymentMethodKey { get; set; }

        public GmpPrintReceiptDto printDetail { get; set; }
        public GmpCommand()
        {
            AutoID = 0;
            OrderID = 0;
            ReturnCode = 0;
            ReturnValue = "";
            OrderKey = Guid.Empty;
            TransactionKey = Guid.Empty;
            PaymentKey = Guid.Empty;
            BufferData = "";
            SubCommandIndex = 0;
            SubCommand = 0;
            Command = "";
            paymentType = PaymentMethodType.None;
            PaymentMethodKey = Guid.Empty;
            printDetail = new GmpPrintReceiptDto();
        }
    }
}
