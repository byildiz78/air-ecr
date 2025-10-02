using Ecr.Module.Services.Ingenico.Connection;
using Ecr.Module.Services.Ingenico.Transaction;
using System;

namespace Ecr.Module.Services.Ingenico.Persistence
{
    /// <summary>
    /// State persistence service
    /// Connection ve Transaction state'lerini persist eder
    /// </summary>
    public class PersistenceService
    {
        private readonly IPersistenceProvider _provider;
        private readonly ConnectionManager _connectionManager;
        private readonly TransactionManager _transactionManager;

        /// <summary>
        /// Auto-save enabled mi?
        /// </summary>
        public bool AutoSaveEnabled { get; set; } = true;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="provider">Persistence provider (default: File-based)</param>
        public PersistenceService(IPersistenceProvider provider = null)
        {
            _provider = provider ?? new FilePersistenceProvider();
            _connectionManager = ConnectionManager.Instance;
            _transactionManager = TransactionManager.Instance;
        }

        /// <summary>
        /// Mevcut state'i kaydet
        /// </summary>
        public bool SaveCurrentState()
        {
            try
            {
                var state = new PersistedState
                {
                    ApplicationVersion = GetApplicationVersion()
                };

                // Connection state
                var connectionState = _connectionManager.GetState();
                state.Connection = PersistedConnectionState.FromConnectionState(connectionState);

                // Transaction state
                var transactionInfo = _transactionManager.GetCurrentTransaction();
                state.Transaction = PersistedTransactionState.FromTransactionInfo(transactionInfo);

                return _provider.Save(state);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveCurrentState error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// State'i yükle ve restore et
        /// </summary>
        public StateRestoreResult RestoreState()
        {
            var result = new StateRestoreResult
            {
                Success = false
            };

            try
            {
                // State var mı?
                if (!_provider.Exists())
                {
                    result.Message = "No persisted state found";
                    return result;
                }

                // Load
                var state = _provider.Load();
                if (state == null)
                {
                    result.Message = "Failed to load persisted state";
                    return result;
                }

                result.PersistedState = state;

                // Age check (çok eski ise restore etme)
                TimeSpan age = DateTime.Now - state.SavedAt;
                if (age.TotalHours > 24)
                {
                    result.Message = $"State too old ({age.TotalHours:F1} hours), not restoring";
                    result.StateAge = age;
                    return result;
                }

                result.StateAge = age;

                // Connection state restore
                result.ConnectionRestored = RestoreConnectionState(state.Connection);

                // Transaction state restore
                result.TransactionRestored = RestoreTransactionState(state.Transaction);

                result.Success = result.ConnectionRestored || result.TransactionRestored;
                result.Message = result.Success ? "State restored successfully" : "Failed to restore state";

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Restore exception: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// Connection state'i restore et
        /// </summary>
        private bool RestoreConnectionState(PersistedConnectionState persisted)
        {
            try
            {
                // Interface set et
                if (persisted.CurrentInterface > 0)
                {
                    _connectionManager.SetInterface(persisted.CurrentInterface);
                }

                // Pairing status set et
                if (persisted.IsPaired && !string.IsNullOrEmpty(persisted.EcrSerialNumber))
                {
                    _connectionManager.SetPairingStatus(true, persisted.EcrSerialNumber);
                }

                // Status set et (dikkatli - validation gerekebilir)
                if (persisted.Status == Models.ConnectionStatus.Connected)
                {
                    // Health check yaparak validate et
                    bool isHealthy = _connectionManager.PerformHealthCheck();
                    if (!isHealthy)
                    {
                        // Health check başarısız, NotConnected yap
                        _connectionManager.UpdateConnectionStatus(Models.ConnectionStatus.NotConnected);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RestoreConnectionState error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Transaction state'i restore et
        /// </summary>
        private bool RestoreTransactionState(PersistedTransactionState persisted)
        {
            try
            {
                // Transaction varsa ve tamamlanmamışsa restore et
                if (persisted.Handle > 0 &&
                    persisted.State != TransactionState.None &&
                    persisted.State != TransactionState.Completed &&
                    persisted.State != TransactionState.Cancelled)
                {
                    // Transaction timeout kontrolü
                    TimeSpan timeSinceUpdate = DateTime.Now - persisted.LastUpdateTime;
                    if (timeSinceUpdate.TotalMinutes > 30) // 30 dakikadan eski
                    {
                        // Çok eski, restore etme
                        System.Diagnostics.Debug.WriteLine("Transaction too old, not restoring");
                        return false;
                    }

                    // Transaction'ı restore et
                    _transactionManager.StartTransaction(
                        persisted.Handle,
                        persisted.UniqueId,
                        persisted.OrderKey
                    );

                    // State'i güncelle
                    _transactionManager.UpdateState(persisted.State);

                    // Validation yap
                    var validation = _transactionManager.ValidateCurrentTransaction();
                    if (!validation.IsValid)
                    {
                        // Invalid transaction, cancel et
                        _transactionManager.CancelTransaction();
                        return false;
                    }

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RestoreTransactionState error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// State'i temizle
        /// </summary>
        public bool ClearPersistedState()
        {
            return _provider.Clear();
        }

        /// <summary>
        /// Application version al
        /// </summary>
        private string GetApplicationVersion()
        {
            try
            {
                return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
            catch
            {
                return "Unknown";
            }
        }
    }

    /// <summary>
    /// State restore sonucu
    /// </summary>
    public class StateRestoreResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public PersistedState PersistedState { get; set; }
        public TimeSpan StateAge { get; set; }
        public bool ConnectionRestored { get; set; }
        public bool TransactionRestored { get; set; }

        public StateRestoreResult()
        {
            Success = false;
            Message = string.Empty;
        }
    }
}