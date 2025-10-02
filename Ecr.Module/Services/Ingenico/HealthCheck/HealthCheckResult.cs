using Ecr.Module.Services.Ingenico.GmpIngenico;
using System;

namespace Ecr.Module.Services.Ingenico.HealthCheck
{
    /// <summary>
    /// Health check sonucu
    /// </summary>
    public class HealthCheckResult
    {
        /// <summary>
        /// Check başarılı mı?
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// Check zamanı
        /// </summary>
        public DateTime CheckTime { get; set; }

        /// <summary>
        /// Check süresi (ms)
        /// </summary>
        public long DurationMs { get; set; }

        /// <summary>
        /// Kullanılan strateji
        /// </summary>
        public HealthCheckStrategy Strategy { get; set; }

        /// <summary>
        /// PING sonucu
        /// </summary>
        public PingCheckResult PingResult { get; set; }

        /// <summary>
        /// ECHO sonucu
        /// </summary>
        public EchoCheckResult EchoResult { get; set; }

        /// <summary>
        /// Error kodu (varsa)
        /// </summary>
        public uint ErrorCode { get; set; }

        /// <summary>
        /// Error mesajı
        /// </summary>
        public string ErrorMessage { get; set; }

        public HealthCheckResult()
        {
            IsHealthy = false;
            CheckTime = DateTime.Now;
            DurationMs = 0;
            ErrorMessage = string.Empty;
        }
    }

    /// <summary>
    /// PING check sonucu
    /// </summary>
    public class PingCheckResult
    {
        public bool Success { get; set; }
        public uint ReturnCode { get; set; }
        public long DurationMs { get; set; }
        public string Message { get; set; }

        public PingCheckResult()
        {
            Success = false;
            Message = string.Empty;
        }
    }

    /// <summary>
    /// ECHO check sonucu
    /// </summary>
    public class EchoCheckResult
    {
        public bool Success { get; set; }
        public uint ReturnCode { get; set; }
        public long DurationMs { get; set; }
        public ST_ECHO? EchoData { get; set; }
        public string Message { get; set; }

        // ECHO'dan gelen bilgiler
        public string ActiveCashier { get; set; }
        public int ActiveCashierNo { get; set; }
        public int EcrStatus { get; set; }
        public byte EcrMode { get; set; }

        public EchoCheckResult()
        {
            Success = false;
            Message = string.Empty;
            ActiveCashier = string.Empty;
        }
    }
}