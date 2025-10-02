using Ecr.Module.Services.Ingenico.Models;
using System;

namespace Ecr.Module.Services.Ingenico.Connection
{
    /// <summary>
    /// Connection durumu ve metadata
    /// </summary>
    public class ConnectionState
    {
        public ConnectionStatus Status { get; set; }
        public uint CurrentInterface { get; set; }
        public DateTime LastSuccessfulConnection { get; set; }
        public DateTime LastConnectionAttempt { get; set; }
        public int ConsecutiveFailureCount { get; set; }
        public uint LastErrorCode { get; set; }
        public string LastErrorMessage { get; set; }
        public bool IsPaired { get; set; }
        public string EcrSerialNumber { get; set; }

        public ConnectionState()
        {
            Status = ConnectionStatus.NotConnected;
            CurrentInterface = 0;
            LastSuccessfulConnection = DateTime.MinValue;
            LastConnectionAttempt = DateTime.MinValue;
            ConsecutiveFailureCount = 0;
            LastErrorCode = 0;
            LastErrorMessage = string.Empty;
            IsPaired = false;
            EcrSerialNumber = string.Empty;
        }

        /// <summary>
        /// Connection sağlıklı mı?
        /// </summary>
        public bool IsHealthy()
        {
            return Status == ConnectionStatus.Connected &&
                   CurrentInterface > 0 &&
                   IsPaired &&
                   ConsecutiveFailureCount == 0;
        }

        /// <summary>
        /// Reconnection gerekli mi?
        /// </summary>
        public bool NeedsReconnection()
        {
            return Status == ConnectionStatus.NotConnected ||
                   CurrentInterface == 0 ||
                   !IsPaired;
        }
    }
}