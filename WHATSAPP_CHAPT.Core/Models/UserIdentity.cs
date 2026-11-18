#nullable enable

namespace WHATSAPP_CHAPT.Core.Models
{
    /// <summary>
    /// Identité d'un utilisateur.
    /// </summary>
    public class UserIdentity
    {
        /// <summary>
        /// Identifiant unique.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Nom affiché.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;
    }
}
