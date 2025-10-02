using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Ecr.Module.Services.Ingenico.Diagnostics
{
    /// <summary>
    /// Performance metrics collector
    /// Tracks connection success rates, durations, error frequencies
    /// </summary>
    public class DiagnosticMetrics
    {
        private static readonly object _lock = new object();
        private static DiagnosticMetrics _instance;

        private readonly ConcurrentDictionary<string, MetricCounter> _counters;
        private readonly ConcurrentDictionary<string, MetricDuration> _durations;
        private readonly ConcurrentDictionary<uint, int> _errorFrequencies;

        private DiagnosticMetrics()
        {
            _counters = new ConcurrentDictionary<string, MetricCounter>();
            _durations = new ConcurrentDictionary<string, MetricDuration>();
            _errorFrequencies = new ConcurrentDictionary<uint, int>();

            InitializeDefaultMetrics();
        }

        public static DiagnosticMetrics Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new DiagnosticMetrics();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Initialize default metric counters
        /// </summary>
        private void InitializeDefaultMetrics()
        {
            // Connection metrics
            _counters.TryAdd("connection.attempts", new MetricCounter());
            _counters.TryAdd("connection.successes", new MetricCounter());
            _counters.TryAdd("connection.failures", new MetricCounter());

            // Transaction metrics
            _counters.TryAdd("transaction.started", new MetricCounter());
            _counters.TryAdd("transaction.completed", new MetricCounter());
            _counters.TryAdd("transaction.cancelled", new MetricCounter());

            // Health check metrics
            _counters.TryAdd("healthcheck.ping.success", new MetricCounter());
            _counters.TryAdd("healthcheck.ping.failure", new MetricCounter());
            _counters.TryAdd("healthcheck.echo.success", new MetricCounter());
            _counters.TryAdd("healthcheck.echo.failure", new MetricCounter());

            // Recovery metrics
            _counters.TryAdd("recovery.attempts", new MetricCounter());
            _counters.TryAdd("recovery.successes", new MetricCounter());

            // Duration metrics
            _durations.TryAdd("connection.duration", new MetricDuration());
            _durations.TryAdd("transaction.duration", new MetricDuration());
            _durations.TryAdd("healthcheck.duration", new MetricDuration());
        }

        /// <summary>
        /// Increment counter
        /// </summary>
        public void IncrementCounter(string metricName)
        {
            var counter = _counters.GetOrAdd(metricName, _ => new MetricCounter());
            counter.Increment();
        }

        /// <summary>
        /// Record duration
        /// </summary>
        public void RecordDuration(string metricName, long durationMs)
        {
            var duration = _durations.GetOrAdd(metricName, _ => new MetricDuration());
            duration.Record(durationMs);
        }

        /// <summary>
        /// Record error occurrence
        /// </summary>
        public void RecordError(uint errorCode)
        {
            _errorFrequencies.AddOrUpdate(errorCode, 1, (key, count) => count + 1);
        }

        /// <summary>
        /// Get counter value
        /// </summary>
        public long GetCounterValue(string metricName)
        {
            return _counters.TryGetValue(metricName, out var counter) ? counter.Value : 0;
        }

        /// <summary>
        /// Get connection success rate
        /// </summary>
        public double GetConnectionSuccessRate()
        {
            long attempts = GetCounterValue("connection.attempts");
            long successes = GetCounterValue("connection.successes");

            return attempts > 0 ? (double)successes / attempts * 100 : 0;
        }

        /// <summary>
        /// Get average duration
        /// </summary>
        public double GetAverageDuration(string metricName)
        {
            return _durations.TryGetValue(metricName, out var duration)
                ? duration.Average
                : 0;
        }

        /// <summary>
        /// Get top errors by frequency
        /// </summary>
        public List<ErrorFrequency> GetTopErrors(int count = 10)
        {
            return _errorFrequencies
                .OrderByDescending(kvp => kvp.Value)
                .Take(count)
                .Select(kvp => new ErrorFrequency
                {
                    ErrorCode = kvp.Key,
                    Count = kvp.Value
                })
                .ToList();
        }

        /// <summary>
        /// Get metrics snapshot
        /// </summary>
        public MetricsSnapshot GetSnapshot()
        {
            var snapshot = new MetricsSnapshot
            {
                Timestamp = DateTime.Now,
                ConnectionSuccessRate = GetConnectionSuccessRate(),
                AverageConnectionDuration = GetAverageDuration("connection.duration"),
                AverageTransactionDuration = GetAverageDuration("transaction.duration"),
                TotalConnectionAttempts = GetCounterValue("connection.attempts"),
                TotalTransactions = GetCounterValue("transaction.started"),
                TopErrors = GetTopErrors(5)
            };

            return snapshot;
        }

        /// <summary>
        /// Reset all metrics
        /// </summary>
        public void Reset()
        {
            _counters.Clear();
            _durations.Clear();
            _errorFrequencies.Clear();
            InitializeDefaultMetrics();
        }
    }

    /// <summary>
    /// Metric counter (thread-safe)
    /// </summary>
    public class MetricCounter
    {
        private long _value;

        public long Value => _value;

        public void Increment()
        {
            System.Threading.Interlocked.Increment(ref _value);
        }
    }

    /// <summary>
    /// Metric duration tracker
    /// </summary>
    public class MetricDuration
    {
        private readonly object _lock = new object();
        private long _totalMs;
        private long _count;
        private long _minMs = long.MaxValue;
        private long _maxMs = long.MinValue;

        public double Average => _count > 0 ? (double)_totalMs / _count : 0;
        public long Min => _minMs == long.MaxValue ? 0 : _minMs;
        public long Max => _maxMs == long.MinValue ? 0 : _maxMs;
        public long Count => _count;

        public void Record(long durationMs)
        {
            lock (_lock)
            {
                _totalMs += durationMs;
                _count++;

                if (durationMs < _minMs)
                    _minMs = durationMs;

                if (durationMs > _maxMs)
                    _maxMs = durationMs;
            }
        }
    }

    /// <summary>
    /// Error frequency entry
    /// </summary>
    public class ErrorFrequency
    {
        public uint ErrorCode { get; set; }
        public int Count { get; set; }
    }

    /// <summary>
    /// Metrics snapshot
    /// </summary>
    public class MetricsSnapshot
    {
        public DateTime Timestamp { get; set; }
        public double ConnectionSuccessRate { get; set; }
        public double AverageConnectionDuration { get; set; }
        public double AverageTransactionDuration { get; set; }
        public long TotalConnectionAttempts { get; set; }
        public long TotalTransactions { get; set; }
        public List<ErrorFrequency> TopErrors { get; set; }

        public MetricsSnapshot()
        {
            TopErrors = new List<ErrorFrequency>();
        }
    }
}