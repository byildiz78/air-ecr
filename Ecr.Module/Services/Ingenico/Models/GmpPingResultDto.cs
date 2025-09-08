using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecr.Module.Services.Ingenico.Models
{
    public class GmpPingResultDto
    {
        public uint ReturnCode { get; set; }
        public string ReturnCodeMessage { get; set; }
        public string ReturnStringMessage { get; set; }

        public GmpPingResultDto()
        {
            ReturnCode = 0;
            ReturnCodeMessage = "";
            ReturnStringMessage = "";
        }
    }
}
