using System;
using System.Drawing;
using System.Windows.Forms;

namespace Ecr.Module.Forms.Components
{
    /// <summary>
    /// Dashboard panel showing API status and statistics
    /// </summary>
    public class DashboardPanel : UserControl
    {
        private Panel _statusCard;
        private Panel _statsCard;
        private Panel _deviceInfoCard;
        private Label _lblApiStatus;
        private Label _lblUptime;
        private Panel _lblTotalRequests;
        private Panel _lblSuccessRate;
        private Panel _lblErrorCount;
        private Button _btnToggleApi;
        private Button _btnRestart;
        private RichTextBox _txtDeviceInfo;

        private DateTime _startTime;
        private Timer _uptimeTimer;
        private int _totalRequests = 0;
        private int _successfulRequests = 0;
        private int _failedRequests = 0;

        public event EventHandler ToggleApiClicked;
        public event EventHandler RestartClicked;

        public DashboardPanel()
        {
            InitializeComponents();
            StyleComponents();
            ArrangeComponents();
            StartUptimeTimer();
            UpdateStats(); // Initialize stats display
        }

        private void InitializeComponents()
        {
            _statusCard = CreateModernCard("🔌 API Durumu", 230);
            _statsCard = CreateModernCard("📊 İstatistikler", 265);  // 55 (başlık) + 30 (boşluk) + 180 (kart yüksekliği)
            _deviceInfoCard = CreateModernCard("💳 Cihaz Bilgileri", 320);

            _lblApiStatus = CreateLabel("Durduruldu", new Font("Segoe UI", 16F, FontStyle.Bold), Color.FromArgb(204, 204, 204));
            _lblUptime = CreateLabel("Çalışma Süresi: 00:00:00", new Font("Segoe UI", 10F), Color.FromArgb(180, 180, 180));

            _lblTotalRequests = CreateModernStatCard("📥", "Toplam İstek", "0", Color.FromArgb(33, 150, 243));
            _lblSuccessRate = CreateModernStatCard("✓", "Başarı Oranı", "0%", Color.FromArgb(76, 175, 80));
            _lblErrorCount = CreateModernStatCard("✗", "Hata Sayısı", "0", Color.FromArgb(244, 67, 54));

            _txtDeviceInfo = new RichTextBox
            {
                ReadOnly = true,
                BackColor = Color.FromArgb(37, 37, 38),
                ForeColor = Color.FromArgb(220, 220, 220),
                Font = new Font("Segoe UI", 9.5F),
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Text = "Cihaz bilgisi bekleniyor...",
                WordWrap = true
            };

            _btnToggleApi = CreateModernButton("🚀 API'yi Başlat", Color.FromArgb(0, 122, 204), Color.FromArgb(0, 150, 255));
            _btnToggleApi.Click += (s, e) => ToggleApiClicked?.Invoke(this, EventArgs.Empty);

            _btnRestart = CreateModernButton("🔄 Yeniden Başlat", Color.FromArgb(196, 43, 28), Color.FromArgb(220, 60, 40));
            _btnRestart.Click += (s, e) => RestartClicked?.Invoke(this, EventArgs.Empty);
        }

        private Panel CreateCard(string title, int height)
        {
            var card = new Panel
            {
                BackColor = Color.FromArgb(45, 45, 48),
                Height = height,
                Padding = new Padding(20)
            };

            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 20)
            };

