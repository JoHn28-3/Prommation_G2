#nullable enable

using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace WHATSAPP_CHAPT.App.Search
{
    /// <summary>
    /// Fournit des méthodes utilitaires pour la normalisation et la recherche textuelle avec tolérance aux fautes.
    /// </summary>
    public static class TextSearch
    {
        private static readonly char[] WordSeparators = { ' ', '\t', '\r', '\n', '-', '_', '\'', '.', ',' };

        /// <summary>
        /// Normalise une chaîne de caractères (suppression des accents, mise en minuscules, suppression des espaces superflus).
        /// </summary>
        /// <param name="text">Texte à normaliser.</param>
        /// <returns>Texte normalisé.</returns>
        public static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalizedString.Length);

            foreach (var c in normalizedString)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC).Trim().ToLowerInvariant();
        }

        /// <summary>
        /// Calcule la distance de Levenshtein entre deux chaînes.
        /// </summary>
        /// <param name="a">Première chaîne.</param>
        /// <param name="b">Deuxième chaîne.</param>
        /// <returns>Nombre minimal d'insertions, suppressions ou substitutions.</returns>
        public static int Levenshtein(string a, string b)
        {
            if (string.IsNullOrEmpty(a))
            {
                return b?.Length ?? 0;
            }

            if (string.IsNullOrEmpty(b))
            {
                return a.Length;
            }

            int n = a.Length;
            int m = b.Length;
            int[,] d = new int[n + 1, m + 1];

            for (int i = 0; i <= n; i++)
            {
                d[i, 0] = i;
            }

            for (int j = 0; j <= m; j++)
            {
                d[0, j] = j;
            }

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }

        /// <summary>
        /// Évalue la pertinence d'une recherche par rapport à un nom de conversation.
        /// </summary>
        /// <param name="query">Saisie de l'utilisateur.</param>
        /// <param name="name">Nom à comparer.</param>
        /// <returns>
        /// 0 si le nom commence par la saisie,
        /// 1 si un mot commence par la saisie,
        /// 2 si le nom contient la saisie,
        /// 3 en cas de correspondance avec tolérance aux fautes (Levenshtein),
        /// ou null si aucune correspondance.
        /// </returns>
        public static int? Score(string query, string name)
        {
            string q = Normalize(query);
            string n = Normalize(name);

            if (string.IsNullOrEmpty(q) || string.IsNullOrEmpty(n))
            {
                return null;
            }

            // Score 0 : le nom commence par la saisie
            if (n.StartsWith(q, StringComparison.Ordinal))
            {
                return 0;
            }

            var words = n.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries);

            // Score 1 : un mot du nom commence par la saisie
            if (words.Any(w => w.StartsWith(q, StringComparison.Ordinal)))
            {
                return 1;
            }

            // Score 2 : le nom contient la saisie
            if (n.Contains(q, StringComparison.Ordinal))
            {
                return 2;
            }

            // Score 3 : tolérance aux fautes (Levenshtein) uniquement si ce n'est pas un score 0 à 2
            if (q.Length >= 3)
            {
                int threshold = q.Length <= 4 ? 1 : 2;

                foreach (var word in words)
                {
                    if (Levenshtein(q, word) <= threshold)
                    {
                        return 3;
                    }
                }
            }

            return null;
        }
    }
}
