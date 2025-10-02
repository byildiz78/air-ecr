using Ecr.Module.Services.Ingenico.Connection;
using Ecr.Module.Services.Ingenico.GmpIngenico;
using Ecr.Module.Services.Ingenico.Interface;
using Ecr.Module.Services.Ingenico.Models;
using Ecr.Module.Services.Ingenico.ReceiptHeader;
using Ecr.Module.Services.Ingenico.Retry;
using Ecr.Module.Services.Ingenico.Settings;
using System;
using System.Text;

namespace Ecr.Module.Services.Ingenico.Pairing
{
    /// <summary>
    /// Yeni modüler Pairing Provider
    /// Retry logic, connection management ve interface validation entegre edilmiş
    /// </summary>
    public class PairingGmpProviderV2
    {
        private readonly ConnectionManager _connectionManager;
        private readonly InterfaceManager _interfaceManager;

        public PairingGmpProviderV2()
        {
            _connectionManager = ConnectionManager.Instance;
            _interfaceManager = InterfaceManager.Instance;
        }

        /// <summary>
        /// GMP Pairing işlemini yap
        /// </summary>
        public GmpPairingDto GmpPairing()
        {
            try
            {
                // V1 mantığı: DataStore.Connection kontrol et
                if (DataStore.Connection == ConnectionStatus.Connected)
                {
                    // Zaten bağlıyız, PING ile kontrol et
                    var pingResult = GmpPing();
                    if (pingResult.ReturnCode == Defines.TRAN_RESULT_OK)
                    {
                        DataStore.Connection = ConnectionStatus.Connected;
                        return DataStore.gmpResult;
                    }
                }

                // V1 mantığı: Connection NotConnected ise devam et
                if (DataStore.Connection == ConnectionStatus.NotConnected)
                {
                    // DLL Version kontrolü
                    if (!CheckDllVersion())
                    {
                        DataStore.gmpResult.ReturnMessage = "DLL version uyumsuz veya okunamadı";
                        return DataStore.gmpResult;
                    }

                    // Interface seç (V1 ile aynı mantık)
                    uint[] interfaceList = new uint[20];
                    uint interfaceCount = GMPSmartDLL.FP3_GetInterfaceHandleList(interfaceList, (uint)interfaceList.Length);

                    if (interfaceCount == 0)
                    {
                        DataStore.gmpResult.ReturnMessage = "Gmp bağlantı noktası bulunamadı!";
                        return DataStore.gmpResult;
                    }

                    // V1 gibi son interface'i seç
                    for (uint i = 0; i < interfaceCount; i++)
                    {
                        DataStore.CurrentInterface = interfaceList[i];
                        DataStore.gmpResult.GmpInfo.CurrentInterface = interfaceList[i];
                    }

                    if (DataStore.CurrentInterface == 0)
                    {
                        DataStore.gmpResult.ReturnMessage = "Gmp bağlantı noktası bulunamadı!";
                        return DataStore.gmpResult;
                    }

                    _connectionManager.SetInterface(DataStore.CurrentInterface);

                    // Echo ile device bilgilerini al (retry ile)
                    if (!PerformEchoWithRetry())
                    {
                        return DataStore.gmpResult;
                    }

                    // Pairing yap (retry ile)
                    if (!PerformPairingWithRetry())
                    {
                        return DataStore.gmpResult;
                    }
                }

                return DataStore.gmpResult;
            }
            catch (Exception ex)
            {
                DataStore.gmpResult.ReturnMessage = $"Pairing exception: {ex.Message}";
                _connectionManager.UpdateConnectionStatus(ConnectionStatus.NotConnected, 0xFFFFu, ex.Message);
                return DataStore.gmpResult;
            }
        }

