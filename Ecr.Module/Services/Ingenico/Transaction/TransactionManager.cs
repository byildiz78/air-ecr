using Ecr.Module.Services.Ingenico.Connection;
using Ecr.Module.Services.Ingenico.GmpIngenico;
using Ecr.Module.Services.Ingenico.Models;
using System;

namespace Ecr.Module.Services.Ingenico.Transaction
{
    /// <summary>
    /// Centralized transaction yönetimi
    /// Thread-safe singleton pattern
    /// PDF Section 5.2 - Transaction Management
    /// </summary>
    public class TransactionManager
    {
        private static readonly object _lock = new object();
        private static TransactionManager _instance;
        private TransactionInfo _currentTransaction;
        private readonly ConnectionManager _connectionManager;

        private TransactionManager()
        {
            _currentTransaction = new TransactionInfo();
            _connectionManager = ConnectionManager.Instance;
        }

        public static TransactionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new TransactionManager();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Mevcut transaction'ı al
        /// </summary>
        public TransactionInfo GetCurrentTransaction()
        {
            lock (_lock)
            {
                return _currentTransaction;
            }
        }

        /// <summary>
        /// Aktif transaction var mı?
        /// </summary>
        public bool HasActiveTransaction()
        {
            lock (_lock)
            {
                return _currentTransaction.IsHandleValid &&
                       _currentTransaction.State != TransactionState.Completed &&
                       _currentTransaction.State != TransactionState.Cancelled;
            }
        }

        /// <summary>
        /// Transaction başlat
        /// </summary>
        public void StartTransaction(ulong handle, string uniqueId, string orderKey)
        {
            lock (_lock)
            {
                _currentTransaction = new TransactionInfo
                {
                    Handle = handle,
                    State = TransactionState.Started,
                    UniqueId = uniqueId,
                    OrderKey = orderKey,
                    StartTime = DateTime.Now,
                    LastUpdateTime = DateTime.Now,
                    TimeoutMs = 300000 // 5 dakika
                };

                // DataStore ile sync (backward compatibility)
                DataStore.ActiveTransactionHandle = handle;
                DataStore.MergeUniqueID = uniqueId;
            }
        }

        /// <summary>
        /// Transaction state'i güncelle
        /// </summary>
        public void UpdateState(TransactionState newState)
        {
            lock (_lock)
            {
                if (_currentTransaction.Handle > 0)
                {
                    _currentTransaction.State = newState;
                    _currentTransaction.LastUpdateTime = DateTime.Now;
                }
            }
        }

        /// <summary>
        /// Transaction'ı tamamla
        /// </summary>
        public void CompleteTransaction()
        {
            lock (_lock)
            {
                _currentTransaction.State = TransactionState.Completed;
                _currentTransaction.LastUpdateTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Transaction'ı iptal et
        /// </summary>
        public void CancelTransaction()
        {
            lock (_lock)
            {
                _currentTransaction.State = TransactionState.Cancelled;
                _currentTransaction.LastUpdateTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Transaction'ı reset et
        /// </summary>
        public void ResetTransaction()
        {
            lock (_lock)
            {
                _currentTransaction = new TransactionInfo();
                DataStore.ActiveTransactionHandle = 0;
                DataStore.MergeUniqueID = string.Empty;
            }
        }

        /// <summary>
        /// Transaction handle'ı validate et
        /// </summary>
        public TransactionValidationResult ValidateCurrentTransaction()
        {
            lock (_lock)
            {
                if (!_currentTransaction.IsHandleValid)
                {
                    return new TransactionValidationResult
                    {
                        Handle = _currentTransaction.Handle,
                        IsValid = false,
                        ErrorMessage = "No active transaction"
                    };
                }

                var connectionState = _connectionManager.GetState();
                if (connectionState.CurrentInterface == 0)
                {
                    return new TransactionValidationResult
                    {
                        Handle = _currentTransaction.Handle,
                        IsValid = false,
                        ErrorMessage = "No valid interface"
                    };
                }

                // FP3_GetTicket ile validate et
                return TransactionValidator.ValidateWithGetTicket(
                    connectionState.CurrentInterface,
                    _currentTransaction.Handle,
                    Defines.TIMEOUT_DEFAULT
                );
            }
        }

        /// <summary>
        /// Ticket bilgisini güncelle (FP3_GetTicket sonrası)
        /// </summary>
        public void UpdateTicketInfo(ST_TICKET ticket)
        {
            lock (_lock)
            {
                if (_currentTransaction.Handle > 0)
                {
                    _currentTransaction.TicketInfo = ticket;
                    _currentTransaction.LastUpdateTime = DateTime.Now;
                }
            }
        }

        /// <summary>
        /// Transaction timeout kontrolü
        /// </summary>
        public bool IsCurrentTransactionTimedOut()
        {
            lock (_lock)
            {
                return _currentTransaction.IsTimedOut;
            }
        }

        /// <summary>
        /// Transaction recovery gerekiyor mu?
        /// </summary>
        public bool NeedsRecovery()
        {
            lock (_lock)
            {
                return _currentTransaction.NeedsRecovery;
            }
        }

        /// <summary>
        /// Error durumunu set et
        /// </summary>
        public void SetError(uint errorCode)
        {
            lock (_lock)
            {
                _currentTransaction.State = TransactionState.Error;
                _currentTransaction.LastErrorCode = errorCode;
                _currentTransaction.LastUpdateTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Transaction recovery yap
        /// PDF Section 5.2.3 - Transaction Recovery
        /// </summary>
        public TransactionRecoveryResult RecoverTransaction()
        {
            lock (_lock)
            {
                var result = new TransactionRecoveryResult
                {
                    OriginalHandle = _currentTransaction.Handle,
                    OriginalState = _currentTransaction.State
                };

                // Validation yap
                var validation = ValidateCurrentTransaction();
                result.ValidationResult = validation;

                if (!validation.IsValid)
                {
                    // Transaction gerçekten yok, reset et
                    if (validation.ErrorCode == Defines.APP_ERR_GMP3_NO_HANDLE)
                    {
                        ResetTransaction();
                        result.RecoveryAction = TransactionRecoveryAction.Reset;
                        result.Success = true;
                        result.Message = "Transaction not found, reset completed";
                    }
                    else
                    {
                        result.RecoveryAction = TransactionRecoveryAction.Failed;
                        result.Success = false;
                        result.Message = validation.ErrorMessage;
                    }
                }
                else if (validation.IsActive)
                {
                    // Transaction hala aktif, ticket bilgisini güncelle
                    UpdateTicketInfo(validation.TicketInfo);

                    result.RecoveryAction = TransactionRecoveryAction.Resumed;
                    result.Success = true;
                    result.Message = "Transaction resumed successfully";
                }

                return result;
            }
        }
    }

    /// <summary>
    /// Transaction recovery action
    /// </summary>
    public enum TransactionRecoveryAction
    {
        None,
        Reset,
        Resumed,
        Cancelled,
        Failed
    }

    /// <summary>
    /// Transaction recovery sonucu
    /// </summary>
    public class TransactionRecoveryResult
    {
        public bool Success { get; set; }
        public TransactionRecoveryAction RecoveryAction { get; set; }
        public string Message { get; set; }
        public ulong OriginalHandle { get; set; }
        public TransactionState OriginalState { get; set; }
        public TransactionValidationResult ValidationResult { get; set; }

        public TransactionRecoveryResult()
        {
            Success = false;
            RecoveryAction = TransactionRecoveryAction.None;
            Message = string.Empty;
        }
    }
}