using System;

namespace Ecr.Module.Services.Ingenico.Diagnostics
{
    /// <summary>
    /// Diagnostic logger interface
    /// </summary>
    public interface IDiagnosticLogger
    {
        /// <summary>
        /// Log event
        /// </summary>
        void Log(DiagnosticEvent diagnosticEvent);

        /// <summary>
        /// Log with level and message
        /// </summary>
        void Log(LogLevel level, LogCategory category, string message, string source = null);

        /// <summary>
        /// Log error
        /// </summary>
        void LogError(LogCategory category, string message, uint? errorCode = null, Exception exception = null, string source = null);

        /// <summary>
        /// Log warning
        /// </summary>
        void LogWarning(LogCategory category, string message, string source = null);

        /// <summary>
        /// Log information
        /// </summary>
        void LogInformation(LogCategory category, string message, string source = null);

        /// <summary>
        /// Log debug
        /// </summary>
        void LogDebug(LogCategory category, string message, string source = null);

        /// <summary>
        /// Log performance
        /// </summary>
        void LogPerformance(string operation, long durationMs, string source = null);
    }
}