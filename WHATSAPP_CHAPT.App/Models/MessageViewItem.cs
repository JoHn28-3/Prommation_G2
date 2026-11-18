#nullable enable

using System;
using WHATSAPP_CHAPT.Core.Models;

namespace WHATSAPP_CHAPT.App.Models
{
    /// <summary>
    /// Modèle immuable de présentation d'un message dans la liste de discussion.
    /// </summary>
    public sealed class MessageViewItem
    {
        /// <summary>
        /// Identifiant unique du message.
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// Identifiant de l'auteur du message.
        /// </summary>
        public Guid AuthorId { get; init; }

        /// <summary>
        /// Nom de l'auteur du message.
        /// </summary>
        public string AuthorName { get; init; } = string.Empty;

        /// <summary>
        /// Contenu texte du message.
        /// </summary>
        public string Text { get; init; } = string.Empty;

        /// <summary>
        /// Date et heure du message.
        /// </summary>
        public DateTimeOffset Timestamp { get; init; }

        /// <summary>
        /// Heure formatée pour affichage dans la bulle (HH:mm).
        /// </summary>
        public string Time { get; init; } = string.Empty;

        /// <summary>
        /// Statut d'acheminement du message.
        /// </summary>
        public MessageStatus Status { get; init; }

        /// <summary>
        /// Indique si le message provient de l'utilisateur actuel.
        /// </summary>
        public bool IsMine { get; init; }

        /// <summary>
        /// Indique si le message a fait l'objet d'une modification.
        /// </summary>
        public bool IsEdited { get; init; }

        /// <summary>
        /// Indique si le message est marqué comme supprimé.
        /// </summary>
        public bool IsDeleted { get; init; }

        /// <summary>
        /// Nom de l'auteur du message cité en réponse, le cas échéant.
        /// </summary>
        public string? ReplyAuthorName { get; init; }

        /// <summary>
        /// Aperçu du texte du message cité en réponse, le cas échéant.
        /// </summary>
        public string? ReplyPreview { get; init; }

        /// <summary>
        /// Indique si le nom de l'auteur doit être affiché au-dessus de la bulle (groupe).
        /// </summary>
        public bool ShowAuthorName { get; init; }
    }
}
