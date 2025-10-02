using Ecr.Module.Services.Ingenico.Diagnostics;
using Ecr.Module.Services.Ingenico.Persistence;
using Ecr.Module.Services.Ingenico.Transaction;
using System;

namespace Ecr.Module.Services.Ingenico.FiscalLogManager
{
    /// <summary>
    /// Transaction state tracker - Bridge between PersistenceService and LogManagerOrder
    ///
    /// Purpose: Coordinate saving both order data AND transaction state
    /// - Ensures consistency between LogManager (order data) and PersistenceService (technical state)
    /// - Provides unified interface for transaction tracking
    /// - Thread-safe operations
    ///
    /// IMPORTANT: This is OPTIONAL enhancement
    /// Existing code can continue using LogManagerOrder directly
    /// </summary>
    public class TransactionStateTracker
    {
        private readonly TransactionManager _transactionManager;
        private readonly PersistenceService _persistenceService;
        private readonly LogManagerOrderV2 _logManager;
        private readonly DiagnosticLogger _logger;
        private readonly DiagnosticMetrics _metrics;

        public TransactionStateTracker()
        {
            _transactionManager = TransactionManager.Instance;
            _persistenceService = new PersistenceService();
            _logManager = new LogManagerOrderV2();
            _logger = DiagnosticLogger.Instance;
            _metrics = DiagnosticMetrics.Instance;
        }

