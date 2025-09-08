namespace Ecr.Module.Services.Ingenico.Models
{
    public class GmpPairingDto
    {
        public uint ReturnCode { get; set; }
        public string ReturnCodeMessage { get; set; }
        public string ReturnMessage { get; set; }
        public GmpInfoDto GmpInfo { get; set; }

        public GmpPairingDto()
        {
            ReturnCode = 0;
            ReturnMessage = ""; 
            GmpInfo = new GmpInfoDto();
        }
    }
}
