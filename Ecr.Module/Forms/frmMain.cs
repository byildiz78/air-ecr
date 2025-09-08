using Ecr.Module.Statics;
using Microsoft.Owin.Hosting;
using Serilog;
using System;
using System.Configuration;
using System.Drawing;
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

        public frmMain()
        {
            _logger = AppStatics.GetLogger("EcrLog");
            var port = ConfigurationManager.AppSettings["Ecr.Port"];

            int.TryParse(port, out _port);
            if (_port == 0)
                _port = 9000;

            InitializeComponent();
            InitializeTrayIcon();
            FormClosing += Form1_FormClosing;
            Resize += FrmMain_Resize;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Toggle();
            WindowState = FormWindowState.Minimized;
            ShowInTaskbar = false;
            _trayIcon.Visible = true;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Çarpı tuşuna basıldığında uygulamayı kapatmak yerine gizle
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
            }
        }

        private void btnToggleApi_Click(object sender, EventArgs e)
        {
            Toggle();
        }

        private void Toggle()
        {
            if (!_apiRunning)
            {
                StartApiServer();
                if (_webApp != null) // Başarılıysa
                {
                    btnToggleApi.Text = "API'yi Durdur";
                    btnToggleApi.BackColor = Color.FromArgb(196, 43, 28); // Kırmızı
                    lblStatus.Text = "API Durumu: Çalışıyor";
                    lblStatus.ForeColor = Color.FromArgb(76, 175, 80); // Yeşil
                    _apiRunning = true;
                    UpdateTrayMenuStatus(true);
                }
            }
            else
            {
                StopApiServer();
                btnToggleApi.Text = "API'yi Başlat";
                btnToggleApi.BackColor = Color.FromArgb(0, 122, 204); // Mavi
                lblStatus.Text = "API Durumu: Durduruldu";
                lblStatus.ForeColor = Color.FromArgb(204, 204, 204); // Gri
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
            _trayMenu.Items.Add("-"); // Ayırıcı
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
            _trayMenu.Items[1].Enabled = !isRunning; // "API'yi Başlat" menüsü
            _trayMenu.Items[2].Enabled = isRunning;  // "API'yi Durdur" menüsü
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

        private void OnShowNotificationHistoryClick(object sender, EventArgs e)
        {
            // Ana formu göster
            WindowState = FormWindowState.Normal;
            ShowInTaskbar = true;
            Activate();
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


        private void btnClose_Click(object sender, EventArgs e)
        {
            StopApiServer();
            _trayIcon.Visible = false;
            Application.Exit();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Uygulamayı sistem tepsisine küçült
            WindowState = FormWindowState.Minimized;
            ShowInTaskbar = false;
            _trayIcon.Visible = true;
        }

        private void btnRestart_Click(object sender, EventArgs e)
        {
            ApplicationHelper.RestartApplication();
        }
    }
}