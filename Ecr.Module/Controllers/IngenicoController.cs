using Ecr.Module.Services;
using Ecr.Module.Services.Ingenico;
using Ecr.Module.Services.Ingenico.BankList;
using Ecr.Module.Services.Ingenico.FiscalLogManager;
using Ecr.Module.Services.Ingenico.GmpIngenico;
using Ecr.Module.Services.Ingenico.Models;
using Ecr.Module.Services.Ingenico.Pairing;
using Ecr.Module.Services.Ingenico.Print;
using Ecr.Module.Services.Ingenico.ReceiptHeader;
using Ecr.Module.Services.Ingenico.Reports;
using Ecr.Module.Services.Ingenico.Settings;
using Ecr.Module.Services.Ingenico.SingleMethod;
using Serilog;
using Serilog.Sinks.File.Archive;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Web.Http;
using System.Windows.Forms;

namespace Ecr.Module.Controllers
{
    [RoutePrefix("ingenico")]
    public class IngenicoController : ApiController
    {
        private static ILogger _logger;
        private readonly IngenicoService _ingenicoService;

        public IngenicoController()
        {
            if (_logger == null)
            {
                _logger = new LoggerConfiguration()
                    .Enrich.FromLogContext()
                    .WriteTo.Console()
                    .WriteTo.File("EcrLog\\log_module_.txt",
                        rollingInterval: RollingInterval.Day,
                                          rollOnFileSizeLimit: true,
                                          fileSizeLimitBytes: 100_000_000,
                                          retainedFileCountLimit: 1,
                                          hooks: new ArchiveHooks(CompressionLevel.Fastest))
                    .MinimumLevel.Debug()
                    .CreateLogger();
            }

            _ingenicoService = new IngenicoService();
        }

        [HttpGet]
        [Route("Health")]
        public string Health()
        {
            _logger.Information("API isteği alındı: GET /ingenico/Health");
            ShowNotification("API İsteği Alındı", "GET /ingenico/Health");

            return "Healthy";
        }

        [HttpGet]
        [Route("Completed/{orderKey}")]
        public string Completed(string orderKey)
        {
            _logger.Information("API isteği alındı: GET /ingenico/Completed");
            ShowNotification("API İsteği Alındı", "GET /ingenico/Completed");
            var result = LogManagerOrder.MoveLogFile(orderKey, LogFolderType.Completed);
            return result ? "Completed" : "Error";
        }

        [HttpGet]
        [Route("CashRegisterStatus")]
        public string CashRegisterStatus()
        {
            _logger.Information("API isteği alındı: GET /ingenico/Completed");
            ShowNotification("API İsteği Alındı", "GET /ingenico/Completed");
            var result = DataStore.CashRegisterStatus;
            return result;
        }

        [HttpGet]
        [Route("IsCompleted/{orderKey}")]
        public IngenicoApiResponse<GmpPrintReceiptDto> IsCompleted(string orderKey)
        {
            _logger.Information("API isteği alındı: GET /ingenico/Completed");
            ShowNotification("API İsteği Alındı", "GET /ingenico/Completed");
            var response = PrintReceiptGmpProvider.EftPrintIsCompleted(orderKey);
            return response;
        }

        [HttpGet]
        [Route("GetFiscal/{orderKey}")]
        public IngenicoApiResponse<FiscalOrder> GetFiscal(string orderKey)
        {
            _logger.Information("API isteği alındı: GET /ingenico/GetFiscal");
            ShowNotification("API İsteği Alındı", "GET /ingenico/GetFiscal");
            var response = PrintReceiptGmpProvider.GetEftPrintFiscal(orderKey);
            return response;
        }

