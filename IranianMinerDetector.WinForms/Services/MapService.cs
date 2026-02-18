using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using IranianMinerDetector.WinForms.Models;

namespace IranianMinerDetector.WinForms.Services
{
    public class MapService
    {
        private readonly string _templatesDir;

        public MapService()
        {
            _templatesDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "IranianMinerDetector",
                "Maps");

            if (!Directory.Exists(_templatesDir))
                Directory.CreateDirectory(_templatesDir);
        }

        public string GenerateMap(int scanId, MapMode mode = MapMode.Markers)
        {
            var scan = DatabaseManager.Instance.GetAllScanRecords()
                .FirstOrDefault(s => s.Id == scanId);

            if (scan == null)
                throw new ArgumentException($"Scan {scanId} not found");

            var hosts = DatabaseManager.Instance.GetHostsByScanId(scanId);
            var hostsWithLocation = hosts
                .Where(h => h.Latitude.HasValue && h.Longitude.HasValue)
                .ToList();

            var html = GenerateMapHTML(scan, hostsWithLocation, mode);
            var fileName = $"map_{scanId}_{DateTime.Now:yyyyMMdd_HHmmss}.html";
            var filePath = Path.Combine(_templatesDir, fileName);
            
            File.WriteAllText(filePath, html);
            
            return filePath;
        }

