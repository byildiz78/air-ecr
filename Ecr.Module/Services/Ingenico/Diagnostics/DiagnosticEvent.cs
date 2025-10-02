using System;
using System.Collections.Generic;

namespace Ecr.Module.Services.Ingenico.Diagnostics
{
    /// <summary>
    /// Diagnostic event - structured log entry
    /// </summary>
    public class DiagnosticEvent
    {
        /// <summary>
        /// Event ID (unique)
        /// </summary>
        public Guid EventId { get; set; }

        /// <summary>
        /// Timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Log level
        /// </summary>
        public LogLevel Level { get; set; }

        /// <summary>
        /// Category
        /// </summary>
        public LogCategory Category { get; set; }

        /// <summary>
        /// Message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Source (class/method name)
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Error code (if applicable)
        /// </summary>
        public uint? ErrorCode { get; set; }

        /// <summary>
        /// Exception (if any)
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Additional properties (key-value pairs)
        /// </summary>
        public Dictionary<string, object> Properties { get; set; }

        /// <summary>
        /// Duration (ms) - for performance tracking
        /// </summary>
        public long? DurationMs { get; set; }

        public DiagnosticEvent()
        {
            EventId = Guid.NewGuid();
            Timestamp = DateTime.Now;
            Message = string.Empty;
            Source = string.Empty;
            Properties = new Dictionary<string, object>();
        }

        /// <summary>
        /// Property ekle
        /// </summary>
        public DiagnosticEvent WithProperty(string key, object value)
        {
            Properties[key] = value;
            return this;
        }

        /// <summary>
        /// ToString override - readable format
        /// </summary>
        public override string ToString()
        {
            string result = $"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] [{Category}] {Message}";

            if (ErrorCode.HasValue)
            {
                result += $" (ErrorCode: 0x{ErrorCode.Value:X})";
            }

            if (DurationMs.HasValue)
            {
                result += $" (Duration: {DurationMs}ms)";
            }

            if (!string.IsNullOrEmpty(Source))
            {
                result += $" - {Source}";
            }

            return result;
        }
    }
}