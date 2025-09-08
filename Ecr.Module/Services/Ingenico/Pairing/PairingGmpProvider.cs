using Ecr.Module.Services.Ingenico.GmpIngenico;
using Ecr.Module.Services.Ingenico.Models;
using Ecr.Module.Services.Ingenico.ReceiptHeader;
using Ecr.Module.Services.Ingenico.Settings;
using System;
using System.Text;

namespace Ecr.Module.Services.Ingenico.Pairing
{
    public class PairingGmpProvider
    {
        public GmpPairingDto GmpPairing()
        {
            #region Pairing Aşaması

            try
            {
                if (DataStore.Connection == ConnectionStatus.Connected)
                {
                    var res = GmpPing();
                    if (res.ReturnCode == Defines.TRAN_RESULT_OK)
                    {
                        DataStore.Connection = ConnectionStatus.Connected;
                        return DataStore.gmpResult;
                    }
                }
               

                retry:
                if (DataStore.Connection == ConnectionStatus.NotConnected)
                {
                    #region Versyion Kontrolü
                    DataStore.gmpResult.GmpInfo.m_dllVersion = new byte[24];
                    UInt32 ret = GMPSmartDLL.GMP_GetDllVersionEx(DataStore.gmpResult.GmpInfo.m_dllVersion, (uint)DataStore.gmpResult.GmpInfo.m_dllVersion.Length);
                    if (ret != 0)
                    {
                        return DataStore.gmpResult;
                    }
                    else if (String.Compare(GMP_Tools.SetEncoding(DataStore.gmpResult.GmpInfo.m_dllVersion), Defines.DLL_VERSION_MIN) < 0)
                    {
                        return DataStore.gmpResult;
                    }

                    #endregion

                    #region interface kontrolü

                    UInt32[] InterfaceList = new UInt32[20];
                    UInt32 InterfaceCount = GMPSmartDLL.FP3_GetInterfaceHandleList(InterfaceList, (UInt32)InterfaceList.Length);

                    for (UInt32 Index = 0; Index < InterfaceCount; ++Index)
                    {
                        byte[] ID = new byte[64];
                        UInt32 retcode = GMPSmartDLL.FP3_GetInterfaceID(InterfaceList[Index], ID, (UInt32)ID.Length);
                        string Handle = InterfaceList[Index].ToString("X8") + "-" + GMP_Tools.SetEncoding(ID);

                        DataStore.CurrentInterface = InterfaceList[Index];
                        DataStore.gmpResult.GmpInfo.CurrentInterface = InterfaceList[Index];
                    }
                    if (DataStore.CurrentInterface == 0)
                    {
                        DataStore.gmpResult.ReturnMessage = "Gmp bağlantı noktası bulunamadı!";
                        return DataStore.gmpResult;
                    }

                    #endregion

                    #region Echo - Yazarkasa Aktif Bilgiler Kontrolu
                    ST_ECHO stEcho = new ST_ECHO();
                    DataStore.gmpResult.ReturnCode = Json_GMPSmartDLL.FP3_Echo(DataStore.gmpResult.GmpInfo.CurrentInterface, ref stEcho, Defines.TIMEOUT_ECHO);
                    if (DataStore.gmpResult.ReturnCode != Defines.TRAN_RESULT_OK)
                    {
                        DataStore.gmpResult.ReturnCodeMessage = ErrorClass.DisplayErrorCodeMessage(DataStore.gmpResult.ReturnCode);
                        DataStore.gmpResult.ReturnMessage = ErrorClass.DisplayErrorMessage(DataStore.gmpResult.ReturnCode);
                        //LogManager.Append($"FP3_Echo -> {DataStore.gmpResult.JsonSerialize()}", "PairingGmpProvider -> GmpPairing");
                        return DataStore.gmpResult;
                    }
                    else
                    {
                        try
                        {
                            DataStore.gmpResult.GmpInfo.ActiveCashier = stEcho.activeCashier.name;
                            DataStore.gmpResult.GmpInfo.ActiveCashierNo = stEcho.activeCashier.index + 1;
                            DataStore.gmpResult.GmpInfo.EcrStatus = (int)stEcho.status;
                            DataStore.gmpResult.GmpInfo.ecrMode = stEcho.ecrMode;

                            //LogManager.Append("Gmp Uygulama Versiyonu : " + DataStore.gmpResult.GmpInfo.gmpVersion);
                            //LogManager.Append("GmpPairing() Active Cashier :" + DataStore.gmpResult.GmpInfo.ActiveCashierNo + "- " + DataStore.gmpResult.GmpInfo.ActiveCashier);
                            //LogManager.Append("Gmp Tarih : " + stEcho.date.day + "-" + stEcho.date.month + "-" + stEcho.date.year);
                            //LogManager.Append("Gmp Saat : " + stEcho.time.hour + ":" + stEcho.time.minute + ":" + stEcho.time.second);
                        }
                        catch (Exception ex)
                        {
                            // LogManager.Exception(ex, "Gmp_Pairing.FP3_Echo");
                        }
                    }
                    #endregion

                    #region Yazarkasaya Bağlanılıyor

                    //setting .ini doyasından yazarkasa bilgileri pairing clasına yükleniyor..
                    ST_GMP_PAIR _pairing = new ST_GMP_PAIR();

                    _pairing.szExternalDeviceBrand = SettingsValues.brand;
                    _pairing.szExternalDeviceModel = SettingsValues.model;
                    _pairing.szExternalDeviceSerialNumber = "12344567";
                    _pairing.szEcrSerialNumber = SettingsValues.serialnumber;
                    _pairing.szProcOrderNumber = "000001";
                    _pairing.szProcDate = DateTime.Now.ToShortDateString().Substring(0, 2) + DateTime.Now.ToShortDateString().Substring(3, 2) + DateTime.Now.ToShortDateString().Substring(6, 2);
                    _pairing.szProcTime = DateTime.Now.ToLongTimeString().Substring(0, 2) + DateTime.Now.ToLongTimeString().Substring(3, 2) + DateTime.Now.ToLongTimeString().Substring(6, 2);
                    ST_GMP_PAIR_RESP _pairingResp = new ST_GMP_PAIR_RESP();

                    //LogManager.Append("GmpPairing() Yazarkasaya parametreleri iniden okunuyor...", "GmpPairing");

                    DataStore.gmpResult.ReturnCode = Json_GMPSmartDLL.FP3_StartPairingInit(DataStore.gmpResult.GmpInfo.CurrentInterface, ref _pairing, ref _pairingResp);
                    if (DataStore.gmpResult.ReturnCode != Defines.TRAN_RESULT_OK)
                    {
                        //LogManager.Append("Yazarkasaya BAĞLANTI BAŞARISIZ...");
                        DataStore.gmpResult.ReturnCodeMessage = ErrorClass.DisplayErrorCodeMessage(DataStore.gmpResult.ReturnCode);
                        DataStore.gmpResult.ReturnMessage = ErrorClass.DisplayErrorMessage(DataStore.gmpResult.ReturnCode);
                        // LogManager.Append(string.Format("{0}-{1}", DataStore.gmpResult.ReturnCode, DataStore.gmpResult.ReturnMessage), "GMP_StartPairingInit");
                        return DataStore.gmpResult;
                    }
                    else
                    {
                        if (DataStore.gmpResult.ReturnCode == Defines.APP_ERR_GMP3_INVALID_DATE_TIME)
                        {
#if !DEBUG
                       // CommandHelperGmpProvider.WindowsSystemUpdate(stEcho);
#endif
                        }
                        try
                        {
                            //LogManager.Append("Yazarkasaya Bağlanıldı.. Ecr Seri No : " + DataStore.gmpResult.GmpInfo.EcrSerialNumber, "GMP_StartPairingInit");

                            DataStore.gmpResult.GmpInfo.EcrSerialNumber = _pairingResp.szEcrSerialNumber;


                            #region version info

                            VersionInfo versInfo = new VersionInfo
                            {
                                EcrSerialNumber = DataStore.gmpResult.GmpInfo.EcrSerialNumber,
                                gmpVersion = DataStore.gmpResult.GmpInfo.gmpVersion,
                                ModuleVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                                m_dllVersion = Encoding.Default.GetString(DataStore.gmpResult.GmpInfo.m_dllVersion).Replace("\u0000", ""),
                                DLL_VERSION_MIN = DataStore.gmpResult.GmpInfo.DLL_VERSION_MIN,
                            };
                            DataStore.gmpResult.GmpInfo.Versions = versInfo;
                            #endregion


                        }
                        catch (Exception ex)
                        {
                            //LogManager.Exception(ex, "GmpPairing.TaxGroupsPairing");
                        }
                    }

                    #endregion
                }
                else
                {
                    ST_ECHO stEcho = new ST_ECHO();
                    DataStore.gmpResult.ReturnCode = Json_GMPSmartDLL.FP3_Echo(DataStore.gmpResult.GmpInfo.CurrentInterface, ref stEcho, Defines.TIMEOUT_ECHO);
                    if (DataStore.gmpResult.ReturnCode != Defines.TRAN_RESULT_OK)
                    {
                        DataStore.Connection = ConnectionStatus.NotConnected;
                        goto retry;
                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }

            #endregion
            return DataStore.gmpResult;
        }

        public GmpPingResultDto GmpPing()
        {
            var _pingResult = new GmpPingResultDto();
            try
            {
                _pingResult.ReturnCode = GMPSmartDLL.FP3_Ping(DataStore.gmpResult.GmpInfo.CurrentInterface, 1100);

                switch (_pingResult.ReturnCode)
                {
                    case Defines.TRAN_RESULT_OK:
                        _pingResult.ReturnStringMessage = "EFT-POS Bağlı";
                        DataStore.Connection = ConnectionStatus.Connected;
                        break;
                    case Defines.DLL_RETCODE_RECV_BUSY:
                        _pingResult.ReturnStringMessage = "EFT-POS Meşgul";
                        DataStore.Connection = ConnectionStatus.NotConnected;
                        break;
                    case Defines.DLL_RETCODE_TIMEOUT:
                        _pingResult.ReturnStringMessage = "EFT-POS Bağlantı Yok";
                        DataStore.Connection = ConnectionStatus.NotConnected;
                        break;
                    default:
                        _pingResult.ReturnStringMessage = "EFT-POS Bağlantı Hatalı -" + ErrorClass.DisplayErrorCodeMessage(_pingResult.ReturnCode);
                        DataStore.Connection = ConnectionStatus.NotConnected;
                        break;
                }
            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "FiscalModule.Ingenico.GmpProvider.Pairings");
            }
            return _pingResult;
        }

        public IngenicoApiResponse<GmpPairingDto> pairingControl()
        {
            var response = new IngenicoApiResponse<GmpPairingDto>();
            SettingsInfo.getIniValues();

            SettingsInfo.getGMPIniValues();

            SettingsInfo.setXmlValues();

            var gmpPair = new PairingGmpProvider();
            var result = gmpPair.GmpPairing();
            if (result.ReturnCode == Defines.TRAN_RESULT_OK)
            {
                var header = new Header();
                result.GmpInfo.fiscalHeader = header.GmpGetReceiptHeader().Data;
                ST_ECHO stEcho = new ST_ECHO();
                result.ReturnCode = Json_GMPSmartDLL.FP3_Echo(result.GmpInfo.CurrentInterface, ref stEcho, Defines.TIMEOUT_ECHO);
                result.GmpInfo.ActiveCashier = stEcho.activeCashier.name;
                result.GmpInfo.ActiveCashierNo = stEcho.activeCashier.index + 1;
                result.GmpInfo.EcrStatus = (int)stEcho.status;
                result.GmpInfo.ecrMode = stEcho.ecrMode;
                DataStore.gmpResult = result;
                response.Message = result.ReturnCodeMessage;
                response.ErrorCode = result.ReturnCode.ToString();
                response.Status = result.ReturnCode == Defines.TRAN_RESULT_OK ? true : false;
                DataStore.Connection = response.Status ? ConnectionStatus.Connected : ConnectionStatus.NotConnected;
            }
            else
            {
                response.Message = result.ReturnCodeMessage;
                response.ErrorCode = result.ReturnCode.ToString();
                response.Status = result.ReturnCode == Defines.TRAN_RESULT_OK ? true : false;
                DataStore.Connection = response.Status ? ConnectionStatus.Connected : ConnectionStatus.NotConnected;

            }

            return response;
        }

    }
}