        [HttpGet]
        [Route("GetEftFiscal/{orderKey}")]
        public IngenicoApiResponse<FiscalOrder> GetEftFiscal(string orderKey)
        {
            _logger.Information("API isteği alındı: GET /ingenico/GetEftFiscal");
            ShowNotification("API İsteği Alındı", "GET /ingenico/GetEftFiscal");
            var response = PrintReceiptGmpProvider.GetEftFiscal(orderKey);
            return response;
        }
        [HttpGet]
        [Route("IsWaiting")]
        public IngenicoApiResponse<List<string>> IsWaiting()
        {
            _logger.Information("API isteği alındı: GET /ingenico/IsWaiting");
            ShowNotification("API İsteği Alındı", "GET /ingenico/IsWaiting");
            var response = PrintReceiptGmpProvider.EftPrintWaiting();
            return response;
        }
        [HttpPost]
        [Route("Pairing")]
        public IngenicoApiResponse<GmpPairingDto> Pairing()
        {
            ShowNotification("API İsteği Alındı", "HttpPost /ingenico/Pairing ");
            _logger.Information($"API isteği Alındı: HttpPost /ingenico/Pairing");
            var response = new IngenicoApiResponse<GmpPairingDto>();
            try
            {
                DataStore.gmpxml = Path.Combine(System.Windows.Forms.Application.StartupPath, "GMP.XML");
                DataStore.gmpini = Path.Combine(System.Windows.Forms.Application.StartupPath, "GMP.ini");
                SettingsInfo.getIniValues();

                SettingsInfo.getGMPIniValues();

                SettingsInfo.setXmlValues();

                var gmpPair = new PairingGmpProvider();
                var result = gmpPair.GmpPairing();
                if (result.ReturnCode == Defines.TRAN_RESULT_OK)
                {
                    var gmpBankList = new BankList();
                    result.GmpInfo.BankInfoList = gmpBankList.GetBankList().Data;
                    var header = new Header();
                    result.GmpInfo.fiscalHeader = header.GmpGetReceiptHeader().Data;
                    ST_ECHO stEcho = new ST_ECHO();
                    result.ReturnCode = Json_GMPSmartDLL.FP3_Echo(result.GmpInfo.CurrentInterface, ref stEcho, Defines.TIMEOUT_ECHO);
                    result.GmpInfo.ActiveCashier = stEcho.activeCashier.name;
                    result.GmpInfo.ActiveCashierNo = stEcho.activeCashier.index + 1;
                    result.GmpInfo.EcrStatus = (int)stEcho.status;
                    result.GmpInfo.ecrMode = stEcho.ecrMode;
                    DataStore.gmpResult = result;
                }
                response.Data = result;
                response.Message = result.ReturnCodeMessage;
                response.ErrorCode = result.ReturnCode.ToString();
                response.Status = result.ReturnCode == Defines.TRAN_RESULT_OK ? true : false;
                DataStore.Connection = response.Status ? ConnectionStatus.Connected : ConnectionStatus.NotConnected;
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.ErrorCode = "9999";
                response.Message = ex.Message;
            }
            _logger.Information($"API isteğine cevap verildi : HttpPost /ingenico/Pairing => {Newtonsoft.Json.JsonConvert.SerializeObject(response)}");
            return response;
        }

        [HttpPost]
        [Route("Echo")]
        public IngenicoApiResponse<GmpPairingDto> Echo()
        {
            ShowNotification("API İsteği Alındı", "HttpPost /ingenico/Echo");
            _logger.Information($"API isteği Alındı: HttpPost /ingenico/Echo");
            var response = new IngenicoApiResponse<GmpPairingDto>();
            try
            {
                var result = new GmpPairingDto();
                ST_ECHO stEcho = new ST_ECHO();
                result.ReturnCode = Json_GMPSmartDLL.FP3_Echo(result.GmpInfo.CurrentInterface, ref stEcho, Defines.TIMEOUT_ECHO);
                result.GmpInfo.ActiveCashier = stEcho.activeCashier.name;
                result.GmpInfo.ActiveCashierNo = stEcho.activeCashier.index + 1;
                result.GmpInfo.EcrStatus = (int)stEcho.status;
                result.GmpInfo.ecrMode = stEcho.ecrMode;
                DataStore.gmpResult = result;
                response.Data = result;
                response.Message = result.ReturnCodeMessage;
                response.ErrorCode = result.ReturnCode.ToString();
                response.Status = result.ReturnCode == Defines.TRAN_RESULT_OK ? true : false;
                DataStore.Connection = response.Status ? ConnectionStatus.Connected : ConnectionStatus.NotConnected;
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.ErrorCode = "9999";
                response.Message = ex.Message;
            }
            _logger.Information($"API isteğine cevap verildi : HttpPost /ingenico/Echo => {Newtonsoft.Json.JsonConvert.SerializeObject(response)}");
            return response;
        }

