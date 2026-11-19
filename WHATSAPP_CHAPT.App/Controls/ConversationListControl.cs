#nullable enable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using WHATSAPP_CHAPT.App.Drawing;
using WHATSAPP_CHAPT.App.Models;
using WHATSAPP_CHAPT.App.Search;
using WHATSAPP_CHAPT.App.Services;

namespace WHATSAPP_CHAPT.App.Controls
{
    /// <summary>
    /// Contrôle personnalisé affichant la liste des conversations avec dessin optimisé.
    /// </summary>
    public sealed class ConversationListControl : UserControl
    {
        private readonly VScrollBar _vScrollBar;
        private readonly AvatarCache _avatarCache;

        private List<ConversationListItem> _allItems = new();
        private List<ConversationListItem> _filteredItems = new();
        private Guid? _selectedConversationId;
        private int _hoveredIndex = -1;
        private int _scrollOffset;
        private string _searchQuery = string.Empty;

        private Font _nameFont;
        private Font _previewFont;
        private Font _previewItalicFont;
        private Font _timeFont;
        private Font _badgeFont;
        private Font _emptyFont;

        /// <summary>
        /// Événement levé lors de la sélection d'une conversation.
        /// </summary>
        public event EventHandler<Guid>? SelectedConversationChanged;

        /// <summary>
        /// Événement levé lors d'un clic droit sur une conversation.
        /// </summary>
        public event Action<Guid, Point>? ConversationRightClicked;

        /// <summary>
        /// Initialise une nouvelle instance de <see cref="ConversationListControl"/>.
        /// </summary>
        /// <param name="avatarProvider">Fournisseur de photos de profil (optionnel).</param>
        public ConversationListControl(IAvatarProvider? avatarProvider = null)
        {
            _avatarCache = new AvatarCache(avatarProvider);

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            DoubleBuffered = true;
            BackColor = Theme.Panel;

            _nameFont = new Font("Segoe UI", 9.75f, FontStyle.Bold);
            _previewFont = new Font("Segoe UI", 9f, FontStyle.Regular);
            _previewItalicFont = new Font("Segoe UI", 9f, FontStyle.Italic);
            _timeFont = new Font("Segoe UI", 8.25f, FontStyle.Regular);
            _badgeFont = new Font("Segoe UI", 8f, FontStyle.Bold);
            _emptyFont = new Font("Segoe UI", 10f, FontStyle.Regular);

            _vScrollBar = new VScrollBar
            {
                Dock = DockStyle.Right,
                Visible = false
            };
            _vScrollBar.Scroll += (s, e) =>
            {
                _scrollOffset = _vScrollBar.Value;
                Invalidate();
            };

            Controls.Add(_vScrollBar);
        }

        /// <summary>
        /// Met à jour la liste des conversations et applique le tri par date décroissante.
        /// </summary>
        /// <param name="items">Éléments de conversation.</param>
        /// <param name="preserveSelection">Indique s'il faut conserver la conversation sélectionnée.</param>
        public void SetItems(IEnumerable<ConversationListItem> items, bool preserveSelection = true)
        {
            _allItems = items.OrderByDescending(i => i.LastMessageTimestamp.GetValueOrDefault()).ToList();

            if (!preserveSelection)
            {
                _selectedConversationId = null;
            }

            ApplyFilter();
        }

        /// <summary>
        /// Applique un filtre de recherche insensible à la casse et aux accents.
        /// </summary>
        /// <param name="query">Terme de recherche.</param>
        public void SetSearchFilter(string query)
        {
            _searchQuery = query ?? string.Empty;
            ApplyFilter();
        }

        /// <summary>
        /// Sélectionne une conversation par son identifiant.
        /// </summary>
        /// <param name="conversationId">Identifiant de la conversation.</param>
        public void SelectConversation(Guid conversationId)
        {
            _selectedConversationId = conversationId;
            Invalidate();
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(_searchQuery))
            {
                _filteredItems = new List<ConversationListItem>(_allItems);
            }
            else
            {
                var scored = _allItems
                    .Select(item => (Item: item, Score: TextSearch.Score(_searchQuery, item.DisplayName)))
                    .Where(x => x.Score.HasValue)
                    .Select(x => (x.Item, Score: x.Score!.Value))
                    .ToList();

                bool hasExactOrPartialMatches = scored.Any(x => x.Score <= 2);
                if (hasExactOrPartialMatches)
                {
                    scored = scored.Where(x => x.Score <= 2).ToList();
                }

                _filteredItems = scored
                    .OrderBy(x => x.Score)
                    .ThenByDescending(x => x.Item.LastMessageTimestamp.GetValueOrDefault())
                    .Select(x => x.Item)
                    .ToList();
            }

