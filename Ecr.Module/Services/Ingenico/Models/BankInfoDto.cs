using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecr.Module.Services.Ingenico.Models
{
    public class BankInfoDto
    {
        public string Name { get; set; }
        public string u16BKMId { get; set; }
        public string u16AppId { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public BankInfoDto()
        {
            Name = ""; u16BKMId = ""; u16AppId = ""; Status = ""; Priority = "";
        }
    }
}
