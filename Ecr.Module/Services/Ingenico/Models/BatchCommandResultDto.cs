using Ecr.Module.Services.Ingenico.GmpIngenico;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecr.Module.Services.Ingenico.Models
{
    public class BatchCommandResultDto
    {
        public uint ReturnCode { get; set; }
        public string ReturnMessage { get; set; }

        public List<GmpCommand> GmpCommandInfo { get; set; }
        public ST_TICKET ReceiptInfo { get; set; }
        public BatchCommandResultDto()
        {
            ReturnCode = 9999;
            ReturnMessage = "";
            GmpCommandInfo = new List<GmpCommand>();
            ReceiptInfo = new ST_TICKET();
        }
    }
}
