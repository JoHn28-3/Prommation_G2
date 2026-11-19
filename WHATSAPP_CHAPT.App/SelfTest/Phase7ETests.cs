#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using WHATSAPP_CHAPT.App.Models;

namespace WHATSAPP_CHAPT.App.SelfTest
{
    /// <summary>
    /// Autotests pour la phase E (Recherche Ctrl+F, Notifications et Titre dynamique).
    /// </summary>
    public static class Phase7ETests
    {
        public static void Run(List<(string Name, bool Success, string? Detail)> results)
        {
            Test("TitreFenetre_FormatSansConversation", () =>
            {
                string title = FormatTitle(unreadCount: 0, convTitle: null, status: null);
                if (title != "WHATSAPP") throw new Exception($"Titre attendu 'WHATSAPP' mais reçu '{title}'");
            }, results);

            Test("TitreFenetre_FormatAvecNonLusEtContact", () =>
            {
                string title = FormatTitle(unreadCount: 3, convTitle: "Alice", status: "En ligne");
                if (title != "(3) WHATSAPP - Alice (En ligne)") throw new Exception($"Titre attendu '(3) WHATSAPP - Alice (En ligne)' mais reçu '{title}'");
            }, results);

            Test("TitreFenetre_FormatAppelEnCours", () =>
            {
                string title = FormatTitle(unreadCount: 0, convTitle: "Bob", status: "Appel audio en cours");
                if (title != "WHATSAPP - Bob (Appel audio en cours)") throw new Exception($"Titre attendu 'WHATSAPP - Bob (Appel audio en cours)' mais reçu '{title}'");
            }, results);

            Test("RechercheMessages_FiltreSensibleALaCasseEtDeleted", () =>
            {
                var messages = new List<MessageViewItem>
                {
                    new() { Id = Guid.NewGuid(), Text = "Bonjour tout le monde", IsDeleted = false },
                    new() { Id = Guid.NewGuid(), Text = "Rendez-vous à 14h", IsDeleted = false },
                    new() { Id = Guid.NewGuid(), Text = "bonjour de retour", IsDeleted = false },
                    new() { Id = Guid.NewGuid(), Text = "Bonjour supprimé", IsDeleted = true }
                };

                // Recherche insensible à la casse "bonjour"
                var matches = FilterMessages(messages, "bonjour");
                if (matches.Count != 2) throw new Exception($"Attendu 2 résultats mais obtenu {matches.Count}");
                if (matches.Any(m => m.IsDeleted)) throw new Exception("Un message supprimé a été retourné");
            }, results);

            Test("RechercheMessages_RechercheVideRetourneTout", () =>
            {
                var messages = new List<MessageViewItem>
                {
                    new() { Id = Guid.NewGuid(), Text = "Test 1" },
                    new() { Id = Guid.NewGuid(), Text = "Test 2" }
                };

                var matches = FilterMessages(messages, "");
                if (matches.Count != 2) throw new Exception("La recherche vide doit retourner tous les messages");
            }, results);
        }

        public static string FormatTitle(int unreadCount, string? convTitle, string? status)
        {
            string prefix = unreadCount > 0 ? $"({unreadCount}) " : "";
            if (string.IsNullOrEmpty(convTitle)) return $"{prefix}WHATSAPP";

            string statusPart = string.IsNullOrEmpty(status) ? "" : $" ({status})";
            return $"{prefix}WHATSAPP - {convTitle}{statusPart}";
        }

        public static List<MessageViewItem> FilterMessages(IEnumerable<MessageViewItem> items, string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return items.ToList();

            string q = query.Trim();
            return items
                .Where(m => !m.IsDeleted && m.Text.Contains(q, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private static void Test(string name, Action body, List<(string Name, bool Success, string? Detail)> results)
        {
            try
            {
                body();
                results.Add((name, true, null));
            }
            catch (Exception ex)
            {
                results.Add((name, false, ex.Message));
            }
        }
    }
}
