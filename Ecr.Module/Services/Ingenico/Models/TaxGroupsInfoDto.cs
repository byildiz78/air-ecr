using Ecr.Module.Services.Ingenico.GmpIngenico;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecr.Module.Services.Ingenico.Models
{
    public class TaxGroupsInfoDto
    {
        public uint ReturnCode { get; set; }
        public string ReturnCodeMessage { get; set; }
        public string ReturnMessage { get; set; }
        public List<TaxGroups> taxGroupList { get; set; }

        public List<ST_TAX_RATE> taxRate { get; set; }

        public TaxGroupsInfoDto()
        {
            ReturnCode = 0;
            ReturnCodeMessage = "";
            ReturnMessage = "";
            taxGroupList = new List<TaxGroups>();
            taxRate = new List<ST_TAX_RATE>();
        }
    }

    public class TaxGroups
    {
        public string GroupName { get; set; }
        public int TaxRate { get; set; }
        public string GmpTags { get; set; }
        public int DisplayIndex { get; set; }

        public int TaxIndex { get; set; }
        public string ingenico { get; set; }
        public int TaxGroupID { get; set; }
        public TaxGroups()
        {
            TaxGroupID = 0;
            GroupName = "";
            TaxRate = 0;
            TaxIndex = 0;
            GmpTags = "";
            DisplayIndex = 0;
        }
    }
}
