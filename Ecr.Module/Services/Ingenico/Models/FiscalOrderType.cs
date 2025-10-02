namespace Ecr.Module.Services.Ingenico.Models
{
    public static class FiscalOrderType
    {
        public static FiscalType GetFiscalOrderType(FiscalOrder order)
        {
            if (order.IsReturnMode.Value)
            {
                return FiscalType.Return;
            }
            else if (order.IsVoidedFiscal.Value)
            {
                return FiscalType.Void;
            }

            return FiscalType.Normal;
        }
    }
}
