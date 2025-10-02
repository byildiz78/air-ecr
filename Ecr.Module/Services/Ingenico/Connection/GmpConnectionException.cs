using System;

namespace Ecr.Module.Services.Ingenico.Connection
{
    /// <summary>
    /// GMP Connection hatası için özel exception
    /// </summary>
    public class GmpConnectionException : Exception
    {
        public ConnectionErrorInfo ErrorInfo { get; private set; }

        public GmpConnectionException(ConnectionErrorInfo errorInfo)
            : base(errorInfo.ErrorMessage)
        {
            ErrorInfo = errorInfo;
        }

        public GmpConnectionException(ConnectionErrorInfo errorInfo, Exception innerException)
            : base(errorInfo.ErrorMessage, innerException)
        {
            ErrorInfo = errorInfo;
        }

        public override string ToString()
        {
            return $"GmpConnectionException: {ErrorInfo.ErrorCodeMessage} - {ErrorInfo.ErrorMessage} (Category: {ErrorInfo.Category})";
        }
    }
}