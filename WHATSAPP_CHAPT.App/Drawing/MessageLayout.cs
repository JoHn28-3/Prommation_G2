#nullable enable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WHATSAPP_CHAPT.App.Models;

namespace WHATSAPP_CHAPT.App.Drawing
{
    /// <summary>
    /// Élément de base pour la disposition verticale de la liste de messages.
    /// </summary>
    public abstract class MessageLayoutItem
    {
        /// <summary>
        /// Coordonnée Y absolue dans la liste.
        /// </summary>
        public int Y { get; set; }

        /// <summary>
        /// Hauteur totale de l'élément.
        /// </summary>
        public int Height { get; set; }
    }

    /// <summary>
    /// Élément de séparation par date.
    /// </summary>
    public sealed class DateSeparatorLayoutItem : MessageLayoutItem
    {
        /// <summary>
        /// Texte du séparateur de date (Aujourd'hui, Hier ou jj/MM/aaaa).
        /// </summary>
        public string DateText { get; }

        /// <summary>
        /// Initialise une nouvelle instance de <see cref="DateSeparatorLayoutItem"/>.
        /// </summary>
        /// <param name="dateText">Texte de la date.</param>
        public DateSeparatorLayoutItem(string dateText)
        {
            DateText = dateText;
        }
    }

    /// <summary>
    /// Élément représentant une bulle de message.
    /// </summary>
    public sealed class MessageBubbleLayoutItem : MessageLayoutItem
    {
        /// <summary>
        /// Modèle du message associé.
        /// </summary>
        public MessageViewItem Message { get; }

        /// <summary>
        /// Rectangle complet de la bulle.
        /// </summary>
        public Rectangle BubbleBounds { get; set; }

        /// <summary>
        /// Rectangle du nom de l'auteur dans les groupes.
        /// </summary>
        public Rectangle AuthorBounds { get; set; }

        /// <summary>
        /// Rectangle du bloc de citation en réponse.
        /// </summary>
        public Rectangle ReplyBounds { get; set; }

        /// <summary>
        /// Rectangle de la zone média attachée.
        /// </summary>
        public Rectangle MediaBounds { get; set; }

        /// <summary>
        /// Rectangle du corps du texte.
        /// </summary>
        public Rectangle TextBounds { get; set; }

        /// <summary>
        /// Rectangle de la métadonnée (heure et coches).
        /// </summary>
        public Rectangle TimeBounds { get; set; }

        /// <summary>
        /// Couleur de l'auteur.
        /// </summary>
        public Color AuthorColor { get; set; }

        /// <summary>
        /// Texte affiché (normal ou « Message supprimé »).
        /// </summary>
        public string DisplayText { get; set; } = string.Empty;

        /// <summary>
        /// Texte d'heure formaté.
        /// </summary>
        public string TimeText { get; set; } = string.Empty;

        /// <summary>
        /// Initialise une nouvelle instance de <see cref="MessageBubbleLayoutItem"/>.
        /// </summary>
        /// <param name="message">Message associé.</param>
        public MessageBubbleLayoutItem(MessageViewItem message)
        {
            Message = message;
        }
    }

    /// <summary>
    /// Clé de cache pour la mise en page d'un message.
    /// </summary>
    public readonly record struct MessageLayoutCacheKey(
        Guid Id,
        int Width,
        string Text,
        bool IsEdited,
        bool IsDeleted,
        string? ReplyPreview,
        string? ReplyAuthorName,
        bool ShowAuthorName,
        string? MediaPath,
        string? MediaType);

    /// <summary>
    /// Calculateur et gestionnaire de cache pour la mise en page des messages.
    /// </summary>
    public sealed class MessageLayoutEngine
    {
        private readonly Dictionary<MessageLayoutCacheKey, (int BubbleWidth, int BubbleHeight, Size TextSize)> _sizeCache = new();

        /// <summary>
        /// Construit la clé de cache de mise en page pour un message donné.
        /// </summary>
        public static MessageLayoutCacheKey CreateCacheKey(MessageViewItem msg, int width)
        {
            string displayText = CreateDisplayText(msg);
            return new MessageLayoutCacheKey(
                msg.Id,
                width,
                displayText,
                msg.IsEdited,
                msg.IsDeleted,
                msg.ReplyPreview,
                msg.ReplyAuthorName,
                msg.ShowAuthorName,
                msg.MediaPath,
                msg.MediaType);
        }

