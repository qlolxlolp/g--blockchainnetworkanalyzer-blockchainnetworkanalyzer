using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using IranianMinerDetector.WinForms.Data;
using IranianMinerDetector.WinForms.Models;

namespace IranianMinerDetector.WinForms.Forms
{
    public partial class ReportViewerForm : Form
    {
        private readonly DatabaseManager _db = DatabaseManager.Instance;
        private readonly int _scanId;
        private ScanRecord? _scan;

        public ReportViewerForm(int scanId)
        {
            _scanId = scanId;
            InitializeComponent();
            LoadScanData();
        }

        private void InitializeComponent()
        {
            this.Text = "مشاهده گزارش (Report Viewer)";
            this.Size = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.MinimumSize = new Size(1000, 600);

            // Menu Bar
            var menuStrip = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("فایل (File)");

            var exportPDFItem = new ToolStripMenuItem("خروجی PDF");
            exportPDFItem.Click += async (s, e) => await ExportPDF();
            fileMenu.DropDownItems.Add(exportPDFItem);

            var exportExcelItem = new ToolStripMenuItem("خروجی Excel");
            exportExcelItem.Click += (s, e) => ExportExcel();
            fileMenu.DropDownItems.Add(exportExcelItem);

            var exportCSVItem = new ToolStripMenuItem("خروجی CSV");
            exportCSVItem.Click += (s, e) => ExportCSV();
            fileMenu.DropDownItems.Add(exportCSVItem);

            var exportHTMLItem = new ToolStripMenuItem("خروجی HTML");
            exportHTMLItem.Click += (s, e) => ExportHTML();
            fileMenu.DropDownItems.Add(exportHTMLItem);

            var closeItem = new ToolStripMenuItem("بستن (Close)");
            closeItem.Click += (s, e) => this.Close();
            fileMenu.DropDownItems.Add(closeItem);

            menuStrip.Items.Add(fileMenu);
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);

            // Main Split Container
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 300,
                Panel1MinSize = 250,
                Panel2MinSize = 400
            };
            this.Controls.Add(splitContainer);

            // Left Panel - Summary
            var summaryPanel = splitContainer.Panel1;
            var summaryGroupBox = new GroupBox
            {
                Text = "خلاصه اسکن (Scan Summary)",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };
            summaryPanel.Controls.Add(summaryGroupBox);

            var summaryLabel = new Label
            {
                Name = "summaryLabel",
                Dock = DockStyle.Fill,
                Font = new Font("Tahoma", 9)
            };
            summaryGroupBox.Controls.Add(summaryLabel);

            // Right Panel - Details
            var detailsPanel = splitContainer.Panel2;

