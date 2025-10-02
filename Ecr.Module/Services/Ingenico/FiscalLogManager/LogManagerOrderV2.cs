using Ecr.Module.Services.Ingenico.Diagnostics;
using Ecr.Module.Services.Ingenico.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Ecr.Module.Services.Ingenico.FiscalLogManager
{
    /// <summary>
    /// Enhanced LogManagerOrder with diagnostics and metrics
    /// BACKWARD COMPATIBLE - wraps existing LogManagerOrder
    ///
    /// Purpose: Add observability without breaking existing code
    /// - Diagnostic logging for all operations
    /// - Performance metrics tracking
    /// - Error frequency monitoring
    /// - Thread-safe wrappers
    ///
    /// IMPORTANT: This does NOT replace LogManagerOrder
    /// It enhances it with additional features
    /// </summary>
    public class LogManagerOrderV2
    {
        private readonly DiagnosticLogger _logger;
        private readonly DiagnosticMetrics _metrics;

        public LogManagerOrderV2()
        {
            _logger = DiagnosticLogger.Instance;
            _metrics = DiagnosticMetrics.Instance;
        }

        /// <summary>
        /// Save order with logging and metrics
        /// Wraps LogManagerOrder.SaveOrder()
        /// </summary>
        public void SaveOrder(string orderData, string fileName, string sourceId)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _metrics.IncrementCounter("logmanager.save.attempts");

                _logger.LogInformation(LogCategory.Transaction,
                    $"Saving order: SourceId={sourceId}, FileName={fileName}",
                    source: "LogManagerOrderV2");

                // ✅ Call existing method - NO BREAKING CHANGE
                LogManagerOrder.SaveOrder(orderData, fileName, sourceId);

                stopwatch.Stop();
                _metrics.RecordDuration("logmanager.save.duration", stopwatch.ElapsedMilliseconds);
                _metrics.IncrementCounter("logmanager.save.successes");

                _logger.LogInformation(LogCategory.Transaction,
                    $"Order saved successfully: SourceId={sourceId}, Duration={stopwatch.ElapsedMilliseconds}ms",
                    source: "LogManagerOrderV2");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _metrics.IncrementCounter("logmanager.save.failures");

                _logger.LogError(LogCategory.Transaction,
                    $"Failed to save order: SourceId={sourceId}",
                    exception: ex,
                    source: "LogManagerOrderV2");

                // Re-throw to maintain existing behavior
                throw;
            }
        }

        /// <summary>
        /// Save with raw Save method
        /// Wraps LogManagerOrder.Save()
        /// </summary>
        public void Save(string data, string sourceId, string prefix = null, string afterfix = null,
            string subFolderName = null, string fileName = null, bool prependLogDate = true)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _metrics.IncrementCounter("logmanager.save.raw.attempts");

                _logger.LogDebug(LogCategory.Transaction,
                    $"Saving raw data: SourceId={sourceId}",
                    source: "LogManagerOrderV2");

                // ✅ Call existing method
                LogManagerOrder.Save(data, sourceId, prefix, afterfix, subFolderName, fileName, prependLogDate);

                stopwatch.Stop();
                _metrics.RecordDuration("logmanager.save.raw.duration", stopwatch.ElapsedMilliseconds);
                _metrics.IncrementCounter("logmanager.save.raw.successes");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _metrics.IncrementCounter("logmanager.save.raw.failures");

                _logger.LogError(LogCategory.Transaction,
                    $"Failed to save raw data: SourceId={sourceId}",
                    exception: ex,
                    source: "LogManagerOrderV2");

                // Re-throw to maintain existing behavior
                throw;
            }
        }

        /// <summary>
        /// Get order file with logging
        /// Wraps LogManagerOrder.GetOrderFile()
        /// </summary>
        public List<GmpCommand> GetOrderFile(string sourceId)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _metrics.IncrementCounter("logmanager.get.attempts");

                _logger.LogDebug(LogCategory.Transaction,
                    $"Getting order file: SourceId={sourceId}",
                    source: "LogManagerOrderV2");

                // ✅ Call existing method
                var result = LogManagerOrder.GetOrderFile(sourceId);

                stopwatch.Stop();
                _metrics.RecordDuration("logmanager.get.duration", stopwatch.ElapsedMilliseconds);
                _metrics.IncrementCounter("logmanager.get.successes");

                _logger.LogInformation(LogCategory.Transaction,
                    $"Order file retrieved: SourceId={sourceId}, CommandCount={result?.Count ?? 0}, Duration={stopwatch.ElapsedMilliseconds}ms",
                    source: "LogManagerOrderV2");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _metrics.IncrementCounter("logmanager.get.failures");

                _logger.LogError(LogCategory.Transaction,
                    $"Failed to get order file: SourceId={sourceId}",
                    exception: ex,
                    source: "LogManagerOrderV2");

                // Re-throw to maintain existing behavior
                throw;
            }
        }

        /// <summary>
        /// Get completed order file with logging
        /// Wraps LogManagerOrder.GetOrderFileComplated()
        /// </summary>
        public List<GmpCommand> GetOrderFileCompleted(string sourceId)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _metrics.IncrementCounter("logmanager.getcompleted.attempts");

                _logger.LogDebug(LogCategory.Transaction,
                    $"Getting completed order file: SourceId={sourceId}",
                    source: "LogManagerOrderV2");

                // ✅ Call existing method (note: typo in original "Complated")
                var result = LogManagerOrder.GetOrderFileComplated(sourceId);

                stopwatch.Stop();
                _metrics.RecordDuration("logmanager.getcompleted.duration", stopwatch.ElapsedMilliseconds);
                _metrics.IncrementCounter("logmanager.getcompleted.successes");

                _logger.LogInformation(LogCategory.Transaction,
                    $"Completed order file retrieved: SourceId={sourceId}, CommandCount={result?.Count ?? 0}",
                    source: "LogManagerOrderV2");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _metrics.IncrementCounter("logmanager.getcompleted.failures");

                _logger.LogError(LogCategory.Transaction,
                    $"Failed to get completed order file: SourceId={sourceId}",
                    exception: ex,
                    source: "LogManagerOrderV2");

                // Re-throw to maintain existing behavior
                throw;
            }
        }

        /// <summary>
        /// Move log file with logging
        /// Wraps LogManagerOrder.MoveLogFile()
        /// </summary>
        public bool MoveLogFile(string sourceId, LogFolderType folderType)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _metrics.IncrementCounter("logmanager.move.attempts");

                _logger.LogInformation(LogCategory.Transaction,
                    $"Moving log file: SourceId={sourceId}, TargetFolder={folderType}",
                    source: "LogManagerOrderV2");

                // ✅ Call existing method
                var result = LogManagerOrder.MoveLogFile(sourceId, folderType);

                stopwatch.Stop();
                _metrics.RecordDuration("logmanager.move.duration", stopwatch.ElapsedMilliseconds);

                if (result)
                {
                    _metrics.IncrementCounter("logmanager.move.successes");

                    _logger.LogInformation(LogCategory.Transaction,
                        $"Log file moved successfully: SourceId={sourceId}, TargetFolder={folderType}",
                        source: "LogManagerOrderV2");
                }
                else
                {
                    _metrics.IncrementCounter("logmanager.move.failures");

                    _logger.LogWarning(LogCategory.Transaction,
                        $"Log file move failed: SourceId={sourceId}, TargetFolder={folderType}",
                        source: "LogManagerOrderV2");
                }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _metrics.IncrementCounter("logmanager.move.failures");

                _logger.LogError(LogCategory.Transaction,
                    $"Exception moving log file: SourceId={sourceId}, TargetFolder={folderType}",
                    exception: ex,
                    source: "LogManagerOrderV2");

                // Re-throw to maintain existing behavior
                throw;
            }
        }

        /// <summary>
        /// Rename log file with logging
        /// Wraps LogManagerOrder.RenameLog()
        /// </summary>
        public bool RenameLog(string oldSourceId, string newSourceId)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _metrics.IncrementCounter("logmanager.rename.attempts");

                _logger.LogInformation(LogCategory.Transaction,
                    $"Renaming log file: OldId={oldSourceId}, NewId={newSourceId}",
                    source: "LogManagerOrderV2");

                // ✅ Call existing method
                var result = LogManagerOrder.RenameLog(oldSourceId, newSourceId);

                stopwatch.Stop();
                _metrics.RecordDuration("logmanager.rename.duration", stopwatch.ElapsedMilliseconds);

                if (result)
                {
                    _metrics.IncrementCounter("logmanager.rename.successes");

                    _logger.LogInformation(LogCategory.Transaction,
                        $"Log file renamed successfully: OldId={oldSourceId}, NewId={newSourceId}",
                        source: "LogManagerOrderV2");
                }
                else
                {
                    _metrics.IncrementCounter("logmanager.rename.failures");

                    _logger.LogWarning(LogCategory.Transaction,
                        $"Log file rename failed: OldId={oldSourceId}, NewId={newSourceId}",
                        source: "LogManagerOrderV2");
                }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _metrics.IncrementCounter("logmanager.rename.failures");

                _logger.LogError(LogCategory.Transaction,
                    $"Exception renaming log file: OldId={oldSourceId}, NewId={newSourceId}",
                    exception: ex,
                    source: "LogManagerOrderV2");

                // Re-throw to maintain existing behavior
                throw;
            }
        }

        /// <summary>
        /// List waiting logs with logging
        /// Wraps LogManagerOrder.ListWaitingLogs()
        /// </summary>
        public List<string> ListWaitingLogs(string excludeFileName = null)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _metrics.IncrementCounter("logmanager.list.attempts");

                _logger.LogDebug(LogCategory.Transaction,
                    $"Listing waiting logs: Exclude={excludeFileName ?? "none"}",
                    source: "LogManagerOrderV2");

                // ✅ Call existing method
                var result = LogManagerOrder.ListWaitingLogs(excludeFileName);

                stopwatch.Stop();
                _metrics.RecordDuration("logmanager.list.duration", stopwatch.ElapsedMilliseconds);
                _metrics.IncrementCounter("logmanager.list.successes");

                _logger.LogInformation(LogCategory.Transaction,
                    $"Waiting logs listed: Count={result?.Count ?? 0}",
                    source: "LogManagerOrderV2");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _metrics.IncrementCounter("logmanager.list.failures");

                _logger.LogError(LogCategory.Transaction,
                    "Failed to list waiting logs",
                    exception: ex,
                    source: "LogManagerOrderV2");

                // Re-throw to maintain existing behavior
                throw;
            }
        }

        /// <summary>
        /// Log exception with metrics
        /// Wraps LogManagerOrder.Exception()
        /// </summary>
        public void LogException(Exception ex, string commandName, string sourceId)
        {
            try
            {
                _metrics.IncrementCounter("logmanager.exception.logged");

                _logger.LogError(LogCategory.General,
                    $"Command exception: Command={commandName}, SourceId={sourceId}",
                    exception: ex,
                    source: "LogManagerOrderV2");

                // ✅ Call existing method
                LogManagerOrder.Exception(ex, commandName, sourceId);
            }
            catch (Exception innerEx)
            {
                // Double-fault protection: If logging fails, don't crash
                _logger.LogError(LogCategory.General,
                    "Failed to log exception",
                    exception: innerEx,
                    source: "LogManagerOrderV2");
            }
        }

        /// <summary>
        /// Get log file names with logging
        /// Wraps LogManagerOrder.GetLogFileNames()
        /// </summary>
        public List<string> GetLogFileNames()
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _metrics.IncrementCounter("logmanager.getnames.attempts");

                // ✅ Call existing method
                var result = LogManagerOrder.GetLogFileNames();

                stopwatch.Stop();
                _metrics.RecordDuration("logmanager.getnames.duration", stopwatch.ElapsedMilliseconds);
                _metrics.IncrementCounter("logmanager.getnames.successes");

                _logger.LogDebug(LogCategory.Transaction,
                    $"Log file names retrieved: Count={result?.Count ?? 0}",
                    source: "LogManagerOrderV2");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _metrics.IncrementCounter("logmanager.getnames.failures");

                _logger.LogError(LogCategory.Transaction,
                    "Failed to get log file names",
                    exception: ex,
                    source: "LogManagerOrderV2");

                // Re-throw to maintain existing behavior
                throw;
            }
        }

        /// <summary>
        /// Get order file data (GmpPrintReceiptDto)
        /// Wraps LogManagerOrder.GetOrderFileData()
        /// </summary>
        public List<GmpPrintReceiptDto> GetOrderFileData(string sourceId)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _metrics.IncrementCounter("logmanager.getdata.attempts");

                _logger.LogDebug(LogCategory.Transaction,
                    $"Getting order file data: SourceId={sourceId}",
                    source: "LogManagerOrderV2");

                // ✅ Call existing method
                var result = LogManagerOrder.GetOrderFileData(sourceId);

                stopwatch.Stop();
                _metrics.RecordDuration("logmanager.getdata.duration", stopwatch.ElapsedMilliseconds);
                _metrics.IncrementCounter("logmanager.getdata.successes");

                _logger.LogInformation(LogCategory.Transaction,
                    $"Order file data retrieved: SourceId={sourceId}, Count={result?.Count ?? 0}",
                    source: "LogManagerOrderV2");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _metrics.IncrementCounter("logmanager.getdata.failures");

                _logger.LogError(LogCategory.Transaction,
                    $"Failed to get order file data: SourceId={sourceId}",
                    exception: ex,
                    source: "LogManagerOrderV2");

                // Re-throw to maintain existing behavior
                throw;
            }
        }

        /// <summary>
        /// Get fiscal order
        /// Wraps LogManagerOrder.GetOrderFileFiscal()
        /// </summary>
        public FiscalOrder GetOrderFileFiscal(string sourceId)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _metrics.IncrementCounter("logmanager.getfiscal.attempts");

                _logger.LogDebug(LogCategory.Transaction,
                    $"Getting fiscal order: SourceId={sourceId}",
                    source: "LogManagerOrderV2");

                // ✅ Call existing method
                var result = LogManagerOrder.GetOrderFileFiscal(sourceId);

                stopwatch.Stop();
                _metrics.RecordDuration("logmanager.getfiscal.duration", stopwatch.ElapsedMilliseconds);
                _metrics.IncrementCounter("logmanager.getfiscal.successes");

                _logger.LogInformation(LogCategory.Transaction,
                    $"Fiscal order retrieved: SourceId={sourceId}",
                    source: "LogManagerOrderV2");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _metrics.IncrementCounter("logmanager.getfiscal.failures");

                _logger.LogError(LogCategory.Transaction,
                    $"Failed to get fiscal order: SourceId={sourceId}",
                    exception: ex,
                    source: "LogManagerOrderV2");

                // Re-throw to maintain existing behavior
                throw;
            }
        }
    }
}