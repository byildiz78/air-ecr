using Ecr.Module.Services.Ingenico.Connection;
using Ecr.Module.Services.Ingenico.Diagnostics;
using Ecr.Module.Services.Ingenico.FiscalLogManager;
using Ecr.Module.Services.Ingenico.GmpIngenico;
using Ecr.Module.Services.Ingenico.Models;
using Ecr.Module.Services.Ingenico.Persistence;
using Ecr.Module.Services.Ingenico.Print;
using Ecr.Module.Services.Ingenico.SingleMethod;
using Ecr.Module.Services.Ingenico.Transaction;
using System;
using System.Collections.Generic;

namespace Ecr.Module.Services.Ingenico.Recovery
{
    /// <summary>
    /// Recovery coordinator - Orchestrates recovery between PersistenceService and LogManagerOrder
    ///
    /// Purpose: Application startup recovery orchestrator
    /// - Restores technical state (PersistenceService)
    /// - Validates with device (FP3_GetTicket)
    /// - Checks order data (LogManagerOrder)
    /// - Decides recovery action
    ///
    /// IMPORTANT: This is OPTIONAL enhancement
    /// - Wrapped in try-catch - never breaks application startup
    /// - Existing flow continues if recovery fails
    /// - Non-invasive design
    ///
    /// PDF Reference: Section 5.2.3 - Transaction Recovery
    /// </summary>
    public class RecoveryCoordinator
    {
        private readonly PersistenceService _persistence;
        private readonly TransactionManager _transactionManager;
        private readonly ConnectionManager _connectionManager;
        private readonly LogManagerOrderV2 _logManager;
        private readonly DiagnosticLogger _logger;
        private readonly DiagnosticMetrics _metrics;

        public RecoveryCoordinator()
        {
            _persistence = new PersistenceService();
            _transactionManager = TransactionManager.Instance;
            _connectionManager = ConnectionManager.Instance;
            _logManager = new LogManagerOrderV2();
            _logger = DiagnosticLogger.Instance;
            _metrics = DiagnosticMetrics.Instance;
        }