        /// <summary>
        /// Save both order data AND transaction state
        /// Ensures consistency between file-based order data and technical state
        /// </summary>
        /// <param name="orderData">JSON serialized order data</param>
        /// <param name="orderKey">Order key (unique identifier)</param>
        /// <param name="transactionHandle">GMP transaction handle (0 if not started)</param>
        /// <param name="state">Transaction state</param>
        public TransactionTrackingResult SaveTransactionWithOrder(
            string orderData,
            string orderKey,
            ulong transactionHandle = 0,
            TransactionState state = TransactionState.None)
        {
            var result = new TransactionTrackingResult
            {
                OrderKey = orderKey,
                TransactionHandle = transactionHandle
            };

            try
            {
                _metrics.IncrementCounter("transactiontracker.save.attempts");

                _logger.LogInformation(LogCategory.Transaction,
                    $"Saving transaction with order: OrderKey={orderKey}, Handle={transactionHandle}, State={state}",
                    source: "TransactionStateTracker");

                // 1. Save order data (existing mechanism)
                try
                {
                    _logManager.SaveOrder(orderData, orderKey, orderKey);
                    result.OrderDataSaved = true;

                    _logger.LogDebug(LogCategory.Transaction,
                        $"Order data saved: OrderKey={orderKey}",
                        source: "TransactionStateTracker");
                }
                catch (Exception ex)
                {
                    _logger.LogError(LogCategory.Transaction,
                        $"Failed to save order data: OrderKey={orderKey}",
                        exception: ex,
                        source: "TransactionStateTracker");

                    result.OrderDataSaved = false;
                    result.ErrorMessage = $"Order data save failed: {ex.Message}";
                    // Continue to try state save
                }

                // 2. Update transaction manager state (if handle exists)
                if (transactionHandle > 0 && state != TransactionState.None)
                {
                    try
                    {
                        _transactionManager.UpdateState(state);
                        result.StateUpdated = true;

                        _logger.LogDebug(LogCategory.Transaction,
                            $"Transaction state updated: Handle={transactionHandle}, State={state}",
                            source: "TransactionStateTracker");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(LogCategory.Transaction,
                            $"Failed to update transaction state: Handle={transactionHandle}",
                            exception: ex,
                            source: "TransactionStateTracker");

                        result.StateUpdated = false;
                        // Continue to try persistence save
                    }
                }

                // 3. Persist technical state (if persistence enabled)
                if (_persistenceService.AutoSaveEnabled)
                {
                    try
                    {
                        bool persistSuccess = _persistenceService.SaveCurrentState();
                        result.StatePersisted = persistSuccess;

                        if (persistSuccess)
                        {
                            _logger.LogDebug(LogCategory.Transaction,
                                "Technical state persisted successfully",
                                source: "TransactionStateTracker");
                        }
                        else
                        {
                            _logger.LogWarning(LogCategory.Transaction,
                                "Technical state persistence returned false",
                                source: "TransactionStateTracker");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(LogCategory.Transaction,
                            "Failed to persist technical state",
                            exception: ex,
                            source: "TransactionStateTracker");

                        result.StatePersisted = false;
                        // Don't fail overall operation
                    }
                }

                // Determine overall success
                // At minimum, order data must be saved
                result.Success = result.OrderDataSaved;

                if (result.Success)
                {
                    _metrics.IncrementCounter("transactiontracker.save.successes");

                    _logger.LogInformation(LogCategory.Transaction,
                        $"Transaction tracking saved: OrderKey={orderKey}, OrderData={result.OrderDataSaved}, StateUpdated={result.StateUpdated}, StatePersisted={result.StatePersisted}",
                        source: "TransactionStateTracker");
                }
                else
                {
                    _metrics.IncrementCounter("transactiontracker.save.failures");

                    _logger.LogError(LogCategory.Transaction,
                        $"Transaction tracking failed: OrderKey={orderKey}, Error={result.ErrorMessage}",
                        source: "TransactionStateTracker");
                }

                return result;
            }
            catch (Exception ex)
            {
                _metrics.IncrementCounter("transactiontracker.save.failures");

                _logger.LogError(LogCategory.Transaction,
                    $"Transaction tracking exception: OrderKey={orderKey}",
                    exception: ex,
                    source: "TransactionStateTracker");

                result.Success = false;
                result.ErrorMessage = $"Exception: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// Save transaction state only (no order data)
        /// Use when transaction state changes but order data unchanged
        /// </summary>
        public bool SaveTransactionState()
        {
            try
            {
                _metrics.IncrementCounter("transactiontracker.savestate.attempts");

                var transaction = _transactionManager.GetCurrentTransaction();

                _logger.LogDebug(LogCategory.Transaction,
                    $"Saving transaction state: Handle={transaction.Handle}, State={transaction.State}",
                    source: "TransactionStateTracker");

                if (_persistenceService.AutoSaveEnabled)
                {
                    bool success = _persistenceService.SaveCurrentState();

                    if (success)
                    {
                        _metrics.IncrementCounter("transactiontracker.savestate.successes");

                        _logger.LogDebug(LogCategory.Transaction,
                            "Transaction state saved successfully",
                            source: "TransactionStateTracker");
                    }
                    else
                    {
                        _metrics.IncrementCounter("transactiontracker.savestate.failures");

                        _logger.LogWarning(LogCategory.Transaction,
                            "Transaction state save returned false",
                            source: "TransactionStateTracker");
                    }

                    return success;
                }

                return true; // Auto-save disabled, no-op is success
            }
            catch (Exception ex)
            {
                _metrics.IncrementCounter("transactiontracker.savestate.failures");

                _logger.LogError(LogCategory.Transaction,
                    "Failed to save transaction state",
                    exception: ex,
                    source: "TransactionStateTracker");

                // Don't throw - this is enhancement, not critical
                return false;
            }
        }

        /// <summary>
        /// Complete transaction - move order file to Completed and clear state
        /// </summary>
        public bool CompleteTransaction(string orderKey)
        {
            try
            {
                _metrics.IncrementCounter("transactiontracker.complete.attempts");

                _logger.LogInformation(LogCategory.Transaction,
                    $"Completing transaction: OrderKey={orderKey}",
                    source: "TransactionStateTracker");

                // 1. Update transaction manager
                _transactionManager.CompleteTransaction();

                // 2. Move order files (.txt and _Fiscal.txt) to Completed folder
                // Eğer hedef dosya varsa _old yapılır
                bool moveSuccess = LogManagerOrder.MoveOrderFilesToCompleted(orderKey);

                if (!moveSuccess)
                {
                    _logger.LogWarning(LogCategory.Transaction,
                        $"Failed to move order files to Completed: OrderKey={orderKey}",
                        source: "TransactionStateTracker");
                }

                // 3. Reset transaction
                _transactionManager.ResetTransaction();

                // 4. Save state (cleared state)
                if (_persistenceService.AutoSaveEnabled)
                {
                    _persistenceService.SaveCurrentState();
                }

                _metrics.IncrementCounter("transactiontracker.complete.successes");

                _logger.LogInformation(LogCategory.Transaction,
                    $"Transaction completed: OrderKey={orderKey}, FileMoved={moveSuccess}",
                    source: "TransactionStateTracker");

                return moveSuccess;
            }
            catch (Exception ex)
            {
                _metrics.IncrementCounter("transactiontracker.complete.failures");

                _logger.LogError(LogCategory.Transaction,
                    $"Failed to complete transaction: OrderKey={orderKey}",
                    exception: ex,
                    source: "TransactionStateTracker");

                // Don't throw - this is enhancement
                return false;
            }
        }

        /// <summary>
        /// Cancel transaction - move order file to Cancel and clear state
        /// </summary>
        public bool CancelTransaction(string orderKey)
        {
            try
            {
                _metrics.IncrementCounter("transactiontracker.cancel.attempts");

                _logger.LogInformation(LogCategory.Transaction,
                    $"Cancelling transaction: OrderKey={orderKey}",
                    source: "TransactionStateTracker");

                // 1. Update transaction manager
                _transactionManager.CancelTransaction();

                // 2. Move order file to Cancel folder
                bool moveSuccess = _logManager.MoveLogFile(orderKey, LogFolderType.Cancel);

                if (!moveSuccess)
                {
                    _logger.LogWarning(LogCategory.Transaction,
                        $"Failed to move order file to Cancel: OrderKey={orderKey}",
                        source: "TransactionStateTracker");
                }

                // 3. Reset transaction
                _transactionManager.ResetTransaction();

                // 4. Save state (cleared state)
                if (_persistenceService.AutoSaveEnabled)
                {
                    _persistenceService.SaveCurrentState();
                }

                _metrics.IncrementCounter("transactiontracker.cancel.successes");

                _logger.LogInformation(LogCategory.Transaction,
                    $"Transaction cancelled: OrderKey={orderKey}, FileMoved={moveSuccess}",
                    source: "TransactionStateTracker");

                return moveSuccess;
            }
            catch (Exception ex)
            {
                _metrics.IncrementCounter("transactiontracker.cancel.failures");

                _logger.LogError(LogCategory.Transaction,
                    $"Failed to cancel transaction: OrderKey={orderKey}",
                    exception: ex,
                    source: "TransactionStateTracker");

                // Don't throw - this is enhancement
                return false;
            }
        }

        /// <summary>
        /// Mark transaction as exception - move order file to Exception folder
        /// </summary>
        public bool MarkTransactionAsException(string orderKey, Exception exception = null)
        {
            try
            {
                _metrics.IncrementCounter("transactiontracker.exception.attempts");

                _logger.LogInformation(LogCategory.Transaction,
                    $"Marking transaction as exception: OrderKey={orderKey}",
                    source: "TransactionStateTracker");

                // 1. Log exception to LogManager
                if (exception != null)
                {
                    _logManager.LogException(exception, "Transaction", orderKey);
                }

                // 2. Move order file to Exception folder
                bool moveSuccess = _logManager.MoveLogFile(orderKey, LogFolderType.Exception);

                if (!moveSuccess)
                {
                    _logger.LogWarning(LogCategory.Transaction,
                        $"Failed to move order file to Exception: OrderKey={orderKey}",
                        source: "TransactionStateTracker");
                }

                // 3. Reset transaction
                _transactionManager.ResetTransaction();

                // 4. Save state (cleared state)
                if (_persistenceService.AutoSaveEnabled)
                {
                    _persistenceService.SaveCurrentState();
                }

                _metrics.IncrementCounter("transactiontracker.exception.successes");

                _logger.LogInformation(LogCategory.Transaction,
                    $"Transaction marked as exception: OrderKey={orderKey}, FileMoved={moveSuccess}",
                    source: "TransactionStateTracker");

                return moveSuccess;
            }
            catch (Exception ex)
            {
                _metrics.IncrementCounter("transactiontracker.exception.failures");

                _logger.LogError(LogCategory.Transaction,
                    $"Failed to mark transaction as exception: OrderKey={orderKey}",
                    exception: ex,
                    source: "TransactionStateTracker");

                // Don't throw - this is enhancement
                return false;
            }
        }

        /// <summary>
        /// Check if order file exists in Waiting folder
        /// </summary>
        public bool HasPendingOrder(string orderKey)
        {
            try
            {
                var commands = _logManager.GetOrderFile(orderKey);
                return commands != null && commands.Count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(LogCategory.Transaction,
                    $"Failed to check pending order: OrderKey={orderKey}",
                    exception: ex,
                    source: "TransactionStateTracker");

                return false;
            }
        }
    }

    /// <summary>
    /// Transaction tracking result
    /// </summary>
    public class TransactionTrackingResult
    {
        public bool Success { get; set; }
        public string OrderKey { get; set; }
        public ulong TransactionHandle { get; set; }

        /// <summary>
        /// Order data saved to file (LogManagerOrder)
        /// </summary>
        public bool OrderDataSaved { get; set; }

        /// <summary>
        /// Transaction state updated in TransactionManager
        /// </summary>
        public bool StateUpdated { get; set; }

        /// <summary>
        /// Technical state persisted (PersistenceService)
        /// </summary>
        public bool StatePersisted { get; set; }

        public string ErrorMessage { get; set; }

        public TransactionTrackingResult()
        {
            Success = false;
            OrderKey = string.Empty;
            ErrorMessage = string.Empty;
        }
    }
}