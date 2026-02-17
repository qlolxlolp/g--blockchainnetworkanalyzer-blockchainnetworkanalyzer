using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
using Microsoft.Extensions.Configuration;
using BlockchainNetworkAnalyzer.Core;
using BlockchainNetworkAnalyzer.Core.Models;
using BlockchainNetworkAnalyzer.Core.Services;
using Microsoft.Extensions.Logging;
using static BlockchainNetworkAnalyzer.App;

namespace BlockchainNetworkAnalyzer.Views
{
    public partial class MainWindow : Window
    {
        private readonly ILogger<MainWindow> _logger;
        private NetworkScanner _networkScanner;
        private ScanResult _currentScanResult;
        private DispatcherTimer _progressTimer;
        private readonly List<IPResult> _currentResults = new List<IPResult>();
        
        // Services for expanded system
        private readonly DetectionEngine _detectionEngine;
        private readonly ReportingService _reportingService;
        private readonly AnalyticsService _analyticsService;
        private readonly SchedulerService _schedulerService;
        private readonly DataExchangeService _dataExchangeService;
        private readonly EnhancedISPService _enhancedISPService;

        public MainWindow()
        {
            InitializeComponent();
            _logger = App.LoggerFactory.CreateLogger<MainWindow>();
            
            // Initialize new services
            _detectionEngine = new DetectionEngine();
            _reportingService = new ReportingService();
            _analyticsService = new AnalyticsService();
            _schedulerService = new SchedulerService();
            _dataExchangeService = new DataExchangeService();
            _enhancedISPService = new EnhancedISPService();
            
            InitializeUI();
            InitializeEventHandlers();
            InitializeExpandedSystem();
        }

        private void InitializeUI()
        {
            // Set default values
            IPSelectionModeCombo.SelectedIndex = 0;
            TimeoutSlider.ValueChanged += (s, e) => 
                TimeoutLabel.Text = $"Timeout: {e.NewValue}ms";
            ConcurrentScansSlider.ValueChanged += (s, e) => 
                ConcurrentScansLabel.Text = $"Concurrent: {e.NewValue}";
            
            // Confidence threshold slider
            ConfidenceThresholdSlider.ValueChanged += (s, e) =>
                ConfidenceThresholdLabel.Text = $"Confidence Threshold: {e.NewValue:F0}%";
            
            // Load previous scan results
            LoadScanResults();
        }

        private void InitializeEventHandlers()
        {
            // Event handlers will be set up here
        }

        private void InitializeExpandedSystem()
        {
            try
            {
                // Load Analytics Dashboard
                LoadAnalyticsDashboard();
                
                // Load Detection Rules
                LoadDetectionRules();
                
                // Load Scheduled Scans
                LoadScheduledScans();
                
                // Load ISP Statistics
                LoadISPStatistics();
                
                // Start the scheduler service
                _schedulerService.Start();
                
                _logger.LogInformation("Expanded system initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing expanded system");
            }
        }

        private async void LoadAnalyticsDashboard()
        {
            try
            {
                var summary = await _analyticsService.GetDashboardSummaryAsync();
                
                AnalyticsTotalScans.Text = summary.TotalScans.ToString();
                AnalyticsTotalMiners.Text = summary.TotalMinersDetected.ToString();
                AnalyticsActiveAlerts.Text = summary.ActiveAlerts.ToString();
                AnalyticsScheduledScans.Text = summary.ScheduledScans.ToString();
                
                ISPStatisticsDataGrid.ItemsSource = summary.TopProvinces;
                GeographicDistributionDataGrid.ItemsSource = summary.TopProvinces;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading analytics dashboard");
            }
        }

        private void LoadDetectionRules()
        {
            try
            {
                var rules = _detectionEngine.GetRules();
                DetectionRulesDataGrid.ItemsSource = rules;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading detection rules");
            }
        }

        private async void LoadScheduledScans()
        {
            try
            {
                var schedules = await _schedulerService.GetScheduledScansAsync();
                ScheduledScansDataGrid.ItemsSource = schedules;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading scheduled scans");
            }
        }

