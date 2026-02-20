using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using IranianMinerDetector.WinForms.Data;
using IranianMinerDetector.WinForms.Models;
using IranianMinerDetector.WinForms.Services;

namespace IranianMinerDetector.WinForms.Forms
{
    public partial class MainForm : Form
    {
        private readonly DatabaseManager _db = DatabaseManager.Instance;
        private NetworkScanner? _scanner;
        private ScanRecord? _currentScan;
        private BindingList<HostRecord> _hostsBinding = new();
        private readonly MapService _mapService = new();
        private readonly ReportService _reportService = new();

        public MainForm()
        {
            InitializeComponent();
            InitializeComponents();
            LoadScanHistory();
        }

        private void InitializeComponent()
        {
            this.Text = "Iranian Miner Detector - WinForms Edition";
            this.Size = new Size(1400, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            // Menu Bar
            var menuStrip = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("فایل (File)");
            var toolsMenu = new ToolStripMenuItem("ابزارها (Tools)");
            var helpMenu = new ToolStripMenuItem("راهنما (Help)");

            var newScanItem = new ToolStripMenuItem("اسکن جدید (New Scan)");
            newScanItem.Click += (s, e) => StartNewScan();
            fileMenu.DropDownItems.Add(newScanItem);

            var exitItem = new ToolStripMenuItem("خروج (Exit)");
            exitItem.Click += (s, e) => Application.Exit();
            fileMenu.DropDownItems.Add(exitItem);

            var exportPDFItem = new ToolStripMenuItem("خروجی PDF");
            exportPDFItem.Click += async (s, e) => await ExportPDF();
            toolsMenu.DropDownItems.Add(exportPDFItem);

            var exportExcelItem = new ToolStripMenuItem("خروجی Excel");
            exportExcelItem.Click += (s, e) => ExportExcel();
            toolsMenu.DropDownItems.Add(exportExcelItem);

            var exportCSVItem = new ToolStripMenuItem("خروجی CSV");
            exportCSVItem.Click += (s, e) => ExportCSV();
            toolsMenu.DropDownItems.Add(exportCSVItem);

            var settingsItem = new ToolStripMenuItem("تنظیمات (Settings)");
            settingsItem.Click += (s, e) => ShowSettings();
            toolsMenu.DropDownItems.Add(settingsItem);

            var aboutItem = new ToolStripMenuItem("درباره (About)");
            aboutItem.Click += (s, e) => ShowAbout();
            helpMenu.DropDownItems.Add(aboutItem);

            menuStrip.Items.Add(fileMenu);
            menuStrip.Items.Add(toolsMenu);
            menuStrip.Items.Add(helpMenu);

            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);

            // Main Layout
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 300,
                Panel1MinSize = 250,
                Panel2MinSize = 500
            };

            this.Controls.Add(splitContainer);

            // Left Panel - Configuration
            var configPanel = splitContainer.Panel1;
            var configLabel = new Label
            {
                Text = "تنظیمات اسکن (Scan Configuration)",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Tahoma", 10, FontStyle.Bold)
            };
            configPanel.Controls.Add(configLabel);

            // Province ComboBox
            var provinceLabel = new Label { Text = "استان (Province):", Location = new Point(10, 50), AutoSize = true };
            configPanel.Controls.Add(provinceLabel);

