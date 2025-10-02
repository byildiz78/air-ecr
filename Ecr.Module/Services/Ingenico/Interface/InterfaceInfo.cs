namespace Ecr.Module.Services.Ingenico.Interface
{
    /// <summary>
    /// Interface bilgileri
    /// </summary>
    public class InterfaceInfo
    {
        public uint Handle { get; set; }
        public string InterfaceId { get; set; }
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }

        public InterfaceInfo()
        {
            Handle = 0;
            InterfaceId = string.Empty;
            IsValid = false;
            ErrorMessage = string.Empty;
        }

        public override string ToString()
        {
            return $"Handle: {Handle:X8}, ID: {InterfaceId}, Valid: {IsValid}";
        }
    }
}