        private string GenerateMapHTML(ScanRecord scan, List<HostRecord> hosts, MapMode mode)
        {
            var sb = new StringBuilder();

            // Center on Iran
            var centerLat = 32.4279;
            var centerLon = 53.6880;
            var zoom = 5;

            // Adjust if we have miner locations
            var miners = hosts.Where(h => h.IsMinerDetected).ToList();
            if (miners.Any())
            {
                centerLat = miners.Average(m => m.Latitude ?? centerLat);
                centerLon = miners.Average(m => m.Longitude ?? centerLon);
                zoom = 6;
            }

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset='utf-8' />");
            sb.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0' />");
            sb.AppendLine("    <title>Iranian Miner Detector - Map</title>");
            sb.AppendLine("");
            sb.AppendLine("    <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />");
            sb.AppendLine("    <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>");
            sb.AppendLine("");
            sb.AppendLine("    <style>");
            sb.AppendLine("        * { margin: 0; padding: 0; }");
            sb.AppendLine("        body { font-family: Arial, sans-serif; }");
            sb.AppendLine("        #map { height: 100vh; width: 100%; }");
            sb.AppendLine("        #info {");
            sb.AppendLine("            position: absolute;");
            sb.AppendLine("            top: 10px;");
            sb.AppendLine("            right: 10px;");
            sb.AppendLine("            background: white;");
            sb.AppendLine("            padding: 10px;");
            sb.AppendLine("            border-radius: 5px;");
            sb.AppendLine("            z-index: 1000;");
            sb.AppendLine("            box-shadow: 0 0 10px rgba(0,0,0,0.2);");
            sb.AppendLine("            max-width: 300px;");
            sb.AppendLine("        }");
            sb.AppendLine("        .info-header { font-weight: bold; margin-bottom: 5px; }");
            sb.AppendLine("        .info-row { margin: 3px 0; }");
            sb.AppendLine("        .info-label { font-weight: bold; }");
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("    <div id='info'>");
            sb.AppendLine("        <div class='info-header'>Scan Information</div>");
            sb.AppendLine($"        <div class='info-row'><span class='info-label'>Scan ID:</span> {scan.Id}</div>");
            sb.AppendLine($"        <div class='info-row'><span class='info-label'>Province:</span> {scan.Province ?? 'All'}</div>");
            sb.AppendLine($"        <div class='info-row'><span class='info-label'>City:</span> {scan.City ?? 'All'}</div>");
            sb.AppendLine($"        <div class='info-row'><span class='info-label'>Total IPs:</span> {scan.TotalIPs}</div>");
            sb.AppendLine($"        <div class='info-row'><span class='info-label'>Online:</span> {scan.OnlineHosts}</div>");
            sb.AppendLine($"        <div class='info-row'><span class='info-label'>Miners:</span> <span style='color: red; font-weight: bold;'>{scan.MinersFound}</span></div>");
            sb.AppendLine("    </div>");
            sb.AppendLine("    <div id='map'></div>");
            sb.AppendLine("");
            sb.AppendLine("    <script>");
            sb.AppendLine("        var map = L.map('map').setView([" + centerLat + ", " + centerLon + "], " + zoom + ");");
            sb.AppendLine("");
            sb.AppendLine("        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {");
            sb.AppendLine("            attribution: '© OpenStreetMap contributors'");
            sb.AppendLine("        }).addTo(map);");
            sb.AppendLine("");

            // Add markers
            foreach (var host in hosts)
            {
                var lat = host.Latitude ?? centerLat;
                var lon = host.Longitude ?? centerLon;
                var color = host.IsMinerDetected ? "red" : "blue";
                var icon = host.IsMinerDetected ? "⛏️" : "📍";
                var popup = GeneratePopup(host);

                sb.AppendLine($"        var marker{host.Id} = L.marker([{lat}, {lon}]).addTo(map);");
                sb.AppendLine($"        marker{host.Id}.bindPopup({popup});");

                if (host.IsMinerDetected)
                {
                    // Add circle for miners
                    sb.AppendLine($"        L.circle([{lat}, {lon}], {{");
                    sb.AppendLine($"            color: '{color}',");
                    sb.AppendLine($"            fillColor: '{color}',");
                    sb.AppendLine($"            fillOpacity: 0.3,");
                    sb.AppendLine($"            radius: 50000");
                    sb.AppendLine($"        }}).addTo(map);");
                }
            }

            sb.AppendLine("    </script>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        private string GeneratePopup(HostRecord host)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<div style='min-width: 200px;'>");
            sb.AppendLine($"    <strong>IP: {host.IPAddress}</strong><br/>");
            sb.AppendLine($"    Status: {(host.IsOnline ? "<span style='color: green;'>Online</span>" : "<span style='color: red;'>Offline</span>")}<br/>");
            
            if (host.ResponseTimeMs.HasValue)
                sb.AppendLine($"    Response: {host.ResponseTimeMs}ms<br/>");

            if (host.OpenPorts.Any())
                sb.AppendLine($"    Ports: {string.Join(", ", host.OpenPorts)}<br/>");

            if (host.IsMinerDetected)
                sb.AppendLine($"    <strong style='color: red;'>⛏️ MINER DETECTED</strong><br/>");

            if (!string.IsNullOrEmpty(host.DetectedService))
                sb.AppendLine($"    Service: {host.DetectedService}<br/>");

            if (!string.IsNullOrEmpty(host.ISP))
                sb.AppendLine($"    ISP: {host.ISP}<br/>");

            if (!string.IsNullOrEmpty(host.City) || !string.IsNullOrEmpty(host.Province))
                sb.AppendLine($"    Location: {host.City}, {host.Province}<br/>");

            if (host.ConfidenceScore > 0)
                sb.AppendLine($"    Confidence: {host.ConfidenceScore:P0}");

            sb.AppendLine("</div>");

            return $"`{sb.ToString().Replace(Environment.NewLine, "").Replace("'", "\\'")}`";
        }

        public string GenerateHeatmap(int scanId)
        {
            var scan = DatabaseManager.Instance.GetAllScanRecords()
                .FirstOrDefault(s => s.Id == scanId);

            if (scan == null)
                throw new ArgumentException($"Scan {scanId} not found");

            var hosts = DatabaseManager.Instance.GetHostsByScanId(scanId);
            var hostsWithLocation = hosts
                .Where(h => h.Latitude.HasValue && h.Longitude.HasValue)
                .ToList();

            var html = GenerateHeatmapHTML(scan, hostsWithLocation);
            var fileName = $"heatmap_{scanId}_{DateTime.Now:yyyyMMdd_HHmmss}.html";
            var filePath = Path.Combine(_templatesDir, fileName);

            File.WriteAllText(filePath, html);

            return filePath;
        }

