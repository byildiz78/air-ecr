using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecr.Module.Services.Ingenico.Models
{
    public class CommandDetail
    {
        public string CommandName { get; set; }
        public string CommandStatus { get; set; }
        public string Orderkey { get; set; }
        public string PaymentKey { get; set; }
    }
}
