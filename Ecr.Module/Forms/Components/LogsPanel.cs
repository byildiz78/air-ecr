using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Linq;

namespace Ecr.Module.Forms.Components
{
    /// <summary>
    /// Real-time log viewer panel
    /// Displays Serilog output in real-time
    /// </summary>
    public class LogsPanel : UserControl
    {
        private RichTextBox _logTextBox;
        private ComboBox _cmbLogLevel;
        private Button _btnClear;
        private Button _btnOpenLogFolder;
        private Button _btnRefresh;
        private Label _titleLabel;
        private Label _lblAutoScroll;
        private CheckBox _chkAutoScroll;

        private FileSystemWatcher _logWatcher;
        private string _logFolderPath;

        public LogsPanel()
        {
            InitializeComponents();
            StyleComponents();
            ArrangeComponents();
            InitializeLogWatcher();
        }

        private void InitializeComponents()
        {
            _titleLabel = new Label
            {
                Text = "Sistem Logları",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true
            };

            _cmbLogLevel = new ComboBox
            {
                Width = 120,
                Height = 30,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F),
                FlatStyle = FlatStyle.Flat
            };
            _cmbLogLevel.Items.AddRange(new[] { "Tümü", "Information", "Warning", "Error" });
            _cmbLogLevel.SelectedIndex = 0;
            _cmbLogLevel.SelectedIndexChanged += CmbLogLevel_SelectedIndexChanged;

            _chkAutoScroll = new CheckBox
            {
                Checked = true,
                Width = 20,
                Height = 20,
                BackColor = Color.Transparent
            };

            _lblAutoScroll = new Label
            {
                Text = "Otomatik Kaydır",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(180, 180, 180),
                AutoSize = true
            };

            _btnClear = CreateButton("Temizle", Color.FromArgb(60, 60, 60));
            _btnClear.Click += BtnClear_Click;

            _btnRefresh = CreateButton("Yenile", Color.FromArgb(0, 122, 204));
            _btnRefresh.Click += BtnRefresh_Click;

            _btnOpenLogFolder = CreateButton("Klasörü Aç", Color.FromArgb(76, 175, 80));
            _btnOpenLogFolder.Click += BtnOpenLogFolder_Click;

            _logTextBox = new RichTextBox
            {
                ReadOnly = true,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.FromArgb(220, 220, 220),
                Font = new Font("Consolas", 8.5F),
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Both,
                Dock = DockStyle.Fill,
                WordWrap = false,
                DetectUrls = false
            };
        }