            UpdateScrollParameters();
            Invalidate();
        }

        private void UpdateScrollParameters()
        {
            int itemHeight = LogicalToDeviceUnits(72);
            int totalHeight = _filteredItems.Count * itemHeight;
            int clientHeight = Height;

            if (totalHeight > clientHeight && clientHeight > 0)
            {
                _vScrollBar.Visible = true;
                _vScrollBar.Minimum = 0;
                _vScrollBar.Maximum = totalHeight;
                _vScrollBar.LargeChange = clientHeight;
                _vScrollBar.SmallChange = itemHeight;

                int maxScroll = Math.Max(0, totalHeight - clientHeight);
                if (_scrollOffset > maxScroll)
                {
                    _scrollOffset = maxScroll;
                }
                _vScrollBar.Value = Math.Min(_scrollOffset, _vScrollBar.Maximum);
            }
            else
            {
                _vScrollBar.Visible = false;
                _scrollOffset = 0;
                _vScrollBar.Value = 0;
            }
        }

        /// <inheritdoc/>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateScrollParameters();
            Invalidate();
        }

        /// <inheritdoc/>
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            int itemHeight = LogicalToDeviceUnits(72);
            int totalHeight = _filteredItems.Count * itemHeight;
            int maxScroll = Math.Max(0, totalHeight - Height);

            if (maxScroll <= 0)
            {
                return;
            }

            int delta = -(e.Delta / 120) * itemHeight;
            _scrollOffset = Math.Clamp(_scrollOffset + delta, 0, maxScroll);

            if (_vScrollBar.Visible)
            {
                _vScrollBar.Value = Math.Min(_scrollOffset, _vScrollBar.Maximum);
            }

            UpdateHover(e.Location);
            Invalidate();
        }

        /// <inheritdoc/>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            UpdateHover(e.Location);
        }

        private void UpdateHover(Point location)
        {
            int itemHeight = LogicalToDeviceUnits(72);
            int scrollbarWidth = _vScrollBar.Visible ? _vScrollBar.Width : 0;

            if (location.X >= 0 && location.X <= Width - scrollbarWidth && location.Y >= 0 && location.Y <= Height)
            {
                int index = (location.Y + _scrollOffset) / itemHeight;
                if (index >= 0 && index < _filteredItems.Count)
                {
                    Cursor = Cursors.Hand;
                    if (_hoveredIndex != index)
                    {
                        _hoveredIndex = index;
                        Invalidate();
                    }
                    return;
                }
            }

            Cursor = Cursors.Default;
            if (_hoveredIndex != -1)
            {
                _hoveredIndex = -1;
                Invalidate();
            }
        }

        /// <inheritdoc/>
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoveredIndex != -1)
            {
                _hoveredIndex = -1;
                Cursor = Cursors.Default;
                Invalidate();
            }
        }

        /// <inheritdoc/>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            int itemHeight = LogicalToDeviceUnits(72);
            int scrollbarWidth = _vScrollBar.Visible ? _vScrollBar.Width : 0;

            if (e.X >= 0 && e.X <= Width - scrollbarWidth)
            {
                int index = (e.Y + _scrollOffset) / itemHeight;
                if (index >= 0 && index < _filteredItems.Count)
                {
                    var item = _filteredItems[index];

                    if (e.Button == MouseButtons.Left)
                    {
                        _selectedConversationId = item.ConversationId;
                        SelectedConversationChanged?.Invoke(this, item.ConversationId);
                        Invalidate();
                    }
                    else if (e.Button == MouseButtons.Right)
                    {
                        ConversationRightClicked?.Invoke(item.ConversationId, e.Location);
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.Clear(Theme.Panel);

            if (_filteredItems.Count == 0)
            {
                string emptyText = string.IsNullOrWhiteSpace(_searchQuery)
                    ? "Aucune conversation"
                    : "Aucun résultat";

                TextRenderer.DrawText(g, emptyText, _emptyFont, ClientRectangle, Theme.TextSecondary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                return;
            }

            int itemHeight = LogicalToDeviceUnits(72);
            int avatarSize = LogicalToDeviceUnits(48);
            int paddingLeft = LogicalToDeviceUnits(12);
            int scrollbarWidth = _vScrollBar.Visible ? _vScrollBar.Width : 0;
            int itemWidth = Width - scrollbarWidth;

            int startIndex = Math.Max(0, _scrollOffset / itemHeight);
            int endIndex = Math.Min(_filteredItems.Count - 1, (_scrollOffset + Height) / itemHeight);

            for (int i = startIndex; i <= endIndex; i++)
            {
                var item = _filteredItems[i];
                int y = (i * itemHeight) - _scrollOffset;
                var itemRect = new Rectangle(0, y, itemWidth, itemHeight);

                // Fond selon sélection ou survol
                if (item.ConversationId == _selectedConversationId)
                {
                    using var brush = new SolidBrush(Theme.Selected);
                    g.FillRectangle(brush, itemRect);
                }
                else if (i == _hoveredIndex)
                {
                    using var brush = new SolidBrush(Theme.Hover);
                    g.FillRectangle(brush, itemRect);
                }

                // Avatar circulaire (photo de profil ou initiales)
                int avatarY = y + (itemHeight - avatarSize) / 2;
                var avatar = _avatarCache.GetAvatar(item.OwnerId, item.DisplayName, avatarSize);
                g.DrawImage(avatar, paddingLeft, avatarY);

                // Coordonnées de texte
                int textLeft = paddingLeft + avatarSize + LogicalToDeviceUnits(12);
                int textRight = itemWidth - LogicalToDeviceUnits(12);

                // Ligne 1 : Nom et heure
                int line1Y = y + LogicalToDeviceUnits(14);
                int line1Height = LogicalToDeviceUnits(20);

                var timeSize = TextRenderer.MeasureText(g, item.TimeText, _timeFont, Size.Empty, TextFormatFlags.NoPrefix);
                var timeRect = new Rectangle(textRight - timeSize.Width, line1Y, timeSize.Width, line1Height);
                var timeColor = item.UnreadCount > 0 ? Theme.Accent : Theme.TextSecondary;
                TextRenderer.DrawText(g, item.TimeText, _timeFont, timeRect, timeColor,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                int nameWidth = Math.Max(0, textRight - timeSize.Width - LogicalToDeviceUnits(8) - textLeft);
                var nameRect = new Rectangle(textLeft, line1Y, nameWidth, line1Height);
                TextRenderer.DrawText(g, item.DisplayName, _nameFont, nameRect, Theme.TextPrimary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                // Ligne 2 : Aperçu et pastille de non lus
                int line2Y = y + LogicalToDeviceUnits(38);
                int line2Height = LogicalToDeviceUnits(20);

                int badgeWidth = 0;
                if (item.UnreadCount > 0)
                {
                    string badgeText = item.UnreadCount > 99 ? "99+" : item.UnreadCount.ToString();
                    var badgeTextSize = TextRenderer.MeasureText(g, badgeText, _badgeFont, Size.Empty, TextFormatFlags.NoPrefix);
                    int badgeHeight = LogicalToDeviceUnits(18);
                    badgeWidth = Math.Max(badgeHeight, badgeTextSize.Width + LogicalToDeviceUnits(8));
                    var badgeRect = new Rectangle(textRight - badgeWidth, line2Y + (line2Height - badgeHeight) / 2, badgeWidth, badgeHeight);

                    DrawBadge(g, badgeRect, badgeHeight / 2, Theme.Accent);
                    TextRenderer.DrawText(g, badgeText, _badgeFont, badgeRect, Theme.AccentText,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                }

                int previewWidth = Math.Max(0, textRight - (badgeWidth > 0 ? badgeWidth + LogicalToDeviceUnits(8) : 0) - textLeft);
                var previewRect = new Rectangle(textLeft, line2Y, previewWidth, line2Height);
                var previewFontToUse = item.IsDeleted ? _previewItalicFont : _previewFont;
                TextRenderer.DrawText(g, item.LastMessagePreview, previewFontToUse, previewRect, Theme.TextSecondary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                // Séparateur inférieur fin
                int sepY = y + itemHeight - 1;
                using var pen = new Pen(Theme.Separator);
                g.DrawLine(pen, textLeft, sepY, itemWidth, sepY);
            }
        }

        private static void DrawBadge(Graphics g, Rectangle bounds, int radius, Color fillColor)
        {
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
                _avatarCache.Dispose();
                _nameFont.Dispose();
                _previewFont.Dispose();
                _previewItalicFont.Dispose();
                _timeFont.Dispose();
                _badgeFont.Dispose();
                _emptyFont.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
