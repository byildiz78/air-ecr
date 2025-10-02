using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Ecr.Module.Forms.Components
{
    /// <summary>
    /// Settings panel for GMP.INI configuration
    /// </summary>
    public class SettingsPanel : UserControl
    {
        private string _iniFilePath;

        // UI Components
        private Label _lblTitle;
        private GroupBox _grpConnectionType;
        private RadioButton _rbEthernet;
        private RadioButton _rbSerial;
        private GroupBox _grpEthernet;
        private GroupBox _grpSerial;
        private Button _btnSave;
        private Button _btnReload;

        // Ethernet fields
        private Label _lblIP;
        private TextBox _txtIP;
        private Label _lblPort;
        private TextBox _txtPort;

        // Serial port fields
        private Label _lblPortName;
        private ComboBox _txtPortName;
        private Label _lblBaudRate;
        private ComboBox _cmbBaudRate;
        private Label _lblByteSize;
        private ComboBox _cmbByteSize;
        private Label _lblParity;
        private ComboBox _cmbParity;
        private Label _lblStopBit;
        private ComboBox _cmbStopBit;

        public SettingsPanel()
        {
            // GMP.INI dosyası çalışan exe'nin bulunduğu klasörde olmalı
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _iniFilePath = Path.Combine(baseDir, "GMP.INI");

            InitializeComponents();
            StyleComponents();
            ArrangeComponents();
            LoadSettings();
        }

        private void InitializeComponents()
        {
            _lblTitle = new Label
            {
                Text = "GMP Bağlantı Ayarları",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true
            };

            // Connection type group
            _grpConnectionType = new GroupBox
            {
                Text = "Bağlantı Tipi",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 180, 180),
                Width = 500,
                Height = 80
            };

            _rbEthernet = new RadioButton
            {
                Text = "Ethernet (TCP/IP)",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.White,
                AutoSize = true,
                Checked = true
            };
            _rbEthernet.CheckedChanged += ConnectionTypeChanged;

            _rbSerial = new RadioButton
            {
                Text = "Seri Port (COM)",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.White,
                AutoSize = true
            };
            _rbSerial.CheckedChanged += ConnectionTypeChanged;

            // Ethernet group
            _grpEthernet = new GroupBox
            {
                Text = "Ethernet Ayarları",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 180, 180),
                Width = 500,
                Height = 150
            };

            _lblIP = new Label
            {
                Text = "IP Adresi:",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(200, 200, 200),
                AutoSize = true
            };

            _txtIP = new TextBox
            {
                Width = 200,
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            _lblPort = new Label
            {
                Text = "Port:",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(200, 200, 200),
                AutoSize = true
            };

            _txtPort = new TextBox
            {
                Width = 200,
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Serial group
            _grpSerial = new GroupBox
            {
                Text = "Seri Port Ayarları",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 180, 180),
                Width = 500,
                Height = 280,
                Visible = false
            };

            _lblPortName = new Label
            {
                Text = "COM Port:",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(200, 200, 200),
                AutoSize = true
            };

            _txtPortName = new ComboBox
            {
                Width = 200,
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            // COM1'den COM10'a kadar portları ekle
            for (int i = 1; i <= 10; i++)
            {
                _txtPortName.Items.Add($"COM{i}");
            }
            _txtPortName.SelectedIndex = 0; // Default COM1

            _lblBaudRate = new Label
            {
                Text = "Baud Rate:",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(200, 200, 200),
                AutoSize = true
            };

            _cmbBaudRate = new ComboBox
            {
                Width = 200,
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            _cmbBaudRate.Items.AddRange(new object[] { "9600", "19200", "38400", "57600", "115200" });

            _lblByteSize = new Label
            {
                Text = "Byte Size:",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(200, 200, 200),
                AutoSize = true
            };

            _cmbByteSize = new ComboBox
            {
                Width = 200,
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            _cmbByteSize.Items.AddRange(new object[] { "7", "8" });

            _lblParity = new Label
            {
                Text = "Parity:",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(200, 200, 200),
                AutoSize = true
            };

            _cmbParity = new ComboBox
            {
                Width = 200,
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            _cmbParity.Items.AddRange(new object[] { "0 (None)", "1 (Odd)", "2 (Even)" });

            _lblStopBit = new Label
            {
                Text = "Stop Bit:",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(200, 200, 200),
                AutoSize = true
            };

            _cmbStopBit = new ComboBox
            {
                Width = 200,
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            _cmbStopBit.Items.AddRange(new object[] { "0 (1 bit)", "1 (1.5 bits)", "2 (2 bits)" });

            // Buttons
            _btnSave = new Button
            {
                Text = "Kaydet",
                Width = 120,
                Height = 35,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnSave.FlatAppearance.BorderSize = 0;
            _btnSave.Click += BtnSave_Click;

            _btnReload = new Button
            {
                Text = "Yeniden Yükle",
                Width = 120,
                Height = 35,
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnReload.FlatAppearance.BorderSize = 0;
            _btnReload.Click += BtnReload_Click;
        }

        private void StyleComponents()
        {
            BackColor = Color.FromArgb(37, 37, 38);
            Padding = new Padding(15);
        }

        private void ArrangeComponents()
        {
            _lblTitle.Location = new Point(10, 10);

            // Connection type group
            _grpConnectionType.Location = new Point(20, 50);
            _rbEthernet.Location = new Point(20, 30);
            _rbSerial.Location = new Point(250, 30);

            _grpConnectionType.Controls.Add(_rbEthernet);
            _grpConnectionType.Controls.Add(_rbSerial);

            // Ethernet group
            _grpEthernet.Location = new Point(20, 150);
            _lblIP.Location = new Point(20, 40);
            _txtIP.Location = new Point(120, 37);
            _lblPort.Location = new Point(20, 80);
            _txtPort.Location = new Point(120, 77);

            _grpEthernet.Controls.AddRange(new Control[] { _lblIP, _txtIP, _lblPort, _txtPort });

            // Serial group
            _grpSerial.Location = new Point(20, 150);
            _lblPortName.Location = new Point(20, 40);
            _txtPortName.Location = new Point(120, 37);
            _lblBaudRate.Location = new Point(20, 80);
            _cmbBaudRate.Location = new Point(120, 77);
            _lblByteSize.Location = new Point(20, 120);
            _cmbByteSize.Location = new Point(120, 117);
            _lblParity.Location = new Point(20, 160);
            _cmbParity.Location = new Point(120, 157);
            _lblStopBit.Location = new Point(20, 200);
            _cmbStopBit.Location = new Point(120, 197);

            _grpSerial.Controls.AddRange(new Control[] {
                _lblPortName, _txtPortName,
                _lblBaudRate, _cmbBaudRate,
                _lblByteSize, _cmbByteSize,
                _lblParity, _cmbParity,
                _lblStopBit, _cmbStopBit
            });

            // Buttons
            _btnSave.Location = new Point(20, 460);
            _btnReload.Location = new Point(150, 460);

            Controls.AddRange(new Control[] {
                _lblTitle,
                _grpConnectionType,
                _grpEthernet,
                _grpSerial,
                _btnSave,
                _btnReload
            });
        }

        private void ConnectionTypeChanged(object sender, EventArgs e)
        {
            if (_rbEthernet.Checked)
            {
                _grpEthernet.Visible = true;
                _grpSerial.Visible = false;
            }
            else
            {
                _grpEthernet.Visible = false;
                _grpSerial.Visible = true;
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(_iniFilePath))
                {
                    MessageBox.Show($"GMP.INI dosyası bulunamadı:\n{_iniFilePath}", "Uyarı",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Read IsTcpConnection
                int isTcp = ReadIniInt("CONNECTION", "IsTcpConnection", 1);

                if (isTcp == 1)
                {
                    _rbEthernet.Checked = true;
                    _txtIP.Text = ReadIniString("CONNECTION", "IP", "192.168.2.108");
                    _txtPort.Text = ReadIniString("CONNECTION", "Port", "7500");
                }
                else
                {
                    _rbSerial.Checked = true;

                    // PortName'den COM numarasını çıkar: "\\.\5" -> "COM5"
                    string portName = ReadIniString("CONNECTION", "PortName", "\\\\.\\1");
                    string comNumber = portName.Replace("\\\\.\\", "").Trim();
                    string comPortDisplay = $"COM{comNumber}";

                    // ComboBox'ta varsa seç
                    int portIndex = _txtPortName.Items.IndexOf(comPortDisplay);
                    if (portIndex >= 0)
                        _txtPortName.SelectedIndex = portIndex;
                    else
                        _txtPortName.SelectedIndex = 0; // Default COM1

                    // BaudRate
                    string baudRate = ReadIniString("CONNECTION", "BaudRate", "115200").Trim();
                    int baudIndex = _cmbBaudRate.Items.IndexOf(baudRate);
                    if (baudIndex >= 0)
                        _cmbBaudRate.SelectedIndex = baudIndex;
                    else
                        _cmbBaudRate.SelectedIndex = 4; // Default 115200

                    // ByteSize
                    string byteSize = ReadIniString("CONNECTION", "ByteSize", "8").Trim();
                    int byteIndex = _cmbByteSize.Items.IndexOf(byteSize);
                    if (byteIndex >= 0)
                        _cmbByteSize.SelectedIndex = byteIndex;
                    else
                        _cmbByteSize.SelectedIndex = 1; // Default 8

                    // Parity
                    int parity = ReadIniInt("CONNECTION", "Parity", 0);
                    if (parity >= 0 && parity < _cmbParity.Items.Count)
                        _cmbParity.SelectedIndex = parity;
                    else
                        _cmbParity.SelectedIndex = 0;

                    // StopBit
                    int stopBit = ReadIniInt("CONNECTION", "StopBit", 0);
                    if (stopBit >= 0 && stopBit < _cmbStopBit.Items.Count)
                        _cmbStopBit.SelectedIndex = stopBit;
                    else
                        _cmbStopBit.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ayarlar yüklenirken hata:\n{ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (!File.Exists(_iniFilePath))
                {
                    MessageBox.Show($"GMP.INI dosyası bulunamadı:\n{_iniFilePath}", "Hata",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (_rbEthernet.Checked)
                {
                    // Ethernet settings
                    WriteIniString("CONNECTION", "IsTcpConnection", "1");
                    WriteIniString("CONNECTION", "IP", _txtIP.Text.Trim());
                    WriteIniString("CONNECTION", "Port", _txtPort.Text.Trim());
                }
                else
                {
                    // Serial settings
                    WriteIniString("CONNECTION", "IsTcpConnection", "0");

                    // "COM5" -> "\\.\5" formatına dönüştür
                    string comPort = _txtPortName.SelectedItem.ToString();
                    string portNumber = comPort.Replace("COM", "");
                    WriteIniString("CONNECTION", "PortName", $"\\\\.\\{portNumber}");

                    WriteIniString("CONNECTION", "BaudRate", _cmbBaudRate.SelectedItem.ToString());
                    WriteIniString("CONNECTION", "ByteSize", _cmbByteSize.SelectedItem.ToString());

                    // Extract numeric value from "0 (None)" format
                    string parityValue = _cmbParity.SelectedItem.ToString().Split(' ')[0];
                    WriteIniString("CONNECTION", "Parity", parityValue);
                    WriteIniString("CONNECTION", "fParity", parityValue);

                    string stopBitValue = _cmbStopBit.SelectedItem.ToString().Split(' ')[0];
                    WriteIniString("CONNECTION", "StopBit", stopBitValue);
                }

                var result = MessageBox.Show("Ayarlar başarıyla kaydedildi!\n\nDeğişikliklerin etkili olması için uygulama yeniden başlatılacak.\n\nDevam etmek istiyor musunuz?",
                    "Başarılı", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                {
                    Statics.ApplicationHelper.RestartApplication();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ayarlar kaydedilirken hata:\n{ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnReload_Click(object sender, EventArgs e)
        {
            LoadSettings();
            MessageBox.Show("Ayarlar yeniden yüklendi.", "Bilgi",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #region INI File Operations

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key, string defaultValue,
            StringBuilder retVal, int size, string filePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern long WritePrivateProfileString(string section, string key, string value, string filePath);

        private string ReadIniString(string section, string key, string defaultValue)
        {
            StringBuilder temp = new StringBuilder(255);
            GetPrivateProfileString(section, key, defaultValue, temp, 255, _iniFilePath);
            return temp.ToString().Trim();
        }

        private int ReadIniInt(string section, string key, int defaultValue)
        {
            string value = ReadIniString(section, key, defaultValue.ToString());
            int result;
            if (int.TryParse(value, out result))
                return result;
            return defaultValue;
        }

        private void WriteIniString(string section, string key, string value)
        {
            WritePrivateProfileString(section, key, value, _iniFilePath);
        }

        #endregion
    }
}
