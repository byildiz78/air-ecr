using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ecr.Module.Services.Ingenico.Diagnostics
{
    /// <summary>
    /// Diagnostic logger implementation
    /// Thread-safe, buffered logging
    /// </summary>
    public class DiagnosticLogger : IDiagnosticLogger
    {
        private static readonly object _lock = new object();
        private static DiagnosticLogger _instance;

        private readonly ConcurrentQueue<DiagnosticEvent> _eventQueue;
        private readonly string _logDirectory;
        private readonly int _maxEventsInMemory = 1000;

        /// <summary>
        /// Minimum log level (bunun altındaki loglar yazılmaz)
        /// </summary>
        public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// File logging enabled mi?
        /// </summary>
        public bool FileLoggingEnabled { get; set; } = true;

        /// <summary>
        /// Console logging enabled mi?
        /// </summary>
        public bool ConsoleLoggingEnabled { get; set; } = true;

        /// <summary>
        /// Event handler for external logging
        /// </summary>
        public event EventHandler<DiagnosticEvent> OnEventLogged;

        private DiagnosticLogger()
        {
            _eventQueue = new ConcurrentQueue<DiagnosticEvent>();
            _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "Diagnostics");

            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public static DiagnosticLogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new DiagnosticLogger();
                        }
                    }
                }
                return _instance;
            }
        }

        public void Log(DiagnosticEvent diagnosticEvent)
        {
            if (diagnosticEvent == null)
                return;

            // Level check
            if (diagnosticEvent.Level < MinimumLevel)
                return;

            // Queue'ya ekle
            _eventQueue.Enqueue(diagnosticEvent);

            // Queue size kontrolü
            if (_eventQueue.Count > _maxEventsInMemory)
            {
                FlushEvents();
            }

            // Console log
            if (ConsoleLoggingEnabled)
            {
                Console.WriteLine(diagnosticEvent.ToString());
            }

            // Debug output
            System.Diagnostics.Debug.WriteLine(diagnosticEvent.ToString());

            // Event fire et
            OnEventLogged?.Invoke(this, diagnosticEvent);
        }

        public void Log(LogLevel level, LogCategory category, string message, string source = null)
        {
            var diagnosticEvent = new DiagnosticEvent
            {
                Level = level,
                Category = category,
                Message = message,
                Source = source ?? GetCallerName()
            };

            Log(diagnosticEvent);
        }

        public void LogError(LogCategory category, string message, uint? errorCode = null, Exception exception = null, string source = null)
        {
            var diagnosticEvent = new DiagnosticEvent
            {
                Level = LogLevel.Error,
                Category = category,
                Message = message,
                ErrorCode = errorCode,
                Exception = exception,
                Source = source ?? GetCallerName()
            };

            if (exception != null)
            {
                diagnosticEvent.WithProperty("ExceptionType", exception.GetType().Name);
                diagnosticEvent.WithProperty("StackTrace", exception.StackTrace);
            }

            Log(diagnosticEvent);
        }

        public void LogWarning(LogCategory category, string message, string source = null)
        {
            Log(LogLevel.Warning, category, message, source ?? GetCallerName());
        }

        public void LogInformation(LogCategory category, string message, string source = null)
        {
            Log(LogLevel.Information, category, message, source ?? GetCallerName());
        }

        public void LogDebug(LogCategory category, string message, string source = null)
        {
            Log(LogLevel.Debug, category, message, source ?? GetCallerName());
        }

        public void LogPerformance(string operation, long durationMs, string source = null)
        {
            var diagnosticEvent = new DiagnosticEvent
            {
                Level = LogLevel.Information,
                Category = LogCategory.Performance,
                Message = $"Operation: {operation}",
                DurationMs = durationMs,
                Source = source ?? GetCallerName()
            };

            diagnosticEvent.WithProperty("Operation", operation);
            diagnosticEvent.WithProperty("Duration", durationMs);

            Log(diagnosticEvent);
        }

        /// <summary>
        /// Event'leri file'a flush et
        /// </summary>
        public void FlushEvents()
        {
            if (!FileLoggingEnabled)
                return;

            lock (_lock)
            {
                try
                {
                    if (_eventQueue.IsEmpty)
                        return;

                    // File name (günlük)
                    string fileName = $"diagnostic_{DateTime.Now:yyyyMMdd}.log";
                    string filePath = Path.Combine(_logDirectory, fileName);

                    // Event'leri al
                    var events = new List<DiagnosticEvent>();
                    while (_eventQueue.TryDequeue(out var evt))
                    {
                        events.Add(evt);
                    }

                    // File'a yaz
                    using (var writer = new StreamWriter(filePath, true))
                    {
                        foreach (var evt in events)
                        {
                            writer.WriteLine(evt.ToString());

                            // Properties varsa onları da yaz
                            if (evt.Properties.Any())
                            {
                                foreach (var prop in evt.Properties)
                                {
                                    writer.WriteLine($"  {prop.Key}: {prop.Value}");
                                }
                            }

                            // Exception varsa detayları yaz
                            if (evt.Exception != null)
                            {
                                writer.WriteLine($"  Exception: {evt.Exception.Message}");
                                writer.WriteLine($"  {evt.Exception.StackTrace}");
                            }

                            writer.WriteLine(); // Empty line
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"FlushEvents error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Caller method name al (debugging için)
        /// </summary>
        private string GetCallerName()
        {
            try
            {
                var stackTrace = new System.Diagnostics.StackTrace();
                // 0: GetCallerName, 1: Log method, 2: actual caller
                var frame = stackTrace.GetFrame(2);
                var method = frame?.GetMethod();
                return method != null ? $"{method.DeclaringType?.Name}.{method.Name}" : "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// Dispose (flush remaining events)
        /// </summary>
        public void Dispose()
        {
            FlushEvents();
        }
    }
}