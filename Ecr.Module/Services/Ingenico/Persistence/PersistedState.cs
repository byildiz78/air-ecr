using Ecr.Module.Services.Ingenico.Connection;
using Ecr.Module.Services.Ingenico.Models;
using Ecr.Module.Services.Ingenico.Transaction;
using System;

namespace Ecr.Module.Services.Ingenico.Persistence
{
    /// <summary>
    /// Persist edilecek state model
    /// </summary>
    [Serializable]
    public class PersistedState
    {
        /// <summary>
        /// Persist version (future compatibility için)
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// Save time
        /// </summary>
        public DateTime SavedAt { get; set; }

        /// <summary>
        /// Application version
        /// </summary>
        public string ApplicationVersion { get; set; }

        /// <summary>
        /// Connection state
        /// </summary>
        public PersistedConnectionState Connection { get; set; }

        /// <summary>
        /// Transaction state
        /// </summary>
        public PersistedTransactionState Transaction { get; set; }

        public PersistedState()
        {
            SavedAt = DateTime.Now;
            ApplicationVersion = string.Empty;
            Connection = new PersistedConnectionState();
            Transaction = new PersistedTransactionState();
        }
    }

    /// <summary>
    /// Persist edilecek connection state
    /// </summary>
    [Serializable]
    public class PersistedConnectionState
    {
        public ConnectionStatus Status { get; set; }
        public uint CurrentInterface { get; set; }
        public DateTime LastSuccessfulConnection { get; set; }
        public bool IsPaired { get; set; }
        public string EcrSerialNumber { get; set; }
        public uint LastErrorCode { get; set; }

        public PersistedConnectionState()
        {
            Status = ConnectionStatus.NotConnected;
            CurrentInterface = 0;
            LastSuccessfulConnection = DateTime.MinValue;
            IsPaired = false;
            EcrSerialNumber = string.Empty;
        }

        /// <summary>
        /// ConnectionState'ten oluştur
        /// </summary>
        public static PersistedConnectionState FromConnectionState(ConnectionState state)
        {
            return new PersistedConnectionState
            {
                Status = state.Status,
                CurrentInterface = state.CurrentInterface,
                LastSuccessfulConnection = state.LastSuccessfulConnection,
                IsPaired = state.IsPaired,
                EcrSerialNumber = state.EcrSerialNumber,
                LastErrorCode = state.LastErrorCode
            };
        }
    }

    /// <summary>
    /// Persist edilecek transaction state
    /// </summary>
    [Serializable]
    public class PersistedTransactionState
    {
        public ulong Handle { get; set; }
        public TransactionState State { get; set; }
        public string UniqueId { get; set; }
        public string OrderKey { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public uint LastErrorCode { get; set; }

        public PersistedTransactionState()
        {
            Handle = 0;
            State = TransactionState.None;
            UniqueId = string.Empty;
            OrderKey = string.Empty;
            StartTime = DateTime.MinValue;
            LastUpdateTime = DateTime.MinValue;
        }

        /// <summary>
        /// TransactionInfo'dan oluştur
        /// </summary>
        public static PersistedTransactionState FromTransactionInfo(TransactionInfo info)
        {
            return new PersistedTransactionState
            {
                Handle = info.Handle,
                State = info.State,
                UniqueId = info.UniqueId,
                OrderKey = info.OrderKey,
                StartTime = info.StartTime,
                LastUpdateTime = info.LastUpdateTime,
                LastErrorCode = info.LastErrorCode
            };
        }
    }
}