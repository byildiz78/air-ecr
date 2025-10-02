using System;

namespace Ecr.Module.Services.Ingenico.Retry
{
    /// <summary>
    /// Retry politikası ayarları
    /// </summary>
    public class RetryPolicy
    {
        /// <summary>
        /// Maximum retry sayısı (PDF'e göre 3 önerilir)
        /// </summary>
        public int MaxRetryCount { get; set; } = 3;

        /// <summary>
        /// İlk retry'dan önce bekleme süresi (ms)
        /// </summary>
        public int InitialDelayMs { get; set; } = 500;

        /// <summary>
        /// Progressive delay kullan mı? (her retry'da delay artar)
        /// </summary>
        public bool UseProgressiveDelay { get; set; } = true;

        /// <summary>
        /// Progressive delay multiplier
        /// </summary>
        public double DelayMultiplier { get; set; } = 2.0;

        /// <summary>
        /// Maximum delay süresi (ms)
        /// </summary>
        public int MaxDelayMs { get; set; } = 5000;

        /// <summary>
        /// Retry'lar arasındaki delay süresini hesapla
        /// </summary>
        public int GetDelayForAttempt(int attemptNumber)
        {
            if (!UseProgressiveDelay)
            {
                return InitialDelayMs;
            }

            // Progressive delay: 500ms, 1000ms, 2000ms, ...
            int delay = (int)(InitialDelayMs * Math.Pow(DelayMultiplier, attemptNumber - 1));
            return Math.Min(delay, MaxDelayMs);
        }

        /// <summary>
        /// Default retry policy (PDF önerileri)
        /// </summary>
        public static RetryPolicy Default => new RetryPolicy
        {
            MaxRetryCount = 3,
            InitialDelayMs = 500,
            UseProgressiveDelay = true,
            DelayMultiplier = 2.0,
            MaxDelayMs = 5000
        };

        /// <summary>
        /// Aggressive retry policy (daha fazla retry)
        /// </summary>
        public static RetryPolicy Aggressive => new RetryPolicy
        {
            MaxRetryCount = 5,
            InitialDelayMs = 300,
            UseProgressiveDelay = true,
            DelayMultiplier = 1.5,
            MaxDelayMs = 3000
        };

        /// <summary>
        /// Conservative retry policy (az retry)
        /// </summary>
        public static RetryPolicy Conservative => new RetryPolicy
        {
            MaxRetryCount = 2,
            InitialDelayMs = 1000,
            UseProgressiveDelay = false,
            MaxDelayMs = 1000
        };
    }
}