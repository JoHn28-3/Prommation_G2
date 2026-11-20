#nullable enable

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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
        public event EventHandler<(string Ip, string Payload)>? PayloadReceived;

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

        public async Task StartTcpListenerAsync()
        {
            var listener = new TcpListener(IPAddress.Any, TcpPort);
            listener.Start();
            try
            {
                while (true)
                {
                    using TcpClient client = await listener.AcceptTcpClientAsync();
                    _ = HandleIncomingTcpAsync(client);
                }
            }
            finally
            {
                listener.Stop();
            }
        }

        private async Task HandleIncomingTcpAsync(TcpClient client)
        {
            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[64 * 1024];
                    int read = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (read <= 0) return;

                    string text = Encoding.UTF8.GetString(buffer, 0, read);
                    int separator = text.IndexOf('\n');
                    if (separator <= 0) return;
                    string header = text[..separator].Trim();
                    string payload = text[(separator + 1)..];
                    if (!header.StartsWith("HELLO:", StringComparison.OrdinalIgnoreCase)) return;

                    string ip = ((IPEndPoint)client.Client.RemoteEndPoint!).Address.ToString();
                    PayloadReceived?.Invoke(this, (ip, payload));
                }
            }
            catch
            {
                // Un pair invalide ou deconnecte ne doit pas interrompre l'écoute.
            }
        }

        public async Task SendPayloadAsync(string ip, string payload)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(ip, TcpPort);
            using NetworkStream stream = client.GetStream();
            Guid localId = await GetLocalIdAsync();
            string packet = $"HELLO:{localId:D}\n{payload}";
            byte[] bytes = Encoding.UTF8.GetBytes(packet);
            await stream.WriteAsync(bytes, 0, bytes.Length);
            await stream.FlushAsync();
        }

        public async Task StartDiscoveryBroadcastAsync(CancellationToken cancellationToken)
        {
            using var udp = new UdpClient();
            udp.EnableBroadcast = true;
            Guid localId = await GetLocalIdAsync();
            byte[] bytes = Encoding.UTF8.GetBytes($"HELLO:{localId:D}");
            var endpoint = new IPEndPoint(IPAddress.Broadcast, BroadcastPort);

            while (!cancellationToken.IsCancellationRequested)
            {
                await udp.SendAsync(bytes, bytes.Length, endpoint);
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }
}