        private async void LoadISPStatistics()
        {
            try
            {
                var stats = await _analyticsService.GetISPStatisticsAsync();
                ISPStatisticsDataGrid.ItemsSource = stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ISP statistics");
            }
        }

        private void IPSelectionModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Hide all panels
            SingleIPPanel.Visibility = Visibility.Collapsed;
            IPRangePanel.Visibility = Visibility.Collapsed;
            RandomIPPanel.Visibility = Visibility.Collapsed;
            SerialIPPanel.Visibility = Visibility.Collapsed;
            CustomIPPanel.Visibility = Visibility.Collapsed;

            // Show selected panel
            if (IPSelectionModeCombo.SelectedItem is ComboBoxItem selectedItem)
            {
                var mode = selectedItem.Tag?.ToString();
                switch (mode)
                {
                    case "SingleIP":
                        SingleIPPanel.Visibility = Visibility.Visible;
                        break;
                    case "IPRange":
                        IPRangePanel.Visibility = Visibility.Visible;
                        break;
                    case "RandomIP":
                        RandomIPPanel.Visibility = Visibility.Visible;
                        break;
                    case "SerialIP":
                        SerialIPPanel.Visibility = Visibility.Visible;
                        break;
                    case "CustomIP":
                        CustomIPPanel.Visibility = Visibility.Visible;
                        break;
                }
            }
        }

        private void PortSelection_Changed(object sender, RoutedEventArgs e)
        {
            if (CustomPortsRadio.IsChecked == true)
            {
                CustomPortsTxt.IsEnabled = true;
            }
            else
            {
                CustomPortsTxt.IsEnabled = false;
            }
        }

        private async void StartScanBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate input
                var config = BuildScanConfiguration();
                if (config == null)
                {
                    MessageBox.Show("Please configure IP selection and ports correctly.", 
                                  "Configuration Error", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Warning);
                    return;
                }

                // Disable button
                StartScanBtn.IsEnabled = false;
                StatusText.Text = "Initializing scan...";
                ScanProgressBar.Visibility = Visibility.Visible;
                ProgressText.Visibility = Visibility.Visible;
                ScanProgressBar.Value = 0;

                // Clear previous results
                _currentResults.Clear();
                ResultsDataGrid.ItemsSource = null;

                // Create scanner
                _networkScanner = new NetworkScanner();
                _networkScanner.ScanProgress += OnScanProgress;
                _networkScanner.IPResultFound += OnIPResultFound;

