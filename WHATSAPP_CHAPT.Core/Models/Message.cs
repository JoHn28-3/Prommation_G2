#nullable enable

using System;
using System.Collections.Generic;

namespace WHATSAPP_CHAPT.Core.Models
{
    /// <summary>
    /// Message d'une conversation.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Identifiant unique du message.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Identifiant de la conversation contenant le message.
        /// </summary>
        public Guid ConversationId { get; set; }

        /// <summary>
        /// Identifiant de l'auteur du message.
        /// </summary>
        public Guid AuthorId { get; set; }

        /// <summary>
        /// Contenu texte du message.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Horodatage du message.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Statut actuel du message.
        /// </summary>
        public MessageStatus Status { get; set; }

        /// <summary>
        /// Indique si le message a été édité.
        /// </summary>
        public bool IsEdited { get; set; }

        /// <summary>
        /// Indique si le message a été supprimé.
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Référence au message auquel il répond (null si aucun).
        /// </summary>
        public Guid? ReplyToMessageId { get; set; }

        /// <summary>
        /// Chemin d'accès au fichier média associé (image, audio, document).
        /// </summary>
        public string? MediaPath { get; set; }

        /// <summary>
        /// Type MIME ou catégorie de média (ex: image, audio, document).
        /// </summary>
        public string? MediaType { get; set; }

        /// <summary>
        /// Taille du fichier média en octets.
        /// </summary>
        public long? MediaSize { get; set; }
    }
}
