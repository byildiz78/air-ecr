using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecr.Module.Services.Ingenico.Models
{
    public class VersionInfo
    {
        public string ModuleVersion { get; set; }
        public string m_dllVersion { get; set; }
        public string DLL_VERSION_MIN { get; set; }
        public string EcrSerialNumber { get; set; }
        public string gmpVersion { get; set; }
        public DateTime UpdateDateTime { get; set; }
        public VersionInfo()
        {
            ModuleVersion = "";
            m_dllVersion = "";
            DLL_VERSION_MIN = "";
            EcrSerialNumber = "";
            gmpVersion = "";
            UpdateDateTime = DateTime.Now;
        }
    }
}