        /// <summary>
        /// Attempt recovery on application startup
        /// NON-INVASIVE - doesn't break existing flow
        /// </summary>
        public RecoveryCoordinatorResult AttemptRecovery()
        {
            Console.WriteLine("[RECOVERY] ========================================");
            Console.WriteLine("[RECOVERY] AttemptRecovery - STARTING");
            Console.WriteLine("[RECOVERY] ========================================");

            var result = new RecoveryCoordinatorResult();

            try
            {
                _metrics.IncrementCounter("recovery.coordinator.attempts");

                _logger.LogInformation(LogCategory.Recovery,
                    "Starting recovery coordinator...",
                    source: "RecoveryCoordinator");

                Console.WriteLine("[RECOVERY] AttemptRecovery - Metrics incremented");

                // Step 1: Try restore technical state
                Console.WriteLine("[RECOVERY] AttemptRecovery - Step 1: Restoring state...");
                result.StateRestoreResult = _persistence.RestoreState();
                Console.WriteLine($"[RECOVERY] AttemptRecovery - State restore success: {result.StateRestoreResult.Success}");

                if (!result.StateRestoreResult.Success)
                {
                    Console.WriteLine($"[RECOVERY] AttemptRecovery - No state to restore: {result.StateRestoreResult.Message}");

                    _logger.LogInformation(LogCategory.Recovery,
                        $"No state to restore: {result.StateRestoreResult.Message}",
                        source: "RecoveryCoordinator");

                    result.Success = false;
                    result.Message = "No persisted state found";
                    result.RecoveryAction = RecoveryActionType.None;

                    // Check for orphan orders in Waiting folder
                    Console.WriteLine("[RECOVERY] AttemptRecovery - Checking for orphan orders...");
                    CheckOrphanOrders(result);

                    Console.WriteLine("[RECOVERY] AttemptRecovery - Returning result (no state)");
                    return result;
                }

                // Step 2: Check if transaction was active
                Console.WriteLine("[RECOVERY] AttemptRecovery - Step 2: Checking transaction...");
                Console.WriteLine($"[RECOVERY] AttemptRecovery - Transaction restored: {result.StateRestoreResult.TransactionRestored}");

                if (!result.StateRestoreResult.TransactionRestored)
                {
                    Console.WriteLine("[RECOVERY] AttemptRecovery - No transaction state to restore");

                    _logger.LogInformation(LogCategory.Recovery,
                        "No transaction state to restore",
                        source: "RecoveryCoordinator");

                    result.Success = false;
                    result.Message = "No active transaction to restore";
                    result.RecoveryAction = RecoveryActionType.None;

                    // Check for orphan orders
                    Console.WriteLine("[RECOVERY] AttemptRecovery - Checking for orphan orders...");
                    CheckOrphanOrders(result);

                    Console.WriteLine("[RECOVERY] AttemptRecovery - Returning result (no transaction)");
                    return result;
                }

                // Step 3: Get transaction info
                var transaction = _transactionManager.GetCurrentTransaction();

                _logger.LogInformation(LogCategory.Recovery,
                    $"Transaction state restored: Handle={transaction.Handle}, State={transaction.State}, OrderKey={transaction.OrderKey}",
                    source: "RecoveryCoordinator");

                result.TransactionHandle = transaction.Handle;
                result.TransactionState = transaction.State;
                result.OrderKey = transaction.OrderKey;

                // Step 4: Check connection state
                var connectionState = _connectionManager.GetState();
                if (connectionState.CurrentInterface == 0)
                {
                    _logger.LogWarning(LogCategory.Recovery,
                        "No valid interface - cannot validate transaction",
                        source: "RecoveryCoordinator");

                    result.Success = false;
                    result.Message = "No valid interface";
                    result.RecoveryAction = RecoveryActionType.RequiresConnection;

                    return result;
                }

                // Step 5: Validate with FP3_GetTicket (if connected)
                if (connectionState.Status == Models.ConnectionStatus.Connected)
                {
                    result.ValidationResult = ValidateTransactionWithDevice(transaction);
                }
                else
                {
                    _logger.LogWarning(LogCategory.Recovery,
                        "Not connected - cannot validate transaction with device",
                        source: "RecoveryCoordinator");

                    result.ValidationResult = new TransactionValidationResult
                    {
                        Handle = transaction.Handle,
                        IsValid = false,
                        ErrorMessage = "Not connected to device"
                    };
                }

                // Step 6: Check order data existence
                result.OrderCommands = CheckOrderData(transaction.OrderKey);

                // Step 7: Decide recovery action
                DecideRecoveryAction(result, transaction);

                // Step 8: Execute recovery action (if safe)
                ExecuteRecoveryAction(result, transaction);

                if (result.Success)
                {
                    _metrics.IncrementCounter("recovery.coordinator.successes");
                }
                else
                {
                    _metrics.IncrementCounter("recovery.coordinator.failures");
                }

                _logger.LogInformation(LogCategory.Recovery,
                    $"Recovery coordinator completed: Action={result.RecoveryAction}, Success={result.Success}, Message={result.Message}",
                    source: "RecoveryCoordinator");

                return result;
            }
            catch (Exception ex)
            {
                _metrics.IncrementCounter("recovery.coordinator.failures");

                _logger.LogError(LogCategory.Recovery,
                    "Recovery coordinator exception",
                    exception: ex,
                    source: "RecoveryCoordinator");

                result.Success = false;
                result.Message = $"Recovery exception: {ex.Message}";
                result.RecoveryAction = RecoveryActionType.Failed;

                return result;
            }
        }

