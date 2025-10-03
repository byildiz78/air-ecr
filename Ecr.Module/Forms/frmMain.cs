using Ecr.Module.Statics;
using Microsoft.Owin.Hosting;
using Serilog;
using System;
using System.Configuration;
using System.Windows.Forms;

namespace Ecr.Module.Forms
{
    public partial class frmMain : Form
    {
        private IDisposable _webApp;
        private bool _apiRunning = false;
        private NotifyIcon _trayIcon;
        private ContextMenuStrip _trayMenu;
        private string _baseAddress = "http://+:{0}/"; // + allows both localhost and network access
        private int _port;
        private ILogger _logger;

        // Singleton instance for middleware access
        public static frmMain Instance { get; private set; }

        public frmMain()
        {
            Instance = this; // Set singleton instance

            _logger = AppStatics.GetLogger("EcrLog");
            var port = ConfigurationManager.AppSettings["Ecr.Port"];

            int.TryParse(port, out _port);
            if (_port == 0)
                _port = 9000;

            InitializeComponent();
            InitializeTrayIcon();
            InitializeComponentEvents();
            FormClosing += Form1_FormClosing;
            Resize += FrmMain_Resize;
        }

        private void InitializeComponentEvents()
        {
            // Wire up dashboard panel events
            dashboardPanel.ToggleApiClicked += (s, e) => Toggle();
            dashboardPanel.RestartClicked += (s, e) => ApplicationHelper.RestartApplication();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Toggle();
            // Normal açılsın - kullanıcı isterse minimize edebilir
            WindowState = FormWindowState.Normal;
            ShowInTaskbar = true;
            _trayIcon.Visible = true;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                WindowState = FormWindowState.Minimized;
                ShowInTaskbar = false;
                _trayIcon.Visible = true;
            }
            else
            {
                StopApiServer();
                _trayIcon.Dispose();
            }
        }

