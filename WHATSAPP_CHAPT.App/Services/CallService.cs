#nullable enable

using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WHATSAPP_CHAPT.App.Services
{
    /// <summary>
    /// Service minimal de signalisation TCP pour les appels.
    /// </summary>
    public sealed class CallService : ICallService
    {
        private readonly IConnectionService _connection;
        private TcpClient? _pendingClient;

        /// <inheritdoc/>
        public event EventHandler<(string Ip, int Port)>? IncomingCall;

        /// <summary>
        /// Initialise le service de signalisation.
        /// </summary>
        /// <param name="connection">Service fournissant l’identifiant local.</param>
        public CallService(IConnectionService connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        /// <inheritdoc/>
        public async Task<bool> InitiateCallAsync(string ip, int port = 5000)
        {
            if (string.IsNullOrWhiteSpace(ip))
            {
                throw new ArgumentException("L’adresse IP ne peut pas être vide.", nameof(ip));
            }

            var localId = await _connection.GetLocalIdAsync();
            using var client = new TcpClient();
            await client.ConnectAsync(ip, port);

            using NetworkStream stream = client.GetStream();
            var payload = Encoding.UTF8.GetBytes($"CALL:{localId:D}");
            await stream.WriteAsync(payload);

            var buffer = new byte[256];
            int read = await stream.ReadAsync(buffer);
            string reply = Encoding.UTF8.GetString(buffer, 0, read).Trim();
            return reply.StartsWith("ACK:", StringComparison.OrdinalIgnoreCase)
                && Guid.TryParse(reply[4..], out _);
        }

        /// <summary>
        /// Traite une connexion entrante contenant un message CALL.
        /// </summary>
        internal async Task HandleIncomingCallAsync(TcpClient client, string ip, int port)
        {
            ArgumentNullException.ThrowIfNull(client);

            using NetworkStream stream = client.GetStream();
            var buffer = new byte[256];
            int read = await stream.ReadAsync(buffer);
            string payload = Encoding.UTF8.GetString(buffer, 0, read).Trim();

            if (!payload.StartsWith("CALL:", StringComparison.OrdinalIgnoreCase)
                || !Guid.TryParse(payload[5..], out _))
            {
                client.Dispose();
                return;
            }

            _pendingClient?.Dispose();
            _pendingClient = client;
            IncomingCall?.Invoke(this, (ip, port));
        }

        /// <inheritdoc/>
        public async Task<bool> AnswerCallAsync(bool accept)
        {
            if (_pendingClient == null)
            {
                return false;
            }

            using var client = _pendingClient;
            _pendingClient = null;

            if (!accept)
            {
                return false;
            }

            var localId = await _connection.GetLocalIdAsync();
            using NetworkStream stream = client.GetStream();
            var response = Encoding.UTF8.GetBytes($"ACK:{localId:D}");
            await stream.WriteAsync(response);
            return true;
        }

        /// <inheritdoc/>
        public Task StartVideoAsync() => Task.CompletedTask;

        /// <inheritdoc/>
        public Task EndCallAsync()
        {
            _pendingClient?.Dispose();
            _pendingClient = null;
            return Task.CompletedTask;
        }
    }
}
