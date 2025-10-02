using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Ecr.Module.Services.Ingenico.Diagnostics
{
    /// <summary>
    /// Diagnostic report generator
    /// Generates daily summaries, error frequency analysis
    /// </summary>
    public class DiagnosticReporter
    {
        private readonly DiagnosticMetrics _metrics;
        private readonly string _reportDirectory;

        public DiagnosticReporter()
        {
            _metrics = DiagnosticMetrics.Instance;
            _reportDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "Reports");

            if (!Directory.Exists(_reportDirectory))
            {
                Directory.CreateDirectory(_reportDirectory);
            }
        }

        /// <summary>
        /// Generate daily summary report
        /// </summary>
        public DailySummaryReport GenerateDailySummary()
        {
            var snapshot = _metrics.GetSnapshot();

            var report = new DailySummaryReport
            {
                ReportDate = DateTime.Now,
                ConnectionSuccessRate = snapshot.ConnectionSuccessRate,
                AverageConnectionDuration = snapshot.AverageConnectionDuration,
                AverageTransactionDuration = snapshot.AverageTransactionDuration,
                TotalConnectionAttempts = snapshot.TotalConnectionAttempts,
                TotalTransactions = snapshot.TotalTransactions,
                TopErrors = snapshot.TopErrors
            };

            // Connection details
            report.TotalConnectionSuccesses = _metrics.GetCounterValue("connection.successes");
            report.TotalConnectionFailures = _metrics.GetCounterValue("connection.failures");

            // Transaction details
            report.TotalTransactionsCompleted = _metrics.GetCounterValue("transaction.completed");
            report.TotalTransactionsCancelled = _metrics.GetCounterValue("transaction.cancelled");

            // Health check details
            report.TotalPingSuccess = _metrics.GetCounterValue("healthcheck.ping.success");
            report.TotalPingFailure = _metrics.GetCounterValue("healthcheck.ping.failure");
            report.TotalEchoSuccess = _metrics.GetCounterValue("healthcheck.echo.success");
            report.TotalEchoFailure = _metrics.GetCounterValue("healthcheck.echo.failure");

            // Recovery details
            report.TotalRecoveryAttempts = _metrics.GetCounterValue("recovery.attempts");
            report.TotalRecoverySuccesses = _metrics.GetCounterValue("recovery.successes");

            return report;
        }

        /// <summary>
        /// Save daily summary report to file
        /// </summary>
        public bool SaveDailySummary()
        {
            try
            {
                var report = GenerateDailySummary();
                string fileName = $"daily_summary_{DateTime.Now:yyyyMMdd}.txt";
                string filePath = Path.Combine(_reportDirectory, fileName);

                var sb = new StringBuilder();
                sb.AppendLine("=================================================");
                sb.AppendLine($"  INGENICO ECR - DAILY SUMMARY REPORT");
                sb.AppendLine($"  {report.ReportDate:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine("=================================================");
                sb.AppendLine();

                // Connection Statistics
                sb.AppendLine("CONNECTION STATISTICS:");
                sb.AppendLine($"  Total Attempts: {report.TotalConnectionAttempts}");
                sb.AppendLine($"  Successes: {report.TotalConnectionSuccesses}");
                sb.AppendLine($"  Failures: {report.TotalConnectionFailures}");
                sb.AppendLine($"  Success Rate: {report.ConnectionSuccessRate:F2}%");
                sb.AppendLine($"  Avg Duration: {report.AverageConnectionDuration:F2}ms");
                sb.AppendLine();

                // Transaction Statistics
                sb.AppendLine("TRANSACTION STATISTICS:");
                sb.AppendLine($"  Total Started: {report.TotalTransactions}");
                sb.AppendLine($"  Completed: {report.TotalTransactionsCompleted}");
                sb.AppendLine($"  Cancelled: {report.TotalTransactionsCancelled}");
                sb.AppendLine($"  Avg Duration: {report.AverageTransactionDuration:F2}ms");
                sb.AppendLine();

                // Health Check Statistics
                sb.AppendLine("HEALTH CHECK STATISTICS:");
                sb.AppendLine($"  PING Success: {report.TotalPingSuccess}");
                sb.AppendLine($"  PING Failure: {report.TotalPingFailure}");
                sb.AppendLine($"  ECHO Success: {report.TotalEchoSuccess}");
                sb.AppendLine($"  ECHO Failure: {report.TotalEchoFailure}");
                sb.AppendLine();

                // Recovery Statistics
                sb.AppendLine("RECOVERY STATISTICS:");
                sb.AppendLine($"  Attempts: {report.TotalRecoveryAttempts}");
                sb.AppendLine($"  Successes: {report.TotalRecoverySuccesses}");
                if (report.TotalRecoveryAttempts > 0)
                {
                    double recoveryRate = (double)report.TotalRecoverySuccesses / report.TotalRecoveryAttempts * 100;
                    sb.AppendLine($"  Success Rate: {recoveryRate:F2}%");
                }
                sb.AppendLine();

                // Top Errors
                if (report.TopErrors.Any())
                {
                    sb.AppendLine("TOP ERRORS:");
                    foreach (var error in report.TopErrors)
                    {
                        sb.AppendLine($"  0x{error.ErrorCode:X} - Count: {error.Count}");
                    }
                    sb.AppendLine();
                }

                sb.AppendLine("=================================================");

                File.WriteAllText(filePath, sb.ToString());
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveDailySummary error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Generate error frequency report
        /// </summary>
        public string GenerateErrorFrequencyReport(int topCount = 20)
        {
            var topErrors = _metrics.GetTopErrors(topCount);

            var sb = new StringBuilder();
            sb.AppendLine("=================================================");
            sb.AppendLine($"  ERROR FREQUENCY REPORT");
            sb.AppendLine($"  {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("=================================================");
            sb.AppendLine();

            if (!topErrors.Any())
            {
                sb.AppendLine("No errors recorded.");
            }
            else
            {
                sb.AppendLine($"Top {topErrors.Count} errors:");
                sb.AppendLine();

                int rank = 1;
                foreach (var error in topErrors)
                {
                    sb.AppendLine($"{rank}. Error Code: 0x{error.ErrorCode:X}");
                    sb.AppendLine($"   Count: {error.Count}");

                    // Try to get error description
                    var errorInfo = Connection.ErrorCodeCategorizer.GetErrorInfo(error.ErrorCode);
                    sb.AppendLine($"   Category: {errorInfo.Category}");
                    sb.AppendLine($"   Description: {errorInfo.Description}");
                    sb.AppendLine();

                    rank++;
                }
            }

            sb.AppendLine("=================================================");

            return sb.ToString();
        }

        /// <summary>
        /// Save error frequency report to file
        /// </summary>
        public bool SaveErrorFrequencyReport()
        {
            try
            {
                string report = GenerateErrorFrequencyReport();
                string fileName = $"error_frequency_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string filePath = Path.Combine(_reportDirectory, fileName);

                File.WriteAllText(filePath, report);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveErrorFrequencyReport error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Generate performance summary
        /// </summary>
        public string GeneratePerformanceSummary()
        {
            var snapshot = _metrics.GetSnapshot();

            var sb = new StringBuilder();
            sb.AppendLine("=================================================");
            sb.AppendLine($"  PERFORMANCE SUMMARY");
            sb.AppendLine($"  {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("=================================================");
            sb.AppendLine();

            sb.AppendLine("PERFORMANCE METRICS:");
            sb.AppendLine($"  Connection Success Rate: {snapshot.ConnectionSuccessRate:F2}%");
            sb.AppendLine($"  Avg Connection Duration: {snapshot.AverageConnectionDuration:F2}ms");
            sb.AppendLine($"  Avg Transaction Duration: {snapshot.AverageTransactionDuration:F2}ms");
            sb.AppendLine();

            sb.AppendLine("VOLUME METRICS:");
            sb.AppendLine($"  Total Connection Attempts: {snapshot.TotalConnectionAttempts}");
            sb.AppendLine($"  Total Transactions: {snapshot.TotalTransactions}");
            sb.AppendLine();

            sb.AppendLine("=================================================");

            return sb.ToString();
        }
    }

    /// <summary>
    /// Daily summary report model
    /// </summary>
    public class DailySummaryReport
    {
        public DateTime ReportDate { get; set; }

        // Connection
        public double ConnectionSuccessRate { get; set; }
        public double AverageConnectionDuration { get; set; }
        public long TotalConnectionAttempts { get; set; }
        public long TotalConnectionSuccesses { get; set; }
        public long TotalConnectionFailures { get; set; }

        // Transaction
        public double AverageTransactionDuration { get; set; }
        public long TotalTransactions { get; set; }
        public long TotalTransactionsCompleted { get; set; }
        public long TotalTransactionsCancelled { get; set; }

        // Health Check
        public long TotalPingSuccess { get; set; }
        public long TotalPingFailure { get; set; }
        public long TotalEchoSuccess { get; set; }
        public long TotalEchoFailure { get; set; }

        // Recovery
        public long TotalRecoveryAttempts { get; set; }
        public long TotalRecoverySuccesses { get; set; }

        // Errors
        public List<ErrorFrequency> TopErrors { get; set; }

        public DailySummaryReport()
        {
            TopErrors = new List<ErrorFrequency>();
        }
    }
}