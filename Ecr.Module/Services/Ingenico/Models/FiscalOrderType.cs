using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecr.Module.Services.Ingenico.Models
{
    public static class FiscalOrderType
    {
        public static FiscalType GetFiscalOrderType(FiscalOrder order)
        {
            FiscalType fiscalType = FiscalType.Normal;

            if (order.IsReturnMode.Value)
            {
                fiscalType = FiscalType.Return;
            }
            else if (order.IsVoidedFiscal.Value)
            {
                fiscalType = FiscalType.Void;
            }
            else
            {
                fiscalType = FiscalType.Normal;
            }
            return fiscalType;
        }
    }
}