        [HttpPost]
        [Route("BankList")]
        public IngenicoApiResponse<List<BankInfoDto>> BankList()
        {
            ShowNotification("API İsteği Alındı", "HttpPost /ingenico/BankList");
            _logger.Information($"API isteği Alındı: HttpPost /ingenico/BankList");
            var response = new IngenicoApiResponse<List<BankInfoDto>>();
            try
            {
                var gmpBankList = new BankList();
                response = gmpBankList.GetBankList();

            }
            catch (Exception ex)
            {
                response.Status = false;
                response.ErrorCode = "9999";
                response.Message = ex.Message;
            }
            _logger.Information($"API isteğine cevap verildi : HttpPost /ingenico/BankList => {Newtonsoft.Json.JsonConvert.SerializeObject(response)}");
            return response;
        }

        [HttpPost]
        [Route("Header")]
        public IngenicoApiResponse<FiscalHeaderDto> Header()
        {
            ShowNotification("API İsteği Alındı", "HttpPost /ingenico/Header");
            _logger.Information($"API isteği Alındı: HttpPost /ingenico/Header");
            var response = new IngenicoApiResponse<FiscalHeaderDto>();
            try
            {
                var header = new Header();
                response = header.GmpGetReceiptHeader();

            }
            catch (Exception ex)
            {
                response.Status = false;
                response.ErrorCode = "9999";
                response.Message = ex.Message;
            }
            _logger.Information($"API isteğine cevap verildi : HttpPost /ingenico/Header => {Newtonsoft.Json.JsonConvert.SerializeObject(response)}");
            return response;
        }

        [HttpPost]
        [Route("TaxGroupsPairing")]
        public IngenicoApiResponse<TaxGroupsInfoDto> TaxGroupsPairing([FromBody] List<TaxGroups> taxGroups)
        {
            ShowNotification("API İsteği Alındı", "HttpPost /ingenico/TaxGroupsPairing");
            _logger.Information($"API isteği Alındı: HttpPost /ingenico/TaxGroupsPairing");
            var response = new IngenicoApiResponse<TaxGroupsInfoDto>();
            try
            {
                var taxGroup = new TaxGroup();
                var result = taxGroup.TaxGroupsPairing(taxGroups);
                response.Data = result.Data;
                response.Message = result.Data.ReturnCodeMessage;
                response.ErrorCode = result.Data.ReturnCode.ToString();
                response.Status = result.Data.ReturnCode == Defines.TRAN_RESULT_OK ? true : false;
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.ErrorCode = "9999";
                response.Message = ex.Message;
            }
            _logger.Information($"API isteğine cevap verildi : HttpPost /ingenico/TaxGroupsPairing => {Newtonsoft.Json.JsonConvert.SerializeObject(response)}");
            return response;
        }

        [HttpPost]
        [Route("GetTaxGroups")]
        public IngenicoApiResponse<TaxGroupsInfoDto> GetTaxGroups()
        {
            ShowNotification("API İsteği Alındı", "HttpPost /ingenico/GetTaxGroups");
            _logger.Information($"API isteği Alındı: HttpPost /ingenico/GetTaxGroups");
            var response = new IngenicoApiResponse<TaxGroupsInfoDto>();
            try
            {
                var taxGroup = new TaxGroup();
                var result = taxGroup.GettaxGroups();
                response.Data = result.Data;
                response.Message = result.Data.ReturnCodeMessage;
                response.ErrorCode = result.Data.ReturnCode.ToString();
                response.Status = result.Data.ReturnCode == Defines.TRAN_RESULT_OK ? true : false;
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.ErrorCode = "9999";
                response.Message = ex.Message;
            }
            _logger.Information($"API isteğine cevap verildi : HttpPost /ingenico/GetTaxGroups => {Newtonsoft.Json.JsonConvert.SerializeObject(response)}");
            return response;
        }

        [HttpPost]
        [Route("GetDepartmans")]
        public IngenicoApiResponse<ST_DEPARTMENT[]> GetDepartmans()
        {
            ShowNotification("API İsteği Alındı", "HttpPost /ingenico/GetDepartmans");
            _logger.Information($"API isteği Alındı: HttpPost /ingenico/GetDepartmans");
            var response = new IngenicoApiResponse<ST_DEPARTMENT[]>();
            try
            {
                var taxGroup = new TaxGroup();
                var result = taxGroup.GetDepartmans();
                response.Data = result.Data;
                response.Message = result.Message;
                response.ErrorCode = result.ErrorCode;
                response.Status = result.Status;
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.ErrorCode = "9999";
                response.Message = ex.Message;
            }
            _logger.Information($"API isteğine cevap verildi : HttpPost /ingenico/GetDepartmans => {Newtonsoft.Json.JsonConvert.SerializeObject(response)}");
            return response;
        }

