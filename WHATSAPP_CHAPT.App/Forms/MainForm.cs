#nullable enable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WHATSAPP_CHAPT.App.Controls;
using WHATSAPP_CHAPT.App.Drawing;
using WHATSAPP_CHAPT.App.Models;
using WHATSAPP_CHAPT.Core.Events;
using WHATSAPP_CHAPT.Core.Models;
using WHATSAPP_CHAPT.Core.Services;

namespace WHATSAPP_CHAPT.App.Forms
{
    /// <summary>
    /// Fenêtre principale de l'application WHATSAPP_CHAPT.
    /// </summary>
    public sealed class MainForm : Form
    {
        private readonly IChatService _service;
        private readonly Action? _simulateIncoming;
        private readonly AvatarCache _headerAvatarCache = new();

        private readonly Panel _leftPanel;
        private readonly Panel _headerPanel;
        private readonly Panel? _demoPanel;
        private readonly Panel _searchPanel;
        private readonly TextBox _searchTextBox;
        private readonly ConversationListControl _listControl;

        private readonly Panel _rightPanel;
        private readonly Panel _rightHeaderPanel;
        private readonly MessageListControl _messageListControl;

        private Guid? _selectedConversationId;
        private string? _selectedHeaderTitle;
        private string? _selectedHeaderSubtitle;
        private string? _selectedHeaderInitials;
        private Color _selectedHeaderColor;

        private Font _headerTitleFont;
        private Font _rightEmptyFont;
        private Font _rightSelectedNameFont;
        private Font _rightSubtitleFont;

        /// <summary>
        /// Initialise une nouvelle instance de la fenêtre principale.
        /// </summary>
        /// <param name="service">Service de messagerie.</param>
        /// <param name="simulateIncoming">Action de simulation d'un message entrant (optionnelle).</param>
        public MainForm(IChatService service, Action? simulateIncoming = null)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _simulateIncoming = simulateIncoming;

            AutoScaleMode = AutoScaleMode.Dpi;
            DoubleBuffered = true;
            Text = "WHATSAPP_CHAPT";
            StartPosition = FormStartPosition.CenterScreen;

            MinimumSize = new Size(LogicalToDeviceUnits(900), LogicalToDeviceUnits(600));
            Size = new Size(LogicalToDeviceUnits(1050), LogicalToDeviceUnits(700));
            BackColor = Theme.AppBackground;

            _headerTitleFont = new Font("Segoe UI", 11.5f, FontStyle.Bold);
            _rightEmptyFont = new Font("Segoe UI", 13f, FontStyle.Regular);
            _rightSelectedNameFont = new Font("Segoe UI", 11.5f, FontStyle.Bold);
            _rightSubtitleFont = new Font("Segoe UI", 8.5f, FontStyle.Regular);

            // Panneau gauche de largeur fixe (360 logiques)
            _leftPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = LogicalToDeviceUnits(360),
                BackColor = Theme.Panel
            };

