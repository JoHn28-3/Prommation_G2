#nullable enable

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using WHATSAPP_CHAPT.Storage;
using WHATSAPP_CHAPT.Storage.Models;

namespace WHATSAPP_CHAPT.App.Services
{
    /// <summary>
    /// Service de connexion P2P (UDP 45454, TCP 5000).
    /// </summary>
    public class ConnectionService : IConnectionService
    {
        private readonly StorageContext _storage;
        private const int BroadcastPort = 45454;
        private const int TcpPort = 5000;

        public event EventHandler<(string Ip, string RemoteId)>? Discovered;

        public ConnectionService(StorageContext storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public async Task StartBroadcastListenerAsync()
        {
            try
            {
                using var udpListener = new UdpClient();
                udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpListener.Client.Bind(new IPEndPoint(IPAddress.Any, BroadcastPort));

                while (true)
                {
                    var result = await udpListener.ReceiveAsync();
                    var payload = Encoding.UTF8.GetString(result.Buffer);
                    if (payload.StartsWith("HELLO:"))
                    {
                        var remoteId = payload.Substring(6);
                        Discovered?.Invoke(this, (result.RemoteEndPoint.Address.ToString(), remoteId));
                    }
                }
            }
            catch
            {
                // Listener arrêté ou port indisponible
            }
        }

        public async Task<(bool Success, string RemoteId)> ConnectAsync(string ip, int port = TcpPort)
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(ip, port);
                var stream = client.GetStream();

                var localId = await GetLocalIdAsync();
                var hello = $"HELLO:{localId}";
                var data = Encoding.UTF8.GetBytes(hello);
                await stream.WriteAsync(data, 0, data.Length);

                var buffer = new byte[256];
                var read = await stream.ReadAsync(buffer, 0, buffer.Length);
                var response = Encoding.UTF8.GetString(buffer, 0, read);
                if (!response.StartsWith("HELLO:")) return (false, string.Empty);
                var remoteId = response.Substring(6);

                var peer = new Peer { Id = Guid.NewGuid(), Name = $"Peer-{remoteId}", IpAddress = ip, AddedAt = DateTimeOffset.UtcNow };
                _storage.InsertPeer(peer.Id, peer.Name, peer.IpAddress);
                return (true, remoteId);
            }
            catch
            {
                return (false, string.Empty);
            }
        }

        public Task<Guid> GetLocalIdAsync()
        {
            return _storage.GetLocalIdAsync();
        }
    }
}