            card.Controls.Add(titleLabel);
            return card;
        }

        private Panel CreateModernCard(string title, int height)
        {
            var card = new Panel
            {
                BackColor = Color.FromArgb(45, 45, 48),
                Height = height,
                Padding = new Padding(20)
            };

            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 15)
            };

            // Gradient effect simulation with border
            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(70, 70, 75), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                }
            };

            card.Controls.Add(titleLabel);
            return card;
        }

        private Label CreateLabel(string text, Font font, Color color)
        {
            return new Label
            {
                Text = text,
                Font = font,
                ForeColor = color,
                AutoSize = true
            };
        }

        private Panel CreateStatLabel(string label, string value)
        {
            var panel = new Panel
            {
                Width = 350,
                Height = 80,
                BackColor = Color.FromArgb(60, 60, 63),
                Padding = new Padding(15)
            };

            var lblTitle = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(15, 15),
                AutoSize = true
            };

            var lblValue = new Label
            {
                Name = "value",
                Text = value,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 35),
                AutoSize = true
            };

            panel.Controls.Add(lblTitle);
            panel.Controls.Add(lblValue);
            return panel;
        }

        private Panel CreateModernStatCard(string icon, string label, string value, Color accentColor)
        {
            var containerPanel = new Panel
            {
                Width = 450,
                Height = 180,  // Başlık için +24px
                BackColor = Color.Transparent
            };

            // Title label - kartın üstünde
            var lblTitle = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 11F),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(0, 0),
                Size = new Size(450, 24),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Kart panel
            var panel = new Panel
            {
                Width = 450,
                Height = 120,
                Location = new Point(0, 30),
                BackColor = Color.FromArgb(37, 37, 38),
                Padding = new Padding(20)
            };

            // Value label - kartın içinde ortada
            var lblValue = new Label
            {
                Name = "value",
                Text = value,
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                ForeColor = accentColor,
                Location = new Point(0, 0),
                Size = new Size(450, 120),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Border effect
            panel.Paint += (s, e) =>
            {
                using (var pen = new Pen(accentColor, 3))
                {
                    e.Graphics.DrawLine(pen, 0, 0, 0, panel.Height);
                }
            };

            panel.Controls.Add(lblValue);
            containerPanel.Controls.Add(lblTitle);
            containerPanel.Controls.Add(panel);
            return containerPanel;
        }

        private Button CreateButton(string text, Color bgColor)
        {
            var btn = new Button
            {
                Text = text,
                Width = 150,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                BackColor = bgColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private Button CreateModernButton(string text, Color bgColor, Color hoverColor)
        {
            var btn = new Button
            {
                Text = text,
                Width = 230,
                Height = 45,
                FlatStyle = FlatStyle.Flat,
                BackColor = bgColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;

            // Hover effect
            btn.MouseEnter += (s, e) => btn.BackColor = hoverColor;
            btn.MouseLeave += (s, e) => btn.BackColor = bgColor;

            return btn;
        }

        private void StyleComponents()
        {
            BackColor = Color.FromArgb(37, 37, 38);
            Padding = new Padding(20);
            AutoScroll = true;
        }

        private void ArrangeComponents()
        {
            // Status card layout
            _lblApiStatus.Location = new Point(20, 55);
            _lblUptime.Location = new Point(20, 135);

            // Butonları sağa yasla - form genişliği 1600, panel genişliği ~1560
            _btnRestart.Location = new Point(1310, 125);  // Sağdan 20px boşluk
            _btnToggleApi.Location = new Point(1070, 125);  // Restart'tan 10px önce (1310 - 230 - 10)

            _statusCard.Controls.AddRange(new Control[] { _lblApiStatus, _lblUptime, _btnToggleApi, _btnRestart });
            _statusCard.Dock = DockStyle.Top;

            // Stats card layout - yan yana 3 card (450px genişlik + 50px boşluk)
            _lblTotalRequests.Location = new Point(20, 55);
            _lblSuccessRate.Location = new Point(520, 55);   // 20 + 450 + 50
            _lblErrorCount.Location = new Point(1020, 55);   // 520 + 450 + 50

            _statsCard.Controls.AddRange(new Control[] { _lblTotalRequests, _lblSuccessRate, _lblErrorCount });
            _statsCard.Dock = DockStyle.Top;

            // Device info card layout
            _txtDeviceInfo.Location = new Point(20, 55);
            _txtDeviceInfo.Size = new Size(1540, 240);
            _deviceInfoCard.Controls.Add(_txtDeviceInfo);
            _deviceInfoCard.Dock = DockStyle.Top;

            Controls.Add(_deviceInfoCard);
            Controls.Add(_statsCard);
            Controls.Add(_statusCard);
        }

        private void StartUptimeTimer()
        {
            _uptimeTimer = new Timer { Interval = 1000 };
            _uptimeTimer.Tick += (s, e) => UpdateUptime();
        }

        private void UpdateUptime()
        {
            var uptime = DateTime.Now - _startTime;
            _lblUptime.Text = $"Çalışma Süresi: {uptime:hh\\:mm\\:ss}";
        }

        public void SetApiRunning(bool running)
        {
            if (running)
            {
                _lblApiStatus.Text = "✓ Çalışıyor";
                _lblApiStatus.ForeColor = Color.FromArgb(76, 175, 80); // Green
                _btnToggleApi.Text = "⏸ API'yi Durdur";
                _btnToggleApi.BackColor = Color.FromArgb(196, 43, 28); // Red
                _btnToggleApi.MouseEnter -= null;
                _btnToggleApi.MouseLeave -= null;
                _btnToggleApi.MouseEnter += (s, e) => _btnToggleApi.BackColor = Color.FromArgb(220, 60, 40);
                _btnToggleApi.MouseLeave += (s, e) => _btnToggleApi.BackColor = Color.FromArgb(196, 43, 28);
                _startTime = DateTime.Now;
                _uptimeTimer.Start();
            }
            else
            {
                _lblApiStatus.Text = "⏹ Durduruldu";
                _lblApiStatus.ForeColor = Color.FromArgb(204, 204, 204); // Gray
                _btnToggleApi.Text = "🚀 API'yi Başlat";
                _btnToggleApi.BackColor = Color.FromArgb(0, 122, 204); // Blue
                _btnToggleApi.MouseEnter -= null;
                _btnToggleApi.MouseLeave -= null;
                _btnToggleApi.MouseEnter += (s, e) => _btnToggleApi.BackColor = Color.FromArgb(0, 150, 255);
                _btnToggleApi.MouseLeave += (s, e) => _btnToggleApi.BackColor = Color.FromArgb(0, 122, 204);
                _uptimeTimer.Stop();
                _lblUptime.Text = "Çalışma Süresi: 00:00:00";
            }
        }

        public void IncrementRequestCount(bool success)
        {
            _totalRequests++;
            if (success)
                _successfulRequests++;
            else
                _failedRequests++;

            UpdateStats();
        }

        private void UpdateStats()
        {
            // Update total requests
            var totalValueLabels = _lblTotalRequests.Controls.Find("value", true);
            if (totalValueLabels.Length > 0 && totalValueLabels[0] is Label totalValueLabel)
            {
                totalValueLabel.Text = _totalRequests.ToString();
                totalValueLabel.ForeColor = Color.FromArgb(33, 150, 243); // Blue
            }

            // Update success rate
            var successRate = _totalRequests > 0 ? (_successfulRequests * 100.0 / _totalRequests) : 0;
            var successValueLabels = _lblSuccessRate.Controls.Find("value", true);
            if (successValueLabels.Length > 0 && successValueLabels[0] is Label successValueLabel)
            {
                successValueLabel.Text = $"{successRate:F1}%";
                successValueLabel.ForeColor = Color.FromArgb(76, 175, 80); // Green
            }

            // Update error count
            var errorValueLabels = _lblErrorCount.Controls.Find("value", true);
            if (errorValueLabels.Length > 0 && errorValueLabels[0] is Label errorValueLabel)
            {
                errorValueLabel.Text = _failedRequests.ToString();
                errorValueLabel.ForeColor = Color.FromArgb(244, 67, 54); // Red
            }
        }

        public void ResetStats()
        {
            _totalRequests = 0;
            _successfulRequests = 0;
            _failedRequests = 0;
            UpdateStats();
        }

        public void UpdateDeviceInfo(dynamic gmpInfo)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateDeviceInfo(gmpInfo)));
                return;
            }

            try
            {
                _txtDeviceInfo.Clear();

                // Kasiyer bilgileri
                AppendColoredText("👤 Kasiyer:         ", Color.FromArgb(100, 200, 255));
                AppendColoredText($"{gmpInfo.ActiveCashier} ", Color.FromArgb(255, 255, 255));
                AppendColoredText($"(#{gmpInfo.ActiveCashierNo})\n", Color.FromArgb(150, 150, 150));

                // Seri numarası
                AppendColoredText("🔢 Seri No:         ", Color.FromArgb(100, 200, 255));
                AppendColoredText($"{gmpInfo.EcrSerialNumber}\n", Color.FromArgb(255, 255, 255));

                // DLL Versiyonu
                AppendColoredText("⚙️  DLL Versiyonu:   ", Color.FromArgb(100, 200, 255));
                AppendColoredText($"{gmpInfo.Versions.m_dllVersion}\n", Color.FromArgb(255, 255, 255));

                // Vergi bilgileri
                if (gmpInfo.fiscalHeader != null)
                {
                    AppendColoredText("\n📋 Vergi Bilgileri\n", Color.FromArgb(255, 200, 100));

                    AppendColoredText("   • Vergi No:      ", Color.FromArgb(180, 180, 180));
                    AppendColoredText($"{gmpInfo.fiscalHeader.VATNumber}\n", Color.FromArgb(255, 255, 255));

                    AppendColoredText("   • Vergi Dairesi: ", Color.FromArgb(180, 180, 180));
                    AppendColoredText($"{gmpInfo.fiscalHeader.VATOffice}\n", Color.FromArgb(255, 255, 255));

                    AppendColoredText("   • Mersis No:     ", Color.FromArgb(180, 180, 180));
                    AppendColoredText($"{gmpInfo.fiscalHeader.MersisNo}\n", Color.FromArgb(255, 255, 255));
                }

                // Banka listesi
                if (gmpInfo.BankInfoList != null && gmpInfo.BankInfoList.Count > 0)
                {
                    AppendColoredText("\n💳 Tanımlı Bankalar\n", Color.FromArgb(76, 175, 80));
                    foreach (var bank in gmpInfo.BankInfoList)
                    {
                        AppendColoredText($"   ✓ {bank.Name}", Color.FromArgb(200, 200, 200));
                        AppendColoredText($" (BKM: {bank.u16BKMId})\n", Color.FromArgb(140, 140, 140));
                    }
                }
            }
            catch (Exception ex)
            {
                _txtDeviceInfo.Text = $"❌ Cihaz bilgisi yüklenemedi: {ex.Message}";
            }
        }

        private void AppendColoredText(string text, Color color)
        {
            _txtDeviceInfo.SelectionStart = _txtDeviceInfo.TextLength;
            _txtDeviceInfo.SelectionColor = color;
            _txtDeviceInfo.AppendText(text);
            _txtDeviceInfo.SelectionColor = _txtDeviceInfo.ForeColor;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _uptimeTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
