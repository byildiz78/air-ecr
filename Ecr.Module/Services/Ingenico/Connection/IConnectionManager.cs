using Ecr.Module.Services.Ingenico.Models;

namespace Ecr.Module.Services.Ingenico.Connection
{
    /// <summary>
    /// Connection yönetimi interface
    /// </summary>
    public interface IConnectionManager
    {
        /// <summary>
        /// Mevcut connection durumu
        /// </summary>
        ConnectionState GetState();

        /// <summary>
        /// Connection sağlıklı mı?
        /// </summary>
        bool IsConnected();

        /// <summary>
        /// Error code'u işle ve connection status'ü güncelle
        /// </summary>
        ConnectionErrorInfo ProcessErrorCode(uint errorCode);

        /// <summary>
        /// Connection status'ü güncelle
        /// </summary>
        void UpdateConnectionStatus(ConnectionStatus status, uint errorCode = 0, string errorMessage = "");

        /// <summary>
        /// Interface'i ayarla
        /// </summary>
        void SetInterface(uint interfaceHandle);

        /// <summary>
        /// Pairing durumunu ayarla
        /// </summary>
        void SetPairingStatus(bool isPaired, string ecrSerialNumber = "");

        /// <summary>
        /// Connection state'i reset et
        /// </summary>
        void Reset();

        /// <summary>
        /// Health check yap (PING)
        /// </summary>
        bool PerformHealthCheck();
    }
}