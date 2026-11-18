#nullable enable

namespace WHATSAPP_CHAPT.Core.Models
{
    /// <summary>
    /// Membre d'une conversation.
    /// </summary>
    public class Member
    {
        /// <summary>
        /// Identifiant de l'utilisateur.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Rôle du membre.
        /// </summary>
        public MemberRole Role { get; set; }
    }
}
