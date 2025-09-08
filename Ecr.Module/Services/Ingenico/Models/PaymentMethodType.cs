namespace Ecr.Module.Services.Ingenico.Models
{
    public enum PaymentMethodType
    {
        None = 0,
        Visa = 1,
        GiftCard = 2,
        Point = 3,
        Ibb = 4,
        YemekCeki = 5,
        BankaTransfer = 6,
        AcikHesap = 7,
        Multinet = 8,
        Ticket = 9,
        Sodexo = 10,
        MetropolCard = 11,
        Cash = 12,
        Paye = 13,
        Setcard = 14
    }

    public enum OrderType
    {
        None = 0,
        Normal = 1,
        Return = 2,
        Invoice = 3,
        GiftCard = 4
    }
}
