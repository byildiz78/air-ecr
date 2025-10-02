using Ecr.Module.Services.Ingenico.GmpIngenico;
using Ecr.Module.Services.Ingenico.Models;
using System;

namespace Ecr.Module.Services.Ingenico.Connection
{
    /// <summary>
    /// Centralized connection yönetimi
    /// Tüm connection durumu ve error handling bu sınıf üzerinden yapılır
    /// </summary>
    public class ConnectionManager : IConnectionManager
    {
        private static readonly object _lock = new object();
        private static ConnectionManager _instance;
        private ConnectionState _state;

        private ConnectionManager()
        {
            _state = new ConnectionState();
        }

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static ConnectionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ConnectionManager();
                        }
                    }
                }
                return _instance;
            }
        }

        public ConnectionState GetState()
        {
            lock (_lock)
            {
                return _state;
            }
        }

        public bool IsConnected()
        {
            lock (_lock)
            {
                return _state.Status == ConnectionStatus.Connected &&
                       _state.CurrentInterface > 0 &&
                       _state.IsPaired;
            }
        }

        public ConnectionErrorInfo ProcessErrorCode(uint errorCode)
        {
            var errorInfo = ErrorCodeCategorizer.GetErrorInfo(errorCode);

            lock (_lock)
            {
                _state.LastErrorCode = errorCode;
                _state.LastErrorMessage = errorInfo.ErrorMessage;
                _state.LastConnectionAttempt = DateTime.Now;

                // Error category'ye göre connection status güncelle
                switch (errorInfo.Category)
                {
                    case ConnectionErrorCategory.Success:
                        _state.Status = ConnectionStatus.Connected;
                        _state.LastSuccessfulConnection = DateTime.Now;
                        _state.ConsecutiveFailureCount = 0;
                        break;

                    case ConnectionErrorCategory.Recoverable:
                    case ConnectionErrorCategory.Timeout:
                        _state.Status = ConnectionStatus.NotConnected;
                        _state.ConsecutiveFailureCount++;

                        // Pairing gerekiyorsa flag'i kaldır
                        if (errorInfo.RequiresPairing)
                        {
                            _state.IsPaired = false;
                        }
                        break;

                    case ConnectionErrorCategory.UserActionRequired:
                        // Status değiştirme - kullanıcı aksiyonu bekle
                        _state.ConsecutiveFailureCount++;
                        break;

                    case ConnectionErrorCategory.Fatal:
                        _state.Status = ConnectionStatus.NotConnected;
                        _state.ConsecutiveFailureCount++;
                        break;

                    case ConnectionErrorCategory.Unknown:
                        _state.ConsecutiveFailureCount++;
                        break;
                }
            }

            return errorInfo;
        }

        public void UpdateConnectionStatus(ConnectionStatus status, uint errorCode = 0, string errorMessage = "")
        {
            lock (_lock)
            {
                _state.Status = status;
                _state.LastConnectionAttempt = DateTime.Now;

                if (status == ConnectionStatus.Connected)
                {
                    _state.LastSuccessfulConnection = DateTime.Now;
                    _state.ConsecutiveFailureCount = 0;
                }
                else
                {
                    _state.ConsecutiveFailureCount++;
                }

                if (errorCode > 0)
                {
                    _state.LastErrorCode = errorCode;
                    _state.LastErrorMessage = errorMessage;
                }
            }
        }

        public void SetInterface(uint interfaceHandle)
        {
            lock (_lock)
            {
                _state.CurrentInterface = interfaceHandle;
            }
        }

        public void SetPairingStatus(bool isPaired, string ecrSerialNumber = "")
        {
            lock (_lock)
            {
                _state.IsPaired = isPaired;
                if (!string.IsNullOrEmpty(ecrSerialNumber))
                {
                    _state.EcrSerialNumber = ecrSerialNumber;
                }
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _state = new ConnectionState();
            }
        }

        public bool PerformHealthCheck()
        {
            lock (_lock)
            {
                if (_state.CurrentInterface == 0)
                {
                    return false;
                }

                try
                {
                    // PING ile hızlı health check (1100ms timeout)
                    uint result = GMPSmartDLL.FP3_Ping(_state.CurrentInterface, 1100);

                    if (result == Defines.TRAN_RESULT_OK)
                    {
                        _state.Status = ConnectionStatus.Connected;
                        _state.LastSuccessfulConnection = DateTime.Now;
                        _state.ConsecutiveFailureCount = 0;
                        return true;
                    }
                    else
                    {
                        ProcessErrorCode(result);
                        return false;
                    }
                }
                catch (Exception)
                {
                    _state.Status = ConnectionStatus.NotConnected;
                    _state.ConsecutiveFailureCount++;
                    return false;
                }
            }
        }
    }
}