        private static string CreateDisplayText(MessageViewItem msg)
        {
            if (msg.IsDeleted)
            {
                return "Message supprimé";
            }

            if (string.IsNullOrWhiteSpace(msg.Transcription))
            {
                return msg.Text;
            }

            string prefix = string.IsNullOrWhiteSpace(msg.Text) ? string.Empty : msg.Text + Environment.NewLine;
            return prefix + "Transcription : " + msg.Transcription;
        }

        /// <summary>
        /// Invalide l'ensemble du cache des mesures.
        /// </summary>
        public void InvalidateCache()
        {
            _sizeCache.Clear();
        }

        /// <summary>
        /// Calcule la disposition complète d'une liste de messages.
        /// </summary>
        /// <param name="messages">Liste des messages à afficher.</param>
        /// <param name="containerWidth">Largeur disponible pour le conteneur.</param>
        /// <param name="deviceUnitsConverter">Fonction de mise à l'échelle DPI.</param>
        /// <param name="fontRegular">Police normale du texte.</param>
        /// <param name="fontItalic">Police italique du texte.</param>
        /// <param name="fontAuthor">Police du nom d'auteur.</param>
        /// <param name="fontTime">Police de l'heure.</param>
        /// <param name="fontReply">Police de la citation.</param>
        /// <returns>Liste ordonnée des éléments de mise en page et hauteur totale.</returns>
        public (List<MessageLayoutItem> Items, int TotalHeight) ComputeLayout(
            IReadOnlyList<MessageViewItem> messages,
            int containerWidth,
            Func<int, int> deviceUnitsConverter,
            Font fontRegular,
            Font fontItalic,
            Font fontAuthor,
            Font fontTime,
            Font fontReply)
        {
            var result = new List<MessageLayoutItem>();
            if (messages == null || messages.Count == 0 || containerWidth <= 0)
            {
                return (result, 0);
            }

            int currentY = deviceUnitsConverter(12);
            int maxBubbleWidth = Math.Max(deviceUnitsConverter(150), (int)(containerWidth * 0.65f));
            int padH = deviceUnitsConverter(10);
            int padV = deviceUnitsConverter(6);
            int gap = deviceUnitsConverter(4);
            int marginSide = deviceUnitsConverter(16);
            int marginBottom = deviceUnitsConverter(6);

            DateTime? lastDate = null;

            for (int i = 0; i < messages.Count; i++)
            {
                var msg = messages[i];
                var localDate = msg.Timestamp.ToLocalTime().Date;

                // Séparateur de date
                if (!lastDate.HasValue || lastDate.Value != localDate)
                {
                    string dateText = FormatDateSeparator(localDate);
                    int sepHeight = deviceUnitsConverter(32);
                    var sepItem = new DateSeparatorLayoutItem(dateText)
                    {
                        Y = currentY,
                        Height = sepHeight
                    };
                    result.Add(sepItem);
                    currentY += sepHeight;
                    lastDate = localDate;
                }

                // Préparation du texte et des métadonnées
                string displayText = CreateDisplayText(msg);
                string timeText = (msg.IsEdited ? "modifié " : string.Empty) + msg.Time;
                var textFont = msg.IsDeleted ? fontItalic : fontRegular;

                int availableInnerWidth = maxBubbleWidth - (padH * 2);

                // Cache de mesure du contenu de la bulle
                var cacheKey = CreateCacheKey(msg, maxBubbleWidth);
                if (!_sizeCache.TryGetValue(cacheKey, out var cachedSize))
                {
                    var measuredSize = TextRenderer.MeasureText(
                        displayText,
                        textFont,
                        new Size(availableInnerWidth, int.MaxValue),
                        TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

                    int authorHeight = msg.ShowAuthorName ? deviceUnitsConverter(16) : 0;
                    int replyHeight = !string.IsNullOrEmpty(msg.ReplyPreview) ? deviceUnitsConverter(32) : 0;
                    int mediaHeight = msg.HasMedia
                        ? deviceUnitsConverter(msg.MediaType switch
                        {
                            "image" => 180,
                            "sticker" => 160,
                            _ => 40
                        })
                        : 0;
                    int timeHeight = deviceUnitsConverter(14);

                    int contentWidth = measuredSize.Width;

                    // Largeur minimale pour l'heure et la coche
                    int statusWidth = msg.IsMine ? deviceUnitsConverter(16) : 0;
                    var timeSize = TextRenderer.MeasureText(timeText, fontTime, Size.Empty, TextFormatFlags.NoPrefix);
                    int timeRowWidth = timeSize.Width + statusWidth + deviceUnitsConverter(4);

                    if (msg.ShowAuthorName)
                    {
                        var authorSize = TextRenderer.MeasureText(msg.AuthorName, fontAuthor, Size.Empty, TextFormatFlags.NoPrefix);
                        contentWidth = Math.Max(contentWidth, authorSize.Width);
                    }

                    if (!string.IsNullOrEmpty(msg.ReplyPreview))
                    {
                        contentWidth = Math.Max(contentWidth, deviceUnitsConverter(160));
                    }

                    if (msg.HasMedia)
                    {
                        contentWidth = Math.Max(contentWidth, deviceUnitsConverter(
                            msg.MediaType == "sticker" ? 160 : 220));
                    }

                    contentWidth = Math.Max(contentWidth, timeRowWidth);

                    int bWidth = Math.Min(maxBubbleWidth, contentWidth + (padH * 2));
                    int bHeight = (padV * 2) + authorHeight + (authorHeight > 0 ? gap : 0)
                                            + replyHeight + (replyHeight > 0 ? gap : 0)
                                            + mediaHeight + (mediaHeight > 0 ? gap : 0)
                                            + measuredSize.Height + gap + timeHeight;

                    cachedSize = (bWidth, bHeight, measuredSize);
                    _sizeCache[cacheKey] = cachedSize;
                }

                int bubbleWidth = cachedSize.BubbleWidth;
                int bubbleHeight = cachedSize.BubbleHeight;

                // Position X
                int bubbleX = msg.IsMine
                    ? containerWidth - bubbleWidth - marginSide
                    : marginSide;

                var bubbleRect = new Rectangle(bubbleX, currentY, bubbleWidth, bubbleHeight);
                int innerX = bubbleX + padH;
                int innerY = currentY + padV;

                var bubbleItem = new MessageBubbleLayoutItem(msg)
                {
                    Y = currentY,
                    Height = bubbleHeight + marginBottom,
                    BubbleBounds = bubbleRect,
                    DisplayText = displayText,
                    TimeText = timeText,
                    AuthorColor = GetStableAuthorColor(msg.AuthorId)
                };

                // Sous-rectangles
                if (msg.ShowAuthorName)
                {
                    int authorH = deviceUnitsConverter(16);
                    bubbleItem.AuthorBounds = new Rectangle(innerX, innerY, bubbleWidth - (padH * 2), authorH);
                    innerY += authorH + gap;
                }

                if (!string.IsNullOrEmpty(msg.ReplyPreview))
                {
                    int replyH = deviceUnitsConverter(32);
                    bubbleItem.ReplyBounds = new Rectangle(innerX, innerY, bubbleWidth - (padH * 2), replyH);
                    innerY += replyH + gap;
                }

                if (msg.HasMedia)
                {
                    int mediaH = deviceUnitsConverter(msg.MediaType switch
                    {
                        "image" => 180,
                        "sticker" => 160,
                        _ => 40
                    });
                    bubbleItem.MediaBounds = new Rectangle(innerX, innerY, bubbleWidth - (padH * 2), mediaH);
                    innerY += mediaH + gap;
                }

                bubbleItem.TextBounds = new Rectangle(innerX, innerY, bubbleWidth - (padH * 2), cachedSize.TextSize.Height);
                innerY += cachedSize.TextSize.Height + gap;

                int timeH = deviceUnitsConverter(14);
                bubbleItem.TimeBounds = new Rectangle(innerX, innerY, bubbleWidth - (padH * 2), timeH);

                result.Add(bubbleItem);
                currentY += bubbleHeight + marginBottom;
            }

            return (result, currentY + deviceUnitsConverter(16));
        }

        private static string FormatDateSeparator(DateTime date)
        {
            var today = DateTime.Today;
            if (date == today)
            {
                return "Aujourd'hui";
            }

            if (date == today.AddDays(-1))
            {
                return "Hier";
            }

            return date.ToString("dd/MM/yyyy");
        }

        /// <summary>
        /// Obtient une couleur d'auteur stable à partir du Guid sans utiliser string.GetHashCode.
        /// </summary>
        /// <param name="authorId">Identifiant de l'auteur.</param>
        /// <returns>Couleur assignée dans Theme.AuthorColors.</returns>
        public static Color GetStableAuthorColor(Guid authorId)
        {
            byte[] bytes = authorId.ToByteArray();
            int hash = BitConverter.ToInt32(bytes, 0) ^ BitConverter.ToInt32(bytes, 4);
            int index = Math.Abs(hash) % Theme.AuthorColors.Length;
            return Theme.AuthorColors[index];
        }
    }
}
