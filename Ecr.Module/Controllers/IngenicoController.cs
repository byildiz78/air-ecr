using Ecr.Module.Services;
using Ecr.Module.Services.Ingenico;
using Ecr.Module.Services.Ingenico.BankList;
using Ecr.Module.Services.Ingenico.FiscalLogManager;
using Ecr.Module.Services.Ingenico.GmpIngenico;
using Ecr.Module.Services.Ingenico.Models;
using Ecr.Module.Services.Ingenico.Pairing;
using Ecr.Module.Services.Ingenico.Print;
using Ecr.Module.Services.Ingenico.ReceiptHeader;
using Ecr.Module.Services.Ingenico.Recovery;
using Ecr.Module.Services.Ingenico.Reports;
using Ecr.Module.Services.Ingenico.Settings;
using Ecr.Module.Services.Ingenico.SingleMethod;
using Ecr.Module.Services.Ingenico.Transaction;
using Serilog;
using Serilog.Sinks.File.Archive;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
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

            // Phase 2.1: Startup Recovery Hook
            // DISABLED: Recovery now runs AFTER pairing (when connection is ready)
            // TryRecoveryOnStartup();
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

            // Phase 2.2: Use TransactionStateTracker for completion
            // SAFE: Falls back to existing mechanism if fails
            try
            {
                var tracker = new TransactionStateTracker();
                var result = tracker.CompleteTransaction(orderKey);
                return result ? "Completed" : "Error";
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to complete transaction via tracker: {orderKey}");

                // Fallback to existing mechanism
                var result = LogManagerOrder.MoveLogFile(orderKey, LogFolderType.Completed);
                return result ? "Completed" : "Error";
            }
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

                // V2: Improved retry logic, connection management, interface validation
                var gmpPair = new PairingGmpProviderV2();
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

                    // Pairing başarılı - şimdi orphan transactions için recovery çalıştır
                    TryRecoveryAfterPairing();
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
                    // V2: Improved retry logic, connection management, interface validation
                    var gmpPair = new PairingGmpProviderV2();
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
                                // V2: Improved retry logic, connection management, interface validation
                                var gmpPair = new PairingGmpProviderV2();
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
                // V2: Improved retry logic with ConnectionRetryHelper
                var ping = new PairingGmpProviderV2();
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

        /// <summary>
        /// Phase 2.1: Startup Recovery Hook
        /// SAFE: Try-catch wrapped, never breaks application startup
        /// Attempts to recover incomplete transactions on application startup
        /// </summary>
        private void TryRecoveryOnStartup()
        {
            try
            {
                _logger.Information("Startup: Checking for incomplete transactions...");

                var recovery = new RecoveryCoordinator();
                var result = recovery.AttemptRecovery();

                if (result.Success)
                {
                    switch (result.RecoveryAction)
                    {
                        case RecoveryActionType.Resume:
                            // Found active transaction that can be resumed
                            _logger.Information($"Recovery SUCCESS: Found active transaction - OrderKey={result.OrderKey}, Handle={result.TransactionHandle}, Commands={result.OrderCommands?.Count ?? 0}");

                            // Notify user about incomplete transaction
                            ShowNotification("Transaction Recovery",
                                $"Found incomplete transaction: {result.OrderKey}\nTransaction can be resumed.",
                                ToolTipIcon.Warning);
                            break;

                        case RecoveryActionType.Reset:
                            // Transaction was reset (no longer valid on device)
                            _logger.Information($"Recovery: Transaction reset - OrderKey={result.OrderKey} (transaction no longer exists on device)");
                            break;

                        case RecoveryActionType.Abort:
                            // Transaction aborted (too old or invalid)
                            _logger.Information($"Recovery: Transaction aborted - OrderKey={result.OrderKey} ({result.Message})");
                            break;

                        default:
                            _logger.Information($"Recovery: {result.RecoveryAction} - {result.Message}");
                            break;
                    }
                }
                else
                {
                    // No recovery needed or recovery failed
                    _logger.Information($"Recovery: {result.Message}");

                    // Check for orphan orders
                    if (result.OrphanOrders != null && result.OrphanOrders.Count > 0)
                    {
                        _logger.Warning($"Recovery: Found {result.OrphanOrders.Count} orphan order(s) in Waiting folder (no transaction state)");

                        // Notify user about orphan orders
                        ShowNotification("Orphan Orders Detected",
                            $"Found {result.OrphanOrders.Count} order(s) without transaction state.\nManual intervention may be required.",
                            ToolTipIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                // CRITICAL: Never break application startup
                // Log error and continue
                _logger.Error(ex, "Recovery check failed - continuing application startup");

                // Don't notify user - this is internal error
                // Application continues normally
            }
        }

        /// <summary>
        /// Pairing sonrası orphan transaction recovery
        /// Connection kurulduktan SONRA çalışır - bu sayede GetTicket yapabilir
        /// </summary>
        private void TryRecoveryAfterPairing()
        {
            try
            {
                Console.WriteLine("[RECOVERY-POST-PAIRING] ========================================");
                Console.WriteLine("[RECOVERY-POST-PAIRING] Starting orphan transaction recovery...");
                Console.WriteLine("[RECOVERY-POST-PAIRING] ========================================");

                _logger.Information("Post-Pairing: Checking for orphan transactions...");

                var recovery = new RecoveryCoordinator();

                // Sadece orphan check yapacak bir metod lazım - AttemptRecovery yerine
                // Direkt CheckOrphanOrders'a erişim yok (private), o yüzden AttemptRecovery kullanalım
                var result = recovery.AttemptRecovery();

                Console.WriteLine($"[RECOVERY-POST-PAIRING] Recovery completed: Action={result.RecoveryAction}");

                if (result.OrphanOrders != null && result.OrphanOrders.Count > 0)
                {
                    _logger.Information($"Post-Pairing Recovery: Processed {result.OrphanOrders.Count} orphan order(s)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RECOVERY-POST-PAIRING] ERROR: {ex.Message}");
                _logger.Error(ex, "Post-pairing recovery failed - continuing normally");
                // Don't break pairing flow
            }
        }

        /// <summary>
        /// OrderKey ile transaction durumunu sorgula
        /// GET /ingenico/OrderStatus/{orderKey}
        /// </summary>
        [HttpGet]
        [Route("OrderStatus/{orderKey}")]
        public IHttpActionResult GetOrderStatus(string orderKey)
        {
            try
            {
                _logger.Information($"API isteği alındı: GET /ingenico/OrderStatus/{orderKey}");

                if (string.IsNullOrWhiteSpace(orderKey))
                {
                    return BadRequest("OrderKey boş olamaz");
                }

                var baseFolder = System.Windows.Forms.Application.StartupPath + "\\CommandBackup";

                // Tüm klasörleri ve dosya tiplerini kontrol et
                var result = new OrderStatusResponse
                {
                    OrderKey = orderKey,
                    CheckTime = DateTime.Now
                };

                // Tüm klasörlerde dosyaları ara (Location'ı en fazla dosya olan klasöre set et)
                var folders = new[]
                {
                    new { Name = "Waiting", Path = baseFolder + "\\Waiting" },
                    new { Name = "Completed", Path = baseFolder + "\\Completed" },
                    new { Name = "Cancel", Path = baseFolder + "\\Cancel" },
                    new { Name = "Exception", Path = baseFolder + "\\Exception" },
                    new { Name = "Return", Path = baseFolder + "\\Return" }
                };

                string primaryLocation = null;
                int maxFilesInFolder = 0;

                foreach (var folder in folders)
                {
                    int filesBeforeCheck = result.FilesFound.Count;
                    CheckFolderAndCollect(result, folder.Path, folder.Name, orderKey);
                    int filesAdded = result.FilesFound.Count - filesBeforeCheck;

                    // En fazla dosya olan klasörü primary location olarak belirle
                    if (filesAdded > maxFilesInFolder)
                    {
                        maxFilesInFolder = filesAdded;
                        primaryLocation = folder.Name;
                        result.FolderPath = folder.Path;
                    }
                }

                // Primary location'ı set et
                if (!string.IsNullOrEmpty(primaryLocation))
                {
                    result.Location = primaryLocation;
                }

                // Durum belirleme
                if (result.FilesFound.Count == 0)
                {
                    result.Status = "NotFound";
                    result.StatusDescription = "Bu OrderKey ile ilişkili hiçbir dosya bulunamadı";
                }
                else if (result.Location == "Waiting")
                {
                    result.Status = "InProgress";
                    result.StatusDescription = "Transaction devam ediyor veya tamamlanmamış";
                    // Waiting'de TicketInfo yükleme - henüz tamamlanmamış
                }
                else if (result.Location == "Completed")
                {
                    result.Status = "Completed";
                    result.StatusDescription = "Transaction başarıyla tamamlanmış";

                    // SADECE Completed ise TicketInfo'yu yükle
                    LoadTicketInfoFromFiles(result, orderKey);
                }
                else if (result.Location == "Cancel")
                {
                    result.Status = "Cancelled";
                    result.StatusDescription = "Transaction iptal edilmiş";
                    // Cancel durumunda TicketInfo yok
                }
                else if (result.Location == "Exception")
                {
                    result.Status = "Exception";
                    result.StatusDescription = "Transaction sırasında hata oluşmuş";
                    // Exception durumunda TicketInfo yok
                }
                else if (result.Location == "Return")
                {
                    result.Status = "Returned";
                    result.StatusDescription = "İade işlemi";
                    // Return durumunda TicketInfo olabilir ama şimdilik eklenmedi
                }

                _logger.Information($"OrderStatus sonuç: OrderKey={orderKey}, Status={result.Status}, Location={result.Location}, Files={result.FilesFound.Count}");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"OrderStatus hatası: OrderKey={orderKey}, Error={ex.Message}");
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Klasördeki dosyaları kontrol et ve topla (tüm klasörlerden dosyaları toplar)
        /// </summary>
        private void CheckFolderAndCollect(OrderStatusResponse result, string folderPath, string folderName, string orderKey)
        {
            try
            {
                if (!Directory.Exists(folderPath))
                {
                    return;
                }

                // Pattern'ler: {orderKey}.txt, {orderKey}_Fiscal.txt, {orderKey}_Data.txt
                var patterns = new[] {
                    $"{orderKey}.txt",
                    $"{orderKey}_Fiscal.txt",
                    $"{orderKey}_Data.txt"
                };

                foreach (var pattern in patterns)
                {
                    var filePath = Path.Combine(folderPath, pattern);
                    if (File.Exists(filePath))
                    {
                        // Aynı dosya adı başka klasörde zaten eklenmişse ekleme
                        if (result.FilesFound.Any(f => f.FileName == pattern))
                        {
                            _logger.Information($"Dosya zaten listeye eklenmiş, atlıyorum: {pattern}");
                            continue;
                        }

                        var fileInfo = new FileInfo(filePath);
                        var fileDetail = new FileDetail
                        {
                            FileName = pattern,
                            FilePath = filePath,
                            FileSize = fileInfo.Length,
                            LastModified = fileInfo.LastWriteTime,
                            FileType = GetFileType(pattern),
                            FolderLocation = folderName  // Hangi klasörden geldiğini belirt
                        };

                        result.FilesFound.Add(fileDetail);
                        _logger.Information($"Dosya bulundu: {pattern} -> {folderName}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"CheckFolderAndCollect hatası: Folder={folderName}, Error={ex.Message}");
            }
        }

        /// <summary>
        /// Dosya tipini belirle
        /// </summary>
        private string GetFileType(string fileName)
        {
            if (fileName.EndsWith("_Fiscal.txt"))
                return "Fiscal (Order Data)";
            else if (fileName.EndsWith("_Data.txt"))
                return "Data (Transaction Result)";
            else if (fileName.EndsWith(".txt"))
                return "Commands (GMP Log)";
            else
                return "Unknown";
        }

        /// <summary>
        /// FilesFound listesinden _Data.txt veya .txt dosyasını bulup TicketInfo'yu yükle
        /// </summary>
        private void LoadTicketInfoFromFiles(OrderStatusResponse result, string orderKey)
        {
            try
            {
                // Önce _Data.txt dosyasını dene
                var dataFile = result.FilesFound.FirstOrDefault(f => f.FileName.EndsWith("_Data.txt"));

                if (dataFile != null && File.Exists(dataFile.FilePath))
                {
                    _logger.Information($"_Data.txt dosyası bulundu: {dataFile.FilePath}");
                    LoadTicketInfoFromDataFile(result, dataFile.FilePath, orderKey);
                    return;
                }

                _logger.Warning($"_Data.txt dosyası bulunamadı, .txt (Commands) dosyasından TicketInfo çıkarmaya çalışılıyor: OrderKey={orderKey}");

                // _Data.txt yoksa .txt (Commands) dosyasından dene
                var commandsFile = result.FilesFound.FirstOrDefault(f => f.FileName == $"{orderKey}.txt");

                if (commandsFile != null && File.Exists(commandsFile.FilePath))
                {
                    _logger.Information($"Commands dosyası bulundu: {commandsFile.FilePath}");
                    LoadTicketInfoFromCommandsFile(result, commandsFile.FilePath, orderKey);
                    return;
                }

                _logger.Warning($"TicketInfo yüklenemedi: Ne _Data.txt ne de .txt dosyası bulunamadı: OrderKey={orderKey}");
            }
            catch (Exception ex)
            {
                _logger.Warning($"LoadTicketInfoFromFiles hatası: OrderKey={orderKey}, Error={ex.Message}");
                // Hata olsa bile response'u bozmayalım, sadece TicketInfo null kalır
            }
        }

        /// <summary>
        /// _Data.txt dosyasından TicketInfo yükle
        /// </summary>
        private void LoadTicketInfoFromDataFile(OrderStatusResponse result, string filePath, string orderKey)
        {
            try
            {
                // Dosya çok büyük olabilir, sadece son satırı oku
                var lastLine = File.ReadLines(filePath).LastOrDefault();

                if (string.IsNullOrWhiteSpace(lastLine))
                {
                    _logger.Warning($"_Data.txt dosyası boş: {filePath}");
                    return;
                }

                // JSON parse et
                var dataObject = Newtonsoft.Json.JsonConvert.DeserializeObject<GmpPrintReceiptDto>(lastLine);

                if (dataObject?.TicketInfo != null)
                {
                    result.TicketInfo = dataObject.TicketInfo;
                    _logger.Information($"TicketInfo loaded from _Data.txt: FNo={dataObject.TicketInfo.FNo}, ZNo={dataObject.TicketInfo.ZNo}, TotalAmount={dataObject.TicketInfo.TotalReceiptAmount}");
                }
                else
                {
                    _logger.Warning($"TicketInfo null veya parse edilemedi: OrderKey={orderKey}");
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"LoadTicketInfoFromDataFile hatası: OrderKey={orderKey}, Error={ex.Message}");
            }
        }

        /// <summary>
        /// .txt (Commands) dosyasından TicketInfo çıkar
        /// Herhangi bir komutta FNo > 0 olan TicketInfo'yu bul
        /// </summary>
        private void LoadTicketInfoFromCommandsFile(OrderStatusResponse result, string filePath, string orderKey)
        {
            try
            {
                // Tüm satırları oku (tersten başla, en son valid TicketInfo'yu bul)
                var lines = File.ReadAllLines(filePath);

                _logger.Information($"Commands dosyası okunuyor: {lines.Length} satır bulundu");

                // Son satırdan başa doğru ara - HERHANGİ BİR KOMUTTA FNo > 0 olan TicketInfo'yu bul
                for (int i = lines.Length - 1; i >= 0; i--)
                {
                    var line = lines[i];
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    try
                    {
                        var command = Newtonsoft.Json.JsonConvert.DeserializeObject<GmpCommand>(line);

                        // HERHANGİ BİR KOMUTTA TicketInfo var mı kontrol et
                        if (command != null &&
                            command.printDetail?.TicketInfo != null &&
                            command.printDetail.TicketInfo.FNo > 0)
                        {
                            result.TicketInfo = command.printDetail.TicketInfo;
                            _logger.Information($"TicketInfo loaded from Commands file: FNo={command.printDetail.TicketInfo.FNo}, ZNo={command.printDetail.TicketInfo.ZNo}, Command={command.Command}");
                            return;
                        }
                    }
                    catch
                    {
                        // JSON parse hatası, devam et
                        continue;
                    }
                }

                _logger.Warning($"Commands dosyasında valid TicketInfo (FNo > 0) bulunamadı: OrderKey={orderKey}");
            }
            catch (Exception ex)
            {
                _logger.Warning($"LoadTicketInfoFromCommandsFile hatası: OrderKey={orderKey}, Error={ex.Message}");
            }
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

    /// <summary>
    /// OrderStatus response model
    /// </summary>
    public class OrderStatusResponse
    {
        public string OrderKey { get; set; }
        public string Status { get; set; }
        public string StatusDescription { get; set; }
        public string Location { get; set; }
        public string FolderPath { get; set; }
        public List<FileDetail> FilesFound { get; set; } = new List<FileDetail>();
        public DateTime CheckTime { get; set; }

        /// <summary>
        /// Transaction ticket bilgileri (Completed ise dolu)
        /// </summary>
        public ST_TICKET TicketInfo { get; set; }
    }

    /// <summary>
    /// Dosya detay bilgisi
    /// </summary>
    public class FileDetail
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public DateTime LastModified { get; set; }
        public string FileType { get; set; }
        public string FolderLocation { get; set; }  // Hangi klasörde bulundu
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