using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecr.Module.Services.Ingenico.Models
{
    public enum FiscalType
    {
        Normal = 1,
        Return = 2,
        Void = 3
    }

    public enum FiscalReturnType
    {
        Cash = 1,
        CreditCard = 2,
        CashAndCreditCard = 3
    }

    public enum FiscalUniqueState
    {
        None = 0,
        Equal = 1,
        OrderDateTimeNotEqual = 2,
        OrderIDNotEqual = 3,
        EcrSerialNumberNotEqual = 4,
        EkuNoNotEqual = 5,
        ZNoNotEqual = 6,
        FNoNotEqual = 7,
        AmountDueNotEqual = 8,
        TransactionCountNotEqual = 9
    }
}