        [HttpPost]
        [Route("ReadZReport")]
        public IngenicoApiResponse<string> ReadZReport([FromBody] ReportRequest reportRequest)
        {
            ShowNotification("API İsteği Alındı", "HttpPost /ingenico/ReadZReport");
            _logger.Information($"API isteği Alındı: HttpPost /ingenico/ReadZReport");
            var response = new IngenicoApiResponse<string>();
            try
            {
                var report = new ReportPrinting();
                var result = report.ReportPrint(reportRequest);
                response.Data = result.Data;
                response.Message = result.Message;
                response.ErrorCode = result.ErrorCode;
                response.Status = result.Status;
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.ErrorCode = "9999";
                response.Message = ex.Message;
            }
            _logger.Information($"API isteğine cevap verildi : HttpPost /ingenico/ReadZReport => {Newtonsoft.Json.JsonConvert.SerializeObject(response)}");
            return response;
        }

        [HttpPost]
        [Route("EftPosPrintOrder")]
        public IngenicoApiResponse<GmpPrintReceiptDto> EftPosPrintOrder([FromBody] FiscalOrder fiscalOrder)
        {
            ShowNotification("API İsteği Alındı", "HttpPost /ingenico/EftPosPrintOrder");
            _logger.Information($"API isteği Alındı: HttpPost /ingenico/EftPosPrintOrder");
            var response = new IngenicoApiResponse<GmpPrintReceiptDto>();
            try
            {
                if (DataStore.Connection != ConnectionStatus.Connected)
                {
                    var gmpPair = new PairingGmpProvider();
                    var pairingResponse = gmpPair.pairingControl();
                    if (!pairingResponse.Status)
                    {
                        response.Message = pairingResponse.Message;
                        response.ErrorCode = pairingResponse.ErrorCode.ToString();
                        response.Status = pairingResponse.Status;
                        return response;
                    }
                }
                var fiscaltype = FiscalOrderType.GetFiscalOrderType(fiscalOrder);
                switch (fiscaltype)
                {
                    case FiscalType.Normal:
                        {
                            #region STEP 5 : Normal Fiş Yazımı (İlk fiş yazımı - Yarım kalan fişin yazımını devamı)

                            retry:
                            DataStore.CashRegisterStatus = "YAZARKASA İŞLEME BAŞLIYOR";
                            response = PrintReceiptGmpProvider.EftPosPrintOrder(fiscalOrder);
                            DataStore.CashRegisterStatus = "YAZARKASA BOŞTA";
                            if (response.Data.ReturnStringMessage == "YAZARKASA GEÇERSİZ SIRA NUMARASI" || response.Data.ReturnCode == 2346)
                            {
                                DataStore.Connection = ConnectionStatus.NotConnected;
                                var gmpPair = new PairingGmpProvider();
                                var pairingResponse = gmpPair.pairingControl();
                                if (!pairingResponse.Status)
                                {
                                    response.Message = pairingResponse.Message;
                                    response.ErrorCode = pairingResponse.ErrorCode.ToString();
                                    response.Status = pairingResponse.Status;
                                    return response;
                                }
                                else
                                {
                                    goto retry;
                                }
                            }

                            break;

                            #endregion
                        }
                    case FiscalType.Return:
                        {
                            #region STEP 6 : İade Fişi (Nakit , Kredi, Nakit + Kredi)

                            response = PrintReceiptReturnMode.ReturnPrintOrder(fiscalOrder);
                            break;

                            #endregion
                        }
                    case FiscalType.Void:
                        {
                            #region STEP 7 : İptal Fişi

                            response = PrinterVoidAll.EftPosVoidPrintOrder(fiscalOrder);
                            //LogManager.Append($"STEP 7 -> EftPosVoidPrintOrder -> {printResult.JsonSerialize()}", "printFiscal");
                            break;

                            #endregion
                        }
                }


                response.Status = response.Data.ReturnCode == Defines.TRAN_RESULT_OK ? true : false;
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.ErrorCode = "9999";
                response.Message = ex.Message;
            }
            _logger.Information($"API isteğine cevap verildi : HttpPost /ingenico/EftPosPrintOrder => {Newtonsoft.Json.JsonConvert.SerializeObject(response)}");
            return response;
        }