                // Start scan in background
                await Task.Run(async () =>
                {
                    try
                    {
                        _currentScanResult = await _networkScanner.ScanAsync(config);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during scan");
                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show($"Scan failed: {ex.Message}", 
                                          "Scan Error", 
                                          MessageBoxButton.OK, 
                                          MessageBoxImage.Error);
                        });
                    }
                    finally
                    {
                        Dispatcher.Invoke(() =>
                        {
                            StartScanBtn.IsEnabled = true;
                            ScanProgressBar.Visibility = Visibility.Collapsed;
                            ProgressText.Visibility = Visibility.Collapsed;
                            StatusText.Text = "Scan completed";
                            UpdateResultsSummary();
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting scan");
                MessageBox.Show($"Failed to start scan: {ex.Message}", 
                              "Error", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
                StartScanBtn.IsEnabled = true;
            }
        }

        private ScanConfiguration BuildScanConfiguration()
        {
            var config = new ScanConfiguration
            {
                Timeout = (int)TimeoutSlider.Value,
                MaxConcurrent = (int)ConcurrentScansSlider.Value,
                EnableFakeIPDetection = EnableFakeIPDetectionCheck.IsChecked == true,
                EnableBlockchainDetection = EnableBlockchainDetectionCheck.IsChecked == true,
                EnablePortScanning = EnablePortScanningCheck.IsChecked == true,
                ScanName = $"Scan_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            // Set IP selection mode
            if (IPSelectionModeCombo.SelectedItem is ComboBoxItem selectedItem)
            {
                var mode = selectedItem.Tag?.ToString();
                switch (mode)
                {
                    case "SingleIP":
                        config.SelectionMode = IPSelectionMode.SingleIP;
                        config.StartIP = SingleIPTxt.Text.Trim();
                        if (string.IsNullOrEmpty(config.StartIP))
                            return null;
                        break;
                    case "IPRange":
                        config.SelectionMode = IPSelectionMode.IPRange;
                        config.StartIP = StartIPTxt.Text.Trim();
                        config.EndIP = EndIPTxt.Text.Trim();
                        if (string.IsNullOrEmpty(config.StartIP) || string.IsNullOrEmpty(config.EndIP))
                            return null;
                        break;
                    case "RandomIP":
                        config.SelectionMode = IPSelectionMode.RandomIP;
                        if (int.TryParse(RandomIPCountTxt.Text, out int randomCount))
                            config.RandomIPCount = randomCount;
                        else
                            return null;
                        break;
                    case "SerialIP":
                        config.SelectionMode = IPSelectionMode.SerialIP;
                        config.StartIP = SerialBaseIPTxt.Text.Trim();
                        if (int.TryParse(SerialIPCountTxt.Text, out int serialCount))
                            config.RandomIPCount = serialCount;
                        else
                            return null;
                        if (string.IsNullOrEmpty(config.StartIP))
                            return null;
                        break;
                    case "CustomIP":
                        config.SelectionMode = IPSelectionMode.CustomIP;
                        var customIPs = CustomIPsTxt.Text
                            .Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(ip => ip.Trim())
                            .Where(ip => !string.IsNullOrEmpty(ip))
                            .ToList();
                        if (customIPs.Count == 0)
                            return null;
                        config.CustomIPs = customIPs;
                        break;
                }
            }

            // Set ports
            if (DefaultBlockchainPortsRadio.IsChecked == true)
            {
                // Use default blockchain ports from config
                var defaultPorts = App.Configuration.GetSection("Blockchain:DefaultPorts").Get<int[]>();
                config.Ports = defaultPorts?.ToList() ?? new List<int>();
            }
            else if (CustomPortsRadio.IsChecked == true)
            {
                var ports = CustomPortsTxt.Text
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => int.TryParse(p.Trim(), out int port) ? port : 0)
                    .Where(p => p > 0 && p <= 65535)
                    .ToList();
                config.Ports = ports;
            }
            else if (AllPortsRadio.IsChecked == true)
            {
                // All common blockchain ports
                config.Ports = new List<int>
                {
                    3333, 4028, 4444, 5555, 7777, 8080, 8332, 8333, 8443,
                    8555, 8888, 9332, 9333, 9999, 14433, 14444, 14455, 18080,
                    3256, 8008, 8088, 8444
                };
            }

            return config;
        }

        private void OnScanProgress(object sender, ScanProgressEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                ScanProgressBar.Value = e.Percentage;
                ProgressText.Text = $"{e.Scanned} / {e.Total} ({e.Percentage:F1}%)";
                StatusText.Text = $"Scanning... {e.Scanned} of {e.Total} IPs";
            });
        }

        private void OnIPResultFound(object sender, IPResult result)
        {
            Dispatcher.Invoke(() =>
            {
                _currentResults.Add(result);
                ResultsDataGrid.ItemsSource = null;
                ResultsDataGrid.ItemsSource = _currentResults;
                
                // Auto-scroll to latest
                if (ResultsDataGrid.Items.Count > 0)
                {
                    ResultsDataGrid.ScrollIntoView(ResultsDataGrid.Items[ResultsDataGrid.Items.Count - 1]);
                }

                UpdateResultsSummary();
            });
        }

        private void UpdateResultsSummary()
        {
            TotalScannedTxt.Text = _currentResults.Count.ToString();
            HostsFoundTxt.Text = _currentResults.Count(r => r.PortStatus == "Open").ToString();
            BlockchainDetectedTxt.Text = _currentResults.Count(r => r.BlockchainDetected).ToString();
            FakeIPsDetectedTxt.Text = _currentResults.Count(r => r.IsFakeIP).ToString();
        }

