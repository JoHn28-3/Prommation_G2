#nullable enable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WHATSAPP_CHAPT.App.Controls;
using WHATSAPP_CHAPT.App.Drawing;
using WHATSAPP_CHAPT.App.Logic;
using WHATSAPP_CHAPT.App.Models;
using WHATSAPP_CHAPT.App.Services;
using WHATSAPP_CHAPT.Core.Events;
using WHATSAPP_CHAPT.Core.Models;
using WHATSAPP_CHAPT.Core.Services;

namespace WHATSAPP_CHAPT.App.Forms
{
    /// <summary>
    /// Fenêtre principale de l'application WHATSAPP.
    /// </summary>
    public sealed class MainForm : Form
    {
        private readonly IChatService _service;
        private readonly IAvatarProvider _avatars;
        private readonly Action? _simulateIncoming;
        private readonly AvatarCache _headerAvatarCache;
        private readonly Dictionary<Guid, string> _drafts = new();

        private NavigationBar? _navBar;

        private readonly Panel _leftPanel;

        private readonly Panel _headerPanel;
        private readonly Panel? _demoPanel;
        private readonly Panel _searchPanel;
        private readonly TextBox _searchTextBox;
        private readonly ConversationListControl _listControl;

        private readonly Panel _rightPanel;
        private readonly Panel _rightHeaderPanel;
        private readonly Panel _inChatSearchPanel;
        private readonly TextBox _inChatSearchTextBox;
        private readonly NotifyIcon _notifyIcon;
        private readonly MessageListControl _messageListControl;
        private readonly MessageInputControl _inputControl;
        private readonly Panel _statusBannerPanel;
        private readonly Label _statusBannerLabel;
        private readonly System.Windows.Forms.Timer _statusBannerTimer;

        private Guid? _selectedConversationId;
        private Guid _selectedHeaderOwnerId;
        private string? _selectedHeaderTitle;
        private string? _selectedHeaderSubtitle;
        private Rectangle _myAvatarRect;

        private Font _headerTitleFont;
        private Font _rightEmptyFont;
        private Font _rightSelectedNameFont;
        private Font _rightSubtitleFont;

        /// <summary>
        /// Initialise une nouvelle instance de la fenêtre principale.
        /// </summary>
        /// <param name="service">Service de messagerie.</param>
        /// <param name="avatars">Fournisseur de photos de profil.</param>
        /// <param name="simulateIncoming">Action de simulation d'un message entrant (optionnelle).</param>
        public MainForm(IChatService service, IAvatarProvider avatars, Action? simulateIncoming = null)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _avatars = avatars ?? throw new ArgumentNullException(nameof(avatars));
            _simulateIncoming = simulateIncoming;

            _headerAvatarCache = new AvatarCache(_avatars);
            _avatars.PhotoChanged += OnAvatarPhotoChanged;

            AutoScaleMode = AutoScaleMode.Dpi;
            DoubleBuffered = true;
            Text = "WHATSAPP";
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
            _headerPanel.MouseMove += OnHeaderPanelMouseMove;
            _headerPanel.MouseClick += OnHeaderPanelMouseClick;

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
            _listControl = new ConversationListControl(_avatars)
            {
                Dock = DockStyle.Fill
            };
            _listControl.SelectedConversationChanged += OnConversationSelected;

            if (_simulateIncoming != null)
            {
                _listControl.ConversationRightClicked += OnConversationRightClicked;
            }

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

