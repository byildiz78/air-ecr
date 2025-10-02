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
        private string _baseAddress = "http://localhost:{0}/";
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
                    // Pairing yaparak cihaz bilgilerini al
                    var pairing = new Services.Ingenico.Pairing.PairingGmpProviderV2();
                    var result = pairing.GmpPairing();

                    if (result.ReturnCode == Services.Ingenico.GmpIngenico.Defines.TRAN_RESULT_OK && result.GmpInfo != null)
                    {
                        // Dashboard'a cihaz bilgilerini gönder
                        dashboardPanel.UpdateDeviceInfo(result.GmpInfo);
                        _logger.Information("Cihaz bilgileri dashboard'a yüklendi");
                    }
                    else
                    {
                        _logger.Warning("Cihaz bilgileri alınamadı: {Message}", result.ReturnCodeMessage);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Cihaz bilgileri yüklenirken hata: {Message}", ex.Message);
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
