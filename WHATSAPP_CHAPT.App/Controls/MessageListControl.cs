#nullable enable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WHATSAPP_CHAPT.App.Drawing;
using WHATSAPP_CHAPT.App.Models;
using WHATSAPP_CHAPT.Core.Models;

namespace WHATSAPP_CHAPT.App.Controls
{
    /// <summary>
    /// Contrôle personnalisé affichant la liste des messages sous forme de bulles avec défilement et virtualisation.
    /// </summary>
    public sealed class MessageListControl : UserControl
    {
        private readonly VScrollBar _vScrollBar;
        private readonly MessageLayoutEngine _layoutEngine = new();

        private IReadOnlyList<MessageViewItem> _messages = Array.Empty<MessageViewItem>();
        private IReadOnlyList<MessageViewItem> _rawMessages = Array.Empty<MessageViewItem>();
        private string? _searchQuery;
        private List<MessageLayoutItem> _layoutItems = new();
        private int _totalHeight;
        private int _scrollOffset;
        private int _newMessagesBelow;
        private Rectangle _scrollDownButtonRect;

        private Font _fontRegular;
        private Font _fontItalic;
        private Font _fontAuthor;
        private Font _fontTime;
        private Font _fontReplyName;
        private Font _fontReplyText;
        private Font _fontDate;
        private Font _fontBadge;
        private Font _fontArrow;

        /// <summary>
        /// Événement levé lors d'un clic droit sur un message.
        /// </summary>
        public event Action<Guid, Point>? MessageRightClicked;

        /// <summary>
        /// Initialise une nouvelle instance de <see cref="MessageListControl"/>.
        /// </summary>
        public MessageListControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            DoubleBuffered = true;
            BackColor = Theme.AppBackground;

            _fontRegular = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            _fontItalic = new Font("Segoe UI", 9.5f, FontStyle.Italic);
            _fontAuthor = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            _fontTime = new Font("Segoe UI", 7.5f, FontStyle.Regular);
            _fontReplyName = new Font("Segoe UI", 8f, FontStyle.Bold);
            _fontReplyText = new Font("Segoe UI", 8f, FontStyle.Regular);
            _fontDate = new Font("Segoe UI", 8f, FontStyle.Regular);
            _fontBadge = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            _fontArrow = new Font("Segoe UI", 12f, FontStyle.Bold);

            _vScrollBar = new VScrollBar
            {
                Dock = DockStyle.Right,
                Visible = false
            };
            _vScrollBar.Scroll += (s, e) =>
            {
                _scrollOffset = _vScrollBar.Value;
                CheckIfReachedBottom();
                Invalidate();
            };

            Controls.Add(_vScrollBar);
        }

        /// <summary>
        /// Met à jour la liste des messages affichés.
        /// </summary>
        /// <param name="messages">Liste des messages.</param>
        /// <param name="isNewConversation">Indique s'il s'agit d'une nouvelle conversation (défilement immédiat en bas).</param>
        public void SetMessages(IReadOnlyList<MessageViewItem> messages, bool isNewConversation = false)
        {
            int previousCount = _messages.Count;
            int maxScrollBefore = Math.Max(0, _totalHeight - Height);
            bool wasAtBottom = isNewConversation || (_scrollOffset >= maxScrollBefore - LogicalToDeviceUnits(30));

            _rawMessages = messages ?? Array.Empty<MessageViewItem>();
            ApplySearchFilter();

            RecalculateLayout();

            int newMaxScroll = Math.Max(0, _totalHeight - Height);

            if (wasAtBottom)
            {
                _scrollOffset = newMaxScroll;
                _newMessagesBelow = 0;
            }
            else
            {
                int diff = _messages.Count - previousCount;
                if (diff > 0)
                {
                    _newMessagesBelow += diff;
                }
                _scrollOffset = Math.Min(_scrollOffset, newMaxScroll);
            }

            UpdateScrollbar();
            Invalidate();
        }

        /// <summary>
        /// Applique un filtre de recherche instantanée sur les messages (Ctrl+F).
        /// </summary>
        public void SetSearchQuery(string? query)
        {
            _searchQuery = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
            ApplySearchFilter();
            RecalculateLayout();
            UpdateScrollbar();
            Invalidate();
        }