        private Button CreateButton(string text, Color bgColor)
        {
            var btn = new Button
            {
                Text = text,
                Width = 100,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = bgColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void StyleComponents()
        {
            BackColor = Color.FromArgb(37, 37, 38);
            Padding = new Padding(15);
        }

        private void ArrangeComponents()
        {
            // Header panel
            var headerPanel = new Panel
            {
                Height = 50,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(37, 37, 38)
            };

            _titleLabel.Location = new Point(0, 10);
            _cmbLogLevel.Location = new Point(Width - 550, 10);
            _chkAutoScroll.Location = new Point(Width - 410, 13);
            _lblAutoScroll.Location = new Point(Width - 385, 13);
            _btnRefresh.Location = new Point(Width - 220, 10);
            _btnOpenLogFolder.Location = new Point(Width - 330, 10);
            _btnClear.Location = new Point(Width - 110, 10);

            headerPanel.Controls.AddRange(new Control[]
            {
                _titleLabel, _cmbLogLevel, _chkAutoScroll, _lblAutoScroll,
                _btnRefresh, _btnOpenLogFolder, _btnClear
            });

            Controls.Add(headerPanel);
            Controls.Add(_logTextBox);
        }

        private void InitializeLogWatcher()
        {
            try
            {
                // Önce EcrLog klasörünü dene
                _logFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EcrLog");

                // EcrLog yoksa Logs klasörünü dene
                if (!Directory.Exists(_logFolderPath))
                {
                    _logFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                }

                if (!Directory.Exists(_logFolderPath))
                {
                    AppendLog($"[INFO] Log klasörü bulunamadı: {_logFolderPath}", Color.Yellow);
                    return;
                }

                AppendLog($"[INFO] Log klasörü: {_logFolderPath}", Color.Cyan);

                _logWatcher = new FileSystemWatcher(_logFolderPath)
                {
                    Filter = "*.txt", // log_module_*.txt dosyaları
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
                };

                _logWatcher.Changed += LogWatcher_Changed;
                _logWatcher.EnableRaisingEvents = true;

                LoadLatestLogs();
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] Log izleyici başlatılamadı: {ex.Message}", Color.Red);
            }
        }

        private void LogWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => LoadLatestLogs()));
            }
            else
            {
                LoadLatestLogs();
            }
        }

        private void LoadLatestLogs()
        {
            try
            {
                if (!Directory.Exists(_logFolderPath))
                {
                    AppendLog($"[ERROR] Log klasörü bulunamadı: {_logFolderPath}", Color.Red);
                    return;
                }

                // Bugünün log dosyasını bul: log_module_20251002.txt
                var today = DateTime.Now.ToString("yyyyMMdd");
                var todayLogFile = Path.Combine(_logFolderPath, $"log_module_{today}.txt");

                if (!File.Exists(todayLogFile))
                {
                    // Bugünün dosyası yoksa en son dosyayı bul
                    var allLogFiles = Directory.GetFiles(_logFolderPath, "log_module_*.txt");
                    AppendLog($"[DEBUG] Bulunan log dosyası sayısı: {allLogFiles.Length}", Color.Cyan);

                    var latestLog = allLogFiles
                        .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                        .FirstOrDefault();

                    if (latestLog == null)
                    {
                        AppendLog($"[INFO] log_module_*.txt dosyası bulunamadı: {_logFolderPath}", Color.Yellow);
                        return;
                    }

                    todayLogFile = latestLog;
                    AppendLog($"[INFO] Log dosyası: {Path.GetFileName(todayLogFile)}", Color.Cyan);
                }

                // Dosyayı KİTLEMEDEN oku (FileShare.ReadWrite)
                var lines = ReadFileWithoutLocking(todayLogFile);

                if (lines.Count == 0)
                {
                    _logTextBox.Clear();
                    AppendLog($"[INFO] Log dosyası boş: {Path.GetFileName(todayLogFile)}", Color.Yellow);
                    return;
                }

                // Son 200 satırı göster
                var lastLines = lines.Skip(Math.Max(0, lines.Count - 200)).ToList();

                _logTextBox.Clear();
                foreach (var line in lastLines)
                {
                    AppendLogLine(line);
                }

                if (_chkAutoScroll.Checked)
                {
                    _logTextBox.SelectionStart = _logTextBox.Text.Length;
                    _logTextBox.ScrollToCaret();
                }
            }
            catch (IOException)
            {
                // File in use, skip this update
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] Log okunamadı: {ex.Message}", Color.Red);
            }
        }

        private List<string> ReadFileWithoutLocking(string filePath)
        {
            var lines = new List<string>();

            // FileShare.ReadWrite ile dosyayı kitlemeden aç
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var streamReader = new StreamReader(fileStream))
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }

            return lines;
        }

        private void AppendLogLine(string line)
        {
            Color color = Color.FromArgb(220, 220, 220);

            if (line.Contains("[INF]") || line.Contains("[Information]"))
                color = Color.FromArgb(100, 200, 255); // Blue
            else if (line.Contains("[WRN]") || line.Contains("[Warning]"))
                color = Color.FromArgb(255, 200, 100); // Orange
            else if (line.Contains("[ERR]") || line.Contains("[Error]"))
                color = Color.FromArgb(255, 100, 100); // Red

            // Apply filter
            var filter = _cmbLogLevel.SelectedItem?.ToString();
            if (filter != "Tümü")
            {
                if (!line.Contains($"[{filter}]") && !line.Contains($"[{filter.Substring(0, 3).ToUpper()}]"))
                    return;
            }

            AppendLog(line + Environment.NewLine, color);
        }

        private void AppendLog(string text, Color color)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => AppendLog(text, color)));
                return;
            }

            // WordWrap'i zorla false tut
            _logTextBox.WordWrap = false;

            _logTextBox.SelectionStart = _logTextBox.TextLength;
            _logTextBox.SelectionColor = color;
            _logTextBox.AppendText(text);

            // Rengi sıfırla
            _logTextBox.SelectionColor = _logTextBox.ForeColor;
        }

        private void CmbLogLevel_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadLatestLogs();
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            _logTextBox.Clear();
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            LoadLatestLogs();
        }

        private void BtnOpenLogFolder_Click(object sender, EventArgs e)
        {
            try
            {
                if (Directory.Exists(_logFolderPath))
                {
                    System.Diagnostics.Process.Start("explorer.exe", _logFolderPath);
                }
                else
                {
                    MessageBox.Show("Log klasörü bulunamadı!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Klasör açılamadı: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _logWatcher?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
