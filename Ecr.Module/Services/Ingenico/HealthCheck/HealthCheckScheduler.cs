using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ecr.Module.Services.Ingenico.HealthCheck
{
    /// <summary>
    /// Background health check scheduler
    /// Periyodik olarak device health check yapar
    /// Optional - production'da kullanılabilir
    /// </summary>
    public class HealthCheckScheduler : IDisposable
    {
        private readonly HealthCheckService _healthCheckService;
        private Timer _timer;
        private bool _isRunning;
        private readonly object _lock = new object();

        /// <summary>
        /// Check interval (ms)
        /// Default: 30 saniye
        /// </summary>
        public int IntervalMs { get; set; } = 30000;

        /// <summary>
        /// Kullanılacak strateji
        /// Default: PingFirst (recommended)
        /// </summary>
        public HealthCheckStrategy Strategy { get; set; } = HealthCheckStrategy.PingFirst;

        /// <summary>
        /// Check level
        /// Default: Standard
        /// </summary>
        public HealthCheckLevel Level { get; set; } = HealthCheckLevel.Standard;

        /// <summary>
        /// Son check sonucu
        /// </summary>
        public HealthCheckResult LastResult { get; private set; }

        /// <summary>
        /// Health check event
        /// </summary>
        public event EventHandler<HealthCheckEventArgs> OnHealthCheckCompleted;

        /// <summary>
        /// Health status changed event
        /// </summary>
        public event EventHandler<HealthStatusChangedEventArgs> OnHealthStatusChanged;

        private bool _lastHealthStatus = false;

        public HealthCheckScheduler()
        {
            _healthCheckService = new HealthCheckService();
        }

        /// <summary>
        /// Scheduler'ı başlat
        /// </summary>
        public void Start()
        {
            lock (_lock)
            {
                if (_isRunning)
                {
                    return;
                }

                _isRunning = true;
                _timer = new Timer(
                    PerformScheduledCheck,
                    null,
                    TimeSpan.Zero, // İlk check hemen
                    TimeSpan.FromMilliseconds(IntervalMs)
                );
            }
        }

        /// <summary>
        /// Scheduler'ı durdur
        /// </summary>
        public void Stop()
        {
            lock (_lock)
            {
                if (!_isRunning)
                {
                    return;
                }

                _isRunning = false;
                _timer?.Dispose();
                _timer = null;
            }
        }

        /// <summary>
        /// Scheduled check yap
        /// </summary>
        private void PerformScheduledCheck(object state)
        {
            if (!_isRunning)
            {
                return;
            }

            try
            {
                // Health check yap
                var result = _healthCheckService.PerformHealthCheck(Strategy, Level);
                LastResult = result;

                // Event fire et
                OnHealthCheckCompleted?.Invoke(this, new HealthCheckEventArgs
                {
                    Result = result,
                    Timestamp = DateTime.Now
                });

                // Status değiştiyse event fire et
                if (result.IsHealthy != _lastHealthStatus)
                {
                    OnHealthStatusChanged?.Invoke(this, new HealthStatusChangedEventArgs
                    {
                        IsHealthy = result.IsHealthy,
                        PreviousStatus = _lastHealthStatus,
                        Timestamp = DateTime.Now,
                        Result = result
                    });

                    _lastHealthStatus = result.IsHealthy;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Scheduled health check error: {ex.Message}");
            }
        }

        /// <summary>
        /// Scheduler running mi?
        /// </summary>
        public bool IsRunning
        {
            get
            {
                lock (_lock)
                {
                    return _isRunning;
                }
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }

    /// <summary>
    /// Health check event args
    /// </summary>
    public class HealthCheckEventArgs : EventArgs
    {
        public HealthCheckResult Result { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Health status changed event args
    /// </summary>
    public class HealthStatusChangedEventArgs : EventArgs
    {
        public bool IsHealthy { get; set; }
        public bool PreviousStatus { get; set; }
        public DateTime Timestamp { get; set; }
        public HealthCheckResult Result { get; set; }
    }
}