using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace IranianMinerDetector.WinForms.Models
{
    public class ISPInfo
    {
        public string Name { get; set; } = string.Empty;
        public string NamePersian { get; set; } = string.Empty;
        public string ASN { get; set; } = string.Empty;
        public List<string> IPRanges { get; set; } = new List<string>();
        public double RiskScore { get; set; } = 0.5;
        public int DetectionCount { get; set; } = 0;

        public List<IPNetwork> Networks
        {
            get
            {
                var networks = new List<IPNetwork>();
                foreach (var cidr in IPRanges)
                {
                    try
                    {
                        networks.Add(IPNetwork.Parse(cidr));
                    }
                    catch
                    {
                        // Skip invalid CIDR
                    }
                }
                return networks;
            }
        }
    }

    public class IPNetwork
    {
        public IPAddress Address { get; private set; } = IPAddress.Any;
        public int PrefixLength { get; private set; } = 0;
        private IPAddress? _networkAddress;
        private IPAddress? _broadcastAddress;

        public IPNetwork(IPAddress address, int prefixLength)
        {
            Address = address;
            PrefixLength = prefixLength;
            CalculateNetworkAddresses();
        }

        public static IPNetwork Parse(string cidr)
        {
            var parts = cidr.Split('/');
            if (parts.Length != 2)
                throw new ArgumentException("Invalid CIDR format");

            var address = IPAddress.Parse(parts[0]);
            var prefixLength = int.Parse(parts[1]);

            return new IPNetwork(address, prefixLength);
        }

        private void CalculateNetworkAddresses()
        {
            var addressBytes = Address.GetAddressBytes();
            var maskBytes = GetSubnetMaskBytes(PrefixLength, addressBytes.Length);

            for (int i = 0; i < addressBytes.Length; i++)
            {
                addressBytes[i] &= maskBytes[i];
            }

            _networkAddress = new IPAddress(addressBytes);

            // Broadcast address
            var broadcastBytes = (byte[])addressBytes.Clone();
            for (int i = 0; i < broadcastBytes.Length; i++)
            {
                broadcastBytes[i] |= (byte)~maskBytes[i];
            }
            _broadcastAddress = new IPAddress(broadcastBytes);
        }

        private byte[] GetSubnetMaskBytes(int prefixLength, int addressLength)
        {
            var maskBytes = new byte[addressLength];
            for (int i = 0; i < addressLength; i++)
            {
                if (prefixLength >= 8)
                {
                    maskBytes[i] = 255;
                    prefixLength -= 8;
                }
                else if (prefixLength > 0)
                {
                    maskBytes[i] = (byte)~(255 >> prefixLength);
                    prefixLength = 0;
                }
                else
                {
                    maskBytes[i] = 0;
                }
            }
            return maskBytes;
        }

        public bool Contains(IPAddress address)
        {
            var addressBytes = address.GetAddressBytes();
            var networkBytes = _networkAddress?.GetAddressBytes();

            if (addressBytes.Length != networkBytes?.Length)
                return false;

            var maskBytes = GetSubnetMaskBytes(PrefixLength, addressBytes.Length);

            for (int i = 0; i < addressBytes.Length; i++)
            {
                if ((addressBytes[i] & maskBytes[i]) != (networkBytes[i] & maskBytes[i]))
                    return false;
            }

            return true;
        }
    }
}
