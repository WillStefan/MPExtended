﻿#region Copyright (C) 2011-2012 MPExtended
// Copyright (C) 2011-2012 MPExtended Developers, http://mpextended.github.com/
// 
// MPExtended is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MPExtended is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MPExtended. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace MPExtended.Libraries.Service.Network
{
    public static class NetworkInformation
    {
        private static string _ipAddress;

        public static string GetIPAddress()
        {
            if (_ipAddress == null)
                _ipAddress = LoadIPAddress();
            return _ipAddress;
        }

        private static string LoadIPAddress()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(x => x.OperationalStatus == OperationalStatus.Up);
            
            string[] preferedInterfaces = new string[] { "Local Area Connection" };
            var lanAddresses = interfaces.Where(x => preferedInterfaces.Contains(x.Name))
                    .SelectMany(x => x.GetIPProperties().UnicastAddresses.Select(a => a.Address))
                    .Where(x => x.AddressFamily == AddressFamily.InterNetwork || (Configuration.Services.EnableIPv6 && x.AddressFamily == AddressFamily.InterNetworkV6))
                    .Select(x => x.ToString());
            if (lanAddresses.Any())
                return lanAddresses.First();

            var addresses = interfaces
                .SelectMany(x => x.GetIPProperties().UnicastAddresses.Select(a => a.Address))
                .Where(x => x.AddressFamily == AddressFamily.InterNetwork || (Configuration.Services.EnableIPv6 && x.AddressFamily == AddressFamily.InterNetworkV6))
                .Select(x => x.ToString());
            if (addresses.Any(x => x != "127.0.0.1"))
                return addresses.First(x => x != "127.0.0.1");
            if (addresses.Any())
                return addresses.First();
            return "127.0.0.1";
        }

        private static IEnumerable<IPAddress> GetIPAddressList(bool enableIPv6)
        {
            return Dns.GetHostEntry(Dns.GetHostName())
                .AddressList
                .Where(x => x.AddressFamily == AddressFamily.InterNetwork || (enableIPv6 && x.AddressFamily == AddressFamily.InterNetworkV6))
                .Distinct()
                .ToArray();
        }

        public static IEnumerable<string> GetIPAddresses()
        {
            return GetIPAddressList(Configuration.Services.EnableIPv6).Select(x => x.ToString()).ToList();
        }

        public static IEnumerable<string> GetMACAddresses()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(x => x.OperationalStatus == OperationalStatus.Up)
                .Where(x => x.GetPhysicalAddress() != null)
                .Select(x => x.GetPhysicalAddress().ToString())
                .Where(x => x.Length == 12)
                .Distinct()
                .ToList();
        }

        public static bool IsOnLAN(IPAddress address)
        {
            if (IsLocalAddress(address))
            {
                return true;
            }

            var info = NetworkInterface.GetAllNetworkInterfaces()
                .Where(x => x.OperationalStatus == OperationalStatus.Up && x.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .SelectMany(x => x.GetIPProperties().UnicastAddresses)
                .Where(x => x.Address.AddressFamily == AddressFamily.InterNetwork) // TODO: IPv6 support
                .Where(x => x.IPv4Mask != null)
                .First();

            return address.IsInSameSubnet(info.Address, info.IPv4Mask);
        }

        public static bool IsOnLAN(string address)
        {
            if (IsLocalAddress(address))
            {
                return true;
            }

            return IsOnLAN(IPAddress.Parse(address));
        }

        public static bool IsLocalAddress(IPAddress address)
        {
            return address.ToString() == "127.0.0.1" || address.ToString() == "::1" ||
                GetIPAddressList(true).Contains(address);
        }

        public static bool IsLocalAddress(string address)
        {
            if(address == "localhost" || address == "::1" || address == "127.0.0.1")
            {
                return true;
            }

            bool isLocal = Dns.GetHostAddresses(address)
                .Where(x => x.AddressFamily == AddressFamily.InterNetwork || x.AddressFamily == AddressFamily.InterNetworkV6 && Configuration.Services.EnableIPv6)
                .Any(x => IsLocalAddress(x));
            if(isLocal)
            {
                return true;
            }

            IPAddress ipAddr;
            if(IPAddress.TryParse(address, out ipAddr))
            {
                return IsLocalAddress(ipAddr);
            }
            return false;
        }

        public static Dictionary<string, string> GetNetworkInterfaces(bool enableIPv6)
        {
            Dictionary<string, string> ifaces = new Dictionary<string, string>();
            foreach (var iface in NetworkInterface.GetAllNetworkInterfaces().Where(x => x.OperationalStatus == OperationalStatus.Up))
            {
                foreach (IPAddressInformation unicast in iface.GetIPProperties().UnicastAddresses)
                {
                    if (unicast.Address.AddressFamily == AddressFamily.InterNetwork ||
                       (unicast.Address.AddressFamily == AddressFamily.InterNetworkV6 && enableIPv6))
                    {
                        ifaces.Add(iface.Name, unicast.Address.ToString());
                    }
                }
            }

            return ifaces;
        }

        public static Dictionary<string, string> GetNetworkInterfaces()
        {
            return GetNetworkInterfaces(Configuration.Services.EnableIPv6);
        }

        public static bool IsValid(IPAddress address, bool enableIPv6)
        {
            return address.AddressFamily == AddressFamily.InterNetwork ||
                (address.AddressFamily == AddressFamily.InterNetworkV6 && enableIPv6);
        }
    }
}