            // En-tête gauche : Titre et avatar de l'utilisateur courant
            _headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = LogicalToDeviceUnits(60),
                BackColor = Theme.Header
            };
            _headerPanel.Paint += OnHeaderPanelPaint;

            // Bandeau de démonstration (masqué si simulateIncoming est null)
            if (_simulateIncoming != null)
            {
                _demoPanel = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = LogicalToDeviceUnits(44),
                    BackColor = Theme.Header
                };

                var lblDemo = new Label
                {
                    Text = "Mode démonstration",
                    Font = new Font("Segoe UI", 8.25f, FontStyle.Bold),
                    ForeColor = Theme.Accent,
                    AutoSize = true,
                    Location = new Point(LogicalToDeviceUnits(12), LogicalToDeviceUnits(14))
                };

                var btnSimulate = new Button
                {
                    Text = "Simuler un message reçu",
                    Font = new Font("Segoe UI", 8.25f, FontStyle.Regular),
                    BackColor = Theme.Accent,
                    ForeColor = Theme.AccentText,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    AutoSize = true,
                    Location = new Point(LogicalToDeviceUnits(160), LogicalToDeviceUnits(8)),
                    Height = LogicalToDeviceUnits(28)
                };
                btnSimulate.FlatAppearance.BorderSize = 0;
                btnSimulate.Click += (s, e) => _simulateIncoming();

                _demoPanel.Controls.Add(lblDemo);
                _demoPanel.Controls.Add(btnSimulate);
            }

            // Liste des conversations
            _listControl = new ConversationListControl
            {
                Dock = DockStyle.Fill
            };
            _listControl.SelectedConversationChanged += OnConversationSelected;

            // Zone de recherche
            _searchPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = LogicalToDeviceUnits(50),
                BackColor = Theme.Panel,
                Padding = new Padding(LogicalToDeviceUnits(10), LogicalToDeviceUnits(8), LogicalToDeviceUnits(10), LogicalToDeviceUnits(8))
            };

            var searchContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.SearchBackground,
                Padding = new Padding(LogicalToDeviceUnits(8), LogicalToDeviceUnits(6), LogicalToDeviceUnits(8), LogicalToDeviceUnits(6))
            };

            _searchTextBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = Theme.SearchBackground,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                Dock = DockStyle.Fill,
                PlaceholderText = "Rechercher ou démarrer une discussion"
            };
            _searchTextBox.TextChanged += (s, e) => _listControl.SetSearchFilter(_searchTextBox.Text);

            searchContainer.Controls.Add(_searchTextBox);
            _searchPanel.Controls.Add(searchContainer);

            // Assemblage du panneau gauche dans le bon ordre d'affichage (Top vers le bas)
            _leftPanel.Controls.Add(_listControl);
            _leftPanel.Controls.Add(_searchPanel);
            if (_demoPanel != null)
            {
                _leftPanel.Controls.Add(_demoPanel);
            }
            _leftPanel.Controls.Add(_headerPanel);

            // Zone droite
            _rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.AppBackground
            };
            _rightPanel.Paint += OnRightPanelPaint;

            // En-tête de la zone droite
            _rightHeaderPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = LogicalToDeviceUnits(60),
                BackColor = Theme.Header,
                Visible = false
            };
            _rightHeaderPanel.Paint += OnRightHeaderPanelPaint;

            // Contrôle d'affichage des messages
            _messageListControl = new MessageListControl
            {
                Dock = DockStyle.Fill,
                Visible = false
            };

            _rightPanel.Controls.Add(_messageListControl);
            _rightPanel.Controls.Add(_rightHeaderPanel);

            // Ajout des panneaux principaux
            Controls.Add(_rightPanel);
            Controls.Add(_leftPanel);

            // Abonnement aux événements du service
            _service.ChatEventRaised += OnChatEventRaised;
        }

        /// <inheritdoc/>
        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            await ReloadConversationsAsync();
        }

        /// <inheritdoc/>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _service.ChatEventRaised -= OnChatEventRaised;
            base.OnFormClosed(e);
        }

        private async void OnChatEventRaised(object? sender, ChatEvent e)
        {
            if (IsDisposed || Disposing)
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke(new Action(async () =>
                {
                    await ReloadConversationsAsync();
                    if (_selectedConversationId.HasValue && e.ConversationId == _selectedConversationId.Value)
                    {
                        await LoadMessagesForSelectedConversationAsync(isNewConversation: false);
                    }
                }));
                return;
            }

            await ReloadConversationsAsync();
            if (_selectedConversationId.HasValue && e.ConversationId == _selectedConversationId.Value)
            {
                await LoadMessagesForSelectedConversationAsync(isNewConversation: false);
            }
        }

        private async void OnConversationSelected(object? sender, Guid conversationId)
        {
            _selectedConversationId = conversationId;
            await _service.MarkConversationReadAsync(conversationId);
            await LoadMessagesForSelectedConversationAsync(isNewConversation: true);
        }

        private async Task ReloadConversationsAsync()
        {
            var conversations = await _service.GetConversationsAsync();
            var contacts = await _service.GetContactsAsync();
            var items = new List<ConversationListItem>();

            foreach (var conv in conversations)
            {
                var messages = await _service.GetMessagesAsync(conv.Id);
                int unread = await _service.GetUnreadCountAsync(conv.Id);
                var lastMsg = messages.LastOrDefault();

                string displayName;
                if (!string.IsNullOrWhiteSpace(conv.Title))
                {
                    displayName = conv.Title;
                }
                else
                {
                    var otherMember = conv.Members.FirstOrDefault(m => m.UserId != _service.CurrentUser.Id);
                    var contact = otherMember != null ? contacts.FirstOrDefault(c => c.Id == otherMember.UserId) : null;
                    displayName = contact?.DisplayName ?? "Discussion";
                }

                string preview;
                bool isDeleted = false;

                if (lastMsg == null)
                {
                    preview = string.Empty;
                }
                else if (lastMsg.IsDeleted)
                {
                    preview = "Message supprimé";
                    isDeleted = true;
                }
                else
                {
                    string prefix;
                    if (lastMsg.AuthorId == _service.CurrentUser.Id)
                    {
                        prefix = "Vous : ";
                    }
                    else if (conv.Type == ConversationType.Group)
                    {
                        var author = contacts.FirstOrDefault(c => c.Id == lastMsg.AuthorId);
                        string authorName = author?.DisplayName ?? "Membre";
                        prefix = $"{authorName} : ";
                    }
                    else
                    {
                        prefix = string.Empty;
                    }

                    string cleanContent = lastMsg.Content.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
                    preview = prefix + cleanContent;
                }

                items.Add(new ConversationListItem
                {
                    ConversationId = conv.Id,
                    DisplayName = displayName,
                    LastMessagePreview = preview,
                    LastMessageTimestamp = lastMsg?.Timestamp,
                    TimeText = ConversationListItem.FormatTime(lastMsg?.Timestamp),
                    UnreadCount = unread,
                    IsDeleted = isDeleted,
                    Initials = ConversationListItem.GetInitials(displayName),
                    AvatarColor = ConversationListItem.GetAvatarColor(displayName)
                });
            }

            _listControl.SetItems(items, preserveSelection: true);
        }

        private async Task LoadMessagesForSelectedConversationAsync(bool isNewConversation = false)
        {
            if (!_selectedConversationId.HasValue)
            {
                _rightHeaderPanel.Visible = false;
                _messageListControl.Visible = false;
                _rightPanel.Invalidate();
                return;
            }

            var conversationId = _selectedConversationId.Value;
            var conversations = await _service.GetConversationsAsync();
            var conv = conversations.FirstOrDefault(c => c.Id == conversationId);

            if (conv == null)
            {
                _selectedConversationId = null;
                _rightHeaderPanel.Visible = false;
                _messageListControl.Visible = false;
                _rightPanel.Invalidate();
                return;
            }

            var contacts = await _service.GetContactsAsync();
            var contactsMap = contacts.ToDictionary(c => c.Id, c => c.DisplayName);

            string title;
            string? subtitle = null;

            if (!string.IsNullOrWhiteSpace(conv.Title))
            {
                title = conv.Title;
                subtitle = $"{conv.Members.Count} membres";
            }
            else
            {
                var otherMember = conv.Members.FirstOrDefault(m => m.UserId != _service.CurrentUser.Id);
                string otherName = otherMember != null && contactsMap.TryGetValue(otherMember.UserId, out var name)
                    ? name
                    : "Discussion";
                title = otherName;
                subtitle = null;
            }

            _selectedHeaderTitle = title;
            _selectedHeaderSubtitle = subtitle;
            _selectedHeaderInitials = ConversationListItem.GetInitials(title);
            _selectedHeaderColor = ConversationListItem.GetAvatarColor(title);

            var rawMessages = await _service.GetMessagesAsync(conversationId);
            var messagesMap = rawMessages.ToDictionary(m => m.Id);

            bool isGroup = conv.Type == ConversationType.Group;
            var viewItems = new List<MessageViewItem>(rawMessages.Count);

            for (int i = 0; i < rawMessages.Count; i++)
            {
                var m = rawMessages[i];
                bool isMine = m.AuthorId == _service.CurrentUser.Id;

                string authorName;
                if (isMine)
                {
                    authorName = "Vous";
                }
                else if (contactsMap.TryGetValue(m.AuthorId, out var contactName))
                {
                    authorName = contactName;
                }
                else
                {
                    authorName = "Membre";
                }

                bool showAuthorName = false;
                if (isGroup && !isMine)
                {
                    if (i == 0)
                    {
                        showAuthorName = true;
                    }
                    else
                    {
                        var prev = rawMessages[i - 1];
                        if (prev.AuthorId != m.AuthorId || prev.Timestamp.ToLocalTime().Date != m.Timestamp.ToLocalTime().Date)
                        {
                            showAuthorName = true;
                        }
                    }
                }

                string? replyAuthorName = null;
                string? replyPreview = null;
                if (m.ReplyToMessageId.HasValue)
                {
                    if (messagesMap.TryGetValue(m.ReplyToMessageId.Value, out var replyMsg))
                    {
                        if (replyMsg.AuthorId == _service.CurrentUser.Id)
                        {
                            replyAuthorName = "Vous";
                        }
                        else if (contactsMap.TryGetValue(replyMsg.AuthorId, out var rAuthor))
                        {
                            replyAuthorName = rAuthor;
                        }
                        else
                        {
                            replyAuthorName = "Membre";
                        }

                        replyPreview = replyMsg.IsDeleted
                            ? "Message supprimé"
                            : replyMsg.Content.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
                    }
                    else
                    {
                        replyAuthorName = null;
                        replyPreview = "Message introuvable";
                    }
                }

                var viewItem = new MessageViewItem
                {
                    Id = m.Id,
                    AuthorId = m.AuthorId,
                    AuthorName = authorName,
                    Text = m.Content,
                    Timestamp = m.Timestamp,
                    Time = m.Timestamp.ToLocalTime().ToString("HH:mm"),
                    Status = m.Status,
                    IsMine = isMine,
                    IsEdited = m.IsEdited,
                    IsDeleted = m.IsDeleted,
                    ReplyAuthorName = replyAuthorName,
                    ReplyPreview = replyPreview,
                    ShowAuthorName = showAuthorName
                };

                viewItems.Add(viewItem);
            }

            _rightHeaderPanel.Visible = true;
            _messageListControl.Visible = true;
            _rightHeaderPanel.Invalidate();
            _messageListControl.SetMessages(viewItems, isNewConversation);
        }

        private void OnHeaderPanelPaint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            int padding = LogicalToDeviceUnits(12);

            // Nom de l'application
            var titleRect = new Rectangle(padding, 0, _headerPanel.Width - LogicalToDeviceUnits(60), _headerPanel.Height);
            TextRenderer.DrawText(g, "WHATSAPP_CHAPT", _headerTitleFont, titleRect, Theme.Accent,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            // Avatar de l'utilisateur courant
            int avatarSize = LogicalToDeviceUnits(38);
            int avatarX = _headerPanel.Width - avatarSize - padding;
            int avatarY = (_headerPanel.Height - avatarSize) / 2;

            var userInitials = ConversationListItem.GetInitials(_service.CurrentUser.DisplayName);
            var userColor = ConversationListItem.GetAvatarColor(_service.CurrentUser.DisplayName);
            var avatarBmp = _headerAvatarCache.GetAvatar(userInitials, userColor, avatarSize);

            g.DrawImage(avatarBmp, avatarX, avatarY);

            // Bordure inférieure
            using var pen = new Pen(Theme.Separator);
            g.DrawLine(pen, 0, _headerPanel.Height - 1, _headerPanel.Width, _headerPanel.Height - 1);
        }

        private void OnRightHeaderPanelPaint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            int padding = LogicalToDeviceUnits(16);
            int avatarSize = LogicalToDeviceUnits(38);
            int avatarY = (_rightHeaderPanel.Height - avatarSize) / 2;

            if (!string.IsNullOrEmpty(_selectedHeaderInitials))
            {
                var avatarBmp = _headerAvatarCache.GetAvatar(_selectedHeaderInitials, _selectedHeaderColor, avatarSize);
                g.DrawImage(avatarBmp, padding, avatarY);
            }

            int textX = padding + avatarSize + LogicalToDeviceUnits(12);
            int textW = _rightHeaderPanel.Width - textX - padding;

            if (string.IsNullOrEmpty(_selectedHeaderSubtitle))
            {
                var titleRect = new Rectangle(textX, 0, textW, _rightHeaderPanel.Height);
                TextRenderer.DrawText(g, _selectedHeaderTitle ?? string.Empty, _rightSelectedNameFont, titleRect, Theme.TextPrimary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
            else
            {
                int line1H = LogicalToDeviceUnits(22);
                int line2H = LogicalToDeviceUnits(18);
                int totalH = line1H + line2H;
                int startY = (_rightHeaderPanel.Height - totalH) / 2;

                var titleRect = new Rectangle(textX, startY, textW, line1H);
                TextRenderer.DrawText(g, _selectedHeaderTitle ?? string.Empty, _rightSelectedNameFont, titleRect, Theme.TextPrimary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                var subRect = new Rectangle(textX, startY + line1H, textW, line2H);
                TextRenderer.DrawText(g, _selectedHeaderSubtitle, _rightSubtitleFont, subRect, Theme.TextSecondary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }

            using var pen = new Pen(Theme.Separator);
            g.DrawLine(pen, 0, _rightHeaderPanel.Height - 1, _rightHeaderPanel.Width, _rightHeaderPanel.Height - 1);
        }

        private void OnRightPanelPaint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Theme.AppBackground);

            // Bordure séparatrice gauche
            using (var borderPen = new Pen(Theme.Separator))
            {
                g.DrawLine(borderPen, 0, 0, 0, _rightPanel.Height);
            }

            if (!_selectedConversationId.HasValue || string.IsNullOrEmpty(_selectedHeaderTitle))
            {
                TextRenderer.DrawText(g, "Sélectionnez une conversation", _rightEmptyFont,
                    _rightPanel.ClientRectangle, Theme.TextSecondary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _headerAvatarCache.Dispose();
                _headerTitleFont.Dispose();
                _rightEmptyFont.Dispose();
                _rightSelectedNameFont.Dispose();
                _rightSubtitleFont.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
