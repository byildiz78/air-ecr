namespace Ecr.Module.Services.Ingenico.Diagnostics
{
    /// <summary>
    /// Log level
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Trace - very detailed
        /// </summary>
        Trace = 0,

        /// <summary>
        /// Debug - detailed
        /// </summary>
        Debug = 1,

        /// <summary>
        /// Information - general info
        /// </summary>
        Information = 2,

        /// <summary>
        /// Warning - potential problem
        /// </summary>
        Warning = 3,

        /// <summary>
        /// Error - error occurred
        /// </summary>
        Error = 4,

        /// <summary>
        /// Critical - critical error
        /// </summary>
        Critical = 5
    }

    /// <summary>
    /// Log category
    /// </summary>
    public enum LogCategory
    {
        /// <summary>
        /// Connection related
        /// </summary>
        Connection,

        /// <summary>
        /// Transaction related
        /// </summary>
        Transaction,

        /// <summary>
        /// Health check related
        /// </summary>
        HealthCheck,

        /// <summary>
        /// Recovery related
        /// </summary>
        Recovery,

        /// <summary>
        /// Pairing related
        /// </summary>
        Pairing,

        /// <summary>
        /// Interface related
        /// </summary>
        Interface,

        /// <summary>
        /// Performance related
        /// </summary>
        Performance,

        /// <summary>
        /// General
        /// </summary>
        General
    }
}