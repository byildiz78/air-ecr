using System;
using System.Drawing;
using System.Windows.Forms;

namespace Ecr.Module.Forms.Components
{
    /// <summary>
    /// Modern request/response log viewer component
    /// Displays HTTP requests and responses in real-time
    /// </summary>
    public class RequestLogViewer : UserControl
    {
        private ListView _listView;
        private Label _titleLabel;
        private Button _btnClear;
        private Button _btnExport;
        private Button _btnSortByDuration;
        private Label _lblCount;

        private int _requestCount = 0;
        private int _responseCount = 0;

        public RequestLogViewer()
        {
            InitializeComponents();
            StyleComponents();
            ArrangeComponents();
        }

        private void InitializeComponents()
        {
            _titleLabel = new Label
            {
                Text = "İstek & Yanıt Geçmişi",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true
            };

            _lblCount = new Label
            {
                Text = "İstek: 0 | Yanıt: 0",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(180, 180, 180),
                AutoSize = true
            };

            _btnClear = new Button
            {
                Text = "Temizle",
                Width = 100,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F)
            };
            _btnClear.FlatAppearance.BorderSize = 0;
            _btnClear.Click += BtnClear_Click;

            _btnExport = new Button
            {
                Text = "Dışa Aktar",
                Width = 100,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F)
            };
            _btnExport.FlatAppearance.BorderSize = 0;
            _btnExport.Click += BtnExport_Click;

