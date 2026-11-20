#nullable enable

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Text.Json;

namespace WHATSAPP_CHAPT.App.Services
{
    public sealed class UserProfile
    {
        public string DisplayName { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string ThemeMode { get; set; } = "dark";
        public string Username { get; set; } = string.Empty;
        public string StatusText { get; set; } = string.Empty;
    }

    public sealed class ProfileStore
    {
        private readonly string _profilePath;

        public ProfileStore(string dataFolder)
        {
            _profilePath = Path.Combine(dataFolder, "profile.json");
        }

        public bool Exists => File.Exists(_profilePath);

        public static string DetectLocalIp()
        {
            foreach (NetworkInterface network in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (network.OperationalStatus != OperationalStatus.Up
                    || network.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                foreach (UnicastIPAddressInformation address in
                         network.GetIPProperties().UnicastAddresses)
                {
                    IPAddress ip = address.Address;
                    if (ip.AddressFamily != AddressFamily.InterNetwork
                        || IPAddress.IsLoopback(ip))
                        continue;

                    byte[] bytes = ip.GetAddressBytes();
                    if (bytes[0] == 169 && bytes[1] == 254)
                        continue;

                    bool privateAddress =
                        bytes[0] == 10
                        || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                        || (bytes[0] == 192 && bytes[1] == 168);
                    if (privateAddress)
                        return ip.ToString();
                }
            }

            return string.Empty;
        }

        public UserProfile Load()
        {
            if (!Exists)
                return new UserProfile { IpAddress = DetectLocalIp() };

            try
            {
                return JsonSerializer.Deserialize<UserProfile>(File.ReadAllText(_profilePath))
                    ?? new UserProfile { IpAddress = DetectLocalIp() };
            }
            catch
            {
                return new UserProfile { IpAddress = DetectLocalIp() };
            }
        }

        public void Save(UserProfile profile)
        {
            ArgumentNullException.ThrowIfNull(profile);
            profile.DisplayName = profile.DisplayName.Trim();
            profile.IpAddress = profile.IpAddress.Trim();
            File.WriteAllText(_profilePath, JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