        private void ApplySearchFilter()
        {
            if (string.IsNullOrEmpty(_searchQuery))
            {
                _messages = _rawMessages;
            }
            else
            {
                var q = _searchQuery;
                var filtered = new List<MessageViewItem>();
                foreach (var m in _rawMessages)
                {
                    if (!m.IsDeleted && m.Text.Contains(q, StringComparison.OrdinalIgnoreCase))
                    {
                        filtered.Add(m);
                    }
                }
                _messages = filtered;
            }
        }

        private void RecalculateLayout()
        {
            int scrollbarWidth = _vScrollBar.Visible ? _vScrollBar.Width : 0;
            int contentWidth = Width - scrollbarWidth;

            var (items, totalH) = _layoutEngine.ComputeLayout(
                _messages,
                contentWidth,
                LogicalToDeviceUnits,
                _fontRegular,
                _fontItalic,
                _fontAuthor,
                _fontTime,
                _fontReplyText);

            _layoutItems = items;
            _totalHeight = totalH;
        }

        private void UpdateScrollbar()
        {
            int clientHeight = Height;
            if (_totalHeight > clientHeight && clientHeight > 0)
            {
                _vScrollBar.Visible = true;
                _vScrollBar.Minimum = 0;
                _vScrollBar.Maximum = _totalHeight;
                _vScrollBar.LargeChange = clientHeight;
                _vScrollBar.SmallChange = LogicalToDeviceUnits(30);

                int maxScroll = Math.Max(0, _totalHeight - clientHeight);
                _scrollOffset = Math.Clamp(_scrollOffset, 0, maxScroll);
                _vScrollBar.Value = Math.Min(_scrollOffset, _vScrollBar.Maximum);
            }
            else
            {
                _vScrollBar.Visible = false;
                _scrollOffset = 0;
                _vScrollBar.Value = 0;
            }

            UpdateScrollDownButtonPosition();
        }

        private void UpdateScrollDownButtonPosition()
        {
            int btnSize = LogicalToDeviceUnits(40);
            int scrollbarWidth = _vScrollBar.Visible ? _vScrollBar.Width : 0;
            int x = Width - scrollbarWidth - btnSize - LogicalToDeviceUnits(16);
            int y = Height - btnSize - LogicalToDeviceUnits(16);
            _scrollDownButtonRect = new Rectangle(x, y, btnSize, btnSize);
        }

        private void CheckIfReachedBottom()
        {
            int maxScroll = Math.Max(0, _totalHeight - Height);
            if (_scrollOffset >= maxScroll - LogicalToDeviceUnits(15))
            {
                _newMessagesBelow = 0;
            }
        }

