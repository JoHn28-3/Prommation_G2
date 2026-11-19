using System;

namespace WHATSAPP_CHAPT.Storage.Models
{
    /// <summary>
    /// Représente un pair (autre machine) découvert via le réseau P2P.
    /// </summary>
    public class Peer
    {
        /// <summary>
        /// Identifiant unique du pair (GUID généré localement).
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Nom affiché du pair (peut être le display name de l'utilisateur distant).
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Adresse IP du pair (IPv4 ou IPv6 sous forme texte).
        /// </summary>
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// Date d'ajout en UTC.
        /// </summary>
        public DateTimeOffset AddedAt { get; set; }
    }
}