            var provinceComboBox = new ComboBox
            {
                Location = new Point(10, 75),
                Width = 270,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            provinceComboBox.Items.AddRange(IranianGeography.ProvinceNames.ToArray());
            provinceComboBox.SelectedIndexChanged += (s, e) =>
            {
                var cities = IranianGeography.GetCitiesByProvince(provinceComboBox.SelectedItem?.ToString() ?? "");
                cityComboBox.Items.Clear();
                cityComboBox.Items.AddRange(cities.Select(c => c.Name).ToArray());
            };
            provinceComboBox.Name = "provinceComboBox";
            configPanel.Controls.Add(provinceComboBox);

            // City ComboBox
            var cityLabel = new Label { Text = "شهر (City):", Location = new Point(10, 105), AutoSize = true };
            configPanel.Controls.Add(cityLabel);

            var cityComboBox = new ComboBox
            {
                Location = new Point(10, 130),
                Width = 270,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cityComboBox.Name = "cityComboBox";
            configPanel.Controls.Add(cityComboBox);

            // ISP ComboBox
            var ispLabel = new Label { Text = "ISP:", Location = new Point(10, 160), AutoSize = true };
            configPanel.Controls.Add(ispLabel);

            var ispComboBox = new ComboBox
            {
                Location = new Point(10, 185),
                Width = 270,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            ispComboBox.Items.Add("All ISPs");
            ispComboBox.Items.AddRange(IranianISPs.ISPNames.ToArray());
            ispComboBox.SelectedIndex = 0;
            ispComboBox.Name = "ispComboBox";
            configPanel.Controls.Add(ispComboBox);

            // IP Range TextBox
            var ipRangeLabel = new Label { Text = "محدوده IP (IP Range):", Location = new Point(10, 215), AutoSize = true };
            configPanel.Controls.Add(ipRangeLabel);

            var ipRangeTextBox = new TextBox
            {
                Location = new Point(10, 240),
                Width = 270,
                PlaceholderText = "e.g., 192.168.1.0/24 or 192.168.1.1-192.168.1.100"
            };
            ipRangeTextBox.Name = "ipRangeTextBox";
            configPanel.Controls.Add(ipRangeTextBox);

            // Ports TextBox
            var portsLabel = new Label { Text = "پورت‌ها (Ports):", Location = new Point(10, 270), AutoSize = true };
            configPanel.Controls.Add(portsLabel);

            var portsTextBox = new TextBox
            {
                Location = new Point(10, 295),
                Width = 270,
                Text = "8332,8333,30303,3333,4028,4444,18081"
            };
            portsTextBox.Name = "portsTextBox";
            configPanel.Controls.Add(portsTextBox);

            // Timeout NumericUpDown
            var timeoutLabel = new Label { Text = "زمان انتظار (ms):", Location = new Point(10, 325), AutoSize = true };
            configPanel.Controls.Add(timeoutLabel);

            var timeoutNumeric = new NumericUpDown
            {
                Location = new Point(10, 350),
                Width = 100,
                Minimum = 500,
                Maximum = 10000,
                Value = 3000
            };
            timeoutNumeric.Name = "timeoutNumeric";
            configPanel.Controls.Add(timeoutNumeric);

            // Concurrency NumericUpDown
            var concurrencyLabel = new Label { Text = "تعداد همزمان:", Location = new Point(120, 325), AutoSize = true };
            configPanel.Controls.Add(concurrencyLabel);

            var concurrencyNumeric = new NumericUpDown
            {
                Location = new Point(120, 350),
                Width = 100,
                Minimum = 10,
                Maximum = 500,
                Value = 100
            };
            concurrencyNumeric.Name = "concurrencyNumeric";
            configPanel.Controls.Add(concurrencyNumeric);

            // Buttons
            var startButton = new Button
            {
                Text = "شروع اسکن (Start Scan)",
                Location = new Point(10, 390),
                Width = 125,
                Height = 40,
                BackColor = Color.Green,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            startButton.Click += async (s, e) => await StartScan();
            startButton.Name = "startButton";
            configPanel.Controls.Add(startButton);

            var stopButton = new Button
            {
                Text = "توقف (Stop)",
                Location = new Point(145, 390),
                Width = 130,
                Height = 40,
                BackColor = Color.Red,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            stopButton.Click += (s, e) => StopScan();
            stopButton.Name = "stopButton";
            configPanel.Controls.Add(stopButton);

            // Progress Section
            var progressLabel = new Label { Text = "پیشرفت (Progress):", Location = new Point(10, 445), AutoSize = true };
            configPanel.Controls.Add(progressLabel);

            var progressBar = new ProgressBar
            {
                Location = new Point(10, 470),
                Width = 270,
                Height = 23
            };
            progressBar.Name = "progressBar";
            configPanel.Controls.Add(progressBar);

            var statusLabel = new Label
            {
                Location = new Point(10, 500),
                Width = 270,
                Height = 40,
                Text = "آماده برای شروع اسکن"
            };
            statusLabel.Name = "statusLabel";
            configPanel.Controls.Add(statusLabel);

            // Statistics
            var statsGroupBox = new GroupBox
            {
                Text = "آمار (Statistics)",
                Location = new Point(10, 550),
                Width = 270,
                Height = 120
            };
            configPanel.Controls.Add(statsGroupBox);

            var totalIPsLabel = new Label { Location = new Point(10, 25), AutoSize = true };
            totalIPsLabel.Name = "totalIPsLabel";
            statsGroupBox.Controls.Add(totalIPsLabel);

            var onlineLabel = new Label { Location = new Point(10, 50), AutoSize = true };
            onlineLabel.Name = "onlineLabel";
            statsGroupBox.Controls.Add(onlineLabel);

            var minersLabel = new Label { Location = new Point(10, 75), AutoSize = true };
            minersLabel.Name = "minersLabel";
            statsGroupBox.Controls.Add(minersLabel);

            // Right Panel - Results
            var resultsPanel = splitContainer.Panel2;

            // Tab Control
            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };
            resultsPanel.Controls.Add(tabControl);

            // Results Tab
            var resultsTabPage = new TabPage("نتایج (Results)");
            tabControl.TabPages.Add(resultsTabPage);

            // DataGridView
            var dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D
            };
            dataGridView.Name = "resultsDataGridView";
            dataGridView.Columns.Add("IPAddress", "IP Address");
            dataGridView.Columns.Add("IsOnline", "Status");
            dataGridView.Columns.Add("ResponseTimeMs", "Response (ms)");
            dataGridView.Columns.Add("OpenPorts", "Open Ports");
            dataGridView.Columns.Add("IsMinerDetected", "Miner");
            dataGridView.Columns.Add("ConfidenceScore", "Confidence");
            dataGridView.Columns.Add("DetectedService", "Service");
            dataGridView.Columns.Add("ISP", "ISP");
            dataGridView.Columns.Add("City", "Location");
            dataGridView.Columns.Add("ScannedAt", "Scanned At");

            resultsTabPage.Controls.Add(dataGridView);

            // Map Tab
            var mapTabPage = new TabPage("نقشه (Map)");
            tabControl.TabPages.Add(mapTabPage);

            // WebView2 for map
            var webView = new WebView2
            {
                Dock = DockStyle.Fill,
                DefaultBackgroundColor = Color.White
            };
            webView.Name = "mapWebView";
            webView.CreationProperties = new Microsoft.Web.WebView2.WinForms.CoreWebView2CreationProperties
            {
                UserDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\IranianMinerDetector\\WebView2"
            };
            mapTabPage.Controls.Add(webView);

            // Ensure WebView2 is initialized
            Task.Run(async () =>
            {
                try
                {
                    await webView.EnsureCoreWebView2Async();
                }
                catch
                {
                    // WebView2 initialization may fail on some systems
                }
            });

            // Log Tab
            var logTabPage = new TabPage("گزارشات (Log)");
            tabControl.TabPages.Add(logTabPage);

            var logTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.White,
                Font = new Font("Consolas", 9)
            };
            logTextBox.Name = "logTextBox";
            logTabPage.Controls.Add(logTextBox);

            // History Tab
            var historyTabPage = new TabPage("تاریخچه (History)");
            tabControl.TabPages.Add(historyTabPage);

            var historyDataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            historyDataGridView.Name = "historyDataGridView";
            historyDataGridView.Columns.Add("Id", "ID");
            historyDataGridView.Columns.Add("StartTime", "Start Time");
            historyDataGridView.Columns.Add("Province", "Province");
            historyDataGridView.Columns.Add("City", "City");
            historyDataGridView.Columns.Add("TotalIPs", "Total IPs");
            historyDataGridView.Columns.Add("OnlineHosts", "Online");
            historyDataGridView.Columns.Add("MinersFound", "Miners");
            historyDataGridView.Columns.Add("Status", "Status");

            historyDataGridView.CellDoubleClick += async (s, e) =>
            {
                if (historyDataGridView.SelectedRows.Count > 0)
                {
                    var scanId = Convert.ToInt32(historyDataGridView.SelectedRows[0].Cells["Id"].Value);
                    await LoadScanResults(scanId);
                }
            };

            historyTabPage.Controls.Add(historyDataGridView);

            // Status Bar
            var statusStrip = new StatusStrip();
            var toolStripStatusLabel = new ToolStripStatusLabel("آماده");
            toolStripStatusLabel.Name = "toolStripStatusLabel";
            statusStrip.Items.Add(toolStripStatusLabel);
            this.Controls.Add(statusStrip);
        }

        private async Task StartScan()
        {
            var config = GetScanConfiguration();
            _scanner = new NetworkScanner(config.MaxConcurrency);

            var startButton = this.Controls.Find("startButton", true).FirstOrDefault() as Button;
            var stopButton = this.Controls.Find("stopButton", true).FirstOrDefault() as Button;
            var progressBar = this.Controls.Find("progressBar", true).FirstOrDefault() as ProgressBar;
            var statusLabel = this.Controls.Find("statusLabel", true).FirstOrDefault() as Label;

            if (startButton != null) startButton.Enabled = false;
            if (stopButton != null) stopButton.Enabled = true;
            if (statusLabel != null) statusLabel.Text = "در حال اسکن...";

            _hostsBinding.Clear();
            var dataGridView = this.Controls.Find("resultsDataGridView", true).FirstOrDefault() as DataGridView;
            if (dataGridView != null) dataGridView.DataSource = null;

            _scanner.ProgressUpdated += (s, e) =>
            {
                if (progressBar != null)
                {
                    progressBar.Maximum = e.TotalIPs;
                    progressBar.Value = e.CurrentIP;
                }
                if (statusLabel != null)
                {
                    statusLabel.Text = $"{e.CurrentIP}/{e.TotalIPs} - Online: {e.OnlineHosts} - Miners: {e.MinersFound}";
                }

                UpdateStatistics(e);
            };

            _scanner.HostFound += (s, host) =>
            {
                _hostsBinding.Add(host);
                if (dataGridView != null && !dataGridView.IsDisposed)
                {
                    if (dataGridView.InvokeRequired)
                    {
                        dataGridView.Invoke(new Action(() => dataGridView.DataSource = _hostsBinding));
                    }
                    else
                    {
                        dataGridView.DataSource = _hostsBinding;
                    }
                }

                AddLogEntry($"Found: {host.IPAddress} - Miner: {host.IsMinerDetected}", host.IsMinerDetected ? Color.Red : Color.Green);
            };

            _scanner.LogMessage += (s, message) =>
            {
                AddLogEntry(message, Color.Black);
            };

            try
            {
                _currentScan = await _scanner.StartScanAsync(config);
                AddLogEntry($"Scan completed. Found {_currentScan.MinersFound} miners.", Color.Blue);

                if (_currentScan.Id > 0)
                {
                    LoadScanHistory();

                    // Generate and show map
                    try
                    {
                        var mapPath = _mapService.GenerateMap(_currentScan.Id);
                        var webView = this.Controls.Find("mapWebView", true).FirstOrDefault() as WebView2;
                        if (webView != null && webView.CoreWebView2 != null)
                        {
                            webView.CoreWebView2.Navigate(mapPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        AddLogEntry($"Map generation failed: {ex.Message}", Color.Orange);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Scan error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AddLogEntry($"Error: {ex.Message}", Color.Red);
            }
            finally
            {
                if (startButton != null) startButton.Enabled = true;
                if (stopButton != null) stopButton.Enabled = false;
                if (statusLabel != null) statusLabel.Text = "اسکن کامل شد";
            }
        }

        private void StopScan()
        {
            _scanner?.CancelScan();
            AddLogEntry("Scan stopped by user", Color.Orange);
        }

        private void StartNewScan()
        {
            _hostsBinding.Clear();
            var dataGridView = this.Controls.Find("resultsDataGridView", true).FirstOrDefault() as DataGridView;
            if (dataGridView != null) dataGridView.DataSource = null;

            var progressBar = this.Controls.Find("progressBar", true).FirstOrDefault() as ProgressBar;
            if (progressBar != null) progressBar.Value = 0;

            var statusLabel = this.Controls.Find("statusLabel", true).FirstOrDefault() as Label;
            if (statusLabel != null) statusLabel.Text = "آماده برای شروع اسکن";

            ResetStatistics();
        }

        private ScanConfiguration GetScanConfiguration()
        {
            var provinceComboBox = this.Controls.Find("provinceComboBox", true).FirstOrDefault() as ComboBox;
            var cityComboBox = this.Controls.Find("cityComboBox", true).FirstOrDefault() as ComboBox;
            var ispComboBox = this.Controls.Find("ispComboBox", true).FirstOrDefault() as ComboBox;
            var ipRangeTextBox = this.Controls.Find("ipRangeTextBox", true).FirstOrDefault() as TextBox;
            var portsTextBox = this.Controls.Find("portsTextBox", true).FirstOrDefault() as TextBox;
            var timeoutNumeric = this.Controls.Find("timeoutNumeric", true).FirstOrDefault() as NumericUpDown;
            var concurrencyNumeric = this.Controls.Find("concurrencyNumeric", true).FirstOrDefault() as NumericUpDown;

            var config = new ScanConfiguration
            {
                Province = provinceComboBox?.SelectedItem?.ToString(),
                City = cityComboBox?.SelectedItem?.ToString(),
                ISP = ispComboBox?.SelectedIndex > 0 ? ispComboBox?.SelectedItem?.ToString() : null,
                IPRange = ipRangeTextBox?.Text?.Trim(),
                TimeoutMs = (int)(timeoutNumeric?.Value ?? 3000),
                MaxConcurrency = (int)(concurrencyNumeric?.Value ?? 100)
            };

            if (portsTextBox != null)
            {
                config.Ports = portsTextBox.Text.Split(',')
                    .Where(p => int.TryParse(p.Trim(), out _))
                    .Select(p => int.Parse(p.Trim()))
                    .ToList();
            }

            return config;
        }

        private void UpdateStatistics(ScanProgress progress)
        {
            var totalIPsLabel = this.Controls.Find("totalIPsLabel", true).FirstOrDefault() as Label;
            var onlineLabel = this.Controls.Find("onlineLabel", true).FirstOrDefault() as Label;
            var minersLabel = this.Controls.Find("minersLabel", true).FirstOrDefault() as Label;

            if (totalIPsLabel != null) totalIPsLabel.Text = $"Total IPs: {progress.TotalIPs}";
            if (onlineLabel != null) onlineLabel.Text = $"Online: {progress.OnlineHosts}";
            if (minersLabel != null) minersLabel.Text = $"Miners: {progress.MinersFound}";
        }

        private void ResetStatistics()
        {
            var totalIPsLabel = this.Controls.Find("totalIPsLabel", true).FirstOrDefault() as Label;
            var onlineLabel = this.Controls.Find("onlineLabel", true).FirstOrDefault() as Label;
            var minersLabel = this.Controls.Find("minersLabel", true).FirstOrDefault() as Label;

            if (totalIPsLabel != null) totalIPsLabel.Text = "Total IPs: 0";
            if (onlineLabel != null) onlineLabel.Text = "Online: 0";
            if (minersLabel != null) minersLabel.Text = "Miners: 0";
        }

        private void LoadScanHistory()
        {
            var historyDataGridView = this.Controls.Find("historyDataGridView", true).FirstOrDefault() as DataGridView;
            if (historyDataGridView == null) return;

            var scans = _db.GetAllScanRecords();
            historyDataGridView.DataSource = scans;
        }

        private async Task LoadScanResults(int scanId)
        {
            var hosts = _db.GetHostsByScanId(scanId);
            _hostsBinding = new BindingList<HostRecord>(hosts);

            var dataGridView = this.Controls.Find("resultsDataGridView", true).FirstOrDefault() as DataGridView;
            if (dataGridView != null) dataGridView.DataSource = _hostsBinding;

            try
            {
                var mapPath = _mapService.GenerateMap(scanId);
                var webView = this.Controls.Find("mapWebView", true).FirstOrDefault() as WebView2;
                if (webView != null && webView.CoreWebView2 != null)
                {
                    webView.CoreWebView2.Navigate(mapPath);
                }
            }
            catch (Exception ex)
            {
                AddLogEntry($"Map generation failed: {ex.Message}", Color.Orange);
            }
        }

        private void AddLogEntry(string message, Color color)
        {
            var logTextBox = this.Controls.Find("logTextBox", true).FirstOrDefault() as RichTextBox;
            if (logTextBox == null) return;

            if (logTextBox.InvokeRequired)
            {
                logTextBox.Invoke(new Action(() => AddLogEntry(message, color)));
                return;
            }

            logTextBox.SelectionStart = logTextBox.TextLength;
            logTextBox.SelectionLength = 0;
            logTextBox.SelectionColor = color;
            logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            logTextBox.ScrollToCaret();
        }

        private async Task ExportPDF()
        {
            if (_currentScan == null)
            {
                MessageBox.Show("No scan selected for export.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var filePath = await _reportService.GeneratePDFReportAsync(_currentScan.Id);
                MessageBox.Show($"PDF report saved to: {filePath}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PDF export failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportExcel()
        {
            if (_currentScan == null)
            {
                MessageBox.Show("No scan selected for export.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var filePath = _reportService.GenerateExcelReport(_currentScan.Id);
                MessageBox.Show($"Excel report saved to: {filePath}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Excel export failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportCSV()
        {
            if (_currentScan == null)
            {
                MessageBox.Show("No scan selected for export.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var filePath = _reportService.GenerateCSVReport(_currentScan.Id);
                MessageBox.Show($"CSV report saved to: {filePath}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"CSV export failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowSettings()
        {
            using var settingsForm = new SettingsForm();
            var result = settingsForm.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                AddLogEntry("Settings updated successfully.", Color.Green);
            }
        }

        private void ShowAbout()
        {
            using var aboutForm = new AboutForm();
            aboutForm.ShowDialog(this);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _scanner?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
