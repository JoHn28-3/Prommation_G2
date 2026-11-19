#nullable enable

using System;
using System.Collections.Generic;
using System.Drawing;
using WHATSAPP_CHAPT.App.Drawing;
using WHATSAPP_CHAPT.App.Models;
using WHATSAPP_CHAPT.Core.Models;

namespace WHATSAPP_CHAPT.App.SelfTest
{
    /// <summary>
    /// Autotests pour la phase F (Messagerie et Media).
    /// </summary>
    public static class Phase7FTests
    {
        public static void Run(List<(string Name, bool Success, string? Detail)> results)
        {
            Test("MessageMedia_ProprietesEtHasMedia", () =>
            {
                var msg = new MessageViewItem
                {
                    Id = Guid.NewGuid(),
                    Text = "Photo de vacances",
                    MediaPath = @"C:\photos\vacances.jpg",
                    MediaType = "image",
                    MediaSize = 1024 * 500
                };

                if (!msg.HasMedia) throw new Exception("HasMedia devrait être vrai");
                if (msg.MediaType != "image") throw new Exception($"MediaType incorrect: {msg.MediaType}");
                if (msg.MediaSize != 512000) throw new Exception("MediaSize incorrect");
            }, results);

            Test("MessageLayout_CalculMediaBounds", () =>
            {
                var engine = new MessageLayoutEngine();
                using var fontReg = new Font("Segoe UI", 9.5f);
                using var fontItalic = new Font("Segoe UI", 9.5f, FontStyle.Italic);
                using var fontAuthor = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                using var fontTime = new Font("Segoe UI", 7.5f);
                using var fontReply = new Font("Segoe UI", 8f);

                var items = new List<MessageViewItem>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Text = "Regarde ce document",
                        MediaPath = @"C:\docs\rapport.pdf",
                        MediaType = "document",
                        MediaSize = 2048,
                        Timestamp = DateTimeOffset.UtcNow,
                        Time = "12:00"
                    }
                };

                var (layoutItems, totalHeight) = engine.ComputeLayout(
                    items,
                    400,
                    x => x,
                    fontReg,
                    fontItalic,
                    fontAuthor,
                    fontTime,
                    fontReply);

                if (layoutItems.Count != 2) // DateSeparator + Bubble
                    throw new Exception($"Nombre d'éléments de layout inattendu: {layoutItems.Count}");

                var bubble = layoutItems[1] as MessageBubbleLayoutItem;
                if (bubble == null) throw new Exception("L'élément de bulle est null");
                if (bubble.MediaBounds.IsEmpty) throw new Exception("MediaBounds ne devrait pas être vide");
                if (bubble.MediaBounds.Height <= 0) throw new Exception("Hauteur de MediaBounds invalide");
            }, results);

            Test("MessageSansMedia_MediaBoundsEstVide", () =>
            {
                var engine = new MessageLayoutEngine();
                using var fontReg = new Font("Segoe UI", 9.5f);
                using var fontItalic = new Font("Segoe UI", 9.5f, FontStyle.Italic);
                using var fontAuthor = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                using var fontTime = new Font("Segoe UI", 7.5f);
                using var fontReply = new Font("Segoe UI", 8f);

                var items = new List<MessageViewItem>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Text = "Message texte simple",
                        Timestamp = DateTimeOffset.UtcNow,
                        Time = "12:01"
                    }
                };

                var (layoutItems, _) = engine.ComputeLayout(
                    items,
                    400,
                    x => x,
                    fontReg,
                    fontItalic,
                    fontAuthor,
                    fontTime,
                    fontReply);

                var bubble = layoutItems[1] as MessageBubbleLayoutItem;
                if (bubble == null) throw new Exception("Bulle introuvable");
                if (!bubble.MediaBounds.IsEmpty) throw new Exception("MediaBounds devrait être vide pour message sans média");
            }, results);
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