        /// <summary>
        /// Validate transaction with device using FP3_GetTicket
        /// </summary>
        private TransactionValidationResult ValidateTransactionWithDevice(TransactionInfo transaction)
        {
            try
            {
                _logger.LogInformation(LogCategory.Recovery,
                    $"Validating transaction with device: Handle={transaction.Handle}",
                    source: "RecoveryCoordinator");

                var validation = _transactionManager.ValidateCurrentTransaction();

                if (validation.IsValid)
                {
                    _logger.LogInformation(LogCategory.Recovery,
                        $"Transaction validated successfully: Handle={transaction.Handle}, IsActive={validation.IsActive}",
                        source: "RecoveryCoordinator");
                }
                else
                {
                    _logger.LogWarning(LogCategory.Recovery,
                        $"Transaction validation failed: Handle={transaction.Handle}, Error={validation.ErrorMessage}",
                        source: "RecoveryCoordinator");
                }

                return validation;
            }
            catch (Exception ex)
            {
                _logger.LogError(LogCategory.Recovery,
                    $"Transaction validation exception: Handle={transaction.Handle}",
                    exception: ex,
                    source: "RecoveryCoordinator");

                return new TransactionValidationResult
                {
                    Handle = transaction.Handle,
                    IsValid = false,
                    ErrorMessage = $"Validation exception: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Check if order data exists in LogManager
        /// </summary>
        private List<GmpCommand> CheckOrderData(string orderKey)
        {
            try
            {
                if (string.IsNullOrEmpty(orderKey))
                {
                    _logger.LogWarning(LogCategory.Recovery,
                        "OrderKey is empty - cannot check order data",
                        source: "RecoveryCoordinator");
                    return new List<GmpCommand>();
                }

                _logger.LogInformation(LogCategory.Recovery,
                    $"Checking order data: OrderKey={orderKey}",
                    source: "RecoveryCoordinator");

                var orderFile = _logManager.GetOrderFile(orderKey);

                if (orderFile != null && orderFile.Count > 0)
                {
                    _logger.LogInformation(LogCategory.Recovery,
                        $"Order data found: OrderKey={orderKey}, CommandCount={orderFile.Count}",
                        source: "RecoveryCoordinator");
                }
                else
                {
                    _logger.LogWarning(LogCategory.Recovery,
                        $"No order data found: OrderKey={orderKey}",
                        source: "RecoveryCoordinator");
                }

                return orderFile ?? new List<GmpCommand>();
            }
            catch (Exception ex)
            {
                _logger.LogError(LogCategory.Recovery,
                    $"Failed to check order data: OrderKey={orderKey}",
                    exception: ex,
                    source: "RecoveryCoordinator");

                return new List<GmpCommand>();
            }
        }

        /// <summary>
        /// Decide recovery action based on validation and order data
        /// </summary>
        private void DecideRecoveryAction(RecoveryCoordinatorResult result, TransactionInfo transaction)
        {
            _logger.LogInformation(LogCategory.Recovery,
                "Deciding recovery action...",
                source: "RecoveryCoordinator");

            // Age check - transaction too old?
            TimeSpan age = DateTime.Now - transaction.LastUpdateTime;
            if (age.TotalMinutes > 30)
            {
                _logger.LogWarning(LogCategory.Recovery,
                    $"Transaction too old ({age.TotalMinutes:F1} minutes) - aborting",
                    source: "RecoveryCoordinator");

                result.Success = false;
                result.Message = $"Transaction too old ({age.TotalMinutes:F1} minutes)";
                result.RecoveryAction = RecoveryActionType.Abort;
                return;
            }

            // No order data?
            if (result.OrderCommands == null || result.OrderCommands.Count == 0)
            {
                _logger.LogWarning(LogCategory.Recovery,
                    "Transaction state exists but no order data found",
                    source: "RecoveryCoordinator");

                result.Success = false;
                result.Message = "No order data found";
                result.RecoveryAction = RecoveryActionType.Abort;
                return;
            }

            // Validation failed?
            if (result.ValidationResult == null || !result.ValidationResult.IsValid)
            {
                _logger.LogWarning(LogCategory.Recovery,
                    $"Transaction validation failed: {result.ValidationResult?.ErrorMessage}",
                    source: "RecoveryCoordinator");

                // Check error code
                if (result.ValidationResult?.ErrorCode == Defines.APP_ERR_GMP3_NO_HANDLE)
                {
                    // Transaction no longer exists on device
                    result.Success = false;
                    result.Message = "Transaction no longer exists on device";
                    result.RecoveryAction = RecoveryActionType.Reset;
                }
                else
                {
                    // Other validation error
                    result.Success = false;
                    result.Message = result.ValidationResult?.ErrorMessage ?? "Validation failed";
                    result.RecoveryAction = RecoveryActionType.RequiresManualIntervention;
                }

                return;
            }

            // Transaction validated successfully
            if (result.ValidationResult.IsActive)
            {
                _logger.LogInformation(LogCategory.Recovery,
                    "Transaction is active on device - can resume",
                    source: "RecoveryCoordinator");

                result.Success = true;
                result.Message = "Transaction can be resumed";
                result.RecoveryAction = RecoveryActionType.Resume;
            }
            else
            {
                _logger.LogWarning(LogCategory.Recovery,
                    "Transaction is not active on device",
                    source: "RecoveryCoordinator");

                result.Success = false;
                result.Message = "Transaction is not active";
                result.RecoveryAction = RecoveryActionType.Reset;
            }
        }

        /// <summary>
        /// Execute recovery action (if safe and automatic)
        /// </summary>
        private void ExecuteRecoveryAction(RecoveryCoordinatorResult result, TransactionInfo transaction)
        {
            try
            {
                switch (result.RecoveryAction)
                {
                    case RecoveryActionType.Reset:
                        // Safe to reset - transaction doesn't exist on device
                        _logger.LogInformation(LogCategory.Recovery,
                            "Executing Reset action - clearing transaction state",
                            source: "RecoveryCoordinator");

                        _transactionManager.ResetTransaction();

                        // Move order file to Exception folder
                        if (!string.IsNullOrEmpty(transaction.OrderKey))
                        {
                            _logManager.MoveLogFile(transaction.OrderKey, LogFolderType.Exception);
                        }

                        result.ActionExecuted = true;
                        break;

                    case RecoveryActionType.Abort:
                        // Abort - transaction too old or invalid
                        _logger.LogInformation(LogCategory.Recovery,
                            "Executing Abort action - clearing transaction state",
                            source: "RecoveryCoordinator");

                        _transactionManager.ResetTransaction();

                        // Move order file to Exception folder
                        if (!string.IsNullOrEmpty(transaction.OrderKey))
                        {
                            _logManager.MoveLogFile(transaction.OrderKey, LogFolderType.Exception);
                        }

                        result.ActionExecuted = true;
                        break;

                    case RecoveryActionType.Resume:
                        // Resume - DO NOT execute automatically
                        // Leave state as-is for application to handle
                        _logger.LogInformation(LogCategory.Recovery,
                            "Resume action - leaving state for application to handle",
                            source: "RecoveryCoordinator");

                        result.ActionExecuted = false;
                        break;

                    case RecoveryActionType.RequiresManualIntervention:
                    case RecoveryActionType.RequiresConnection:
                        // Manual intervention required - do nothing
                        _logger.LogInformation(LogCategory.Recovery,
                            $"{result.RecoveryAction} action - no automatic execution",
                            source: "RecoveryCoordinator");

                        result.ActionExecuted = false;
                        break;

                    default:
                        result.ActionExecuted = false;
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(LogCategory.Recovery,
                    $"Failed to execute recovery action: {result.RecoveryAction}",
                    exception: ex,
                    source: "RecoveryCoordinator");

                result.ActionExecuted = false;
            }
        }

        /// <summary>
        /// Check for orphan orders in Waiting folder
        /// Orders without corresponding transaction state
        /// </summary>
        private void CheckOrphanOrders(RecoveryCoordinatorResult result)
        {
            try
            {
                Console.WriteLine("[RECOVERY] CheckOrphanOrders - Starting...");

                var waitingFiles = _logManager.ListWaitingLogs();
                Console.WriteLine($"[RECOVERY] CheckOrphanOrders - Found {waitingFiles?.Count ?? 0} waiting files");

                if (waitingFiles != null && waitingFiles.Count > 0)
                {
                    foreach (var file in waitingFiles)
                    {
                        Console.WriteLine($"[RECOVERY] CheckOrphanOrders - Waiting file: {file}");
                    }

                    _logger.LogWarning(LogCategory.Recovery,
                        $"Found {waitingFiles.Count} orphan order(s) in Waiting folder (no transaction state)",
                        source: "RecoveryCoordinator");

                    result.OrphanOrders = waitingFiles;

                    // Try to void orphan fiscal transactions
                    Console.WriteLine("[RECOVERY] CheckOrphanOrders - Calling TryVoidOrphanFiscalOrders...");
                    TryVoidOrphanFiscalOrders(waitingFiles);

                    result.Message = $"Found {waitingFiles.Count} orphan order(s) - attempting to void active transactions";
                }
                else
                {
                    Console.WriteLine("[RECOVERY] CheckOrphanOrders - No orphan files found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RECOVERY] CheckOrphanOrders - ERROR: {ex.Message}");
                Console.WriteLine($"[RECOVERY] CheckOrphanOrders - StackTrace: {ex.StackTrace}");

                _logger.LogError(LogCategory.Recovery,
                    "Failed to check orphan orders",
                    exception: ex,
                    source: "RecoveryCoordinator");
            }
        }

        /// <summary>
        /// Try to void orphan fiscal transactions on Ingenico device
        /// If GetTicket returns data, the transaction is still active - void it
        /// </summary>
        private void TryVoidOrphanFiscalOrders(List<string> orphanFiles)
        {
            try
            {
                Console.WriteLine("[RECOVERY] TryVoidOrphanFiscalOrders - Starting...");
                Console.WriteLine($"[RECOVERY] TryVoidOrphanFiscalOrders - Total orphan files: {orphanFiles.Count}");

                // Filter only Fiscal files (without .txt extension)
                var fiscalFiles = orphanFiles.FindAll(f => f.EndsWith("_Fiscal"));
                Console.WriteLine($"[RECOVERY] TryVoidOrphanFiscalOrders - Fiscal files: {fiscalFiles.Count}");

                foreach (var f in fiscalFiles)
                {
                    Console.WriteLine($"[RECOVERY] TryVoidOrphanFiscalOrders - Fiscal file: {f}");
                }

                if (fiscalFiles.Count == 0)
                {
                    Console.WriteLine("[RECOVERY] TryVoidOrphanFiscalOrders - No Fiscal files, exiting");
                    _logger.LogInformation(LogCategory.Recovery,
                        "No orphan Fiscal files found - skipping void check",
                        source: "RecoveryCoordinator");
                    return;
                }

                _logger.LogInformation(LogCategory.Recovery,
                    $"Found {fiscalFiles.Count} orphan Fiscal file(s) - checking for active transactions",
                    source: "RecoveryCoordinator");

                foreach (var fiscalFile in fiscalFiles)
                {
                    Console.WriteLine($"[RECOVERY] TryVoidOrphanFiscalOrders - Processing: {fiscalFile}");
                    TryVoidSingleOrphanTransaction(fiscalFile);
                }

                Console.WriteLine("[RECOVERY] TryVoidOrphanFiscalOrders - Completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RECOVERY] TryVoidOrphanFiscalOrders - ERROR: {ex.Message}");
                Console.WriteLine($"[RECOVERY] TryVoidOrphanFiscalOrders - StackTrace: {ex.StackTrace}");

                _logger.LogError(LogCategory.Recovery,
                    "Failed to void orphan fiscal orders",
                    exception: ex,
                    source: "RecoveryCoordinator");
            }
        }

        /// <summary>
        /// Try to void a single orphan transaction
        /// </summary>
        private void TryVoidSingleOrphanTransaction(string fiscalFileName)
        {
            try
            {
                Console.WriteLine($"[RECOVERY] TryVoidSingleOrphanTransaction - File: {fiscalFileName}");

                // Extract OrderKey from filename: "GUID_Fiscal" -> "GUID"
                string orderKey = fiscalFileName;
                Console.WriteLine($"[RECOVERY] TryVoidSingleOrphanTransaction - OrderKey: {orderKey}");

                _logger.LogInformation(LogCategory.Recovery,
                    $"Processing orphan Fiscal file: OrderKey={orderKey}",
                    source: "RecoveryCoordinator");

                // Check if there's an active transaction on device by reading the file
                Console.WriteLine($"[RECOVERY] TryVoidSingleOrphanTransaction - Reading fiscal file...");
                var fiscalData = _logManager.GetOrderFileFiscal(orderKey);

                if (fiscalData == null)
                {
                    Console.WriteLine($"[RECOVERY] TryVoidSingleOrphanTransaction - Fiscal data is NULL");
                    _logger.LogWarning(LogCategory.Recovery,
                        $"Could not read Fiscal file: OrderKey={orderKey}",
                        source: "RecoveryCoordinator");
                    return;
                }

                Console.WriteLine($"[RECOVERY] TryVoidSingleOrphanTransaction - Fiscal data loaded successfully");

                // Eğer nullable alanlar null ise, bu önceki void attempt'ten kalmıştır - skip et
                if (!fiscalData.PrintInvoice.HasValue || !fiscalData.IsFiscalOrder.HasValue)
                {
                    Console.WriteLine($"[RECOVERY] TryVoidSingleOrphanTransaction - Fiscal data has null fields (previous void attempt), moving to Exception");
                    _logger.LogWarning(LogCategory.Recovery,
                        $"Fiscal file has null fields (corrupted or previous void attempt): OrderKey={orderKey}",
                        source: "RecoveryCoordinator");

                    // Exception'a taşı
                    _logManager.MoveLogFile(orderKey, LogFolderType.Exception);
                    _metrics.IncrementCounter("recovery.orphan.corrupted");
                    return;
                }

                // Check connection
                var connectionState = _connectionManager.GetState();
                Console.WriteLine($"[RECOVERY] TryVoidSingleOrphanTransaction - Connection Status: {connectionState.Status}, Interface: {connectionState.CurrentInterface}");

                if (connectionState.Status != Models.ConnectionStatus.Connected || connectionState.CurrentInterface == 0)
                {
                    Console.WriteLine($"[RECOVERY] TryVoidSingleOrphanTransaction - Not connected, skipping");
                    _logger.LogWarning(LogCategory.Recovery,
                        $"Not connected - cannot check transaction on device: OrderKey={orderKey}",
                        source: "RecoveryCoordinator");
                    return;
                }

                // Call EftPosPrintOrder with IsVoidedFiscal=true to void the transaction
                Console.WriteLine($"[RECOVERY] TryVoidSingleOrphanTransaction - Calling VoidOrphanTransaction...");
                _logger.LogInformation(LogCategory.Recovery,
                    $"Attempting to void orphan transaction: OrderKey={orderKey}",
                    source: "RecoveryCoordinator");

                var voidResult = VoidOrphanTransaction(fiscalData, orderKey);

                if (voidResult)
                {
                    Console.WriteLine($"[RECOVERY] TryVoidSingleOrphanTransaction - Void SUCCESSFUL, moving to Cancel");
                    _logger.LogInformation(LogCategory.Recovery,
                        $"Successfully voided orphan transaction: OrderKey={orderKey}",
                        source: "RecoveryCoordinator");

                    // Move files to Cancel folder
                    _logManager.MoveLogFile(orderKey, LogFolderType.Cancel);
                    _metrics.IncrementCounter("recovery.orphan.voided");
                }
                else
                {
                    Console.WriteLine($"[RECOVERY] TryVoidSingleOrphanTransaction - Void FAILED, moving to Exception");
                    _logger.LogWarning(LogCategory.Recovery,
                        $"Failed to void orphan transaction (may not exist on device): OrderKey={orderKey}",
                        source: "RecoveryCoordinator");

                    // Move files to Exception folder
                    _logManager.MoveLogFile(orderKey, LogFolderType.Exception);
                    _metrics.IncrementCounter("recovery.orphan.exception");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RECOVERY] TryVoidSingleOrphanTransaction - ERROR: {ex.Message}");
                Console.WriteLine($"[RECOVERY] TryVoidSingleOrphanTransaction - StackTrace: {ex.StackTrace}");

                _logger.LogError(LogCategory.Recovery,
                    $"Exception while voiding orphan transaction: File={fiscalFileName}",
                    exception: ex,
                    source: "RecoveryCoordinator");
            }
        }

        /// <summary>
        /// Void orphan transaction by calling EftPosPrintOrder with IsVoidedFiscal=true
        /// </summary>
        private bool VoidOrphanTransaction(FiscalOrder fiscalData, string orderKey)
        {
            try
            {
                Console.WriteLine($"[RECOVERY] VoidOrphanTransaction - OrderKey: {orderKey}");
                Console.WriteLine($"[RECOVERY] VoidOrphanTransaction - Current IsVoidedFiscal: {fiscalData.IsVoidedFiscal}");

                // Set IsVoidedFiscal to true to void the transaction
                fiscalData.IsVoidedFiscal = true;
                Console.WriteLine($"[RECOVERY] VoidOrphanTransaction - Set IsVoidedFiscal = true");

                _logger.LogInformation(LogCategory.Recovery,
                    $"Calling EftPosPrintOrder to void transaction: OrderKey={orderKey}",
                    source: "RecoveryCoordinator");

                // Call print service to void
                Console.WriteLine($"[RECOVERY] VoidOrphanTransaction - Calling PrintReceiptGmpProvider.EftPosPrintOrder...");
                var voidResult = PrintVoid.EftPosVoidPrintOrder();
               
                if (voidResult != null && voidResult.ReturnCode == 0  && voidResult.ReturnCode == Defines.TRAN_RESULT_OK)
                {
                    Console.WriteLine($"[RECOVERY] VoidOrphanTransaction - SUCCESS!");
                    return true;
                }
                else
                {
                    Console.WriteLine($"[RECOVERY] VoidOrphanTransaction - FAILED");
                   
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RECOVERY] VoidOrphanTransaction - EXCEPTION: {ex.Message}");
                Console.WriteLine($"[RECOVERY] VoidOrphanTransaction - StackTrace: {ex.StackTrace}");

                _logger.LogError(LogCategory.Recovery,
                    $"Exception during void operation: OrderKey={orderKey}",
                    exception: ex,
                    source: "RecoveryCoordinator");
                return false;
            }
        }
    }

    /// <summary>
    /// Recovery action to take
    /// </summary>
    public enum RecoveryActionType
    {
        None,                           // No recovery needed
        Resume,                         // Transaction can be resumed
        Reset,                          // Reset transaction state
        Abort,                          // Abort transaction (too old, invalid)
        RequiresManualIntervention,     // Manual intervention required
        RequiresConnection,             // Connection required first
        Failed                          // Recovery failed
    }

    /// <summary>
    /// Recovery coordinator result
    /// </summary>
    public class RecoveryCoordinatorResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public RecoveryActionType RecoveryAction { get; set; }
        public bool ActionExecuted { get; set; }

        /// <summary>
        /// State restore result from PersistenceService
        /// </summary>
        public StateRestoreResult StateRestoreResult { get; set; }

        /// <summary>
        /// Transaction validation result from FP3_GetTicket
        /// </summary>
        public TransactionValidationResult ValidationResult { get; set; }

        /// <summary>
        /// Transaction handle
        /// </summary>
        public ulong TransactionHandle { get; set; }

        /// <summary>
        /// Transaction state
        /// </summary>
        public TransactionState TransactionState { get; set; }

        /// <summary>
        /// Order key
        /// </summary>
        public string OrderKey { get; set; }

        /// <summary>
        /// Order commands from LogManager
        /// </summary>
        public List<GmpCommand> OrderCommands { get; set; }

        /// <summary>
        /// Orphan orders found in Waiting folder (no transaction state)
        /// </summary>
        public List<string> OrphanOrders { get; set; }

        public RecoveryCoordinatorResult()
        {
            Success = false;
            Message = string.Empty;
            RecoveryAction = RecoveryActionType.None;
            ActionExecuted = false;
            OrderKey = string.Empty;
            OrderCommands = new List<GmpCommand>();
            OrphanOrders = new List<string>();
        }
    }
}