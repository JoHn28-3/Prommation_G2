#nullable enable

namespace WHATSAPP_CHAPT.Core.Models
{
    /// <summary>
    /// Conversation entre utilisateurs.
    /// </summary>
    public class Conversation
    {
        /// <summary>
        /// Identifiant unique de la conversation.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Type de conversation (directe ou groupe).
        /// </summary>
        public ConversationType Type { get; set; }

        /// <summary>
        /// Titre de la conversation (null pour les discussions directes).
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Membres participants.
        /// </summary>
        public IReadOnlyList<Member> Members { get; set; } = new List<Member>();

        /// <summary>
        /// Identifiants des membres de la conversation.
        /// </summary>
        public IReadOnlyList<Guid> MemberIds => Members.Select(m => m.UserId).ToList();

        /// <summary>
        /// Indique si la conversation est archivée (Phase I).
        /// </summary>
        public bool IsArchived { get; set; }
    }
}