        private string GenerateHeatmapHTML(ScanRecord scan, List<HostRecord> hosts)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset='utf-8' />");
            sb.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0' />");
            sb.AppendLine("    <title>Iranian Miner Detector - Heatmap</title>");
            sb.AppendLine("");
            sb.AppendLine("    <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />");
            sb.AppendLine("    <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>");
            sb.AppendLine("    <script src='https://cdnjs.cloudflare.com/ajax/libs/leaflet.heat/0.2.0/leaflet-heat.js'></script>");
            sb.AppendLine("");
            sb.AppendLine("    <style>");
            sb.AppendLine("        * { margin: 0; padding: 0; }");
            sb.AppendLine("        body { font-family: Arial, sans-serif; }");
            sb.AppendLine("        #map { height: 100vh; width: 100%; }");
            sb.AppendLine("        #legend {");
            sb.AppendLine("            position: absolute;");
            sb.AppendLine("            bottom: 20px;");
            sb.AppendLine("            right: 20px;");
            sb.AppendLine("            background: white;");
            sb.AppendLine("            padding: 10px;");
            sb.AppendLine("            border-radius: 5px;");
            sb.AppendLine("            z-index: 1000;");
            sb.AppendLine("        }");
            sb.AppendLine("        .legend-item { display: flex; align-items: center; margin: 5px 0; }");
            sb.AppendLine("        .legend-color { width: 20px; height: 20px; margin-right: 10px; }");
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("    <div id='map'></div>");
            sb.AppendLine("    <div id='legend'>");
            sb.AppendLine("        <strong>Legend</strong><br/>");
            sb.AppendLine("        <div class='legend-item'>");
            sb.AppendLine("            <div class='legend-color' style='background: red;'></div>");
            sb.AppendLine("            <span>High Density</span>");
            sb.AppendLine("        </div>");
            sb.AppendLine("        <div class='legend-item'>");
            sb.AppendLine("            <div class='legend-color' style='background: yellow;'></div>");
            sb.AppendLine("            <span>Medium Density</span>");
            sb.AppendLine("        </div>");
            sb.AppendLine("        <div class='legend-item'>");
            sb.AppendLine("            <div class='legend-color' style='background: green;'></div>");
            sb.AppendLine("            <span>Low Density</span>");
            sb.AppendLine("        </div>");
            sb.AppendLine("    </div>");
            sb.AppendLine("");
            sb.AppendLine("    <script>");
            sb.AppendLine("        var map = L.map('map').setView([32.4279, 53.6880], 5);");
            sb.AppendLine("");
            sb.AppendLine("        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {");
            sb.AppendLine("            attribution: '© OpenStreetMap contributors'");
            sb.AppendLine("        }).addTo(map);");
            sb.AppendLine("");

            // Generate heat data
            if (hosts.Any())
            {
                sb.AppendLine("        var heatData = [");
                var heatPoints = new List<string>();
                foreach (var host in hosts)
                {
                    var lat = host.Latitude ?? 0;
                    var lon = host.Longitude ?? 0;
                    var intensity = host.IsMinerDetected ? 1.0 : 0.3;
                    heatPoints.Add($"[{lat}, {lon}, {intensity}]");
                }
                sb.AppendLine($"            {string.Join(",\n            ", heatPoints)}");
                sb.AppendLine("        ];");
                sb.AppendLine("");
                sb.AppendLine("        var heat = L.heatLayer(heatData, {");
                sb.AppendLine("            radius: 25,");
                sb.AppendLine("            blur: 15,");
                sb.AppendLine("            maxZoom: 10,");
                sb.AppendLine("            max: 1.0");
                sb.AppendLine("        }).addTo(map);");
            }

            sb.AppendLine("    </script>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }
    }

    public enum MapMode
    {
        Markers,
        Heatmap
    }
}
