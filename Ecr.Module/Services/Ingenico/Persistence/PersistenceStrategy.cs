namespace Ecr.Module.Services.Ingenico.Persistence
{
    /// <summary>
    /// State persistence stratejisi
    /// </summary>
    public enum PersistenceStrategy
    {
        /// <summary>
        /// No persistence
        /// </summary>
        None,

        /// <summary>
        /// File-based persistence (JSON)
        /// </summary>
        File,

        /// <summary>
        /// Windows Registry
        /// </summary>
        Registry,

        /// <summary>
        /// Database
        /// </summary>
        Database
    }

    /// <summary>
    /// Persist edilecek data tipi
    /// </summary>
    public enum PersistenceDataType
    {
        /// <summary>
        /// Connection state
        /// </summary>
        ConnectionState,

        /// <summary>
        /// Transaction state
        /// </summary>
        TransactionState,

        /// <summary>
        /// Application settings
        /// </summary>
        Settings
    }
}