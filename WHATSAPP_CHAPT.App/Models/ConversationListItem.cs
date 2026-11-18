#nullable enable

using System;
using System.Drawing;
using System.Globalization;
using System.Text;
using WHATSAPP_CHAPT.App.Drawing;

namespace WHATSAPP_CHAPT.App.Models
{
    /// <summary>
    /// Modèle de présentation d'une conversation dans la liste.
    /// </summary>
    public sealed class ConversationListItem
    {
        /// <summary>
        /// Identifiant unique de la conversation.
        /// </summary>
        public Guid ConversationId { get; init; }

        /// <summary>
        /// Nom affiché de la conversation.
        /// </summary>
        public string DisplayName { get; init; } = string.Empty;

        /// <summary>
        /// Aperçu textuel du dernier message.
        /// </summary>
        public string LastMessagePreview { get; init; } = string.Empty;

        /// <summary>
        /// Date et heure du dernier message.
        /// </summary>
        public DateTimeOffset? LastMessageTimestamp { get; init; }

        /// <summary>
        /// Texte représentant l'heure ou la date du dernier message.
        /// </summary>
        public string TimeText { get; init; } = string.Empty;

        /// <summary>
        /// Nombre de messages non lus.
        /// </summary>
        public int UnreadCount { get; init; }

        /// <summary>
        /// Indique si le dernier message est supprimé.
        /// </summary>
        public bool IsDeleted { get; init; }

        /// <summary>
        /// Initiales calculées pour l'avatar.
        /// </summary>
        public string Initials { get; init; } = string.Empty;

        /// <summary>
        /// Couleur de fond de l'avatar.
        /// </summary>
        public Color AvatarColor { get; init; } = Theme.AvatarPalette[0];

        /// <summary>
        /// Calcule les initiales à partir d'un nom.
        /// </summary>
        /// <param name="name">Nom source.</param>
        /// <returns>Initiales (1 ou 2 lettres en majuscules).</returns>
        public static string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "?";
            }

            var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[1][0])}";
            }

            var single = parts[0];
            if (single.Length >= 2)
            {
                return $"{char.ToUpperInvariant(single[0])}{char.ToUpperInvariant(single[1])}";
            }

            return char.ToUpperInvariant(single[0]).ToString();
        }

        /// <summary>
        /// Obtient une couleur déterminée dans la palette pour un nom donné.
        /// </summary>
        /// <param name="name">Nom source.</param>
        /// <returns>Couleur correspondante.</returns>
        public static Color GetAvatarColor(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Theme.AvatarPalette[0];
            }

            int hash = Math.Abs(name.GetHashCode());
            return Theme.AvatarPalette[hash % Theme.AvatarPalette.Length];
        }

        /// <summary>
        /// Formate la date et heure du dernier message.
        /// </summary>
        /// <param name="timestamp">Horodatage du message.</param>
        /// <returns>Texte formaté (HH:mm, Hier ou jj/MM).</returns>
        public static string FormatTime(DateTimeOffset? timestamp)
        {
            if (!timestamp.HasValue)
            {
                return string.Empty;
            }

            var localDate = timestamp.Value.ToLocalTime().Date;
            var today = DateTime.Today;

            if (localDate == today)
            {
                return timestamp.Value.ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture);
            }

            if (localDate == today.AddDays(-1))
            {
                return "Hier";
            }

            return timestamp.Value.ToLocalTime().ToString("dd/MM", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Supprime les accents d'un texte pour la recherche insensible aux diacritiques.
        /// </summary>
        /// <param name="text">Texte d'entrée.</param>
        /// <returns>Texte sans accents.</returns>
        public static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);

            foreach (var ch in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(ch);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
