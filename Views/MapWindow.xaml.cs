using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Web.WebView2.Core;
using BlockchainNetworkAnalyzer.Core.Models;
using BlockchainNetworkAnalyzer.Core.Services;
using Microsoft.Extensions.Logging;
using static BlockchainNetworkAnalyzer.App;
using System.IO;

namespace BlockchainNetworkAnalyzer.Views
{
    public partial class MapWindow : Window
    {
        private readonly ILogger<MapWindow> _logger;
        private readonly GeolocationService _geolocationService;
        private readonly RoutingService _routingService;
        private WebView2 _webView;
        private List<MinerMarker> _minerMarkers = new List<MinerMarker>();
        private MinerMarker _selectedMiner;

        public MapWindow()
        {
            InitializeComponent();
            _logger = App.LoggerFactory.CreateLogger<MapWindow>();
            _geolocationService = new GeolocationService();
            _routingService = new RoutingService();
            InitializeMap();
        }

        private async void InitializeMap()
        {
            try
            {
                // Create WebView2 control
                _webView = new WebView2
                {
                    Name = "MapWebView"
                };

                MapContainer.Children.Clear();
                MapContainer.Children.Add(_webView);

                // Wait for WebView2 to initialize
                await _webView.EnsureCoreWebView2Async();

                // Load map HTML
                var mapHtmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "map.html");
                if (File.Exists(mapHtmlPath))
                {
                    _webView.CoreWebView2.Navigate(mapHtmlPath);
                }
                else
                {
                    // Load inline HTML
                    await LoadMapHTML();
                }

                // Setup message handler for JavaScript communication
                _webView.CoreWebView2.WebMessageReceived += WebView_WebMessageReceived;

                MapStatusText.Text = "Map loaded successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing map");
                MapStatusText.Text = $"Error: {ex.Message}";
            }
        }

        private async Task LoadMapHTML()
        {
            var htmlContent = GenerateMapHTML();
            _webView.CoreWebView2.NavigateToString(htmlContent);
        }

        private string GenerateMapHTML()
        {
            return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <title>Miner Locations Map</title>
    <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />
    <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
    <style>
        body { margin: 0; padding: 0; }
        #map { width: 100%; height: 100vh; }
    </style>
</head>
<body>
    <div id='map'></div>
    <script>
        // Initialize map centered on Iran
        var map = L.map('map').setView([32.4279, 53.6880], 6);
        
        // Add OpenStreetMap tile layer
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap contributors',
            maxZoom: 19
        }).addTo(map);
        
        var markers = [];
        
        // Function to add marker
        window.addMarker = function(lat, lng, title, description, ipAddress, type) {
            var iconColor = type === 'miner' ? 'red' : 'blue';
            var icon = L.divIcon({
                className: 'custom-marker',
                html: '<div style=''background-color: ' + iconColor + '; width: 20px; height: 20px; border-radius: 50%; border: 2px solid white;''></div>',
                iconSize: [20, 20]
            });
            
            var marker = L.marker([lat, lng], { icon: icon }).addTo(map);
            
            var popupContent = '<b>' + title + '</b><br/>' +
                              'IP: ' + ipAddress + '<br/>' +
                              description;
            
            marker.bindPopup(popupContent);
            
            marker.on('click', function() {
                window.chrome.webview.postMessage({
                    type: 'markerClicked',
                    ipAddress: ipAddress,
                    lat: lat,
                    lng: lng
                });
            });
            
            markers.push(marker);
            return marker;
        };
        
        // Function to add route
        window.addRoute = function(routePoints) {
            var polyline = L.polyline(routePoints, { color: 'blue', weight: 5 }).addTo(map);
            map.fitBounds(polyline.getBounds());
            return polyline;
        };
        
        // Function to clear all markers
        window.clearMarkers = function() {
            markers.forEach(function(marker) {
                map.removeLayer(marker);
            });
            markers = [];
        };
        
        // Function to fit bounds
        window.fitBounds = function(minLat, minLng, maxLat, maxLng) {
            map.fitBounds([[minLat, minLng], [maxLat, maxLng]]);
        };
        
        // Listen for messages from C#
        window.chrome.webview.addEventListener('message', function(event) {
            var data = event.data;
            if (data.type === 'addMarker') {
                window.addMarker(data.lat, data.lng, data.title, data.description, data.ipAddress, data.markerType);
            } else if (data.type === 'clearMarkers') {
                window.clearMarkers();
            } else if (data.type === 'fitBounds') {
                window.fitBounds(data.minLat, data.minLng, data.maxLat, data.maxLng);
            }
        });
    </script>
