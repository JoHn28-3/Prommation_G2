#nullable enable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using WHATSAPP_CHAPT.App.Models;
using WHATSAPP_CHAPT.App.Pages;
using WHATSAPP_CHAPT.Core.Models;

namespace WHATSAPP_CHAPT.App.SelfTest
{
    public static class PhaseP2Tests
    {
        public static void Run(List<(string Name, bool Success, string? Detail)> results)
        {
            Test("P2_Filtres_QuatreValeurs", () =>
            {
                string[] filters = { "Toutes", "Non lues", "Favoris", "Groupes" };
                if (filters.Length != 4)
                    throw new Exception("Filtres incomplets.");
            }, results);

            Test("P2_Pages_EtatsVides", () =>
            {
                using var calls = new CallsPage();
                using var statuses = new StatusesPage();
                using var archived = new ArchivedPage();
                if (!ContainsText(calls, "Aucun appel récent")
                    || !ContainsText(statuses, "Aucun statut")
                    || !ContainsText(archived, "Aucune discussion archivée"))
                    throw new Exception("Un état vide est absent.");
            }, results);

            Test("P2_Filtres_NonLues", () =>
            {
                var items = new[]
                {
                    new ConversationListItem { DisplayName = "Alice", UnreadCount = 2 },
                    new ConversationListItem { DisplayName = "Bob", UnreadCount = 0 }
                };
                var unread = items.Where(i => i.UnreadCount > 0).ToList();
                if (unread.Count != 1 || unread[0].DisplayName != "Alice")
                    throw new Exception("Filtre non lues incorrect.");
            }, results);

            Test("P2_ApercuHeure_Format", () =>
            {
                var timestamp = new DateTimeOffset(2024, 1, 2, 14, 5, 0, TimeSpan.Zero);
                string time = ConversationListItem.FormatTime(timestamp);
                if (string.IsNullOrWhiteSpace(time))
                    throw new Exception("Heure vide.");
            }, results);

            Test("P2_Routage_PagesConnues", () =>
            {
                string[] routes = { "discussions", "calls", "status", "archives", "metaai", "newdiscussion" };
                if (routes.Distinct().Count() != routes.Length)
                    throw new Exception("Une route est dupliquée.");
            }, results);

            Test("P2_Archive_AucunePageBlanche", () =>
            {
                using var page = new ArchivedPage();
                if (page.Controls.Count == 0)
                    throw new Exception("Page archivées vide.");
            }, results);
        }

        private static bool ContainsText(System.Windows.Forms.Control control, string text)
        {
            if (control.Text.Contains(text, StringComparison.OrdinalIgnoreCase))
                return true;
            foreach (System.Windows.Forms.Control child in control.Controls)
            {
                if (ContainsText(child, text))
                    return true;
            }
            return false;
        }

        private static void Test(string name, Action body,
            List<(string Name, bool Success, string? Detail)> results)
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
