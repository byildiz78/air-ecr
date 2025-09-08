using System.Collections.Generic;

namespace Ecr.Module.Services.Ingenico.Models
{
    public class GmpInfoDto
    {
        public uint CurrentInterface { get; set; }
        public string ModuleVersion { get; set; }
        public string ActiveCashier { get; set; }
        public int ActiveCashierNo { get; set; }
        public byte[] m_dllVersion { get; set; }
        public string DLL_VERSION_MIN { get; set; }
        public string EcrSerialNumber { get; set; }
        public bool EftPosIsConnected { get; set; } 
        public int EcrStatus { get; set; }
        public byte ecrMode { get; set; }
        public FiscalHeaderDto fiscalHeader { get; set; }
        public List<BankInfoDto> BankInfoList { get; set; }
        public VersionInfo Versions { get; set; }
        public TaxGroupsInfoDto TaxGroupsInfos { get; set; }
        public string gmpVersion { get; set; }

        public GmpInfoDto()
        {
            ModuleVersion = "";
            ActiveCashier = "";
            ActiveCashierNo = 0;
            m_dllVersion = new byte[24];
            DLL_VERSION_MIN = "";
            EcrSerialNumber = "";
            EftPosIsConnected = false;
            ecrMode = 0;
            BankInfoList = new List<BankInfoDto>();
            fiscalHeader = new FiscalHeaderDto();
            gmpVersion = "";
            Versions = new VersionInfo();
            TaxGroupsInfos = new TaxGroupsInfoDto();
        }
    }
}
