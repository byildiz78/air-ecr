namespace Ecr.Module.Services.Ingenico.Connection
{
    /// <summary>
    /// Error detay bilgileri
    /// </summary>
    public class ConnectionErrorInfo
    {
        public uint ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorCodeMessage { get; set; }
        public ConnectionErrorCategory Category { get; set; }
        public bool RequiresReconnection { get; set; }
        public bool RequiresPairing { get; set; }
        public bool RequiresUserAction { get; set; }
        public string UserActionMessage { get; set; }

        public ConnectionErrorInfo()
        {
            ErrorMessage = string.Empty;
            ErrorCodeMessage = string.Empty;
            UserActionMessage = string.Empty;
            Category = ConnectionErrorCategory.Unknown;
        }
    }
}