        private void StartApiServer()
        {
            try
            {
                _baseAddress = string.Format(_baseAddress, _port);
                _webApp = WebApp.Start<Startup>(url: _baseAddress);
                _logger.Information("API sunucusu başlatıldı: {BaseAddress}", _baseAddress);

                dashboardPanel.SetApiRunning(true);

                // Cihaz bilgilerini al ve dashboard'a göster
                LoadDeviceInfo();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "API sunucusu başlatılamadı: {Message}", ex.Message);
            }
        }

        private void StopApiServer()
        {
            if (_webApp != null)
            {
                _webApp.Dispose();
                _webApp = null;
                _logger.Information("API sunucusu durduruldu.");

                dashboardPanel.SetApiRunning(false);
            }
        }

        private void Toggle()
        {
            if (!_apiRunning)
            {
                StartApiServer();
                if (_webApp != null)
                {
                    _apiRunning = true;
                    UpdateTrayMenuStatus(true);
                }
            }
            else
            {
                StopApiServer();
                _apiRunning = false;
                UpdateTrayMenuStatus(false);
            }
        }

        #region Sistem Tepsisi (System Tray) İşlemleri

        private void InitializeTrayIcon()
        {
            _trayMenu = new ContextMenuStrip();
            _trayMenu.Items.Add("Göster", null, OnTrayOpenClick);
            _trayMenu.Items.Add("API'yi Başlat", null, OnTrayStartApiClick);
            _trayMenu.Items.Add("API'yi Durdur", null, OnTrayStopApiClick);
            _trayMenu.Items.Add("-");
            _trayMenu.Items.Add("Çıkış", null, OnTrayExitClick);

            _trayIcon = new NotifyIcon()
            {
                Icon = Icon,
                ContextMenuStrip = _trayMenu,
                Text = "ECR Host",
                Visible = true
            };

            _trayIcon.DoubleClick += OnTrayOpenClick;
        }

        private void UpdateTrayMenuStatus(bool isRunning)
        {
            _trayMenu.Items[1].Enabled = !isRunning;
            _trayMenu.Items[2].Enabled = isRunning;
        }

        private void OnTrayOpenClick(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            ShowInTaskbar = true;
            Activate();
        }

        private void OnTrayStartApiClick(object sender, EventArgs e)
        {
            if (!_apiRunning)
                Toggle();
        }

        private void OnTrayStopApiClick(object sender, EventArgs e)
        {
            if (_apiRunning)
                Toggle();
        }

        private void OnTrayExitClick(object sender, EventArgs e)
        {
            StopApiServer();
            _trayIcon.Visible = false;
            Application.Exit();
        }

        private void FrmMain_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                ShowInTaskbar = false;
                _trayIcon.Visible = true;
            }
        }

        #endregion

        #region Device Info Loading

        private async void LoadDeviceInfo()
        {
            try
            {
                await System.Threading.Tasks.Task.Run(() =>
                {
                    // Settings yükle
                    Services.Ingenico.Models.DataStore.gmpxml = System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, "GMP.XML");
                    Services.Ingenico.Models.DataStore.gmpini = System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, "GMP.ini");
                    Services.Ingenico.Settings.SettingsInfo.getIniValues();
                    Services.Ingenico.Settings.SettingsInfo.getGMPIniValues();
                    Services.Ingenico.Settings.SettingsInfo.setXmlValues();

                    // Pairing yaparak cihaz bilgilerini al
                    var pairing = new Services.Ingenico.Pairing.PairingGmpProviderV2();
                    var result = pairing.GmpPairing();

                    if (result.ReturnCode == Services.Ingenico.GmpIngenico.Defines.TRAN_RESULT_OK && result.GmpInfo != null)
                    {
                        // Bank list ve header bilgilerini ekle
                        var gmpBankList = new Services.Ingenico.BankList.BankList();
                        result.GmpInfo.BankInfoList = gmpBankList.GetBankList().Data;

                        var header = new Services.Ingenico.ReceiptHeader.Header();
                        result.GmpInfo.fiscalHeader = header.GmpGetReceiptHeader().Data;

                        // Echo bilgilerini al
                        Services.Ingenico.GmpIngenico.ST_ECHO stEcho = new Services.Ingenico.GmpIngenico.ST_ECHO();
                        result.ReturnCode = Services.Ingenico.GmpIngenico.Json_GMPSmartDLL.FP3_Echo(
                            result.GmpInfo.CurrentInterface,
                            ref stEcho,
                            Services.Ingenico.GmpIngenico.Defines.TIMEOUT_ECHO);
                        result.GmpInfo.ActiveCashier = stEcho.activeCashier.name;
                        result.GmpInfo.ActiveCashierNo = stEcho.activeCashier.index + 1;
                        result.GmpInfo.EcrStatus = (int)stEcho.status;
                        result.GmpInfo.ecrMode = stEcho.ecrMode;

                        // CRITICAL: Global state'i güncelle
                        Services.Ingenico.Models.DataStore.gmpResult = result;
                        Services.Ingenico.Models.DataStore.Connection = Services.Ingenico.Models.ConnectionStatus.Connected;

                        // Dashboard'a cihaz bilgilerini gönder
                        dashboardPanel.UpdateDeviceInfo(result.GmpInfo);
                        _logger.Information("Cihaz bilgileri dashboard'a yüklendi");

                        // Pairing başarılı - şimdi orphan transactions için recovery çalıştır
                        TryRecoveryAfterPairing();

                        // 7 günden eski dosyaları temizle
                        CleanOldOrderFiles();
                    }
                    else
                    {
                        Services.Ingenico.Models.DataStore.Connection = Services.Ingenico.Models.ConnectionStatus.NotConnected;
                        _logger.Warning("Cihaz bilgileri alınamadı: {Message}", result.ReturnCodeMessage);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Cihaz bilgileri yüklenirken hata: {Message}", ex.Message);
            }
        }

        private void CleanOldOrderFiles()
        {
            try
            {
                Console.WriteLine("[CLEANUP] ========================================");
                Console.WriteLine("[CLEANUP] Starting cleanup of old order files (>7 days)...");
                Console.WriteLine("[CLEANUP] ========================================");

                var baseFolder = System.Windows.Forms.Application.StartupPath + "\\CommandBackup";
                var folders = new[]
                {
                    baseFolder + "\\Waiting",
                    baseFolder + "\\Completed",
                    baseFolder + "\\Cancel",
                    baseFolder + "\\Exception",
                    baseFolder + "\\Return"
                };

                int totalDeleted = 0;
                var cutoffDate = DateTime.Now.AddDays(-7);

                foreach (var folder in folders)
                {
                    if (!System.IO.Directory.Exists(folder))
                        continue;

                    var files = System.IO.Directory.GetFiles(folder, "*.txt");
                    Console.WriteLine($"[CLEANUP] Checking folder: {System.IO.Path.GetFileName(folder)} - {files.Length} files");

                    foreach (var file in files)
                    {
                        try
                        {
                            var fileInfo = new System.IO.FileInfo(file);
                            if (fileInfo.CreationTime < cutoffDate)
                            {
                                Console.WriteLine($"[CLEANUP] Deleting old file: {fileInfo.Name} (Created: {fileInfo.CreationTime:yyyy-MM-dd HH:mm:ss})");
                                System.IO.File.Delete(file);
                                totalDeleted++;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[CLEANUP] Error deleting file {System.IO.Path.GetFileName(file)}: {ex.Message}");
                            _logger.Warning($"Eski dosya silinemedi: {System.IO.Path.GetFileName(file)} - {ex.Message}");
                        }
                    }
                }

                Console.WriteLine($"[CLEANUP] Cleanup completed. Total files deleted: {totalDeleted}");
                _logger.Information($"Eski order dosyaları temizlendi: {totalDeleted} dosya silindi (>7 gün)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLEANUP] ERROR: {ex.Message}");
                _logger.Error(ex, "Eski dosyaları temizlerken hata oluştu");
            }
        }

        private void TryRecoveryAfterPairing()
        {
            try
            {
                Console.WriteLine("[RECOVERY-POST-PAIRING] ========================================");
                Console.WriteLine("[RECOVERY-POST-PAIRING] Starting orphan transaction recovery...");
                Console.WriteLine("[RECOVERY-POST-PAIRING] ========================================");

                _logger.Information("Post-Pairing: Checking for orphan transactions...");

                var recovery = new Services.Ingenico.Recovery.RecoveryCoordinator();
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

        #endregion

        #region Public Methods for Request Logging

        public void LogRequest(string method, string path)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => requestLogViewer.AddRequest(method, path)));
            }
            else
            {
                requestLogViewer.AddRequest(method, path);
            }
        }

        public void LogResponse(int statusCode, string path, long durationMs, string body = "")
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() =>
                {
                    requestLogViewer.AddResponse(statusCode, path, durationMs, body);
                    dashboardPanel.IncrementRequestCount(statusCode >= 200 && statusCode < 300);
                }));
            }
            else
            {
                requestLogViewer.AddResponse(statusCode, path, durationMs, body);
                dashboardPanel.IncrementRequestCount(statusCode >= 200 && statusCode < 300);
            }
        }

        #endregion
    }
}