        /// <inheritdoc/>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            _layoutEngine.InvalidateCache();
            RecalculateLayout();
            UpdateScrollbar();
            Invalidate();
        }

        /// <inheritdoc/>
        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            _layoutEngine.InvalidateCache();
            RecalculateLayout();
            UpdateScrollbar();
            Invalidate();
        }

        /// <inheritdoc/>
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            int maxScroll = Math.Max(0, _totalHeight - Height);
            if (maxScroll <= 0)
            {
                return;
            }

            int delta = -(e.Delta / 120) * LogicalToDeviceUnits(40);
            _scrollOffset = Math.Clamp(_scrollOffset + delta, 0, maxScroll);

            if (_vScrollBar.Visible)
            {
                _vScrollBar.Value = Math.Min(_scrollOffset, _vScrollBar.Maximum);
            }

            CheckIfReachedBottom();
            Invalidate();
        }

        /// <inheritdoc/>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && _newMessagesBelow > 0 && _scrollDownButtonRect.Contains(e.Location))
            {
                ScrollToBottom();
                return;
            }

            if (e.Button == MouseButtons.Right)
            {
                int targetY = e.Y + _scrollOffset;
                int firstIndex = FindFirstVisibleIndex(targetY);
                if (firstIndex >= 0)
                {
                    for (int i = firstIndex; i < _layoutItems.Count; i++)
                    {
                        var item = _layoutItems[i];
                        if (item.Y > targetY)
                        {
                            break;
                        }

                        if (item is MessageBubbleLayoutItem bubble && bubble.BubbleBounds.Contains(e.X, targetY))
                        {
                            MessageRightClicked?.Invoke(bubble.Message.Id, e.Location);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Fait défiler la liste des messages jusqu'au bas.
        /// </summary>
        public void ScrollToBottom()
        {
            int maxScroll = Math.Max(0, _totalHeight - Height);
            _scrollOffset = maxScroll;
            _newMessagesBelow = 0;
            if (_vScrollBar.Visible)
            {
                _vScrollBar.Value = Math.Min(_scrollOffset, _vScrollBar.Maximum);
            }
            Invalidate();
        }

        private int FindFirstVisibleIndex(int targetY)
        {
            if (_layoutItems.Count == 0)
            {
                return -1;
            }

            int low = 0;
            int high = _layoutItems.Count - 1;
            int result = -1;

            while (low <= high)
            {
                int mid = (low + high) / 2;
                var item = _layoutItems[mid];

                if (item.Y + item.Height >= targetY)
                {
                    result = mid;
                    high = mid - 1;
                }
                else
                {
                    low = mid + 1;
                }
            }

            return result >= 0 ? result : 0;
        }

        /// <inheritdoc/>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.Clear(Theme.AppBackground);

            if (_messages.Count == 0)
            {
                TextRenderer.DrawText(g, "Aucun message", _fontRegular, ClientRectangle, Theme.TextSecondary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                return;
            }

            int firstIndex = FindFirstVisibleIndex(_scrollOffset);
            if (firstIndex < 0)
            {
                return;
            }

            int clientBottom = _scrollOffset + Height;
            int radius = LogicalToDeviceUnits(12);

            for (int i = firstIndex; i < _layoutItems.Count; i++)
            {
                var item = _layoutItems[i];
                if (item.Y > clientBottom)
                {
                    break;
                }

                if (item is DateSeparatorLayoutItem dateSep)
                {
                    DrawDateSeparator(g, dateSep);
                }
                else if (item is MessageBubbleLayoutItem bubble)
                {
                    DrawMessageBubble(g, bubble, radius);
                }
            }

            // Bouton rond « ↓ » avec compteur de nouveaux messages
            if (_newMessagesBelow > 0)
            {
                DrawScrollDownButton(g);
            }
        }

        private void DrawDateSeparator(Graphics g, DateSeparatorLayoutItem sep)
        {
            int y = sep.Y - _scrollOffset;
            var textSize = TextRenderer.MeasureText(g, sep.DateText, _fontDate, Size.Empty, TextFormatFlags.NoPrefix);
            int pillWidth = textSize.Width + LogicalToDeviceUnits(20);
            int pillHeight = LogicalToDeviceUnits(22);
            int pillX = (Width - (_vScrollBar.Visible ? _vScrollBar.Width : 0) - pillWidth) / 2;
            int pillY = y + (sep.Height - pillHeight) / 2;

            var pillRect = new Rectangle(pillX, pillY, pillWidth, pillHeight);
            DrawRoundedRectangle(g, pillRect, pillHeight / 2, Theme.Header);

            TextRenderer.DrawText(g, sep.DateText, _fontDate, pillRect, Theme.TextSecondary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        private void DrawMessageBubble(Graphics g, MessageBubbleLayoutItem bubble, int radius)
        {
            int offset = _scrollOffset;
            var bRect = new Rectangle(bubble.BubbleBounds.X, bubble.BubbleBounds.Y - offset, bubble.BubbleBounds.Width, bubble.BubbleBounds.Height);

            Color bubbleColor = bubble.Message.IsMine ? Theme.BubbleOutgoing : Theme.BubbleIncoming;
            DrawRoundedRectangle(g, bRect, radius, bubbleColor);

            // 1. Nom de l'auteur dans les groupes
            if (bubble.Message.ShowAuthorName && !bubble.AuthorBounds.IsEmpty)
            {
                var aRect = new Rectangle(bubble.AuthorBounds.X, bubble.AuthorBounds.Y - offset, bubble.AuthorBounds.Width, bubble.AuthorBounds.Height);
                TextRenderer.DrawText(g, bubble.Message.AuthorName, _fontAuthor, aRect, bubble.AuthorColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }

            // 2. Bloc de citation en réponse
            if (!bubble.ReplyBounds.IsEmpty && !string.IsNullOrEmpty(bubble.Message.ReplyPreview))
            {
                var rRect = new Rectangle(bubble.ReplyBounds.X, bubble.ReplyBounds.Y - offset, bubble.ReplyBounds.Width, bubble.ReplyBounds.Height);

                Color replyBarColor = bubble.Message.IsMine ? Theme.OutgoingMeta : Theme.Accent;
                Color replyBg = bubble.Message.IsMine ? Theme.BubbleOutgoingReplyBackground : Theme.Hover;

                DrawRoundedRectangle(g, rRect, LogicalToDeviceUnits(4), replyBg);

                // Barre verticale gauche
                int barW = LogicalToDeviceUnits(3);
                using (var brush = new SolidBrush(replyBarColor))
                {
                    g.FillRectangle(brush, rRect.X, rRect.Y, barW, rRect.Height);
                }

                int innerReplyX = rRect.X + barW + LogicalToDeviceUnits(6);
                int innerReplyW = rRect.Width - barW - LogicalToDeviceUnits(10);
                int halfH = rRect.Height / 2;

                var replyNameRect = new Rectangle(innerReplyX, rRect.Y + LogicalToDeviceUnits(2), innerReplyW, halfH);
                TextRenderer.DrawText(g, bubble.Message.ReplyAuthorName ?? "Message", _fontReplyName, replyNameRect, replyBarColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                var replyPreviewRect = new Rectangle(innerReplyX, rRect.Y + halfH, innerReplyW, halfH - LogicalToDeviceUnits(2));
                Color replyTextColor = bubble.Message.IsMine ? Theme.OutgoingText : Theme.TextSecondary;
                TextRenderer.DrawText(g, bubble.Message.ReplyPreview, _fontReplyText, replyPreviewRect, replyTextColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }

            // 2b. Zone de média attachée (Phase F)
            if (bubble.Message.HasMedia && !bubble.MediaBounds.IsEmpty)
            {
                var mRect = new Rectangle(bubble.MediaBounds.X, bubble.MediaBounds.Y - offset, bubble.MediaBounds.Width, bubble.MediaBounds.Height);
                Color mediaBg = Color.FromArgb(40, 0, 0, 0);
                DrawRoundedRectangle(g, mRect, LogicalToDeviceUnits(4), mediaBg);

                string mediaFileName = System.IO.Path.GetFileName(bubble.Message.MediaPath) ?? "Fichier";
                string mediaLabel = bubble.Message.MediaType switch
                {
                    "image" => $"Image  {mediaFileName}",
                    "video" => $"Vidéo  {mediaFileName}",
                    "audio" => $"Audio  {mediaFileName}",
                    _ => $"Fichier  {mediaFileName}"
                };

                TextRenderer.DrawText(g, mediaLabel, _fontRegular, mRect, Theme.Accent,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }

            // 3. Corps du texte
            var tRect = new Rectangle(bubble.TextBounds.X, bubble.TextBounds.Y - offset, bubble.TextBounds.Width, bubble.TextBounds.Height);
            Color textColor = bubble.Message.IsDeleted
                ? Theme.TextSecondary
                : (bubble.Message.IsMine ? Theme.OutgoingText : Theme.TextPrimary);

            var fontToUse = bubble.Message.IsDeleted ? _fontItalic : _fontRegular;
            TextRenderer.DrawText(g, bubble.DisplayText, fontToUse, tRect, textColor,
                TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

            // 4. Heure et statut en bas à droite
            var timeRect = new Rectangle(bubble.TimeBounds.X, bubble.TimeBounds.Y - offset, bubble.TimeBounds.Width, bubble.TimeBounds.Height);
            Color metaColor = bubble.Message.IsMine ? Theme.OutgoingMeta : Theme.TextSecondary;

            var timeSize = TextRenderer.MeasureText(g, bubble.TimeText, _fontTime, Size.Empty, TextFormatFlags.NoPrefix);
            int statusWidth = bubble.Message.IsMine ? LogicalToDeviceUnits(16) : 0;
            int timeX = timeRect.Right - timeSize.Width - statusWidth;

            var textTimeRect = new Rectangle(timeX, timeRect.Y, timeSize.Width, timeRect.Height);
            TextRenderer.DrawText(g, bubble.TimeText, _fontTime, textTimeRect, metaColor,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            if (bubble.Message.IsMine)
            {
                int iconX = timeRect.Right - statusWidth + LogicalToDeviceUnits(2);
                int iconY = timeRect.Y + (timeRect.Height - LogicalToDeviceUnits(10)) / 2;
                DrawStatusIcon(g, bubble.Message.Status, iconX, iconY, metaColor);
            }
        }

        private void DrawStatusIcon(Graphics g, MessageStatus status, int x, int y, Color metaColor)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (status == MessageStatus.Pending)
            {
                // Petite horloge
                int d = LogicalToDeviceUnits(9);
                using var pen = new Pen(metaColor, 1.2f);
                g.DrawEllipse(pen, x, y, d, d);
                int cx = x + (d / 2);
                int cy = y + (d / 2);
                g.DrawLine(pen, cx, cy, cx, cy - LogicalToDeviceUnits(3));
                g.DrawLine(pen, cx, cy, cx + LogicalToDeviceUnits(3), cy);
            }
            else if (status == MessageStatus.Sent)
            {
                // Une seule coche
                using var pen = new Pen(metaColor, 1.5f);
                DrawCheckmark(g, pen, x, y);
            }
            else if (status == MessageStatus.Delivered)
            {
                // Deux coches grises/métadonnées
                using var pen = new Pen(metaColor, 1.5f);
                DrawCheckmark(g, pen, x, y);
                DrawCheckmark(g, pen, x + LogicalToDeviceUnits(4), y);
            }
            else if (status == MessageStatus.Read)
            {
                // Deux coches en TickRead
                using var pen = new Pen(Theme.TickRead, 1.5f);
                DrawCheckmark(g, pen, x, y);
                DrawCheckmark(g, pen, x + LogicalToDeviceUnits(4), y);
            }
        }

        private void DrawCheckmark(Graphics g, Pen pen, int x, int y)
        {
            int w = LogicalToDeviceUnits(8);
            int h = LogicalToDeviceUnits(8);
            var points = new[]
            {
                new Point(x, y + (h / 2)),
                new Point(x + (w / 3), y + h),
                new Point(x + w, y + 1)
            };
            g.DrawLines(pen, points);
        }

        private void DrawScrollDownButton(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(Theme.Header);
            using var pen = new Pen(Theme.Separator, LogicalToDeviceUnits(1));

            g.FillEllipse(brush, _scrollDownButtonRect);
            g.DrawEllipse(pen, _scrollDownButtonRect);

            // Flèche « ↓ »
            TextRenderer.DrawText(g, "↓", _fontArrow, _scrollDownButtonRect, Theme.TextPrimary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            // Badge avec nombre de nouveaux messages
            int badgeSize = LogicalToDeviceUnits(18);
            var badgeRect = new Rectangle(
                _scrollDownButtonRect.Right - (badgeSize / 2) - LogicalToDeviceUnits(6),
                _scrollDownButtonRect.Top - LogicalToDeviceUnits(4),
                badgeSize,
                badgeSize);

            DrawRoundedRectangle(g, badgeRect, badgeSize / 2, Theme.Accent);
            string badgeText = _newMessagesBelow > 99 ? "99+" : _newMessagesBelow.ToString();
            TextRenderer.DrawText(g, badgeText, _fontBadge, badgeRect, Theme.AccentText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        private static void DrawRoundedRectangle(Graphics g, Rectangle bounds, int radius, Color fillColor)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            using var path = new GraphicsPath();
            int diameter = radius * 2;
            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(fillColor);
            g.FillPath(brush, path);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fontRegular.Dispose();
                _fontItalic.Dispose();
                _fontAuthor.Dispose();
                _fontTime.Dispose();
                _fontReplyName.Dispose();
                _fontReplyText.Dispose();
                _fontDate.Dispose();
                _fontBadge.Dispose();
                _fontArrow.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