            _btnSortByDuration = new Button
            {
                Text = "Süre ↓",
                Width = 80,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            _btnSortByDuration.FlatAppearance.BorderSize = 0;
            _btnSortByDuration.Click += BtnSortByDuration_Click;

            _listView = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                Font = new Font("Consolas", 9F),
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 5, 0, 0) // Üstten 5px boşluk
            };

            // Kolonlar - genişlikler toplam ~1000px, Path geri kalanı alacak
            _listView.Columns.Add("Zaman", 130);
            _listView.Columns.Add("Yön", 70);
            _listView.Columns.Add("Method", 85);
            _listView.Columns.Add("Path", 600); // Ana içerik
            _listView.Columns.Add("Status", 80);
            _listView.Columns.Add("Süre (sn)", 85);

            _listView.ColumnClick += ListView_ColumnClick;

            // ListView boyutu değiştiğinde path kolonunu otomatik genişlet
            _listView.SizeChanged += (s, e) =>
            {
                if (_listView.Columns.Count > 0)
                {
                    // Sabit kolonların toplamı
                    int fixedWidth = 130 + 70 + 85 + 80 + 85; // 450px
                    // Path kolonu geri kalan alanı alsın
                    int availableWidth = _listView.ClientSize.Width - fixedWidth - 5; // 5px margin
                    if (availableWidth > 300)
                        _listView.Columns[3].Width = availableWidth;
                }
            };
        }

        private void StyleComponents()
        {
            BackColor = Color.FromArgb(37, 37, 38);
            Padding = new Padding(0);
        }

        private void ArrangeComponents()
        {
            // Header panel
            var headerPanel = new Panel
            {
                Height = 60,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(45, 45, 48),
                Padding = new Padding(15, 10, 15, 10)
            };

            _titleLabel.Location = new Point(15, 12);
            _lblCount.Location = new Point(200, 15);

            _btnSortByDuration.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _btnClear.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _btnExport.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            // Butonları sağ üste hizala
            headerPanel.SizeChanged += (s, e) =>
            {
                _btnExport.Location = new Point(headerPanel.Width - 115, 15);
                _btnClear.Location = new Point(headerPanel.Width - 225, 15);
                _btnSortByDuration.Location = new Point(headerPanel.Width - 315, 15);
            };

            headerPanel.Controls.AddRange(new Control[] { _titleLabel, _lblCount, _btnSortByDuration, _btnClear, _btnExport });

            Controls.Add(headerPanel);
            Controls.Add(_listView);
        }

        public void AddRequest(string method, string path, string timestamp = null)
        {
            _requestCount++;
            UpdateCountLabel();

            var item = new ListViewItem(timestamp ?? DateTime.Now.ToString("HH:mm:ss.fff"));
            item.SubItems.Add("→ REQ");
            item.SubItems.Add(method);
            item.SubItems.Add(path);
            item.SubItems.Add("-");
            item.SubItems.Add("-");
            item.ForeColor = Color.FromArgb(100, 200, 255); // Light blue
            item.Tag = new { Type = "Request", Method = method, Path = path, Timestamp = timestamp };

            _listView.Items.Add(item);
            _listView.EnsureVisible(_listView.Items.Count - 1);
        }

        public void AddResponse(int statusCode, string path, long durationMs, string body = "")
        {
            _responseCount++;
            UpdateCountLabel();

            var durationSec = durationMs / 1000.0;
            var item = new ListViewItem(DateTime.Now.ToString("HH:mm:ss.fff"));
            item.SubItems.Add("← RES");
            item.SubItems.Add("-");
            item.SubItems.Add(path);
            item.SubItems.Add(statusCode.ToString());
            item.SubItems.Add($"{durationSec:F2}s");

            // Color code based on status
            if (statusCode >= 200 && statusCode < 300)
                item.ForeColor = Color.FromArgb(100, 255, 100); // Green
            else if (statusCode >= 400)
                item.ForeColor = Color.FromArgb(255, 100, 100); // Red
            else
                item.ForeColor = Color.FromArgb(255, 200, 100); // Orange

            item.Tag = new { Type = "Response", StatusCode = statusCode, Path = path, DurationMs = durationMs, Body = body };

            _listView.Items.Add(item);
            _listView.EnsureVisible(_listView.Items.Count - 1);
        }

        private void ListView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Kolon sıralaması
            var sorter = _listView.ListViewItemSorter as ListViewColumnSorter;

            if (sorter == null)
            {
                sorter = new ListViewColumnSorter();
                _listView.ListViewItemSorter = sorter;
            }

            if (e.Column == sorter.SortColumn)
            {
                // Aynı kolona tıklandı - sıralamayı ters çevir
                sorter.Order = sorter.Order == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            }
            else
            {
                // Yeni kolon - artan sıralama
                sorter.SortColumn = e.Column;
                sorter.Order = SortOrder.Ascending;
            }

            _listView.Sort();
        }

        private void UpdateCountLabel()
        {
            _lblCount.Text = $"İstek: {_requestCount} | Yanıt: {_responseCount}";
        }

        private void BtnSortByDuration_Click(object sender, EventArgs e)
        {
            var sorter = _listView.ListViewItemSorter as ListViewColumnSorter;

            if (sorter == null)
            {
                sorter = new ListViewColumnSorter();
                _listView.ListViewItemSorter = sorter;
            }

            // Süre kolonuna göre sırala (kolon 5)
            sorter.SortColumn = 5;

            // Toggle artan/azalan
            if (sorter.Order == SortOrder.Descending)
            {
                sorter.Order = SortOrder.Ascending;
                _btnSortByDuration.Text = "Süre ↑";
            }
            else
            {
                sorter.Order = SortOrder.Descending;
                _btnSortByDuration.Text = "Süre ↓";
            }

            _listView.Sort();
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            _listView.Items.Clear();
            _requestCount = 0;
            _responseCount = 0;
            UpdateCountLabel();
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "Text File|*.txt";
                sfd.FileName = $"requests_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    var lines = new System.Collections.Generic.List<string>();
                    foreach (ListViewItem item in _listView.Items)
                    {
                        var line = $"{item.SubItems[0].Text}\t{item.SubItems[1].Text}\t{item.SubItems[2].Text}\t{item.SubItems[3].Text}\t{item.SubItems[4].Text}\t{item.SubItems[5].Text}";
                        lines.Add(line);
                    }
                    System.IO.File.WriteAllLines(sfd.FileName, lines);
                    MessageBox.Show("Log dosyası kaydedildi!", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }

    /// <summary>
    /// ListView column sorter
    /// </summary>
    public class ListViewColumnSorter : System.Collections.IComparer
    {
        public int SortColumn { get; set; }
        public SortOrder Order { get; set; }

        public ListViewColumnSorter()
        {
            SortColumn = 0;
            Order = SortOrder.None;
        }

        public int Compare(object x, object y)
        {
            var itemX = x as ListViewItem;
            var itemY = y as ListViewItem;

            if (itemX == null || itemY == null)
                return 0;

            var textX = itemX.SubItems[SortColumn].Text;
            var textY = itemY.SubItems[SortColumn].Text;

            int result;

            // Sayısal değerleri özel olarak işle
            if (SortColumn == 4) // Status kolonu
            {
                if (int.TryParse(textX, out int numX) && int.TryParse(textY, out int numY))
                    result = numX.CompareTo(numY);
                else
                    result = string.Compare(textX, textY);
            }
            else if (SortColumn == 5) // Süre kolonu
            {
                // "1.25s" formatından sayıyı çıkar
                var numTextX = textX.TrimEnd('s');
                var numTextY = textY.TrimEnd('s');
                if (double.TryParse(numTextX, out double dblX) && double.TryParse(numTextY, out double dblY))
                    result = dblX.CompareTo(dblY);
                else
                    result = string.Compare(textX, textY);
            }
            else
            {
                result = string.Compare(textX, textY);
            }

            return Order == SortOrder.Descending ? -result : result;
        }
    }
}
