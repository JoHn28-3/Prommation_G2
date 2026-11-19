using System;
using System.Threading.Tasks;

namespace WHATSAPP_CHAPT.App.Services
{
    /// <summary>
    /// Service de connexion P2P.
    /// </summary>
    public interface IConnectionService
    {
        /// <summary>
        /// Etablit une connexion TCP vers le pair.
        /// Retourne le booléen de succès et l'identifiant distant.
        /// </summary>
        Task<(bool Success, string RemoteId)> ConnectAsync(string ip, int port = 5000);

        /// <summary>
        /// Lance l'écoute UDP de découverte sur le port 45454.
        /// L'événement <c>Discovered</c> est déclenché lorsqu'un packet est reçu.
        /// </summary>
        Task StartBroadcastListenerAsync();

        /// <summary>
        /// Événement déclenché lorsqu'un pair est découvert via broadcast.
        /// </summary>
        event EventHandler<(string Ip, string RemoteId)> Discovered;
    }
}
