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
        private readonly ProfileStore? _profileStore;
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
        private readonly Panel _sectionPanel;
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
        public MainForm(IChatService service, IAvatarProvider avatars, ProfileStore? profileStore = null)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _avatars = avatars ?? throw new ArgumentNullException(nameof(avatars));
            _profileStore = profileStore;

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

            _demoPanel = null;

            // Liste des conversations
            _listControl = new ConversationListControl(_avatars)
            {
                Dock = DockStyle.Fill
            };
            _listControl.SelectedConversationChanged += OnConversationSelected;

            _listControl.ConversationRightClicked += OnConversationRightClicked;

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

            _sectionPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.AppBackground,
                Visible = false
            };

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
            _messageListControl.MediaClicked += OnMediaClicked;

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
            _rightPanel.Controls.Add(_sectionPanel);
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
            foreach (var option in new[]
            {
                ("Désactivés", 0),
                ("24 heures", 24),
                ("7 jours", 168),
                ("90 jours", 2160)
            })
            {
                var optionItem = new ToolStripMenuItem(option.Item1);
                optionItem.Click += async (s, e) =>
                {
                    if (!_selectedConversationId.HasValue)
                    {
                        ShowStatusMessage("Sélectionnez une discussion.");
                        return;
                    }
                    bool ok = await _service.SetEphemeralAsync(_selectedConversationId.Value, option.Item2);
                    ShowStatusMessage(ok
                        ? $"Messages éphémères : {option.Item1}"
                        : "Impossible de modifier ce réglage.");
                };
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
                    ShowSectionView(null);
                    break;
                case "metaai":
                    ShowSectionView("metaai");
                    break;
                case "profile":
                    OpenSettingsDialog();
                    break;
                case "contacts":
                    ShowSectionView("contacts");
                    break;
                case "archives":
                    ShowSectionView("archives");
                    break;
                case "calls":
                    ShowSectionView("calls");
                    break;
                case "status":
                    ShowSectionView("status");
                    break;
                case "media":
                    ShowSectionView("media");
                    break;
                default:
                    ShowStatusMessage($"Section : {key}");
                    break;
            }
        }

        private void ShowSectionView(string? section)
        {
            if (section == null)
            {
                _sectionPanel.Visible = false;
                _sectionPanel.Controls.Clear();
                if (_selectedConversationId.HasValue)
                {
                    _rightHeaderPanel.Visible = true;
                    _messageListControl.Visible = true;
                    _inputControl.Visible = true;
                    _inputControl.Enabled = true;
                }
                return;
            }

            _rightHeaderPanel.Visible = false;
            _messageListControl.Visible = false;
            _inputControl.Visible = false;
            _inChatSearchPanel.Visible = false;
            _statusBannerPanel.Visible = false;

            foreach (Control control in _sectionPanel.Controls)
                control.Dispose();
            _sectionPanel.Controls.Clear();

            if (section == "metaai")
            {
                var meta = new MetaAiForm
                {
                    TopLevel = false,
                    FormBorderStyle = FormBorderStyle.None,
                    Dock = DockStyle.Fill
                };
                meta.Show();
                _sectionPanel.Controls.Add(meta);
            }
            else
            {
                var title = new Label
                {
                    Dock = DockStyle.Top,
                    Height = LogicalToDeviceUnits(60),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(LogicalToDeviceUnits(24), 0, 0, 0),
                    Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                    ForeColor = Theme.TextPrimary,
                    BackColor = Theme.Header
                };
                title.Text = section switch
                {
                    "contacts" => "Contacts",
                    "archives" => "Discussions archivées",
                    "calls" => "Historique des appels",
                    "status" => "Statuts",
                    "media" => "Médias",
                    _ => section
                };

                var content = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Theme.AppBackground,
                    Padding = new Padding(LogicalToDeviceUnits(24))
                };

                var empty = new Label
                {
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 11f),
                    ForeColor = Theme.TextSecondary
                };
                empty.Text = section switch
                {
                    "contacts" => "Utilisez le bouton + pour enregistrer un nouveau contact.",
                    "archives" => "Aucune discussion archivée.",
                    "calls" => "Aucun appel dans l'historique.",
                    "status" => "Aucun statut disponible.",
                    "media" => "Les médias apparaîtront ici depuis vos discussions.",
                    _ => "Aucun contenu."
                };
                content.Controls.Add(empty);
                _ = PopulateSectionAsync(section, content, empty);

                if (section == "contacts")
                {
                    var add = new Button
                    {
                        Text = "+",
                        Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                        Size = new Size(LogicalToDeviceUnits(56), LogicalToDeviceUnits(56)),
                        Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                        Location = new Point(content.Width - LogicalToDeviceUnits(80), content.Height - LogicalToDeviceUnits(80)),
                        BackColor = Theme.Accent,
                        ForeColor = Theme.AccentText,
                        FlatStyle = FlatStyle.Flat,
                        Cursor = Cursors.Hand
                    };
                    add.FlatAppearance.BorderSize = 0;
                    add.Click += (s, e) => OpenNewContactDialog();
                    content.Controls.Add(add);
                    add.BringToFront();
                }

                _sectionPanel.Controls.Add(content);
                _sectionPanel.Controls.Add(title);
            }

            _sectionPanel.Visible = true;
            _sectionPanel.BringToFront();
        }

        private async Task PopulateSectionAsync(string section, Panel content, Label empty)
        {
            try
            {
                if (section == "contacts")
                {
                    var contacts = await _service.GetContactsAsync();
                    if (contacts.Count == 0)
                        return;

                    empty.Visible = false;
                    content.Controls.Clear();
                    var list = new FlowLayoutPanel
                    {
                        Dock = DockStyle.Fill,
                        FlowDirection = FlowDirection.TopDown,
                        WrapContents = false,
                        AutoScroll = true,
                        BackColor = Theme.AppBackground
                    };
                    foreach (var contact in contacts)
                    {
                        var card = new Button
                        {
                            Text = $"{contact.DisplayName}\n{contact.IpAddress ?? "IP non enregistrée"}",
                            Width = Math.Max(260, content.Width - LogicalToDeviceUnits(120)),
                            Height = LogicalToDeviceUnits(64),
                            TextAlign = ContentAlignment.MiddleLeft,
                            BackColor = Theme.Panel,
                            ForeColor = Theme.TextPrimary,
                            FlatStyle = FlatStyle.Flat,
                            Margin = new Padding(0, 0, 0, LogicalToDeviceUnits(8)),
                            Tag = contact
                        };
                        card.FlatAppearance.BorderSize = 0;
                        card.Click += async (s, e) =>
                        {
                            if (card.Tag is not Contact target)
                                return;
                            var conversations = await _service.GetConversationsAsync();
                            var direct = conversations.FirstOrDefault(c =>
                                c.Type == ConversationType.Direct
                                && c.Members.Any(m => m.UserId == target.Id));
                            if (direct != null)
                                OnConversationSelected(this, direct.Id);
                            ShowSectionView(null);
                        };
                        list.Controls.Add(card);
                    }
                    content.Controls.Add(list);
                }
                else if (section == "archives")
                {
                    var conversations = await _service.GetConversationsAsync();
                    var archived = conversations.Where(c => c.IsArchived).ToList();
                    if (archived.Count == 0)
                        return;

                    empty.Visible = false;
                    content.Controls.Clear();
                    var list = new FlowLayoutPanel
                    {
                        Dock = DockStyle.Fill,
                        FlowDirection = FlowDirection.TopDown,
                        WrapContents = false,
                        AutoScroll = true
                    };
                    foreach (var conversation in archived)
                    {
                        var card = new Button
                        {
                            Text = "  " + (conversation.Type == ConversationType.Group
                                ? conversation.Title
                                : "Discussion"),
                            Width = Math.Max(260, content.Width - LogicalToDeviceUnits(120)),
                            Height = LogicalToDeviceUnits(56),
                            TextAlign = ContentAlignment.MiddleLeft,
                            BackColor = Theme.Panel,
                            ForeColor = Theme.TextPrimary,
                            FlatStyle = FlatStyle.Flat,
                            Tag = conversation.Id
                        };
                        card.FlatAppearance.BorderSize = 0;
                        card.Click += async (s, e) =>
                        {
                            await _service.ArchiveConversationAsync((Guid)card.Tag, false);
                            await ReloadConversationsAsync();
                            ShowSectionView("archives");
                        };
                        list.Controls.Add(card);
                    }
                    content.Controls.Add(list);
                }
                else if (section == "media")
                {
                    var conversations = await _service.GetConversationsAsync();
                    var mediaMessages = new List<Core.Models.Message>();
                    foreach (var conversation in conversations)
                        mediaMessages.AddRange((await _service.GetMessagesAsync(conversation.Id))
                            .Where(m => m.MediaPath != null));

                    if (mediaMessages.Count == 0)
                        return;

                    empty.Visible = false;
                    content.Controls.Clear();
                    var list = new FlowLayoutPanel
                    {
                        Dock = DockStyle.Fill,
                        FlowDirection = FlowDirection.LeftToRight,
                        WrapContents = true,
                        AutoScroll = true
                    };
                    foreach (var message in mediaMessages)
                    {
                        if (string.IsNullOrWhiteSpace(message.MediaPath))
                            continue;
                        string mediaPath = message.MediaPath;
                        var card = new Button
                        {
                            Text = System.IO.Path.GetFileName(mediaPath),
                            Size = new Size(LogicalToDeviceUnits(160), LogicalToDeviceUnits(80)),
                            BackColor = Theme.Panel,
                            ForeColor = Theme.TextPrimary,
                            FlatStyle = FlatStyle.Flat,
                            Tag = mediaPath
                        };
                        card.FlatAppearance.BorderSize = 0;
                        card.Click += (s, e) => OnMediaClicked((string)card.Tag, message.MediaType ?? "document");
                        list.Controls.Add(card);
                    }
                    content.Controls.Add(list);
                }
            }
            catch (Exception ex)
            {
                empty.Text = $"Chargement impossible : {ex.Message}";
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

        private void OnMediaClicked(string mediaPath, string mediaType)
        {
            if (string.IsNullOrWhiteSpace(mediaPath) || !System.IO.File.Exists(mediaPath))
            {
                ShowStatusMessage("Le fichier media n'est plus disponible.");
                return;
            }

            if (mediaType.Equals("image", StringComparison.OrdinalIgnoreCase))
            {
                var preview = new Form
                {
                    Text = "Apercu image",
                    Size = new Size(800, 600),
                    StartPosition = FormStartPosition.CenterParent,
                    BackColor = Theme.AppBackground
                };
                var picture = new PictureBox
                {
                    Dock = DockStyle.Fill,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Image = Image.FromFile(mediaPath)
                };
                preview.FormClosed += (s, e) =>
                {
                    picture.Image?.Dispose();
                };
                preview.Controls.Add(picture);
                preview.Show(this);
                return;
            }

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = mediaPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ShowStatusMessage($"Ouverture impossible : {ex.Message}");
            }
        }

        private async void OnAttachmentRequested(string kind)
        {
            if (!_selectedConversationId.HasValue) return;

            if (kind == "voicerecord")
            {
                string voiceMediaRoot = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "WHATSAPP_CHAPT", "Media");
                System.IO.Directory.CreateDirectory(voiceMediaRoot);
                string voicePath = System.IO.Path.Combine(voiceMediaRoot, $"voice_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.wav");
                using var recorder = new VoiceRecorderForm(voicePath);
                if (recorder.ShowDialog(this) == DialogResult.OK
                    && System.IO.File.Exists(recorder.OutputPath))
                {
                    await _service.SendMediaMessageAsync(
                        _selectedConversationId.Value,
                        recorder.OutputPath,
                        "audio",
                        new System.IO.FileInfo(recorder.OutputPath).Length);
                    _messageListControl.ScrollToBottom();
                    ShowStatusMessage("Message vocal envoyé.");
                }
                return;
            }

            if (kind == "screenshot")
            {
                try
                {
                    Rectangle bounds = SystemInformation.VirtualScreen;
                    using var bitmap = new Bitmap(bounds.Width, bounds.Height);
                    using (Graphics graphics = Graphics.FromImage(bitmap))
                    {
                        graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                    }

                    string screenshotMediaRoot = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "WHATSAPP_CHAPT", "Media");
                    System.IO.Directory.CreateDirectory(screenshotMediaRoot);
                    string screenshotPath = System.IO.Path.Combine(
                        screenshotMediaRoot,
                        $"capture_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.png");
                    bitmap.Save(screenshotPath, System.Drawing.Imaging.ImageFormat.Png);
                    await _service.SendMediaMessageAsync(
                        _selectedConversationId.Value,
                        screenshotPath,
                        "image",
                        new System.IO.FileInfo(screenshotPath).Length);
                    _messageListControl.ScrollToBottom();
                    ShowStatusMessage("Capture d'écran envoyée.");
                }
                catch (Exception ex)
                {
                    ShowStatusMessage($"Capture impossible : {ex.Message}");
                }
                return;
            }

            if (kind == "sticker")
            {
                string stickersFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Stickers");
                string emojiFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Emoji");
                var stickerFiles = System.IO.Directory.Exists(stickersFolder)
                    ? System.IO.Directory.GetFiles(stickersFolder, "*.png")
                    : Array.Empty<string>();
                if (stickerFiles.Length == 0 && System.IO.Directory.Exists(emojiFolder))
                    stickerFiles = System.IO.Directory.GetFiles(emojiFolder, "*.png");
                if (stickerFiles.Length == 0)
                {
                    ShowStatusMessage("Aucun fichier sticker PNG disponible.");
                    return;
                }

                var menu = new ContextMenuStrip
                {
                    Renderer = new ThemeContextMenuRenderer(),
                    BackColor = Theme.Header,
                    ForeColor = Theme.TextPrimary
                };
                foreach (string stickerFile in stickerFiles)
                {
                    var item = new ToolStripMenuItem(System.IO.Path.GetFileNameWithoutExtension(stickerFile));
                    item.Image = Image.FromFile(stickerFile);
                    item.Click += async (s, e) =>
                    {
                        await _service.SendMediaMessageAsync(
                            _selectedConversationId.Value,
                            stickerFile,
                            "sticker",
                            new System.IO.FileInfo(stickerFile).Length);
                        _messageListControl.ScrollToBottom();
                    };
                    menu.Items.Add(item);
                }
                menu.Show(_inputControl, new Point(0, -menu.PreferredSize.Height));
                return;
            }

            string filter = kind switch
            {
                "image" or "photo" => "Images|*.png;*.jpg;*.jpeg;*.gif;*.bmp",
                "video" => "Vidéos|*.mp4;*.avi;*.mov;*.mkv|Tous les fichiers|*.*",
                "audio" or "voicerecord" => "Audio|*.mp3;*.wav;*.m4a;*.ogg;*.flac",
                "document" => "Documents|*.pdf;*.doc;*.docx;*.xls;*.xlsx;*.ppt;*.pptx;*.txt;*.zip|Tous les fichiers|*.*",
                _ => "Tous les fichiers|*.*"
            };

            using var dialog = new OpenFileDialog
            {
                Filter = filter,
                Title = "Choisir un fichier à joindre",
                Multiselect = kind == "image" || kind == "photo"
            };
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
            foreach (string sourcePath in dialog.FileNames)
            {
                string safeName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}_{System.IO.Path.GetFileName(sourcePath)}";
                string localPath = System.IO.Path.Combine(mediaRoot, safeName);
                System.IO.File.Copy(sourcePath, localPath, overwrite: false);
                await _service.SendMediaMessageAsync(
                    _selectedConversationId.Value,
                    localPath,
                    mediaType,
                    new System.IO.FileInfo(localPath).Length);
            }
            _messageListControl.ScrollToBottom();
            ShowStatusMessage(dialog.FileNames.Length == 1
                ? "Piece jointe envoyee."
                : $"{dialog.FileNames.Length} pieces jointes envoyees.");
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
                const int longMessageLimit = 4000;
                if (text.Length <= longMessageLimit)
                {
                    await _service.SendMessageAsync(conversationId, text, replyToMessageId);
                }
                else
                {
                    for (int start = 0; start < text.Length; start += longMessageLimit)
                    {
                        int length = Math.Min(longMessageLimit, text.Length - start);
                        string chunk = text.Substring(start, length);
                        Guid? reply = start == 0 ? replyToMessageId : null;
                        await _service.SendMessageAsync(conversationId, chunk, reply);
                    }
                }

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
                IsDeleted = msg.IsDeleted,
                MediaPath = msg.MediaPath,
                MediaType = msg.MediaType,
                MediaSize = msg.MediaSize
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

            if (viewItem.HasMedia)
            {
                var openMedia = new ToolStripMenuItem("Ouvrir la piece jointe");
                openMedia.Click += (s, e) => OnMediaClicked(viewItem.MediaPath!, viewItem.MediaType ?? "document");
                contextMenu.Items.Add(openMedia);

                var downloadMedia = new ToolStripMenuItem("Telecharger / enregistrer sous");
                downloadMedia.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(viewItem.MediaPath)) return;
                    using var save = new SaveFileDialog
                    {
                        FileName = System.IO.Path.GetFileName(viewItem.MediaPath),
                        Title = "Enregistrer la piece jointe"
                    };
                    if (save.ShowDialog(this) == DialogResult.OK)
                    {
                        try
                        {
                            System.IO.File.Copy(viewItem.MediaPath, save.FileName, overwrite: true);
                            ShowStatusMessage("Piece jointe enregistree.");
                        }
                        catch (Exception ex)
                        {
                            ShowStatusMessage($"Enregistrement impossible : {ex.Message}");
                        }
                    }
                };
                contextMenu.Items.Add(downloadMedia);
                contextMenu.Items.Add(new ToolStripSeparator());
            }

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
                var itemDelete = new ToolStripMenuItem(viewItem.IsMine ? "Supprimer pour tous" : "Supprimer pour moi");
                itemDelete.Click += async (s, e) =>
                {
                    var confirm = MessageBox.Show(
                        this,
                        viewItem.IsMine
                            ? "Ce message sera supprimé pour tous les utilisateurs connectes."
                            : "Ce message sera supprimé uniquement sur cet appareil.",
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
                OpenSettingsDialog();
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

            var itemOpen = new ToolStripMenuItem("Ouvrir");
            itemOpen.Click += (s, e) => OnConversationSelected(this, conv.Id);
            contextMenu.Items.Add(itemOpen);

            var itemPin = new ToolStripMenuItem(conv.IsPinned ? "Desepingler" : "Epingler");
            itemPin.Click += async (s, e) =>
            {
                await _service.SetConversationPinnedAsync(conv.Id, !conv.IsPinned);
                await ReloadConversationsAsync();
            };
            contextMenu.Items.Add(itemPin);

            var itemMute = new ToolStripMenuItem(conv.IsMuted ? "Reactiver les notifications" : "Mettre en sourdine");
            itemMute.Click += async (s, e) =>
            {
                await _service.SetConversationMutedAsync(conv.Id, !conv.IsMuted);
                ShowStatusMessage(conv.IsMuted ? "Notifications activees." : "Discussion mise en sourdine.");
                await ReloadConversationsAsync();
            };
            contextMenu.Items.Add(itemMute);

            var itemEphemeral = new ToolStripMenuItem("Messages ephemeres");
            foreach (var option in new[]
            {
                ("Desactives", 0),
                ("24 heures", 24),
                ("7 jours", 168),
                ("90 jours", 2160)
            })
            {
                var optionItem = new ToolStripMenuItem(option.Item1);
                optionItem.Click += async (s, e) =>
                {
                    await _service.SetEphemeralAsync(conv.Id, option.Item2);
                    ShowStatusMessage($"Messages ephemeres : {option.Item1}");
                };
                itemEphemeral.DropDownItems.Add(optionItem);
            }
            contextMenu.Items.Add(itemEphemeral);

            var itemPassword = new ToolStripMenuItem(conv.PasswordHash == null ? "Proteger par mot de passe" : "Retirer le mot de passe");
            itemPassword.Click += async (s, e) =>
            {
                if (conv.PasswordHash != null)
                {
                    await _service.SetConversationPasswordAsync(conv.Id, null);
                    ShowStatusMessage("Mot de passe retire.");
                }
                else
                {
                    using var prompt = CreatePasswordPrompt();
                    if (prompt.ShowDialog(this) == DialogResult.OK)
                    {
                        string password = prompt.Tag as string ?? string.Empty;
                        if (password.Length >= 4)
                        {
                            await _service.SetConversationPasswordAsync(conv.Id, password);
                            ShowStatusMessage("Conversation protegee.");
                        }
                        else
                        {
                            ShowStatusMessage("Le mot de passe doit contenir au moins 4 caracteres.");
                        }
                    }
                }
            };
            contextMenu.Items.Add(itemPassword);

            var itemArchive = new ToolStripMenuItem(conv.IsArchived ? "Désarchiver la discussion" : "Archiver la discussion");
            itemArchive.Click += async (s, e) =>
            {
                await _service.ArchiveConversationAsync(conv.Id, !conv.IsArchived);
                ShowStatusMessage(conv.IsArchived ? "Discussion désarchivée." : "Discussion archivée.");
                await ReloadConversationsAsync();
            };
            contextMenu.Items.Add(itemArchive);

            if (conv.Type == ConversationType.Direct)
            {
                var itemContactInfo = new ToolStripMenuItem("Voir les informations du contact");
                itemContactInfo.Click += async (s, e) =>
                {
                    var contacts = await _service.GetContactsAsync();
                    var contact = contacts.FirstOrDefault(c => c.Id == targetOwnerId);
                    if (contact != null)
                    {
                        ShowStatusMessage($"{contact.DisplayName} - IP : {contact.IpAddress ?? "non enregistrée"}");
                    }
                };
                contextMenu.Items.Add(itemContactInfo);
            }

            contextMenu.Items.Add(new ToolStripSeparator());
            var itemDelete = new ToolStripMenuItem("Supprimer la discussion");
            itemDelete.Click += async (s, e) =>
            {
                var confirm = MessageBox.Show(this,
                    "La conversation, ses messages et son adresse IP enregistree seront supprimes.",
                    "Supprimer",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (confirm == DialogResult.Yes)
                {
                    await _service.DeleteConversationAsync(conv.Id);
                    if (conv.Type == ConversationType.Direct)
                    {
                        await _service.DeleteContactAsync(targetOwnerId);
                    }
                    await ReloadConversationsAsync();
                }
            };
            contextMenu.Items.Add(itemDelete);
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

        private Form CreatePasswordPrompt()
        {
            var form = new Form
            {
                Text = "Proteger la conversation",
                Size = new Size(360, 170),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Theme.AppBackground,
                ForeColor = Theme.TextPrimary
            };
            var label = new Label
            {
                Text = "Mot de passe (minimum 4 caracteres)",
                Location = new Point(20, 15),
                Size = new Size(300, 22),
                ForeColor = Theme.TextPrimary
            };
            var input = new TextBox
            {
                Location = new Point(20, 45),
                Size = new Size(300, 28),
                PasswordChar = '*',
                BackColor = Theme.SearchBackground,
                ForeColor = Theme.TextPrimary
            };
            var ok = new Button
            {
                Text = "Proteger",
                DialogResult = DialogResult.OK,
                Location = new Point(130, 90),
                Size = new Size(90, 30),
                BackColor = Theme.Accent,
                ForeColor = Theme.AccentText,
                FlatStyle = FlatStyle.Flat
            };
            ok.FlatAppearance.BorderSize = 0;
            ok.Click += (s, e) => form.Tag = input.Text;
            var cancel = new Button
            {
                Text = "Annuler",
                DialogResult = DialogResult.Cancel,
                Location = new Point(230, 90),
                Size = new Size(90, 30),
                BackColor = Theme.Panel,
                ForeColor = Theme.TextPrimary,
                FlatStyle = FlatStyle.Flat
            };
            cancel.FlatAppearance.BorderSize = 0;
            form.Controls.Add(label);
            form.Controls.Add(input);
            form.Controls.Add(ok);
            form.Controls.Add(cancel);
            form.AcceptButton = ok;
            form.CancelButton = cancel;
            return form;
        }

        private async void OpenNewContactDialog()
        {
            using var dlg = new NewContactForm();
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                await _service.AddContactAsync(dlg.ContactName, dlg.IpAddress);
                await ReloadConversationsAsync();
                ShowStatusMessage($"Contact P2P « {dlg.ContactName} » ({dlg.IpAddress}) ajouté.");
            }
        }

        private void OpenSettingsDialog()
        {
            using var dlg = new SettingsForm(_service.CurrentUser.DisplayName, "127.0.0.1", _service.CurrentUser.Id, Theme.Accent, _avatars);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                Theme.SetAccent(dlg.ThemeColor);
                Theme.SetThemeMode(dlg.ThemeMode);
                if (_profileStore != null)
                {
                    var profile = _profileStore.Load();
                    profile.DisplayName = dlg.DisplayName;
                    profile.IpAddress = dlg.LocalIp;
                    profile.ThemeMode = dlg.ThemeMode;
                    _profileStore.Save(profile);
                }
                if (_avatars is IAvatarProvider avatarProvider)
                {
                    _headerPanel.Invalidate();
                }
                _headerPanel.Invalidate();
                _rightPanel.Invalidate();
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
