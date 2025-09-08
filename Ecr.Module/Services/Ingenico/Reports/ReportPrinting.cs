using Ecr.Module.Services.Ingenico.GmpIngenico;
using Ecr.Module.Services.Ingenico.Models;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Ecr.Module.Services.Ingenico.Reports
{
    public class ReportPrinting
    {
        public IngenicoApiResponse<string> ReportPrint(ReportRequest reportType)
        {
            var response = new IngenicoApiResponse<string>();

            try
            {

                ST_FUNCTION_PARAMETERS stFunctionParameters = new ST_FUNCTION_PARAMETERS();
                ushort ZNo = 0;
                ushort FNo = 0;
                ushort EKUNo = 0;
                stFunctionParameters.Password.supervisor = SettingsValues.adminpassword ?? "0000";
                ST_PAYMENT_REQUEST StPaymentRequest = new ST_PAYMENT_REQUEST();
                int againCount = 0;
                uint retcode;
                switch (reportType.ReportType)
                {
                    case ReportType.ZReport:
                        try
                        {
                            retcode = Json_GMPSmartDLL.FP3_FunctionReports(DataStore.CurrentInterface, (int)FunctionFlags.GMP_EXT_DEVICE_FUNC_BIT_Z_RAPOR, ref stFunctionParameters, 120 * 1000);
                            if (retcode == Defines.TRAN_RESULT_OK)
                            {

                                FiscalPrinter_GetCurrentFiscalCounters:
                                retcode = GMPSmartDLL.FP3_GetCurrentFiscalCounters(DataStore.gmpResult.GmpInfo.CurrentInterface, ref ZNo, ref FNo, ref EKUNo);
                                int Z_No = 0;
                                Z_No = Convert.ToInt32(ZNo);//Sonkini verir...

                                if (retcode != Defines.TRAN_RESULT_OK)
                                {
                                    if (againCount < 3)
                                    {
                                        againCount++;
                                        goto FiscalPrinter_GetCurrentFiscalCounters;
                                    }
                                }
                                againCount = 0;

                                stFunctionParameters.start.ZNo = (uint)Z_No;// - 1;

                                ST_Z_REPORT_P16 stZReport = new ST_Z_REPORT_P16();
                                retcode = Json_GMPSmartDLL.FP3_FunctionReadZReportP16(DataStore.CurrentInterface, ref stFunctionParameters, ref stZReport, 120 * 1000);

                                if (retcode == Defines.TRAN_RESULT_OK)
                                {
                                    response.Status = true;
                                    response.Data = Newtonsoft.Json.JsonConvert.SerializeObject(stZReport);
                                    return response;
                                }
                                else
                                {
                                    response.Status = false;
                                    response.Data = "";
                                    response.Message = $"{retcode}-{ErrorClass.DisplayErrorCodeMessage(retcode)}-{ErrorClass.DisplayErrorMessage(retcode)}";
                                    response.ErrorCode = retcode.ToString();
                                    return response;
                                }
                            }
                            else
                            {
                                response.Status = false;
                                response.Data = "";
                                response.Message = $"{retcode}-{ErrorClass.DisplayErrorCodeMessage(retcode)}-{ErrorClass.DisplayErrorMessage(retcode)}";
                                response.ErrorCode = retcode.ToString();
                                return response;
                            }
                        }
                        catch (Exception ex)
                        {
                            response.Status = false;
                            response.Data = "";
                            response.Message = $"{ex.Message}";
                            return response;
                        }
                        break;
                    case ReportType.XReport:
                        try
                        {
                            if (!string.IsNullOrEmpty(reportType.startDate) && !string.IsNullOrEmpty(reportType.lastDate))
                            {
                                stFunctionParameters = DateParams(reportType, stFunctionParameters);
                            }
                            retcode = Json_GMPSmartDLL.FP3_FunctionReports(DataStore.CurrentInterface, (int)FunctionFlags.GMP_EXT_DEVICE_FUNC_BIT_X_RAPOR, ref stFunctionParameters, 120 * 1000);
                            if (retcode == Defines.TRAN_RESULT_OK)
                            {
                                response.Status = true;
                                response.Data = "X RAPORU YAZDIRILDI";
                                return response;
                            }
                            else
                            {
                                response.Status = false;
                                response.Data = "";
                                response.ErrorCode = retcode.ToString();

                                response.Message = $"{retcode}-{ErrorClass.DisplayErrorCodeMessage(retcode)}-{ErrorClass.DisplayErrorMessage(retcode)}";
                                return response;

                            }
                        }
                        catch (Exception ex)
                        {
                            response.Status = false;
                            response.Data = "";
                            response.Message = $"{ex.Message}";
                            return response;

                        }
                        break;
                    case ReportType.EkuReport:

                        try
                        {
                            retcode = Json_GMPSmartDLL.FP3_FunctionReports(DataStore.CurrentInterface, (int)FunctionFlags.GMP_EXT_DEVICE_FUNC_BIT_EKU_RAPOR, ref stFunctionParameters, 120 * 1000);
                            if (retcode == Defines.TRAN_RESULT_OK)
                            {
                                response.Status = true;
                                response.Data = "EKÜ RAPORU YAZDIRILDI";
                                return response;
                            }
                            else
                            {
                                response.Status = false;
                                response.Data = "";
                                response.ErrorCode = retcode.ToString();

                                response.Message = $"{retcode}-{ErrorClass.DisplayErrorCodeMessage(retcode)}-{ErrorClass.DisplayErrorMessage(retcode)}";
                                return response;
                            }
                        }
                        catch (Exception ex)
                        {
                            response.Status = false;
                            response.Data = "";
                            response.Message = $"{ex.Message}";
                            return response;
                        }
                        break;
                    case ReportType.KumulatifReport:

                        try
                        {
                            if (!string.IsNullOrEmpty(reportType.startDate) && !string.IsNullOrEmpty(reportType.lastDate))
                            {
                                stFunctionParameters = DateParams(reportType, stFunctionParameters);
                            }
                            retcode = Json_GMPSmartDLL.FP3_FunctionReports(DataStore.CurrentInterface, (int)FunctionFlags.GMP_EXT_DEVICE_FUNC_BIT_MALI_KUMULATIF, ref stFunctionParameters, 120 * 1000);
                            if (retcode == Defines.TRAN_RESULT_OK)
                            {
                                response.Status = true;
                                response.Data = "KÜMÜLATİF RAPORU YAZDIRILDI";
                                return response;
                            }
                            else
                            {
                                response.Status = false;
                                response.Data = "";
                                response.ErrorCode = retcode.ToString();

                                response.Message = $"{retcode}-{ErrorClass.DisplayErrorCodeMessage(retcode)}-{ErrorClass.DisplayErrorMessage(retcode)}";
                                return response;

                            }

                        }
                        catch (Exception ex)
                        {
                            response.Status = false;
                            response.Data = "";
                            response.Message = $"{ex.Message}";
                            return response;

                        }

                        break;
                    case ReportType.MaliReport:
                        try
                        {
                            if (!string.IsNullOrEmpty(reportType.startDate) && !string.IsNullOrEmpty(reportType.lastDate))
                            {
                                stFunctionParameters = DateParams(reportType, stFunctionParameters);
                            }
                            retcode = Json_GMPSmartDLL.FP3_FunctionReports(DataStore.CurrentInterface, (int)FunctionFlags.GMP_EXT_DEVICE_FUNC_BIT_MALI_RAPOR, ref stFunctionParameters, 120 * 1000);
                            if (retcode == Defines.TRAN_RESULT_OK)
                            {
                                response.Status = true;
                                response.Data = "MALİ RAPOR YAZDIRILDI";
                                return response;
                            }
                            else
                            {
                                response.Status = false;
                                response.Data = "";
                                response.ErrorCode = retcode.ToString();

                                response.Message = $"{retcode}-{ErrorClass.DisplayErrorCodeMessage(retcode)}-{ErrorClass.DisplayErrorMessage(retcode)}";
                                return response;

                            }

                        }
                        catch (Exception ex)
                        {
                            response.Status = false;
                            response.Data = "";
                            response.Message = $"{ex.Message}";
                            return response;

                        }
                        break;
                    case ReportType.DateBetweenEkuReport:
                        try
                        {
                            if (!string.IsNullOrEmpty(reportType.startDate) && !string.IsNullOrEmpty(reportType.lastDate))
                            {
                                stFunctionParameters = DateParams(reportType, stFunctionParameters);
                            }
                            retcode = Json_GMPSmartDLL.FP3_FunctionReports(DataStore.CurrentInterface, (int)FunctionFlags.GMP_EXT_DEVICE_FUNC_EKU_RAPOR_IKI_TARIH_ARASI, ref stFunctionParameters, 120 * 1000);
                            if (retcode == Defines.TRAN_RESULT_OK)
                            {
                                response.Status = true;
                                response.Data = "İKİ TARİH ARASI EKÜ RAPOR YAZDIRILDI";
                                return response;
                            }
                            else
                            {
                                response.Status = false;
                                response.Data = "";
                                response.ErrorCode = retcode.ToString();

                                response.Message = $"{retcode}-{ErrorClass.DisplayErrorCodeMessage(retcode)}-{ErrorClass.DisplayErrorMessage(retcode)}";
                                return response;

                            }

                        }
                        catch (Exception ex)
                        {
                            response.Status = false;
                            response.Data = "";
                            response.Message = $"{ex.Message}";
                            return response;

                        }
                        break;
                    case ReportType.ReceiptBetweenEkuReport:
                        try
                        {
                            stFunctionParameters = new ST_FUNCTION_PARAMETERS();
                            stFunctionParameters.start.ZNo = (uint)Convert.ToInt32(reportType.Zno);
                            stFunctionParameters.finish.ZNo = (uint)Convert.ToInt32(reportType.Zno);
                            stFunctionParameters.finish.FNo = (uint)Convert.ToInt32(reportType.startZno);
                            stFunctionParameters.start.FNo = (uint)Convert.ToInt32(reportType.lastZno);

                            retcode = Json_GMPSmartDLL.FP3_FunctionReports(DataStore.CurrentInterface, (int)FunctionFlags.GMP_EXT_DEVICE_FUNC_EKU_RAPOR_FISTEN_FISE, ref stFunctionParameters, 120 * 1000);
                            if (retcode == Defines.TRAN_RESULT_OK)
                            {
                                response.Status = true;
                                response.Data = "EKU RAPOR FISTEN FISE YAZDIRILDI";
                                return response;
                            }
                            else
                            {
                                response.Status = false;
                                response.Data = "";
                                response.ErrorCode = retcode.ToString();

                                response.Message = $"{retcode}-{ErrorClass.DisplayErrorCodeMessage(retcode)}-{ErrorClass.DisplayErrorMessage(retcode)}";
                                return response;

                            }

                        }
                        catch (Exception ex)
                        {
                            response.Status = false;
                            response.Data = "";
                            response.Message = $"{ex.Message}";
                            return response;

                        }
                        break;
                    case ReportType.BankEndOfDay:

                        try
                        {
                            retcode = Json_GMPSmartDLL.FP3_FunctionReports(DataStore.CurrentInterface, (int)FunctionFlags.GMP_EXT_DEVICE_FUNC_BIT_BANKA_GUN_SONU, ref stFunctionParameters, 120 * 1000);
                            if (retcode == Defines.TRAN_RESULT_OK)
                            {
                                response.Status = true;
                                response.Data = "BANKA GÜN SONLARI YAZDIRILDI";
                                return response;
                            }
                            else
                            {
                                response.Status = false;
                                response.Data = "";
                                response.ErrorCode = retcode.ToString();

                                response.Message = $"{retcode}-{ErrorClass.DisplayErrorCodeMessage(retcode)}-{ErrorClass.DisplayErrorMessage(retcode)}";
                                return response;

                            }

                        }
                        catch (Exception ex)
                        {
                            response.Status = false;
                            response.Data = "";
                            response.Message = $"{ex.Message}";
                            return response;

                        }

                        break;
                    case ReportType.ReceiptCopy:
                        try
                        {
                            retcode = Json_GMPSmartDLL.FP3_FunctionReports(DataStore.CurrentInterface, (int)FunctionFlags.GMP_EXT_DEVICE_FUNC_EKU_RAPOR_SON_KOPYA, ref stFunctionParameters, 120 * 1000);
                            if (retcode == Defines.TRAN_RESULT_OK)
                            {
                                response.Status = true;
                                response.Data = "SON KOPYA YAZDIRILDI";
                                return response;
                            }
                            else
                            {
                                response.Status = false;
                                response.Data = "";
                                response.ErrorCode = retcode.ToString();

                                response.Message = $"{retcode}-{ErrorClass.DisplayErrorCodeMessage(retcode)}-{ErrorClass.DisplayErrorMessage(retcode)}";
                                return response;

                            }

                        }
                        catch (Exception ex)
                        {
                            response.Status = false;
                            response.Data = "";
                            response.Message = $"{ex.Message}";
                            return response;

                        }

                        break;
                    case ReportType.EkuRead:
                        try
                        {

                            var result = EkuReading();
                            if (result.ReturnCode != Defines.TRAN_RESULT_OK)
                            {
                                response.Status = false;
                                response.Data = "";
                                response.ErrorCode = result.ReturnCode.ToString();

                                response.Message = $"{result.ReturnCode}-{ErrorClass.DisplayErrorCodeMessage(result.ReturnCode)}-{ErrorClass.DisplayErrorMessage(result.ReturnCode)}";
                                return response;
                            }
                            else
                            {
                                double totalEku = result.EkuInfo.Eku.DataUsedArea + result.EkuInfo.Eku.DataFreeArea;

                                var DataUsedAreaPecent = Math.Round(Convert.ToDouble((result.EkuInfo.Eku.DataUsedArea * 100.0) / totalEku), 2);
                                var DataFreeAreaPecent = 100 - DataUsedAreaPecent;
                                response.Status = true;
                                response.Data = string.Format("Kullanılan Ekü : % {0} , Kalan Ekü : % {1}", DataUsedAreaPecent, DataFreeAreaPecent);
                                return response;
                            }
                        }
                        catch (Exception ex)
                        {
                            response.Status = false;
                            response.Data = "";
                            response.Message = $"{ex.Message}";
                            return response;
                        }


                        break;
                    case ReportType.EkuReading:
                        try
                        {

                            var result = EkuReading();
                            if (result.ReturnCode != Defines.TRAN_RESULT_OK)
                            {
                                response.Status = false;
                                response.Data = "";
                                response.ErrorCode = result.ReturnCode.ToString();

                                response.Message = $"{result.ReturnCode}-{ErrorClass.DisplayErrorCodeMessage(result.ReturnCode)}-{ErrorClass.DisplayErrorMessage(result.ReturnCode)}";
                                return response;
                            }
                            else
                            {
                                response.Data = Newtonsoft.Json.JsonConvert.SerializeObject(result);
                                return response;
                            }
                        }
                        catch (Exception ex)
                        {
                            response.Status = false;
                            response.Data = "";
                            response.Message = $"{ex.Message}";
                            return response;
                        }


                        break;
                    case ReportType.ParamsLoad:
                        try
                        {
                            retcode = Json_GMPSmartDLL.FP3_FunctionReports(DataStore.CurrentInterface, (int)FunctionFlags.GMP_EXT_DEVICE_FUNC_BIT_PARAM_YUKLE, ref stFunctionParameters, 120 * 1000);
                            if (retcode == Defines.TRAN_RESULT_OK)
                            {
                                response.Status = true;
                                response.Data = "PARAMETRE YÜKLEMESİ BAŞARILI OLDU";
                                return response;
                            }
                            else
                            {
                                response.Status = false;
                                response.Data = "";
                                response.ErrorCode = retcode.ToString();

                                response.Message = $"{retcode}-{ErrorClass.DisplayErrorCodeMessage(retcode)}-{ErrorClass.DisplayErrorMessage(retcode)}";
                                return response;

                            }

                        }
                        catch (Exception ex)
                        {
                            response.Status = false;
                            response.Data = "";
                            response.Message = $"{ex.Message}";
                            return response;

                        }
                        break;
                    case ReportType.ZBetween:

                        try
                        {
                            if (!string.IsNullOrEmpty(reportType.startDate) && !string.IsNullOrEmpty(reportType.lastDate))
                            {
                                stFunctionParameters = DateParams(reportType, stFunctionParameters);
                            }
                            retcode = Json_GMPSmartDLL.FP3_FunctionReports(DataStore.CurrentInterface, (int)FunctionFlags.GMP_EXT_DEVICE_FUNC_BIT_Z_RAPOR, ref stFunctionParameters, 120 * 1000);
                            if (retcode == Defines.TRAN_RESULT_OK)
                            {
                                response.Status = true;
                                response.Data = "Z RAPORU YAZDIRILDI";
                                return response;
                            }
                            else
                            {
                                response.Status = false;
                                response.Data = "";
                                response.ErrorCode = retcode.ToString();

                                response.Message = $"{retcode}-{ErrorClass.DisplayErrorCodeMessage(retcode)}-{ErrorClass.DisplayErrorMessage(retcode)}";
                                return response;
                            }
                        }
                        catch (Exception ex)
                        {
                            response.Status = false;
                            response.Data = "";
                            response.Message = $"{ex.Message}";
                            return response;

                        }

                        break;
                    case ReportType.ZNoBetween:
                        try
                        {
                            stFunctionParameters = new ST_FUNCTION_PARAMETERS();

                            int startZNo = 0;
                            int finishZNo = 0;

                            stFunctionParameters.start.ZNo = (uint)Convert.ToUInt32(reportType.startZno);
                            stFunctionParameters.finish.ZNo = (uint)Convert.ToUInt32(reportType.lastZno);
                            retcode = Json_GMPSmartDLL.FP3_FunctionReports(DataStore.CurrentInterface, (int)FunctionFlags.GMP_EXT_DEVICE_FUNC_BIT_Z_RAPOR, ref stFunctionParameters, 120 * 1000);
                            if (retcode == Defines.TRAN_RESULT_OK)
                            {
                                response.Status = true;
                                response.Data = "Z RAPORU YAZDIRILDI";
                                return response;
                            }
                            else
                            {
                                response.Status = false;
                                response.Data = "";
                                response.ErrorCode = retcode.ToString();

                                response.Message = $"{retcode}-{ErrorClass.DisplayErrorCodeMessage(retcode)}-{ErrorClass.DisplayErrorMessage(retcode)}";
                                return response;
                            }
                        }
                        catch (Exception ex)
                        {
                            response.Status = false;
                            response.Data = "";
                            response.Message = $"{ex.Message}";
                            return response;

                        }
                        break;
                    case ReportType.ZreportBetweenWebServis:
                        try
                        {
                            var startZno = Convert.ToInt32(reportType.Zno);

                            FiscalPrinter_GetCurrentFiscalCounters:
                            retcode = GMPSmartDLL.FP3_GetCurrentFiscalCounters(DataStore.gmpResult.GmpInfo.CurrentInterface, ref ZNo, ref FNo, ref EKUNo);

                           var finishZNo = Convert.ToInt32(ZNo);

                            if (retcode != Defines.TRAN_RESULT_OK)
                            {
                                if (againCount < 3)
                                {
                                    againCount++;
                                    goto FiscalPrinter_GetCurrentFiscalCounters;
                                }
                            }
                            List<string> receiptList = new List<string>();
                            for (int i = startZno; i < finishZNo; i++)
                            {
                                stFunctionParameters.start.ZNo = (uint)Convert.ToInt32(reportType.Zno);// - 1;

                                ST_Z_REPORT_P16 stZReport = new ST_Z_REPORT_P16();
                                retcode = Json_GMPSmartDLL.FP3_FunctionReadZReportP16(DataStore.CurrentInterface, ref stFunctionParameters, ref stZReport, 120 * 1000);

                                receiptList.Add(Newtonsoft.Json.JsonConvert.SerializeObject(stZReport));
                            }

                            response.Status = true;
                            response.Data = Newtonsoft.Json.JsonConvert.SerializeObject(receiptList);
                            return response;

                        }
                        catch (Exception ex)
                        {
                            response.Status = false;
                            response.Data = "";
                            response.Message = $"{ex.Message}";
                            return response;
                        }
                        break;
                    case ReportType.ZreportWebServis:
                        try
                        {
                            stFunctionParameters.start.ZNo = (uint)Convert.ToInt32(reportType.Zno);// - 1;

                            ST_Z_REPORT_P16 stZReport = new ST_Z_REPORT_P16();
                            retcode = Json_GMPSmartDLL.FP3_FunctionReadZReportP16(DataStore.CurrentInterface, ref stFunctionParameters, ref stZReport, 120 * 1000);

                            if (retcode == Defines.TRAN_RESULT_OK)
                            {
                                response.Status = true;
                                response.Data = Newtonsoft.Json.JsonConvert.SerializeObject(stZReport);
                                return response;
                            }
                            else
                            {
                                response.Status = false;
                                response.Data = "";
                                response.Message = $"{retcode}-{ErrorClass.DisplayErrorCodeMessage(retcode)}-{ErrorClass.DisplayErrorMessage(retcode)}";
                                response.ErrorCode = retcode.ToString();
                                return response;
                            }

                        }
                        catch (Exception ex)
                        {
                            response.Status = false;
                            response.Data = "";
                            response.Message = $"{ex.Message}";
                            return response;
                        }


                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return response;
        }
        private ST_FUNCTION_PARAMETERS DateParams(ReportRequest req, ST_FUNCTION_PARAMETERS _stFunctionParameters)
        {
            var startDate = Convert.ToDateTime(req.startDate);
            var lastDate = Convert.ToDateTime(req.lastDate);
            _stFunctionParameters.start.date.day = (byte)startDate.Day;
            _stFunctionParameters.start.date.month = (byte)startDate.Month;
            _stFunctionParameters.start.date.year = Convert.ToByte(startDate.Year.ToString().Substring(2, 2));
            _stFunctionParameters.start.time.hour = (byte)startDate.Hour;
            _stFunctionParameters.start.time.minute = (byte)startDate.Minute;
            _stFunctionParameters.start.time.second = (byte)startDate.Second;
            _stFunctionParameters.finish.date.day = (byte)lastDate.Day;
            _stFunctionParameters.finish.date.month = (byte)lastDate.Month;
            _stFunctionParameters.finish.date.year = Convert.ToByte(lastDate.Year.ToString().Substring(2, 2));
            _stFunctionParameters.finish.time.hour = (byte)lastDate.Hour;
            _stFunctionParameters.finish.time.minute = (byte)lastDate.Minute;
            _stFunctionParameters.finish.time.second = (byte)lastDate.Second;
            return _stFunctionParameters;
        }
        public GmpReportsInfoDto EkuReading()
        {
            GmpReportsInfoDto _gmpReportsInfoDto = new GmpReportsInfoDto();
            try
            {
                ST_EKU_MODULE_INFO pstEkuModuleInfo = new ST_EKU_MODULE_INFO();

                uint retcode = Json_GMPSmartDLL.FP3_FunctionEkuReadInfo(DataStore.gmpResult.GmpInfo.CurrentInterface, (ushort)EInfo.TLV_FUNC_INFO_DEVICE, ref pstEkuModuleInfo, Defines.TIMEOUT_DEFAULT);

                _gmpReportsInfoDto.ReturnCode = retcode;
                _gmpReportsInfoDto.ReturnMessage = ErrorClass.DisplayErrorMessage(retcode);
                _gmpReportsInfoDto.EkuInfo = pstEkuModuleInfo;
                if (retcode != Defines.TRAN_RESULT_OK)
                {
                    return _gmpReportsInfoDto;
                }

                retcode = Json_GMPSmartDLL.FP3_FunctionEkuReadInfo(DataStore.gmpResult.GmpInfo.CurrentInterface, (ushort)EInfo.TLV_FUNC_INFO_EKU, ref pstEkuModuleInfo, Defines.TIMEOUT_DEFAULT);
                _gmpReportsInfoDto.ReturnCode = retcode;
                _gmpReportsInfoDto.ReturnMessage = ErrorClass.DisplayErrorMessage(retcode);
                _gmpReportsInfoDto.EkuInfo = pstEkuModuleInfo;
                if (retcode != Defines.TRAN_RESULT_OK)
                {
                    return _gmpReportsInfoDto;
                }
            }
            catch (Exception ex)
            {
            }
            return _gmpReportsInfoDto;

        }
    }
}
