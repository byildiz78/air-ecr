namespace Ecr.Module.Services.Ingenico.HealthCheck
{
    /// <summary>
    /// Health check stratejisi
    /// PDF Section 3.4 - Flow Control (PING/ECHO/BUSY)
    /// </summary>
    public enum HealthCheckStrategy
    {
        /// <summary>
        /// Sadece PING kullan (hızlı, minimum bilgi)
        /// Recommended for frequent checks
        /// </summary>
        PingOnly,

        /// <summary>
        /// Sadece ECHO kullan (yavaş, detaylı bilgi)
        /// Recommended for initial connection
        /// </summary>
        EchoOnly,

        /// <summary>
        /// PING-first: Önce PING, başarısızsa ECHO dene
        /// Recommended strategy (PDF Section 3.4)
        /// </summary>
        PingFirst,

        /// <summary>
        /// PING + ECHO: Her ikisini de çağır
        /// En detaylı ama en yavaş
        /// </summary>
        Both
    }

    /// <summary>
    /// Health check seviyesi
    /// </summary>
    public enum HealthCheckLevel
    {
        /// <summary>
        /// Basic check - sadece bağlantı var mı?
        /// </summary>
        Basic,

        /// <summary>
        /// Standard check - bağlantı + device status
        /// </summary>
        Standard,

        /// <summary>
        /// Detailed check - her şey (cashier, mode, status)
        /// </summary>
        Detailed
    }
}