            var btnCallVideo = new Button
            {
                Text = "📹",
                Font = new Font("Segoe UI", 12f),
                ForeColor = Theme.TextPrimary,
                BackColor = Theme.Header,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(LogicalToDeviceUnits(36), LogicalToDeviceUnits(36)),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            btnCallVideo.Location = new Point(_rightHeaderPanel.Width - LogicalToDeviceUnits(48), (_rightHeaderPanel.Height - LogicalToDeviceUnits(36)) / 2);
            btnCallVideo.FlatAppearance.BorderSize = 0;
            btnCallVideo.Click += (s, e) => StartCall(CallType.Video);

            var btnCallAudio = new Button
            {
                Text = "📞",
                Font = new Font("Segoe UI", 12f),
                ForeColor = Theme.TextPrimary,
                BackColor = Theme.Header,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(LogicalToDeviceUnits(36), LogicalToDeviceUnits(36)),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            btnCallAudio.Location = new Point(_rightHeaderPanel.Width - LogicalToDeviceUnits(90), (_rightHeaderPanel.Height - LogicalToDeviceUnits(36)) / 2);
            btnCallAudio.FlatAppearance.BorderSize = 0;
            btnCallAudio.Click += (s, e) => StartCall(CallType.Audio);

            _rightHeaderPanel.Controls.Add(btnCallVideo);
            _rightHeaderPanel.Controls.Add(btnCallAudio);

            // Contrôle d'affichage des messages
            _messageListControl = new MessageListControl
            {
                Dock = DockStyle.Fill,
                Visible = false
            };
            _messageListControl.MessageRightClicked += OnMessageRightClicked;

            // Contrôle de saisie des messages
            _inputControl = new MessageInputControl
            {
                Dock = DockStyle.Bottom,
                Visible = false,
                Enabled = false
            };
            _inputControl.SendRequested += OnSendRequested;
            _inputControl.EditRequested += OnEditRequested;
            _inputControl.EditCancelled += OnEditCancelled;
            _inputControl.AttachmentRequested += OnAttachmentRequested;
            _inputControl.EmojiRequested += OnEmojiRequested;
            _inputControl.VoiceRequested += OnVoiceRequested;

            // Bandeau inférieur d'information et d'erreur
            _statusBannerPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = LogicalToDeviceUnits(32),
                BackColor = Theme.Header,
                Visible = false
            };
            _statusBannerLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Theme.Accent,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular)
            };
            _statusBannerPanel.Controls.Add(_statusBannerLabel);

            _statusBannerTimer = new System.Windows.Forms.Timer
            {
                Interval = 3000
            };
            _statusBannerTimer.Tick += (s, e) =>
            {
                _statusBannerTimer.Stop();
                _statusBannerPanel.Visible = false;
            };