            // Tab Control
            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };
            detailsPanel.Controls.Add(tabControl);

            // Results Tab
            var resultsTabPage = new TabPage("نتایج (Results)");
            tabControl.TabPages.Add(resultsTabPage);

            var dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
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
            dataGridView.Columns.Add("Province", "Province");
            dataGridView.Columns.Add("City", "City");
            dataGridView.Columns.Add("ScannedAt", "Scanned At");
            resultsTabPage.Controls.Add(dataGridView);

            // Map Tab
            var mapTabPage = new TabPage("نقشه (Map)");
            tabControl.TabPages.Add(mapTabPage);

            var webView = new WebView2
            {
                Dock = DockStyle.Fill,
                DefaultBackgroundColor = Color.White
            };
            webView.Name = "mapWebView";
            webView.CreationProperties = new Microsoft.Web.WebView2.WinForms.CoreWebView2CreationProperties
            {
                UserDataFolder = System.IO.Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                    "IranianMinerDetector\\WebView2")
            };
            mapTabPage.Controls.Add(webView);

            // Status Bar
            var statusStrip = new StatusStrip();
            var toolStripStatusLabel = new ToolStripStatusLabel("Ready");
            toolStripStatusLabel.Name = "toolStripStatusLabel";
            statusStrip.Items.Add(toolStripStatusLabel);
            this.Controls.Add(statusStrip);

            // Initialize WebView2 asynchronously
            this.Load += async (s, e) =>
            {
                try
                {
                    await webView.EnsureCoreWebView2Async();
                }
                catch
                {
                    // WebView2 may not be available
                }
            };
        }

        private void LoadScanData()
        {
            _scan = _db.GetAllScanRecords().FirstOrDefault(s => s.Id == _scanId);

            if (_scan == null)
            {
                MessageBox.Show(
                    $"Scan with ID {_scanId} not found.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            UpdateSummary();

            var hosts = _db.GetHostsByScanId(_scanId);
            var dataGridView = this.Controls.Find("resultsDataGridView", true).FirstOrDefault() as DataGridView;
            if (dataGridView != null)
            {
                var bindingList = new System.ComponentModel.BindingList<HostRecord>(hosts);
                dataGridView.DataSource = bindingList;

                // Style the datagridview
                dataGridView.AlternatingRowsDefaultCellStyle.BackColor = Color.LightGray;
                dataGridView.DefaultCellStyle.Font = new Font("Tahoma", 9);

                foreach (DataGridViewColumn column in dataGridView.Columns)
                {
                    column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                }
            }

            // Load map
            try
            {
                var mapService = new Services.MapService();
                var mapPath = mapService.GenerateMap(_scanId);
                var webView = this.Controls.Find("mapWebView", true).FirstOrDefault() as WebView2;
                if (webView != null && webView.CoreWebView2 != null)
                {
                    webView.CoreWebView2.Navigate(mapPath);
                }
            }
            catch
            {
                // Map may fail to load
            }
        }

        private void UpdateSummary()
        {
            if (_scan == null) return;

            var summaryLabel = this.Controls.Find("summaryLabel", true).FirstOrDefault() as Label;
            if (summaryLabel == null) return;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Scan ID: " + _scan.Id);
            sb.AppendLine();
            sb.AppendLine("اطلاعات اسکن (Scan Information):");
            sb.AppendLine("------------------------------");
            sb.AppendLine($"Start Time: {_scan.StartTime:yyyy-MM-dd HH:mm:ss}");
            if (_scan.EndTime.HasValue)
                sb.AppendLine($"End Time: {_scan.EndTime.Value:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Province: {_scan.Province ?? "All"}");
            sb.AppendLine($"City: {_scan.City ?? "All"}");
            sb.AppendLine($"ISP: {_scan.ISP ?? "All"}");
            sb.AppendLine();
            sb.AppendLine("آمار (Statistics):");
            sb.AppendLine("------------------------------");
            sb.AppendLine($"Total IPs: {_scan.TotalIPs}");
            sb.AppendLine($"Scanned IPs: {_scan.ScannedIPs}");
            sb.AppendLine($"Online Hosts: {_scan.OnlineHosts}");
            sb.AppendLine($"Miners Found: {_scan.MinersFound}");
            sb.AppendLine($"Status: {_scan.Status}");
            sb.AppendLine();
            sb.AppendLine($"Success Rate: {_scan.TotalIPs > 0 ? (_scan.OnlineHosts * 100.0 / _scan.TotalIPs):F2}%");
            sb.AppendLine($"Miner Rate: {_scan.TotalIPs > 0 ? (_scan.MinersFound * 100.0 / _scan.TotalIPs):F2}%");

            summaryLabel.Text = sb.ToString();
        }

        private async System.Threading.Tasks.Task ExportPDF()
        {
            try
            {
                var reportService = new Services.ReportService();
                var filePath = await reportService.GeneratePDFReportAsync(_scanId);
                MessageBox.Show(
                    $"PDF report saved to:\n{filePath}",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"PDF export failed:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void ExportExcel()
        {
            try
            {
                var reportService = new Services.ReportService();
                var filePath = reportService.GenerateExcelReport(_scanId);
                MessageBox.Show(
                    $"Excel report saved to:\n{filePath}",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Excel export failed:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void ExportCSV()
        {
            try
            {
                var reportService = new Services.ReportService();
                var filePath = reportService.GenerateCSVReport(_scanId);
                MessageBox.Show(
                    $"CSV report saved to:\n{filePath}",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"CSV export failed:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void ExportHTML()
        {
            try
            {
                var reportService = new Services.ReportService();
                var filePath = reportService.GenerateHTMLReport(_scanId);
                MessageBox.Show(
                    $"HTML report saved to:\n{filePath}",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"HTML export failed:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
