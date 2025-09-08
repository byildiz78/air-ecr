using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecr.Module.Services.Ingenico.Models
{
    public class ReportRequest
    {
        public ReportType ReportType { get; set; }
        public string Zno { get; set; }
        public string startZno { get; set; }
        public string lastZno { get; set; }
        public string startDate { get; set; }
        public string lastDate { get; set; }
    }
}
