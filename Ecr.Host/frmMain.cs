using Microsoft.Owin.Hosting;
using Ecr.Module;
using Ecr.Module.Controllers;
using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ecr.Module.Services.Ingenico.Models;
using System.Runtime.InteropServices;

namespace Ecr.Host
{
    public partial class frmMain : Form
    {
        private IDisposable _webApp;
        private bool _apiRunning = false;
        private NotifyIcon _trayIcon;
        private ContextMenuStrip _trayMenu;
        private System.Threading.Timer _connectionCheckTimer;
        private const string BaseAddress = "http://localhost:9000/";
        private const int ConnectionCheckInterval = 10000; // 10 saniye

        public frmMain()
        {
            InitializeComponent();
            InitializeTrayIcon();
            this.FormClosing += Form1_FormClosing;
            this.Resize += FrmMain_Resize;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Toggle();
            StartConnectionCheck();
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            _trayIcon.Visible = true;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Çarpı tuşuna basıldığında uygulamayı kapatmak yerine gizle
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
                _trayIcon.Visible = true;
            }
            else
            {
                StopConnectionCheck();
                StopApiServer();
                _trayIcon.Dispose();
            }
        }

        private void StartApiServer()
        {
            try
            {
                _webApp = WebApp.Start<Startup>(url: BaseAddress);
                Program.Logger.Information("API sunucusu başlatıldı: {BaseAddress}", BaseAddress);
            }
            catch (Exception ex)
            {
                Program.Logger.Error(ex, "API sunucusu başlatılamadı: {Message}", ex.Message);
            }
        }

        private void StopApiServer()
        {
            if (_webApp != null)
            {
                _webApp.Dispose();
                _webApp = null;

                Program.Logger.Information("API sunucusu durduruldu.");
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
                Icon = this.Icon,
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
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            this.Activate();
        }

        private void OnTrayStartApiClick(object sender, EventArgs e)
        {
            if (!_apiRunning)
            {
                Toggle();
            }
        }

        private void OnTrayStopApiClick(object sender, EventArgs e)
        {
            if (_apiRunning)
            {
                Toggle();
            }
        }

        private void OnTrayExitClick(object sender, EventArgs e)
        {
            // Gerçekten çıkış yapılacak
            StopConnectionCheck();
            StopApiServer();
            _trayIcon.Visible = false;
            Application.Exit();
        }
        
        private void OnShowNotificationHistoryClick(object sender, EventArgs e)
        {
            // Ana formu göster
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            this.Activate();
        }
        
      
        private void FrmMain_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.ShowInTaskbar = false;
                _trayIcon.Visible = true;
            }
        }

    
        #endregion

        #region Bağlantı Kontrolü

        private void StartConnectionCheck()
        {
            _connectionCheckTimer = new System.Threading.Timer(CheckConnection, null, 0, ConnectionCheckInterval);
            Program.Logger.Information("Bağlantı kontrolü başlatıldı. Kontrol aralığı: {Interval}ms", ConnectionCheckInterval);
        }

        private void StopConnectionCheck()
        {
            if (_connectionCheckTimer != null)
            {
                _connectionCheckTimer.Dispose();
                _connectionCheckTimer = null;
                Program.Logger.Information("Bağlantı kontrolü durduruldu.");
            }
        }

        private void CheckConnection(object state)
        {
            if (!_apiRunning) return;

            try
            {
                // UI thread'de çalıştır
                this.BeginInvoke(new Action(() =>
                {
                    bool isConnected = IsPortOpen("localhost", 9000);

                    if (!isConnected && _apiRunning)
                    {
                        Program.Logger.Warning("API bağlantısı kesildi. Yeniden başlatılıyor...");
                        
                        // API'yi yeniden başlat
                        StopApiServer();
                        _apiRunning = false;
                        StartApiServer();
                        if (_webApp != null)
                        {
                            _apiRunning = true;
                            UpdateTrayMenuStatus(true);
                            btnToggleApi.Text = "API'yi Durdur";
                        }
                    }
                }));
            }
            catch (Exception ex)
            {
                Program.Logger.Error(ex, "Bağlantı kontrolü sırasında hata: {Message}", ex.Message);
            }
        }

        private bool IsPortOpen(string host, int port)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var result = client.BeginConnect(host, port, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));
                    client.EndConnect(result);
                    return success;
                }
            }
            catch
            {
                return false;
            }
        }

        #endregion

        private void button1_Click(object sender, EventArgs e)
        {
            // Gerçekten çıkış yapılacak
            StopConnectionCheck();
            StopApiServer();
            _trayIcon.Visible = false;
            Application.Exit();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Uygulamayı sistem tepsisine küçült
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            _trayIcon.Visible = true;
        }

        
    }

}
