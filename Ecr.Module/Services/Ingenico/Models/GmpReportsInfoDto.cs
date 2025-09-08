using Ecr.Module.Services.Ingenico.GmpIngenico;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecr.Module.Services.Ingenico.Models
{
    public class GmpReportsInfoDto
    {
        public uint ReturnCode { get; set; }
        public string ReturnCodeMessage { get; set; }
        public string ReturnMessage { get; set; }

        public ST_EKU_MODULE_INFO EkuInfo { get; set; }
        public GmpReportsInfoDto()
        {
            ReturnCode = 0;
            ReturnCodeMessage = "";
            ReturnMessage = "";
            EkuInfo = new ST_EKU_MODULE_INFO();
        }
    }
}
