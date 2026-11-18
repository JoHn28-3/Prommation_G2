#nullable enable

namespace WHATSAPP_CHAPT.Core.Models
{
    /// <summary>
    /// Contact d'un utilisateur.
    /// </summary>
    public class Contact
    {
        /// <summary>
        /// Identifiant unique du contact.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Nom affiché du contact.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;
    }
}
