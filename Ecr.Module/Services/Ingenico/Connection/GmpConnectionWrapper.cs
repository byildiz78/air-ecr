using Ecr.Module.Services.Ingenico.GmpIngenico;
using System;

namespace Ecr.Module.Services.Ingenico.Connection
{
    /// <summary>
    /// GMP DLL çağrılarını wrap eden ve otomatik error handling yapan sınıf
    /// Her DLL çağrısı bu wrapper üzerinden yapılmalı
    /// </summary>
    public static class GmpConnectionWrapper
    {
        private static readonly ConnectionManager _connectionManager = ConnectionManager.Instance;

        /// <summary>
        /// DLL result code'unu işle ve connection status'ü güncelle
        /// </summary>
        /// <param name="resultCode">DLL return code</param>
        /// <param name="throwOnError">Hata durumunda exception fırlat</param>
        /// <returns>Error bilgisi</returns>
        public static ConnectionErrorInfo ProcessResult(uint resultCode, bool throwOnError = false)
        {
            var errorInfo = _connectionManager.ProcessErrorCode(resultCode);

            if (throwOnError && !ErrorCodeCategorizer.IsSuccess(resultCode))
            {
                throw new GmpConnectionException(errorInfo);
            }

            return errorInfo;
        }

        /// <summary>
        /// FP3_Ping çağrısını wrap eder
        /// </summary>
        public static uint Ping(uint interfaceHandle, int timeout = 1100)
        {
            try
            {
                uint result = GMPSmartDLL.FP3_Ping(interfaceHandle, timeout);
                ProcessResult(result);
                return result;
            }
            catch (Exception ex)
            {
                // Native exception - connection kesilmiş olabilir
                _connectionManager.UpdateConnectionStatus(
                    Models.ConnectionStatus.NotConnected,
                    0xFFFFu,
                    ex.Message
                );
                throw;
            }
        }

        /// <summary>
        /// FP3_Echo çağrısını wrap eder
        /// </summary>
        public static uint Echo(uint interfaceHandle, ref ST_ECHO stEcho, int timeout)
        {
            try
            {
                uint result = Json_GMPSmartDLL.FP3_Echo(interfaceHandle, ref stEcho, timeout);
                ProcessResult(result);
                return result;
            }
            catch (Exception ex)
            {
                _connectionManager.UpdateConnectionStatus(
                    Models.ConnectionStatus.NotConnected,
                    0xFFFFu,
                    ex.Message
                );
                throw;
            }
        }

        /// <summary>
        /// FP3_StartPairingInit çağrısını wrap eder
        /// </summary>
        public static uint StartPairingInit(uint interfaceHandle, ref ST_GMP_PAIR pairing, ref ST_GMP_PAIR_RESP pairingResp)
        {
            try
            {
                uint result = Json_GMPSmartDLL.FP3_StartPairingInit(interfaceHandle, ref pairing, ref pairingResp);
                var errorInfo = ProcessResult(result);

                // Pairing başarılıysa durumu güncelle
                if (ErrorCodeCategorizer.IsSuccess(result))
                {
                    _connectionManager.SetPairingStatus(true, pairingResp.szEcrSerialNumber);
                }

                return result;
            }
            catch (Exception ex)
            {
                _connectionManager.UpdateConnectionStatus(
                    Models.ConnectionStatus.NotConnected,
                    0xFFFFu,
                    ex.Message
                );
                throw;
            }
        }

        /// <summary>
        /// FP3_GetTicket çağrısını wrap eder
        /// </summary>
        public static uint GetTicket(uint interfaceHandle, ulong transactionHandle, ref ST_TICKET ticket, int timeout)
        {
            try
            {
                uint result = Json_GMPSmartDLL.FP3_GetTicket(interfaceHandle, transactionHandle, ref ticket, timeout);
                ProcessResult(result);
                return result;
            }
            catch (Exception ex)
            {
                _connectionManager.UpdateConnectionStatus(
                    Models.ConnectionStatus.NotConnected,
                    0xFFFFu,
                    ex.Message
                );
                throw;
            }
        }

        /// <summary>
        /// Interface geçerli mi kontrol et
        /// </summary>
        public static bool IsInterfaceValid(uint interfaceHandle)
        {
            if (interfaceHandle == 0)
            {
                return false;
            }

            try
            {
                // Interface ID'yi okuyarak validate et
                byte[] id = new byte[64];
                uint result = GMPSmartDLL.FP3_GetInterfaceID(interfaceHandle, id, (uint)id.Length);
                return result == 0; // 0 = success
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Connection sağlıklı mı?
        /// </summary>
        public static bool IsConnectionHealthy()
        {
            return _connectionManager.IsConnected();
        }

        /// <summary>
        /// Connection state'i al
        /// </summary>
        public static ConnectionState GetConnectionState()
        {
            return _connectionManager.GetState();
        }
    }
}