        /// <summary>
        /// DLL version kontrolü yap
        /// </summary>
        private bool CheckDllVersion()
        {
            try
            {
                DataStore.gmpResult.GmpInfo.m_dllVersion = new byte[24];
                uint ret = GMPSmartDLL.GMP_GetDllVersionEx(
                    DataStore.gmpResult.GmpInfo.m_dllVersion,
                    (uint)DataStore.gmpResult.GmpInfo.m_dllVersion.Length
                );

                if (ret != 0)
                {
                    return false;
                }

                string version = GMP_Tools.SetEncoding(DataStore.gmpResult.GmpInfo.m_dllVersion);
                if (string.Compare(version, Defines.DLL_VERSION_MIN) < 0)
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// Echo işlemini retry ile yap
        /// </summary>
        private bool PerformEchoWithRetry()
        {
            ST_ECHO stEcho = new ST_ECHO();

            var result = ConnectionRetryHelper.EchoWithRetry(
                DataStore.gmpResult.GmpInfo.CurrentInterface,
                ref stEcho,
                Defines.TIMEOUT_ECHO,
                RetryPolicy.Default
            );

            DataStore.gmpResult.ReturnCode = result.Result;

            if (!result.Success)
            {
                DataStore.gmpResult.ReturnCodeMessage = ErrorClass.DisplayErrorCodeMessage(result.Result);
                DataStore.gmpResult.ReturnMessage = ErrorClass.DisplayErrorMessage(result.Result);
                return false;
            }

            // Echo başarılı, bilgileri kaydet
            try
            {
                DataStore.gmpResult.GmpInfo.ActiveCashier = stEcho.activeCashier.name;
                DataStore.gmpResult.GmpInfo.ActiveCashierNo = stEcho.activeCashier.index + 1;
                DataStore.gmpResult.GmpInfo.EcrStatus = (int)stEcho.status;
                DataStore.gmpResult.GmpInfo.ecrMode = stEcho.ecrMode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Echo data parsing error: {ex.Message}");
            }

            return true;
        }

        /// <summary>
        /// Pairing işlemini retry ile yap
        /// </summary>
        private bool PerformPairingWithRetry()
        {
            ST_GMP_PAIR pairing = CreatePairingRequest();
            ST_GMP_PAIR_RESP pairingResp = new ST_GMP_PAIR_RESP();

            var result = ConnectionRetryHelper.PairingWithRetry(
                DataStore.gmpResult.GmpInfo.CurrentInterface,
                ref pairing,
                ref pairingResp,
                RetryPolicy.Conservative // Pairing için conservative
            );

            DataStore.gmpResult.ReturnCode = result.Result;

            if (!result.Success)
            {
                DataStore.gmpResult.ReturnCodeMessage = ErrorClass.DisplayErrorCodeMessage(result.Result);
                DataStore.gmpResult.ReturnMessage = ErrorClass.DisplayErrorMessage(result.Result);
                return false;
            }

            // Pairing başarılı, bilgileri kaydet
            DataStore.gmpResult.GmpInfo.EcrSerialNumber = pairingResp.szEcrSerialNumber;
            _connectionManager.SetPairingStatus(true, pairingResp.szEcrSerialNumber);

            // Version info
            var versInfo = new VersionInfo
            {
                EcrSerialNumber = DataStore.gmpResult.GmpInfo.EcrSerialNumber,
                gmpVersion = DataStore.gmpResult.GmpInfo.gmpVersion,
                ModuleVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                m_dllVersion = Encoding.Default.GetString(DataStore.gmpResult.GmpInfo.m_dllVersion).Replace("\u0000", ""),
                DLL_VERSION_MIN = DataStore.gmpResult.GmpInfo.DLL_VERSION_MIN
            };
            DataStore.gmpResult.GmpInfo.Versions = versInfo;

            return true;
        }

        /// <summary>
        /// Pairing request oluştur
        /// </summary>
        private ST_GMP_PAIR CreatePairingRequest()
        {
            var pairing = new ST_GMP_PAIR
            {
                szExternalDeviceBrand = SettingsValues.brand,
                szExternalDeviceModel = SettingsValues.model,
                szExternalDeviceSerialNumber = "12344567",
                szEcrSerialNumber = SettingsValues.serialnumber,
                szProcOrderNumber = "000001",
                szProcDate = DateTime.Now.ToString("ddMMyy"),
                szProcTime = DateTime.Now.ToString("HHmmss")
            };

            return pairing;
        }

        /// <summary>
        /// Ping işlemi (retry ile)
        /// </summary>
        public GmpPingResultDto GmpPing()
        {
            var pingResult = new GmpPingResultDto();

            var result = ConnectionRetryHelper.PingWithRetry(
                DataStore.gmpResult.GmpInfo.CurrentInterface,
                RetryPolicy.Default
            );

            pingResult.ReturnCode = result.Result;

            switch (pingResult.ReturnCode)
            {
                case Defines.TRAN_RESULT_OK:
                    pingResult.ReturnStringMessage = "EFT-POS Bağlı";
                    DataStore.Connection = ConnectionStatus.Connected;
                    _connectionManager.UpdateConnectionStatus(ConnectionStatus.Connected);
                    break;

                case Defines.DLL_RETCODE_RECV_BUSY:
                    pingResult.ReturnStringMessage = "EFT-POS Meşgul";
                    DataStore.Connection = ConnectionStatus.NotConnected;
                    _connectionManager.UpdateConnectionStatus(ConnectionStatus.NotConnected);
                    break;

                case Defines.DLL_RETCODE_TIMEOUT:
                    pingResult.ReturnStringMessage = "EFT-POS Bağlantı Yok";
                    DataStore.Connection = ConnectionStatus.NotConnected;
                    _connectionManager.UpdateConnectionStatus(ConnectionStatus.NotConnected);
                    break;

                default:
                    pingResult.ReturnStringMessage = "EFT-POS Bağlantı Hatalı - " +
                        ErrorClass.DisplayErrorCodeMessage(pingResult.ReturnCode);
                    DataStore.Connection = ConnectionStatus.NotConnected;
                    _connectionManager.UpdateConnectionStatus(ConnectionStatus.NotConnected);
                    break;
            }

            pingResult.ReturnCodeMessage = ErrorClass.DisplayErrorCodeMessage(pingResult.ReturnCode);
            return pingResult;
        }

        /// <summary>
        /// Pairing control - wrapper method
        /// </summary>
        public IngenicoApiResponse<GmpPairingDto> pairingControl()
        {
            var response = new IngenicoApiResponse<GmpPairingDto>();

            try
            {
                // Settings'i yükle
                SettingsInfo.getIniValues();
                SettingsInfo.getGMPIniValues();
                SettingsInfo.setXmlValues();

                // Pairing yap
                var result = GmpPairing();

                if (result.ReturnCode == Defines.TRAN_RESULT_OK)
                {
                    // Header bilgisini al
                    var header = new Header();
                    result.GmpInfo.fiscalHeader = header.GmpGetReceiptHeader().Data;

                    // Echo bilgilerini güncelle
                    ST_ECHO stEcho = new ST_ECHO();
                    result.ReturnCode = Json_GMPSmartDLL.FP3_Echo(
                        result.GmpInfo.CurrentInterface,
                        ref stEcho,
                        Defines.TIMEOUT_ECHO
                    );

                    if (result.ReturnCode == Defines.TRAN_RESULT_OK)
                    {
                        result.GmpInfo.ActiveCashier = stEcho.activeCashier.name;
                        result.GmpInfo.ActiveCashierNo = stEcho.activeCashier.index + 1;
                        result.GmpInfo.EcrStatus = (int)stEcho.status;
                        result.GmpInfo.ecrMode = stEcho.ecrMode;
                    }

                    DataStore.gmpResult = result;
                }

                response.Data = result;
                response.Message = result.ReturnCodeMessage;
                response.ErrorCode = result.ReturnCode.ToString();
                response.Status = result.ReturnCode == Defines.TRAN_RESULT_OK;

                // V1 mantığı: DataStore.Connection'ı da güncelle
                DataStore.Connection = response.Status ? ConnectionStatus.Connected : ConnectionStatus.NotConnected;
                _connectionManager.UpdateConnectionStatus(
                    response.Status ? ConnectionStatus.Connected : ConnectionStatus.NotConnected,
                    result.ReturnCode,
                    result.ReturnMessage
                );
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.ErrorCode = "9999";
                response.Message = ex.Message;
                DataStore.Connection = ConnectionStatus.NotConnected;
                _connectionManager.UpdateConnectionStatus(ConnectionStatus.NotConnected, 0xFFFFu, ex.Message);
            }

            return response;
        }
    }
}