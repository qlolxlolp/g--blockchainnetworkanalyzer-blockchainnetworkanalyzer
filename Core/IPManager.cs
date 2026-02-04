using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using BlockchainNetworkAnalyzer.Core.Models;
using static BlockchainNetworkAnalyzer.App;

namespace BlockchainNetworkAnalyzer.Core
{
    public class IPManager
    {
        private readonly ILogger<IPManager> _logger;
        private static readonly Random _random = new Random();

        public IPManager()
        {
            _logger = App.LoggerFactory.CreateLogger<IPManager>();
        }

        public List<string> GenerateIPs(ScanConfiguration config)
        {
            switch (config.SelectionMode)
            {
                case IPSelectionMode.SingleIP:
                    return new List<string> { config.StartIP };

                case IPSelectionMode.IPRange:
                    return GenerateIPRange(config.StartIP, config.EndIP);

                case IPSelectionMode.RandomIP:
                    return GenerateRandomIPs(config.RandomIPCount);

                case IPSelectionMode.SerialIP:
                    return GenerateSerialIPs(config.StartIP, config.RandomIPCount);

                case IPSelectionMode.CustomIP:
                    return config.CustomIPs;

                default:
                    throw new ArgumentException("Invalid IP selection mode");
            }
        }

        private List<string> GenerateIPRange(string startIP, string endIP)
        {
            var ips = new List<string>();
            
            if (!IsValidIP(startIP) || !IsValidIP(endIP))
            {
                _logger.LogWarning("Invalid IP addresses provided for range generation");
                return ips;
            }

            var start = IPAddress.Parse(startIP).GetAddressBytes();
            var end = IPAddress.Parse(endIP).GetAddressBytes();

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(start);
                Array.Reverse(end);
            }

            var startLong = BitConverter.ToUInt32(start, 0);
            var endLong = BitConverter.ToUInt32(end, 0);

            if (startLong > endLong)
            {
                (startLong, endLong) = (endLong, startLong);
            }

            for (var ip = startLong; ip <= endLong; ip++)
            {
                var bytes = BitConverter.GetBytes(ip);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(bytes);
                }
                ips.Add(new IPAddress(bytes).ToString());
            }

            _logger.LogInformation($"Generated {ips.Count} IPs from range {startIP} to {endIP}");
            return ips;
        }

        private List<string> GenerateRandomIPs(int count)
        {
            var ips = new HashSet<string>();
            var attempts = 0;
            int maxAttempts = count * 10;

            while (ips.Count < count && attempts < maxAttempts)
            {
                var ip = $"{_random.Next(1, 255)}.{_random.Next(0, 255)}.{_random.Next(0, 255)}.{_random.Next(1, 254)}";
                
                // Avoid private IP ranges
                if (!IsPrivateIP(ip))
                {
                    ips.Add(ip);
                }
                attempts++;
            }

            _logger.LogInformation($"Generated {ips.Count} random IPs");
            return ips.ToList();
        }

        private List<string> GenerateSerialIPs(string baseIP, int count)
        {
            var ips = new List<string>();
            
            if (!IsValidIP(baseIP))
            {
                _logger.LogWarning($"Invalid base IP: {baseIP}");
                return ips;
            }

            var ipBytes = IPAddress.Parse(baseIP).GetAddressBytes();
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(ipBytes);
            }

            var baseLong = BitConverter.ToUInt32(ipBytes, 0);

            for (int i = 0; i < count; i++)
            {
                var newLong = (uint)(baseLong + i);
                var bytes = BitConverter.GetBytes(newLong);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(bytes);
                }
                var newIP = new IPAddress(bytes).ToString();
                if (IsValidIP(newIP))
                {
                    ips.Add(newIP);
                }
            }

            _logger.LogInformation($"Generated {ips.Count} serial IPs from {baseIP}");
            return ips;
        }

        public bool IsValidIP(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
                return false;

            return IPAddress.TryParse(ip, out _);
        }

        public bool IsPrivateIP(string ip)
        {
            if (!IsValidIP(ip))
                return false;

            var address = IPAddress.Parse(ip);
            var bytes = address.GetAddressBytes();

            // Private IP ranges:
            // 10.0.0.0 - 10.255.255.255
            // 172.16.0.0 - 172.31.255.255
            // 192.168.0.0 - 192.168.255.255
            // 127.0.0.0 - 127.255.255.255 (loopback)

            return bytes[0] == 10 ||
                   (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                   (bytes[0] == 192 && bytes[1] == 168) ||
                   (bytes[0] == 127);
        }

        public string GetNetworkRange(string ip, int subnetMask = 24)
        {
            if (!IsValidIP(ip))
                return null;

            var address = IPAddress.Parse(ip);
            var bytes = address.GetAddressBytes();
            var maskBits = subnetMask;

            // Calculate network address
            var networkBytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                if (maskBits >= 8)
                {
                    networkBytes[i] = bytes[i];
                    maskBits -= 8;
                }
                else if (maskBits > 0)
                {
                    networkBytes[i] = (byte)(bytes[i] & (255 << (8 - maskBits)));
                    maskBits = 0;
                }
            }

            return new IPAddress(networkBytes).ToString();
        }
    }
}

