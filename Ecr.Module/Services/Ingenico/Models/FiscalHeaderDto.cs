namespace Ecr.Module.Services.Ingenico.Models
{
    public class FiscalHeaderDto
    {
        public uint ReturnCode { get; set; }
        public string ReturnCodeMessage { get; set; }
        public string ReturnStringMessage { get; set; }
        public string ReceiptTitle1 { get; set; }
        public string ReceiptTitle2 { get; set; }
        public string ReceiptAdres { get; set; }
        public string ReceiptAdres2 { get; set; }
        public string ReceiptAdres3 { get; set; }
        public string MersisNo { get; set; }
        public string TicariSicilNo { get; set; }
        public string VATNumber { get; set; }
        public string VATOffice { get; set; }
        public string WebAddress { get; set; }


        public FiscalHeaderDto()
        {
            ReturnCode = 0;
            ReturnCodeMessage = "";
            ReturnStringMessage = "";
            ReceiptTitle1 = "";
            ReceiptTitle2 = "";
            ReceiptAdres = "";
            ReceiptAdres2 = "";
            ReceiptAdres3 = "";
            MersisNo = "";
            TicariSicilNo = "";
            VATNumber = "";
            VATOffice = "";
            WebAddress = "";
        }
    }
}