            // Barre de recherche Ctrl+F dans la discussion courante
            _inChatSearchPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = LogicalToDeviceUnits(36),
                BackColor = Theme.Header,
                Visible = false,
                Padding = new Padding(LogicalToDeviceUnits(8), LogicalToDeviceUnits(4), LogicalToDeviceUnits(8), LogicalToDeviceUnits(4))
            };

            _inChatSearchTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.SearchBackground,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.None,
                PlaceholderText = "Rechercher dans cette discussion... (Échap pour fermer)"
            };
            _inChatSearchTextBox.TextChanged += (s, e) => _messageListControl.SetSearchQuery(_inChatSearchTextBox.Text);

            var btnCloseSearch = new Button
            {
                Text = "✕",
                Dock = DockStyle.Right,
                Width = LogicalToDeviceUnits(28),
                BackColor = Theme.Header,
                ForeColor = Theme.TextSecondary,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCloseSearch.FlatAppearance.BorderSize = 0;
            btnCloseSearch.Click += (s, e) =>
            {
                _inChatSearchPanel.Visible = false;
                _inChatSearchTextBox.Clear();
                _messageListControl.SetSearchQuery(null);
            };

            _inChatSearchPanel.Controls.Add(_inChatSearchTextBox);
            _inChatSearchPanel.Controls.Add(btnCloseSearch);

            _notifyIcon = new NotifyIcon
            {
                Text = "WHATSAPP",
                Icon = SystemIcons.Application,
                Visible = true
            };

            KeyPreview = true;
            KeyDown += OnMainFormKeyDown;

            _rightPanel.Controls.Add(_messageListControl);
            _rightPanel.Controls.Add(_inputControl);
            _rightPanel.Controls.Add(_statusBannerPanel);
            _rightPanel.Controls.Add(_inChatSearchPanel);
            _rightPanel.Controls.Add(_rightHeaderPanel);

            // Ajout des panneaux principaux
            Controls.Add(_rightPanel);
            Controls.Add(_leftPanel);

            // Barre de navigation verticale (dockée à gauche, avant _leftPanel)
            _navBar = new NavigationBar();
            _navBar.SectionSelected += NavBar_SectionSelected;
            Controls.Add(_navBar);

            // Menu principal d'actions (Nouveau groupe, Nouveau contact, Paramètres)
            var menuMain = new ContextMenuStrip
            {
                BackColor = Theme.Header,
                ForeColor = Theme.TextPrimary,
                Renderer = new ThemeContextMenuRenderer()
            };

            var miNewGroup = new ToolStripMenuItem("Nouveau groupe...");
            miNewGroup.Click += async (s, e) => await OpenNewGroupDialogAsync();

            var miNewContact = new ToolStripMenuItem("Nouveau contact (P2P)...");
            miNewContact.Click += (s, e) => OpenNewContactDialog();

            var miSettings = new ToolStripMenuItem("Paramètres...");
            miSettings.Click += (s, e) => OpenSettingsDialog();

            var miEphemeral = new ToolStripMenuItem("Messages éphémères");
            foreach (var option in new[] { "Désactivés", "24 heures", "7 jours", "90 jours" })
            {
                var optionItem = new ToolStripMenuItem(option);
                optionItem.Click += (s, e) => ShowStatusMessage($"Messages éphémères : {option}");
                miEphemeral.DropDownItems.Add(optionItem);
            }

            menuMain.Items.Add(miNewGroup);
            menuMain.Items.Add(miNewContact);
            menuMain.Items.Add(miEphemeral);
            menuMain.Items.Add(new ToolStripSeparator());
            menuMain.Items.Add(miSettings);

            var btnMenu = new Button
            {
                Text = "⋮",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                BackColor = Theme.Header,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(LogicalToDeviceUnits(30), LogicalToDeviceUnits(30)),
                Location = new Point(_headerPanel.Width - LogicalToDeviceUnits(76), (_headerPanel.Height - LogicalToDeviceUnits(30)) / 2),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnMenu.FlatAppearance.BorderSize = 0;
            btnMenu.Click += (s, e) => menuMain.Show(btnMenu, new Point(0, btnMenu.Height));
            _headerPanel.Controls.Add(btnMenu);

            _rightHeaderPanel.Cursor = Cursors.Hand;
            _rightHeaderPanel.Click += async (s, e) => await OpenGroupInfoIfApplicableAsync();

            // Abonnement aux événements du service
            _service.ChatEventRaised += OnChatEventRaised;
        }

        private void NavBar_SectionSelected(string key)
        {
            switch (key)
            {
                case "discussions":
                    // Panneau gauche déjà visible – ne rien cacher
                    break;
                case "metaai":
                    // Ouvrir le panneau Meta AI
                    using (var dlg = new MetaAiForm())
                    {
                        dlg.ShowDialog(this);
                    }
                    break;
                default:
                    ShowStatusMessage($"Section : {key}");
                    break;
            }
        }

        private void ShowStatusMessage(string message)
        {
            _statusBannerLabel.Text = message;
            _statusBannerPanel.Visible = true;
            _statusBannerTimer.Stop();
            _statusBannerTimer.Start();
        }

        private async void OnEditRequested(Guid messageId, string newContent)
        {
            bool ok = await _service.EditMessageAsync(messageId, newContent);
            if (ok)
            {
                _inputControl.SetEditTarget(null);
                if (_selectedConversationId.HasValue)
                {
                    _inputControl.Text = _drafts.GetValueOrDefault(_selectedConversationId.Value, string.Empty);
                }
                _inputControl.FocusInput();
            }
            else
            {
                ShowStatusMessage("Modification impossible.");
            }
        }

        private void OnEditCancelled()
        {
            if (_selectedConversationId.HasValue)
            {
                _inputControl.Text = _drafts.GetValueOrDefault(_selectedConversationId.Value, string.Empty);
            }
            _inputControl.FocusInput();
        }

        private void OnEmojiRequested()
        {
            if (!_inputControl.Enabled) return;

            var menu = new ContextMenuStrip
            {
                Renderer = new ThemeContextMenuRenderer(),
                BackColor = Theme.Header,
                ForeColor = Theme.TextPrimary,
                AutoSize = false,
                Size = new Size(LogicalToDeviceUnits(260), LogicalToDeviceUnits(150))
            };
            var emojiMap = new Dictionary<string, string>
            {
                ["1f600"] = "\U0001F600", ["1f602"] = "\U0001F602", ["1f605"] = "\U0001F605",
                ["1f60a"] = "\U0001F60A", ["1f60d"] = "\U0001F60D", ["1f60e"] = "\U0001F60E",
                ["1f621"] = "\U0001F621", ["1f622"] = "\U0001F622", ["1f62d"] = "\U0001F62D",
                ["1f62e"] = "\U0001F62E", ["1f631"] = "\U0001F631", ["1f634"] = "\U0001F634",
                ["1f644"] = "\U0001F644", ["1f914"] = "\U0001F914", ["1f92c"] = "\U0001F92C",
                ["1f973"] = "\U0001F973"
            };
            foreach (var pair in emojiMap)
            {
                var item = new ToolStripMenuItem { AutoSize = false, Size = new Size(48, 42), Tag = pair.Value };
                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Emoji", pair.Key + ".png");
                if (System.IO.File.Exists(path)) item.Image = new Bitmap(path);
                item.Click += (s, e) =>
                {
                    _inputControl.Text += (string.IsNullOrEmpty(_inputControl.Text) ? string.Empty : " ") + (string)item.Tag;
                    _inputControl.FocusInput();
                };
                menu.Items.Add(item);
            }
            menu.Show(_inputControl, new Point(0, -menu.Height));
        }

        private void OnVoiceRequested()
        {
            OnAttachmentRequested("voicerecord");
        }

        private async void OnAttachmentRequested(string kind)
        {
            if (!_selectedConversationId.HasValue) return;

            string filter = kind switch
            {
                "image" or "photo" => "Images|*.png;*.jpg;*.jpeg;*.gif;*.bmp",
                "video" => "Vidéos|*.mp4;*.avi;*.mov;*.mkv|Tous les fichiers|*.*",
                "audio" or "voicerecord" => "Audio|*.mp3;*.wav;*.m4a;*.ogg;*.flac",
                _ => "Tous les fichiers|*.*"
            };

            using var dialog = new OpenFileDialog { Filter = filter, Title = "Choisir un fichier à joindre" };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            string mediaType = kind switch
            {
                "image" or "photo" => "image",
                "video" => "video",
                "audio" or "voicerecord" => "audio",
                "sticker" => "sticker",
                _ => "document"
            };
            string mediaRoot = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WHATSAPP_CHAPT", "Media");
            System.IO.Directory.CreateDirectory(mediaRoot);
            string safeName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}_{System.IO.Path.GetFileName(dialog.FileName)}";
            string localPath = System.IO.Path.Combine(mediaRoot, safeName);
            System.IO.File.Copy(dialog.FileName, localPath, overwrite: false);
            await _service.SendMediaMessageAsync(_selectedConversationId.Value, localPath, mediaType, new System.IO.FileInfo(localPath).Length);
            _messageListControl.ScrollToBottom();
            ShowStatusMessage("Fichier joint localement à la discussion.");
        }

        private async Task SendLocalAttachmentMessageAsync(string text)
        {
            if (!_selectedConversationId.HasValue) return;
            try
            {
                await _service.SendMessageAsync(_selectedConversationId.Value, text);
                _inputControl.Clear();
                _messageListControl.ScrollToBottom();
                ShowStatusMessage("Média ajouté localement à la discussion.");
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                ShowStatusMessage("Impossible d’ajouter ce média.");
            }
        }

        private async void OnSendRequested(string text, Guid? replyToMessageId)
        {
            if (!_selectedConversationId.HasValue)
            {
                return;
            }

            var conversationId = _selectedConversationId.Value;

            try
            {
                await _service.SendMessageAsync(conversationId, text, replyToMessageId);

                _drafts.Remove(conversationId);
                _inputControl.Clear();
                _inputControl.SetReplyTarget(null);
                _inputControl.FocusInput();
                _messageListControl.ScrollToBottom();
            }
            catch (ArgumentException)
            {
                // Ignorer silencieusement sans planter
            }
            catch (InvalidOperationException)
            {
                // Ignorer silencieusement sans planter
            }
        }

        private async void OnMessageRightClicked(Guid messageId, Point position)
        {
            if (!_selectedConversationId.HasValue)
            {
                return;
            }

            var messages = await _service.GetMessagesAsync(_selectedConversationId.Value);
            var msg = messages.FirstOrDefault(m => m.Id == messageId);
            if (msg == null || msg.IsDeleted)
            {
                return;
            }

            var contacts = await _service.GetContactsAsync();
            string authorName = msg.AuthorId == _service.CurrentUser.Id
                ? "Vous"
                : contacts.FirstOrDefault(c => c.Id == msg.AuthorId)?.DisplayName ?? "Membre";

            var viewItem = new MessageViewItem
            {
                Id = msg.Id,
                AuthorId = msg.AuthorId,
                AuthorName = authorName,
                Text = msg.Content,
                Timestamp = msg.Timestamp,
                Time = msg.Timestamp.ToLocalTime().ToString("HH:mm"),
                Status = msg.Status,
                IsMine = msg.AuthorId == _service.CurrentUser.Id,
                IsEdited = msg.IsEdited,
                IsDeleted = msg.IsDeleted
            };

            var actions = MessageActions.GetAvailableActions(viewItem);
            if (actions.Count == 0)
            {
                return;
            }

            var contextMenu = new ContextMenuStrip
            {
                BackColor = Theme.Header,
                ForeColor = Theme.TextPrimary,
                Renderer = new ThemeContextMenuRenderer()
            };

            if (actions.Contains(MessageAction.Reply))
            {
                var itemReply = new ToolStripMenuItem("Répondre");
                itemReply.Click += (s, e) =>
                {
                    if (_inputControl.IsEditing)
                    {
                        _inputControl.SetEditTarget(null);
                    }
                    _inputControl.SetReplyTarget(viewItem);
                    _inputControl.FocusInput();
                };
                contextMenu.Items.Add(itemReply);
            }

            if (actions.Contains(MessageAction.Copy))
            {
                var itemCopy = new ToolStripMenuItem("Copier");
                itemCopy.Click += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(viewItem.Text))
                    {
                        try
                        {
                            Clipboard.SetText(viewItem.Text);
                        }
                        catch
                        {
                            // Accès au presse-papiers ignoré
                        }
                    }
                };
                contextMenu.Items.Add(itemCopy);
            }

            if (actions.Contains(MessageAction.Edit))
            {
                var itemEdit = new ToolStripMenuItem("Modifier");
                itemEdit.Click += (s, e) =>
                {
                    if (_selectedConversationId.HasValue && !_inputControl.IsEditing)
                    {
                        _drafts[_selectedConversationId.Value] = _inputControl.Text;
                    }
                    _inputControl.SetEditTarget(viewItem);
                };
                contextMenu.Items.Add(itemEdit);
            }

            if (actions.Contains(MessageAction.Delete))
            {
                var itemDelete = new ToolStripMenuItem("Supprimer pour moi");
                itemDelete.Click += async (s, e) =>
                {
                    var confirm = MessageBox.Show(
                        this,
                        "Ce message sera supprimé uniquement sur cet appareil.",
                        "Supprimer le message",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Warning);

                    if (confirm == DialogResult.OK)
                    {
                        bool ok = await _service.DeleteMessageAsync(viewItem.Id);
                        if (!ok)
                        {
                            ShowStatusMessage("Suppression impossible.");
                        }
                    }
                };
                contextMenu.Items.Add(itemDelete);
            }

            contextMenu.Show(_messageListControl, position);
        }

        private void OnAvatarPhotoChanged(object? sender, Guid ownerId)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnAvatarPhotoChanged(sender, ownerId)));
                return;
            }

            _headerPanel.Invalidate();
            _rightHeaderPanel.Invalidate();
            _listControl.Invalidate();
        }

        private void OnHeaderPanelMouseMove(object? sender, MouseEventArgs e)
        {
            _headerPanel.Cursor = _myAvatarRect.Contains(e.Location) ? Cursors.Hand : Cursors.Default;
        }

        private void OnHeaderPanelMouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && _myAvatarRect.Contains(e.Location))
            {
                using var ofd = new OpenFileDialog
                {
                    Title = "Sélectionner une photo de profil",
                    Filter = "Fichiers image (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|Tous les fichiers (*.*)|*.*"
                };

                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    bool ok = _avatars.SetPhoto(_service.CurrentUser.Id, ofd.FileName);
                    if (!ok)
                    {
                        MessageBox.Show(this, "Impossible de charger cette image.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async void OnConversationRightClicked(Guid conversationId, Point position)
        {
            var conversations = await _service.GetConversationsAsync();
            var conv = conversations.FirstOrDefault(c => c.Id == conversationId);
            if (conv == null)
            {
                return;
            }

            var otherMember = conv.Members.FirstOrDefault(m => m.UserId != _service.CurrentUser.Id);
            Guid targetOwnerId = conv.Type == ConversationType.Group ? conv.Id : (otherMember?.UserId ?? conv.Id);

            var contextMenu = new ContextMenuStrip
            {
                BackColor = Theme.Header,
                ForeColor = Theme.TextPrimary,
                Renderer = new ThemeContextMenuRenderer()
            };

            var itemArchive = new ToolStripMenuItem(conv.IsArchived ? "Désarchiver la discussion" : "Archiver la discussion");
            itemArchive.Click += async (s, e) =>
            {
                await _service.ArchiveConversationAsync(conv.Id, !conv.IsArchived);
                ShowStatusMessage(conv.IsArchived ? "Discussion désarchivée." : "Discussion archivée.");
                await ReloadConversationsAsync();
            };
            contextMenu.Items.Add(itemArchive);

            var itemSet = new ToolStripMenuItem("Définir une photo (démo)");
            itemSet.Click += (s, e) =>
            {
                using var ofd = new OpenFileDialog
                {
                    Title = "Sélectionner une photo de profil (démo)",
                    Filter = "Fichiers image (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|Tous les fichiers (*.*)|*.*"
                };

                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    bool ok = _avatars.SetPhoto(targetOwnerId, ofd.FileName);
                    if (!ok)
                    {
                        MessageBox.Show(this, "Impossible de charger cette image.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            var itemRemove = new ToolStripMenuItem("Retirer la photo");
            itemRemove.Click += (s, e) =>
            {
                _avatars.RemovePhoto(targetOwnerId);
            };

            contextMenu.Items.Add(itemSet);
            contextMenu.Items.Add(itemRemove);
            contextMenu.Show(_listControl, position);
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
            _avatars.PhotoChanged -= OnAvatarPhotoChanged;
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
                BeginInvoke(new Action(async () => await HandleChatEventAsync(e)));
                return;
            }

            await HandleChatEventAsync(e);
        }

        private async Task HandleChatEventAsync(ChatEvent e)
        {
            // Notification et marquage « Lu »
            if (e.Type == ChatEventType.MessageAdded && e.MessageId.HasValue)
            {
                var messages = await _service.GetMessagesAsync(e.ConversationId);
                var msg = messages.FirstOrDefault(m => m.Id == e.MessageId.Value);
                if (msg != null && msg.AuthorId != _service.CurrentUser.Id)
                {
                    if (_selectedConversationId.HasValue &&
                        e.ConversationId == _selectedConversationId.Value &&
                        Form.ActiveForm == this)
                    {
                        await _service.MarkConversationReadAsync(_selectedConversationId.Value);
                    }
                    else
                    {
                        var contacts = await _service.GetContactsAsync();
                        var author = contacts.FirstOrDefault(c => c.Id == msg.AuthorId)?.DisplayName ?? "Nouveau message";
                        _notifyIcon.ShowBalloonTip(3000, author, msg.Content, ToolTipIcon.Info);
                    }
                }
            }

            await ReloadConversationsAsync();
            if (_selectedConversationId.HasValue && e.ConversationId == _selectedConversationId.Value)
            {
                await LoadMessagesForSelectedConversationAsync(isNewConversation: false);
            }
        }

        private async void OnConversationSelected(object? sender, Guid conversationId)
        {
            if (_inputControl.IsEditing)
            {
                _inputControl.SetEditTarget(null);
            }
            else if (_selectedConversationId.HasValue)
            {
                _drafts[_selectedConversationId.Value] = _inputControl.Text;
            }

            _selectedConversationId = conversationId;
            await _service.MarkConversationReadAsync(conversationId);
            await LoadMessagesForSelectedConversationAsync(isNewConversation: true);

            _inputControl.Text = _drafts.GetValueOrDefault(conversationId, string.Empty);
            _inputControl.SetReplyTarget(null);
            _inputControl.FocusInput();
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
                var otherMember = conv.Members.FirstOrDefault(m => m.UserId != _service.CurrentUser.Id);

                if (!string.IsNullOrWhiteSpace(conv.Title))
                {
                    displayName = conv.Title;
                }
                else
                {
                    var contact = otherMember != null ? contacts.FirstOrDefault(c => c.Id == otherMember.UserId) : null;
                    displayName = contact?.DisplayName ?? "Discussion";
                }

                Guid ownerId = conv.Type == ConversationType.Group ? conv.Id : (otherMember?.UserId ?? conv.Id);

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
                    OwnerId = ownerId,
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
            await UpdateWindowTitleAsync();
        }

        private async Task LoadMessagesForSelectedConversationAsync(bool isNewConversation = false)
        {
            if (!_selectedConversationId.HasValue)
            {
                _rightHeaderPanel.Visible = false;
                _messageListControl.Visible = false;
                _inputControl.Visible = false;
                _inputControl.Enabled = false;
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
                _inputControl.Visible = false;
                _inputControl.Enabled = false;
                _rightPanel.Invalidate();
                return;
            }

            var contacts = await _service.GetContactsAsync();
            var contactsMap = contacts.ToDictionary(c => c.Id, c => c.DisplayName);

            string title;
            string? subtitle = null;
            var otherMember = conv.Members.FirstOrDefault(m => m.UserId != _service.CurrentUser.Id);

            if (!string.IsNullOrWhiteSpace(conv.Title))
            {
                title = conv.Title;
                subtitle = $"{conv.Members.Count} membres";
            }
            else
            {
                string otherName = otherMember != null && contactsMap.TryGetValue(otherMember.UserId, out var name)
                    ? name
                    : "Discussion";
                title = otherName;
                subtitle = null;
            }

            _selectedHeaderOwnerId = conv.Type == ConversationType.Group ? conv.Id : (otherMember?.UserId ?? conv.Id);
            _selectedHeaderTitle = title;
            _selectedHeaderSubtitle = subtitle;
            await UpdateWindowTitleAsync();

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
                            replyAuthorName = "Vous";
                        else if (contactsMap.TryGetValue(replyMsg.AuthorId, out var rAuthor))
                            replyAuthorName = rAuthor;
                        else
                            replyAuthorName = "Membre";

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
                    ShowAuthorName = showAuthorName,
                    MediaPath = m.MediaPath,
                    MediaType = m.MediaType,
                    MediaSize = m.MediaSize
                };

                viewItems.Add(viewItem);
            }

            _rightHeaderPanel.Visible = true;
            _messageListControl.Visible = true;
            _inputControl.Visible = true;
            _inputControl.Enabled = true;

            _rightHeaderPanel.Invalidate();
            _messageListControl.SetMessages(viewItems, isNewConversation);
        }

        private void OnHeaderPanelPaint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            int padding = LogicalToDeviceUnits(12);

            // Nom de l'application (« WHATSAPP »)
            var titleRect = new Rectangle(padding, 0, _headerPanel.Width - LogicalToDeviceUnits(60), _headerPanel.Height);
            TextRenderer.DrawText(g, "WHATSAPP", _headerTitleFont, titleRect, Theme.Accent,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            // Avatar de l'utilisateur courant
            int avatarSize = LogicalToDeviceUnits(38);
            int avatarX = _headerPanel.Width - avatarSize - padding;
            int avatarY = (_headerPanel.Height - avatarSize) / 2;
            _myAvatarRect = new Rectangle(avatarX, avatarY, avatarSize, avatarSize);

            var avatarBmp = _headerAvatarCache.GetAvatar(_service.CurrentUser.Id, _service.CurrentUser.DisplayName, avatarSize);
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

            var avatarBmp = _headerAvatarCache.GetAvatar(_selectedHeaderOwnerId, _selectedHeaderTitle ?? string.Empty, avatarSize);
            g.DrawImage(avatarBmp, padding, avatarY);

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

        private async Task OpenNewGroupDialogAsync()
        {
            var contacts = await _service.GetContactsAsync();
            var contactNames = contacts.Select(c => c.DisplayName).ToList();
            using var dlg = new NewGroupForm(contactNames);
            dlg.GroupCreated += async (s, e) =>
            {
                var selectedIds = contacts.Where(c => e.Members.Contains(c.DisplayName)).Select(c => c.Id).ToList();
                var created = await _service.CreateGroupAsync(e.Title, selectedIds);
                await ReloadConversationsAsync();
                OnConversationSelected(this, created.Id);
                ShowStatusMessage($"Groupe « {created.Title} » créé avec succès.");
            };
            dlg.ShowDialog(this);
        }

        private void OpenNewContactDialog()
        {
            using var dlg = new NewContactForm();
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                ShowStatusMessage($"Contact P2P « {dlg.ContactName} » ({dlg.IpAddress}) ajouté.");
            }
        }

        private void OpenSettingsDialog()
        {
            using var dlg = new SettingsForm(_service.CurrentUser.DisplayName, "127.0.0.1", _service.CurrentUser.Id, Theme.Accent);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                ShowStatusMessage("Paramètres enregistrés.");
            }
        }

        private async Task OpenGroupInfoIfApplicableAsync()
        {
            if (!_selectedConversationId.HasValue) return;
            var conversations = await _service.GetConversationsAsync();
            var conv = conversations.FirstOrDefault(c => c.Id == _selectedConversationId.Value);
            if (conv == null || conv.Type != ConversationType.Group) return;

            var contacts = await _service.GetContactsAsync();
            var contactsMap = contacts.ToDictionary(c => c.Id, c => c.DisplayName);
            var vm = GroupInfoViewModel.FromConversation(conv, _service.CurrentUser.Id, contactsMap);

            using var form = new GroupInfoForm(vm);
            form.RemoveMemberRequested += async (s, userId) =>
            {
                var ok = await _service.RemoveMemberAsync(conv.Id, userId);
                if (ok)
                {
                    ShowStatusMessage("Membre retiré du groupe.");
                    await ReloadConversationsAsync();
                    await LoadMessagesForSelectedConversationAsync();
                }
            };
            form.LeaveGroupRequested += async (s, e) =>
            {
                var ok = await _service.LeaveGroupAsync(conv.Id);
                if (ok)
                {
                    _selectedConversationId = null;
                    await ReloadConversationsAsync();
                    await LoadMessagesForSelectedConversationAsync();
                    ShowStatusMessage("Vous avez quitté le groupe.");
                }
            };
            form.ShowDialog(this);
        }

        private void StartCall(CallType type)
        {
            if (string.IsNullOrEmpty(_selectedHeaderTitle)) return;
            using var dlg = new CallForm(_selectedHeaderTitle, type);
            dlg.ShowDialog(this);
        }

        private void OnMainFormKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.F)
            {
                if (_selectedConversationId.HasValue)
                {
                    _inChatSearchPanel.Visible = true;
                    _inChatSearchTextBox.Focus();
                    _inChatSearchTextBox.SelectAll();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }
            else if (e.KeyCode == Keys.Escape && _inChatSearchPanel.Visible)
            {
                _inChatSearchPanel.Visible = false;
                _inChatSearchTextBox.Clear();
                _messageListControl.SetSearchQuery(null);
                _inputControl.FocusInput();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private async Task UpdateWindowTitleAsync()
        {
            var conversations = await _service.GetConversationsAsync();
            int totalUnread = 0;
            foreach (var conv in conversations)
            {
                totalUnread += await _service.GetUnreadCountAsync(conv.Id);
            }

            string? status = null;
            if (_selectedConversationId.HasValue)
            {
                var conv = conversations.FirstOrDefault(c => c.Id == _selectedConversationId.Value);
                if (conv != null)
                {
                    status = conv.Type == ConversationType.Group
                        ? $"{conv.Members.Count} participants"
                        : "En ligne";
                }
            }

            Text = SelfTest.Phase7ETests.FormatTitle(totalUnread, _selectedHeaderTitle, status);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _notifyIcon.Dispose();
                _statusBannerTimer.Dispose();
                _avatars.PhotoChanged -= OnAvatarPhotoChanged;
                _headerAvatarCache.Dispose();
                _headerTitleFont.Dispose();
                _rightEmptyFont.Dispose();
                _rightSelectedNameFont.Dispose();
                _rightSubtitleFont.Dispose();
            }

            base.Dispose(disposing);
        }

        private sealed class ThemeContextMenuRenderer : ToolStripProfessionalRenderer
        {
            public ThemeContextMenuRenderer() : base(new ThemeColorTable()) { }

            private sealed class ThemeColorTable : ProfessionalColorTable
            {
                public override Color MenuBorder => Theme.Separator;
                public override Color ToolStripDropDownBackground => Theme.Header;
                public override Color MenuItemSelected => Theme.Selected;
                public override Color MenuItemBorder => Theme.Selected;
            }
        }
    }
}