</body>
</html>";
        }

        private void WebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var message = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(e.TryGetWebMessageAsString());
                
                if (message != null && message.ContainsKey("type"))
                {
                    var type = message["type"].ToString();
                    
                    if (type == "markerClicked")
                    {
                        var ipAddress = message.ContainsKey("ipAddress") ? message["ipAddress"].ToString() : "";
                        Dispatcher.Invoke(() =>
                        {
                            SelectMinerByIP(ipAddress);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error processing web message");
            }
        }

        private async void LoadMinersBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MapStatusText.Text = "Loading miners...";
                LoadMinersBtn.IsEnabled = false;

                // Load miners from database
                var miners = await LoadMinersFromDatabase();

                // Clear existing markers
                await ClearMarkers();

                // Add markers for each miner
                foreach (var miner in miners)
                {
                    await AddMinerMarker(miner);
                }

                _minerMarkers = miners;
                MinerListBox.ItemsSource = miners;
                MarkerCountText.Text = miners.Count.ToString();
                MapStatusText.Text = $"Loaded {miners.Count} miners";

                // Fit bounds to show all markers
                if (miners.Any())
                {
                    await FitBoundsToMarkers(miners);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading miners");
                MapStatusText.Text = $"Error: {ex.Message}";
            }
            finally
            {
                LoadMinersBtn.IsEnabled = true;
            }
        }

        private async Task<List<MinerMarker>> LoadMinersFromDatabase()
        {
            var miners = new List<MinerMarker>();

            try
            {
                using var connection = Core.DatabaseManager.GetConnection();
                connection.Open();

                var sql = @"SELECT DISTINCT 
                            r.IPAddress, 
                            r.BlockchainType,
                            g.Latitude,
                            g.Longitude,
                            g.Address,
                            g.City,
                            g.Province,
                            i.ConnectionType,
                            i.ISP
                            FROM IPResults r
                            LEFT JOIN GeolocationData g ON r.IPAddress = g.IPAddress
                            LEFT JOIN InternetConnectionInfo i ON r.IPAddress = i.IPAddress
                            WHERE r.BlockchainDetected = 1
                            AND g.Latitude IS NOT NULL
                            AND g.Longitude IS NOT NULL";

                using var command = new System.Data.SQLite.SQLiteCommand(sql, connection);
                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    miners.Add(new MinerMarker
                    {
                        IPAddress = reader["IPAddress"]?.ToString(),
                        BlockchainType = reader["BlockchainType"]?.ToString(),
                        Latitude = Convert.ToDouble(reader["Latitude"]),
                        Longitude = Convert.ToDouble(reader["Longitude"]),
                        Address = reader["Address"]?.ToString(),
                        City = reader["City"]?.ToString(),
                        Province = reader["Province"]?.ToString(),
                        ConnectionType = reader["ConnectionType"]?.ToString(),
                        ISP = reader["ISP"]?.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading miners from database");
            }

            return miners;
        }

        private async Task AddMinerMarker(MinerMarker miner)
        {
            if (_webView?.CoreWebView2 == null) return;

            var script = $@"
                window.addMarker(
                    {miner.Latitude},
                    {miner.Longitude},
                    '{miner.IPAddress}',
                    '{miner.Address ?? miner.City ?? ""}',
                    '{miner.IPAddress}',
                    'miner'
                );";

            await _webView.CoreWebView2.ExecuteScriptAsync(script);
        }

        private async Task ClearMarkers()
        {
            if (_webView?.CoreWebView2 == null) return;
            await _webView.CoreWebView2.ExecuteScriptAsync("window.clearMarkers();");
        }

        private async Task FitBoundsToMarkers(List<MinerMarker> markers)
        {
            if (!markers.Any() || _webView?.CoreWebView2 == null) return;

            var minLat = markers.Min(m => m.Latitude);
            var maxLat = markers.Max(m => m.Latitude);
            var minLng = markers.Min(m => m.Longitude);
            var maxLng = markers.Max(m => m.Longitude);

            var script = $"window.fitBounds({minLat}, {minLng}, {maxLat}, {maxLng});";
            await _webView.CoreWebView2.ExecuteScriptAsync(script);
        }

        private void MinerListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MinerListBox.SelectedItem is MinerMarker miner)
            {
                _selectedMiner = miner;
                ShowDetailsBtn.IsEnabled = true;
                NavigateBtn.IsEnabled = true;
                ShowRouteBtn.IsEnabled = true;
            }
        }

        private void SelectMinerByIP(string ipAddress)
        {
            var miner = _minerMarkers.FirstOrDefault(m => m.IPAddress == ipAddress);
            if (miner != null)
            {
                MinerListBox.SelectedItem = miner;
            }
        }

        private async void ShowRouteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMiner == null) return;

            try
            {
                // Get current location (or user location)
                // For now, use a default location
                var startLat = 35.6892; // Tehran
                var startLng = 51.3890;

                var route = await _routingService.GetRouteAsync(
                    startLat, startLng,
                    _selectedMiner.Latitude, _selectedMiner.Longitude);

                if (route.Success && _webView?.CoreWebView2 != null)
                {
                    // Add route to map
                    var routePoints = route.Steps.Select(s => 
                        $"[{s.StartLatitude}, {s.StartLongitude}]").ToList();
                    
                    var script = $"window.addRoute([{string.Join(", ", routePoints)}]);";
                    await _webView.CoreWebView2.ExecuteScriptAsync(script);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing route");
                MessageBox.Show($"Error showing route: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowDetailsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMiner == null) return;

            var details = $"IP Address: {_selectedMiner.IPAddress}\n" +
                         $"Blockchain Type: {_selectedMiner.BlockchainType}\n" +
                         $"Address: {_selectedMiner.Address}\n" +
                         $"City: {_selectedMiner.City}\n" +
                         $"Province: {_selectedMiner.Province}\n" +
                         $"Connection Type: {_selectedMiner.ConnectionType}\n" +
                         $"ISP: {_selectedMiner.ISP}";

            MessageBox.Show(details, "Miner Details", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void NavigateBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMiner == null) return;

            // Open in external browser with Google Maps
            var url = $"https://www.google.com/maps/dir/?api=1&destination={_selectedMiner.Latitude},{_selectedMiner.Longitude}";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        private void ClearMapBtn_Click(object sender, RoutedEventArgs e)
        {
            ClearMarkers();
            _minerMarkers.Clear();
            MinerListBox.ItemsSource = null;
            MarkerCountText.Text = "0";
            _selectedMiner = null;
            ShowDetailsBtn.IsEnabled = false;
            NavigateBtn.IsEnabled = false;
            ShowRouteBtn.IsEnabled = false;
        }
    }

    public class MinerMarker
    {
        public string IPAddress { get; set; }
        public string BlockchainType { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Province { get; set; }
        public string ConnectionType { get; set; }
        public string ISP { get; set; }
    }
}

