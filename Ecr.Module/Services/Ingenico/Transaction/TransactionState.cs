using System;

namespace Ecr.Module.Services.Ingenico.Transaction
{
    /// <summary>
    /// Transaction durumu enum
    /// PDF Section 5.2 - Transaction States
    /// </summary>
    public enum TransactionState
    {
        /// <summary>
        /// Transaction yok - idle state
        /// </summary>
        None = 0,

        /// <summary>
        /// Transaction başlatıldı (FP3_Start çağrıldı)
        /// </summary>
        Started = 1,

        /// <summary>
        /// Items ekleniyor
        /// </summary>
        AddingItems = 2,

        /// <summary>
        /// Payment alınıyor
        /// </summary>
        PaymentInProgress = 3,

        /// <summary>
        /// Transaction tamamlandı
        /// </summary>
        Completed = 4,

        /// <summary>
        /// Transaction iptal edildi
        /// </summary>
        Cancelled = 5,

        /// <summary>
        /// Hata durumu - recovery gerekli
        /// </summary>
        Error = 6
    }

    /// <summary>
    /// Transaction bilgileri
    /// </summary>
    public class TransactionInfo
    {
        /// <summary>
        /// Transaction handle (FP3_Start'tan dönen)
        /// </summary>
        public ulong Handle { get; set; }

        /// <summary>
        /// Transaction durumu
        /// </summary>
        public TransactionState State { get; set; }

        /// <summary>
        /// Unique ID (local veya TSM-based)
        /// </summary>
        public string UniqueId { get; set; }

        /// <summary>
        /// Order key (application level)
        /// </summary>
        public string OrderKey { get; set; }

        /// <summary>
        /// Transaction başlangıç zamanı
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Son güncelleme zamanı
        /// </summary>
        public DateTime LastUpdateTime { get; set; }

        /// <summary>
        /// Transaction timeout süresi (ms)
        /// </summary>
        public int TimeoutMs { get; set; }

        /// <summary>
        /// Ticket bilgisi (FP3_GetTicket'tan gelen)
        /// </summary>
        public GmpIngenico.ST_TICKET TicketInfo { get; set; }

        /// <summary>
        /// Son hata kodu
        /// </summary>
        public uint LastErrorCode { get; set; }

        /// <summary>
        /// Handle geçerli mi?
        /// </summary>
        public bool IsHandleValid => Handle > 0 && State != TransactionState.None;

        /// <summary>
        /// Transaction timeout oldu mu?
        /// </summary>
        public bool IsTimedOut =>
            State != TransactionState.None &&
            State != TransactionState.Completed &&
            State != TransactionState.Cancelled &&
            (DateTime.Now - LastUpdateTime).TotalMilliseconds > TimeoutMs;

        /// <summary>
        /// Transaction recovery gerekiyor mu?
        /// </summary>
        public bool NeedsRecovery => State == TransactionState.Error || IsTimedOut;

        public TransactionInfo()
        {
            Handle = 0;
            State = TransactionState.None;
            UniqueId = string.Empty;
            OrderKey = string.Empty;
            StartTime = DateTime.MinValue;
            LastUpdateTime = DateTime.MinValue;
            TimeoutMs = 300000; // 5 dakika default
            LastErrorCode = 0;
        }
    }
}