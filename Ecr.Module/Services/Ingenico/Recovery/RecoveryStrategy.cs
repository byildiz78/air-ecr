namespace Ecr.Module.Services.Ingenico.Recovery
{
    /// <summary>
    /// Recovery stratejisi
    /// Error kategorisine göre farklı recovery yaklaşımları
    /// </summary>
    public enum RecoveryStrategy
    {
        /// <summary>
        /// No recovery - error fatal
        /// </summary>
        None,

        /// <summary>
        /// Automatic reconnection
        /// </summary>
        AutoReconnect,

        /// <summary>
        /// Pairing gerekli
        /// </summary>
        RequiresPairing,

        /// <summary>
        /// User action gerekli (kağıt yükle, kasiyer girişi vb)
        /// </summary>
        RequiresUserAction,

        /// <summary>
        /// Transaction recovery
        /// </summary>
        TransactionRecovery,

        /// <summary>
        /// Manual intervention
        /// </summary>
        ManualIntervention
    }

    /// <summary>
    /// Recovery action tipi
    /// </summary>
    public enum RecoveryActionType
    {
        /// <summary>
        /// Interface yeniden seç
        /// </summary>
        Reselect_Interface,

        /// <summary>
        /// Pairing yap
        /// </summary>
        Perform_Pairing,

        /// <summary>
        /// PING check
        /// </summary>
        Ping_Check,

        /// <summary>
        /// ECHO check
        /// </summary>
        Echo_Check,

        /// <summary>
        /// Transaction validate
        /// </summary>
        Validate_Transaction,

        /// <summary>
        /// Transaction cancel
        /// </summary>
        Cancel_Transaction,

        /// <summary>
        /// User message göster
        /// </summary>
        Show_User_Message,

        /// <summary>
        /// Retry işlem
        /// </summary>
        Retry_Operation,

        /// <summary>
        /// Reset connection
        /// </summary>
        Reset_Connection
    }
}