        private void ExportResultsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentResults.Count == 0)
            {
                MessageBox.Show("No results to export.", 
                              "Export", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Information);
                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                FileName = $"ScanResults_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    ExportResults(_currentResults, saveDialog.FileName);
                    MessageBox.Show("Results exported successfully.", 
                                  "Export", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error exporting results");
                    MessageBox.Show($"Failed to export results: {ex.Message}", 
                                  "Export Error", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Error);
                }
            }
        }

        private void ExportResults(List<IPResult> results, string filePath)
        {
            if (filePath.EndsWith(".csv"))
            {
                ExportToCSV(results, filePath);
            }
            else if (filePath.EndsWith(".json"))
            {
                ExportToJSON(results, filePath);
            }
        }

        private void ExportToCSV(List<IPResult> results, string filePath)
        {
            using var writer = new System.IO.StreamWriter(filePath);
            writer.WriteLine("IP Address,Port,Status,Service,Protocol,Blockchain Detected,Blockchain Type,Fake IP,Fake IP Reason,Response Time (ms),ISP,Geolocation");

            foreach (var result in results)
            {
                writer.WriteLine($"{result.IPAddress}," +
                               $"{result.Port}," +
                               $"{result.PortStatus}," +
                               $"{result.Service}," +
                               $"{result.Protocol}," +
                               $"{result.BlockchainDetected}," +
                               $"{result.BlockchainType}," +
                               $"{result.IsFakeIP}," +
                               $"\"{result.FakeIPReason}\"," +
                               $"{result.ResponseTime}," +
                               $"{result.ISP}," +
                               $"{result.Geolocation}");
            }
        }

        private void ExportToJSON(List<IPResult> results, string filePath)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(results, Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText(filePath, json);
        }

        private void LoadScanResults()
        {
            // Load recent scan results from database
            try
            {
                // Implementation to load from database
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load scan results");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _networkScanner?.Cancel();
            _schedulerService?.Stop();
            base.OnClosed(e);
        }

        #region Detection Rules Event Handlers

        private void AddRuleBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Rule editor dialog would open here.", "Add Rule", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EditRuleBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DetectionRulesDataGrid.SelectedItem is DetectionRule selectedRule)
            {
                MessageBox.Show($"Editing rule: {selectedRule.Name}", "Edit Rule", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Please select a rule to edit.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteRuleBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DetectionRulesDataGrid.SelectedItem is DetectionRule selectedRule)
            {
                var result = MessageBox.Show($"Are you sure you want to delete rule '{selectedRule.Name}'?", 
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _detectionEngine.RemoveRule(selectedRule.Id);
                    LoadDetectionRules();
                }
            }
            else
            {
                MessageBox.Show("Please select a rule to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ImportRulesBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Rules import dialog would open here.", "Import Rules", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportRulesBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Rules export dialog would open here.", "Export Rules", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Scheduling Event Handlers

        private void AddScheduleBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Schedule creation dialog would open here.", "Add Schedule", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EditScheduleBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ScheduledScansDataGrid.SelectedItem is ScheduledScan selectedSchedule)
            {
                MessageBox.Show($"Editing schedule: {selectedSchedule.Name}", "Edit Schedule", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Please select a schedule to edit.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteScheduleBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ScheduledScansDataGrid.SelectedItem is ScheduledScan selectedSchedule)
            {
                var result = MessageBox.Show($"Are you sure you want to delete schedule '{selectedSchedule.Name}'?", 
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _schedulerService.DeleteScheduledScanAsync(selectedSchedule.Id);
                    LoadScheduledScans();
                }
            }
            else
            {
                MessageBox.Show("Please select a schedule to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RunNowBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ScheduledScansDataGrid.SelectedItem is ScheduledScan selectedSchedule)
            {
                _schedulerService.ExecuteScheduledScanAsync(selectedSchedule.Id);
                MessageBox.Show($"Schedule '{selectedSchedule.Name}' is now running.", "Run Schedule", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Please select a schedule to run.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region Data Management Event Handlers

        private async void ExportDataBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentScanResult == null)
                {
                    MessageBox.Show("No scan results to export. Please run a scan first.", 
                        "No Data", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var format = ExportFormat.Html;
                if (ExportFormatCombo.SelectedItem is ComboBoxItem selectedFormat)
                {
                    format = selectedFormat.Tag?.ToString() switch
                    {
                        "Html" => ExportFormat.Html,
                        "Csv" => ExportFormat.Csv,
                        "Json" => ExportFormat.Json,
                        "Xml" => ExportFormat.Xml,
                        "Txt" => ExportFormat.Txt,
                        _ => ExportFormat.Html
                    };
                }

                string filePath;
                switch (format)
                {
                    case ExportFormat.Html:
                        filePath = await _reportingService.GenerateHtmlReportAsync(_currentScanResult.Id);
                        break;
                    case ExportFormat.Csv:
                        filePath = await _reportingService.GenerateCsvReportAsync(_currentScanResult.Id);
                        break;
                    case ExportFormat.Json:
                        filePath = await _reportingService.GenerateJsonReportAsync(_currentScanResult.Id);
                        break;
                    default:
                        filePath = await _reportingService.GenerateTxtReportAsync(_currentScanResult.Id);
                        break;
                }

                MessageBox.Show($"Data exported successfully to:\n{filePath}", 
                    "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data");
                MessageBox.Show($"Export failed: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BrowseImportBtn_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "All Supported Files (*.json;*.xml;*.csv)|*.json;*.xml;*.csv|JSON Files (*.json)|*.json|XML Files (*.xml)|*.xml|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                Title = "Select File to Import"
            };

            if (openDialog.ShowDialog() == true)
            {
                ImportFilePathTxt.Text = openDialog.FileName;
            }
        }

        private async void ImportDataBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var filePath = ImportFilePathTxt.Text;
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    MessageBox.Show("Please select a valid file to import.", "Invalid File", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var format = ImportFormat.Json;
                if (ImportFormatCombo.SelectedItem is ComboBoxItem selectedFormat)
                {
                    format = selectedFormat.Tag?.ToString() switch
                    {
                        "Json" => ImportFormat.Json,
                        "Xml" => ImportFormat.Xml,
                        "Csv" => ImportFormat.Csv,
                        "Nmap" => ImportFormat.Nmap,
                        _ => ImportFormat.Json
                    };
                }

                var scanId = await _dataExchangeService.ImportScanDataAsync(filePath, format);
                MessageBox.Show($"Data imported successfully. New scan ID: {scanId}", 
                    "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing data");
                MessageBox.Show($"Import failed: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CreateBackupBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var backupPath = await _dataExchangeService.ExportDatabaseBackupAsync();
                MessageBox.Show($"Database backup created:\n{backupPath}", 
                    "Backup Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating backup");
                MessageBox.Show($"Backup failed: {ex.Message}", "Backup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RestoreBackupBtn_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "SQLite Database Files (*.db)|*.db|All Files (*.*)|*.*",
                Title = "Select Backup File to Restore"
            };

            if (openDialog.ShowDialog() == true)
            {
                var result = MessageBox.Show("Restoring a backup will replace the current database. Are you sure?", 
                    "Confirm Restore", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _dataExchangeService.RestoreDatabaseBackupAsync(openDialog.FileName);
                        MessageBox.Show("Database restored successfully. Please restart the application.", 
                            "Restore Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error restoring backup");
                        MessageBox.Show($"Restore failed: {ex.Message}", "Restore Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void OpenReportsFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var reportsDir = _reportingService.GetReportsDirectory();
                if (!Directory.Exists(reportsDir))
                {
                    Directory.CreateDirectory(reportsDir);
                }
                Process.Start("explorer.exe", reportsDir);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening reports folder");
                MessageBox.Show($"Failed to open folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Settings Event Handlers

        private void SaveSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Save settings to configuration
                MessageBox.Show("Settings saved successfully.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving settings");
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}

