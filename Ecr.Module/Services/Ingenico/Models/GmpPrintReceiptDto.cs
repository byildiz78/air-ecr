using Ecr.Module.Services.Ingenico.GmpIngenico;
using System.Collections.Generic;

namespace Ecr.Module.Services.Ingenico.Models
{
    public class GmpPrintReceiptDto
    {
        public uint ReturnCode { get; set; }
        public string ReturnCodeMessage { get; set; }
        public string ReturnStringMessage { get; set; }
        public string GmpCommandName { get; set; }
        public string FiscalUniqueKey { get; set; }
        public string FiscalGmpUniqueKey { get; set; }
        public ReceiptData PrintReceiptInfo { get; set; }
        public ST_TICKET TicketInfo { get; set; }
        public List<GmpCommand> GmpCommandInfo { get; set; }
        public GmpPrintReceiptDto()
        {
            ReturnCode = 0;
            ReturnCodeMessage = "";
            PrintReceiptInfo = new ReceiptData();
            TicketInfo = new ST_TICKET();
            GmpCommandInfo = new List<GmpCommand>();
            FiscalUniqueKey = "";
            FiscalGmpUniqueKey = "";
        }
    }
}
