#nullable enable

using System;
using System.Net;
using System.Threading.Tasks;

namespace WHATSAPP_CHAPT.App.Services
{
    /// <summary>
    /// Service d'appel peer‑to‑peer.
    /// Fournit les opérations de signalisation pour un appel audio/vidéo.
    /// </summary>
    public interface ICallService
    {
        /// <summary>
        /// Initialise une connexion de signalisation vers le pair.
        /// Retourne true si le pair accepte l'appel.
        /// </summary>
        Task<bool> InitiateCallAsync(string ip, int port = 5000);

        /// <summary>
        /// Accepte ou refuse un appel entrant (appelé par le listener).
        /// </summary>
        Task<bool> AnswerCallAsync(bool accept);

        /// <summary>
        /// Démarre le flux vidéo (placeholder). Utilise UDP sur le port de broadcast fourni.
        /// </summary>
        Task StartVideoAsync();

        /// <summary>
        /// Termine l'appel et libère les ressources.
        /// </summary>
        Task EndCallAsync();

        /// <summary>
        /// Evénement déclenché lorsqu'un appel entrant est détecté.
        /// </summary>
        event EventHandler<(string Ip, int Port)> IncomingCall;
    }
}
