using Ecr.Module.Services.Ingenico.GmpIngenico;

namespace Ecr.Module.Services.Ingenico.Models
{
    public class GmpPrepareDto
    {
        public TPrepareCommand prepareCommand { get; set; }
        public int bufferLen { get; set; }
        public byte[] buffer { get; set; }
        public ST_INVIOCE_INFO InvoiceInfo { get; set; }
        public GmpCommand gmpCommand { get; set; }
        public GmpPrepareDto()
        {
            bufferLen = 0;
            buffer = new byte[1024];
            InvoiceInfo = new ST_INVIOCE_INFO();
            gmpCommand = new GmpCommand();
        }
    }

    public enum TPrepareCommand
    {
        prepare_ok,
        prepare_error,
        prepare_default,
        prepare_Start,
        prepare_TicketHeader,
        prepare_OptionFlags,
        prepare_ItemSale,
        prepare_VoidItem,
        prepare_Minus,
        prepare_Payment,
        prepare_PrintTotalsAndPayments,
        prepare_PrintUserMessage,
        prepare_PrintBeforeMF,
        prepare_PrintMF,
        prepare_Close,
        prepare_VoidAll,
        FiscalPrinter_MultipleCommand,
        prepare_SetInvoice,
        prepare_VoidPayment,
        prepare_KasaAvans,

        FiscalPrinter_ok,
        FiscalPrinter_error,
        FiscalPrinter_GetHandle,
        FiscalPrinter_Start,
        FiscalPrinter_TicketHeader,
        FiscalPrinter_OptionFlags,
        FiscalPrinter_ItemSale,
        FiscalPrinter_VoidItem,
        FiscalPrinter_Minus,
        FiscalPrinter_Payment,
        FiscalPrinter_PrintTotalsAndPayments,
        FiscalPrinter_PrintBeforeMF,
        FiscalPrinter_PrintMF,
        FiscalPrinter_PrintUserMessage,
        FiscalPrinter_VoidAll,
        FiscalPrinter_VoidPayment,
        FiscalPrinter_SetInvoice,
        FiscalPrinter_GetTicket,
        FiscalPrinter_Close,
        FiscalPrinter_KasaAvans,
        FiscalPrinter_KasaPayment,
        FiscalPrinter_FunctionBankingRefund_Refund,
        FiscalPrinter_FunctionBankingRefund_Void,
        prepare_Plus
    }
}