        [HttpPost]
        [Route("Ping")]
        public IngenicoApiResponse<GmpPingResultDto> Ping()
        {
            ShowNotification("API İsteği Alındı", "HttpPost /ingenico/Ping");
            _logger.Information($"API isteği Alındı: HttpPost /ingenico/Ping");
            var response = new IngenicoApiResponse<GmpPingResultDto>();
            var ret = new GmpPingResultDto();
            try
            {
                var ping = new PairingGmpProvider();
                ret = ping.GmpPing();

                if (ret.ReturnCode == Defines.TRAN_RESULT_OK)
                {
                    response.Status = true;
                    response.ErrorCode = 0.ToString();
                    response.Message = ret.ReturnStringMessage;
                    response.Data = ret;
                }
                else
                {
                    response.Status = false;
                    response.ErrorCode = ret.ReturnCode.ToString();
                    response.Message = ret.ReturnStringMessage + " - " + ret.ReturnCodeMessage;
                }

            }
            catch (Exception ex)
            {
                response.Status = false;
                response.ErrorCode = "9999";
                response.Message = ex.Message;
            }
            _logger.Information($"API isteğine cevap verildi : HttpPost /ingenico/Ping => {Newtonsoft.Json.JsonConvert.SerializeObject(response)}");
            return response;
        }

        [HttpPost]
        [Route("ReportPrint")]
        public IngenicoApiResponse<string> ReportPrint([FromBody] ReportRequest reportRequest)
        {
            ShowNotification("API İsteği Alındı", "HttpPost /ingenico/ReportPrint");
            _logger.Information($"API isteği Alındı: HttpPost /ingenico/ReportPrint");
            var response = new IngenicoApiResponse<string>();
            var ret = new IngenicoApiResponse<string>();
            try
            {
                var reportprint = new ReportPrinting();
                ret = reportprint.ReportPrint(reportRequest);

                if (ret.Status)
                {
                    response.Status = true;
                    response.ErrorCode = 0.ToString();
                    response.Message = ret.Message;
                    response.Data = ret.Data;
                }
                else
                {
                    response.Status = false;
                    response.ErrorCode = ret.ErrorCode;
                    response.Message = ret.Message;
                }

            }
            catch (Exception ex)
            {
                response.Status = false;
                response.ErrorCode = "9999";
                response.Message = ex.Message;
            }
            _logger.Information($"API isteğine cevap verildi : HttpPost /ingenico/ReportPrint => {Newtonsoft.Json.JsonConvert.SerializeObject(response)}");
            return response;
        }

        [HttpPost]
        [Route("SetHeader")]
        public IngenicoApiResponse<FiscalHeaderDto> SetHeader([FromBody] FiscalHeaderDto reportRequest)
        {
            ShowNotification("API İsteği Alındı", "HttpPost /ingenico/SetHeader");
            _logger.Information($"API isteği Alındı: HttpPost /ingenico/SetHeader");
            var response = new IngenicoApiResponse<FiscalHeaderDto>();
            var ret = new IngenicoApiResponse<FiscalHeaderDto>();
            try
            {
                var header = new Header();
                ret = header.GmpReceiptHeaderSend(reportRequest);

                if (ret.Status)
                {
                    response.Status = true;
                    response.ErrorCode = 0.ToString();
                    response.Message = ret.Message;
                    response.Data = ret.Data;
                }
                else
                {
                    response.Status = false;
                    response.ErrorCode = ret.ErrorCode;
                    response.Message = ret.Message;
                }

            }
            catch (Exception ex)
            {
                response.Status = false;
                response.ErrorCode = "9999";
                response.Message = ex.Message;
            }
            _logger.Information($"API isteğine cevap verildi : HttpPost /ingenico/SetHeader => {Newtonsoft.Json.JsonConvert.SerializeObject(response)}");
            return response;
        }

        private void ShowNotification(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
        {
            try
            {
                // Ana uygulamaya bildirim gönder
                var notifyEvent = new ApiNotificationEvent
                {
                    Title = title,
                    Message = message,
                    Icon = icon
                };

                ApiEvents.OnNotification?.Invoke(null, notifyEvent);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Bildirim gösterilirken hata oluştu: {Message}", ex.Message);
            }
        }

    }

    public class ApiNotificationEvent : EventArgs
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public ToolTipIcon Icon { get; set; }
    }

    public static class ApiEvents
    {
        public static EventHandler<ApiNotificationEvent> OnNotification;
    }
}