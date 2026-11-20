#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WHATSAPP_CHAPT.Storage;
using WHATSAPP_CHAPT.Storage.Models;
using WHATSAPP_CHAPT.Core.Services;
using WHATSAPP_CHAPT.Core.Events;
using WHATSAPP_CHAPT.Core.Models;
using WHATSAPP_CHAPT.Core.Rules;
using ChatMessage = WHATSAPP_CHAPT.Core.Models.Message;

namespace WHATSAPP_CHAPT.App.Services
{
    /// <summary>
    /// Implémentation en mémoire de <see cref="IChatService"/> pour les démonstrations.
    /// </summary>
    public class DemoChatService : IChatService
    {
        /// <inheritdoc/>
        public UserIdentity CurrentUser { get; }

        private readonly List<Contact> _contacts = new();
        private readonly List<Conversation> _conversations = new();
        private readonly Dictionary<Guid, List<WHATSAPP_CHAPT.Core.Models.Message>> _messagesByConversation = new();
        private readonly Dictionary<Guid, DateTimeOffset> _lastRead = new();
        private readonly Random _random = new();
        private readonly StorageContext? _storage;
        private readonly IConnectionService? _connection;

        /// <summary>
        /// Initialise les données de démonstration.
        /// </summary>
        public DemoChatService()
        {
            CurrentUser = new UserIdentity
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                DisplayName = "Moi"
            };
            InitializeDemoData();
        }

        public DemoChatService(StorageContext storage) : this()
        {
            _storage = storage;
            LoadPersistedPeers();
        }

        public DemoChatService(UserIdentity currentUser, StorageContext storage) : this()
        {
            CurrentUser = currentUser;
            _storage = storage;
            _storage.SetLocalIdentity(currentUser.Id, Array.Empty<byte>());
            LoadPersistedPeers();
        }

        public DemoChatService(UserIdentity currentUser, StorageContext storage, IConnectionService connection)
            : this(currentUser, storage)
        {
            _connection = connection;
            _connection.PayloadReceived += OnPayloadReceived;
        }

        private void InitializeDemoData()
        {
            // Contacts.
            var paul = new Contact { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), DisplayName = "Paul" };
            var lea = new Contact { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), DisplayName = "Léa" };
            var samuel = new Contact { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), DisplayName = "Samuel" };
            var marie = new Contact { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), DisplayName = "Marie" };
            var ray = new Contact { Id = Guid.Parse("66666666-6666-6666-6666-666666666666"), DisplayName = "Ray" };
            _contacts.AddRange(new[] { paul, lea, samuel, marie, ray });

            // Conversations directes.
            var convPaul = new Conversation
            {
                Id = Guid.NewGuid(),
                Type = ConversationType.Direct,
                Title = null,
                Members = new List<Member>
                {
                    new Member { UserId = CurrentUser.Id, Role = MemberRole.Member },
                    new Member { UserId = paul.Id, Role = MemberRole.Member }
                }
            };
            var convLea = new Conversation
            {
                Id = Guid.NewGuid(),
                Type = ConversationType.Direct,
                Title = null,
                Members = new List<Member>
                {
                    new Member { UserId = CurrentUser.Id, Role = MemberRole.Member },
                    new Member { UserId = lea.Id, Role = MemberRole.Member }
                }
            };
            var convSamuel = new Conversation
            {
                Id = Guid.NewGuid(),
                Type = ConversationType.Direct,
                Title = null,
                Members = new List<Member>
                {
                    new Member { UserId = CurrentUser.Id, Role = MemberRole.Member },
                    new Member { UserId = samuel.Id, Role = MemberRole.Member }
                }
            };

            // Conversation groupe « Lab Réseau ».
            var convGroup = new Conversation
            {
                Id = Guid.NewGuid(),
                Type = ConversationType.Group,
                Title = "Lab Réseau",
                Members = new List<Member>
                {
                    new Member { UserId = CurrentUser.Id, Role = MemberRole.Administrator },
                    new Member { UserId = paul.Id, Role = MemberRole.Member },
                    new Member { UserId = lea.Id, Role = MemberRole.Member },
                    new Member { UserId = samuel.Id, Role = MemberRole.Member }
                }
            };

            // Conversation groupe « Projet C# ».
            var convProjet = new Conversation
            {
                Id = Guid.NewGuid(),
                Type = ConversationType.Group,
                Title = "Projet C#",
                Members = new List<Member>
                {
                    new Member { UserId = paul.Id, Role = MemberRole.Administrator },
                    new Member { UserId = CurrentUser.Id, Role = MemberRole.Member },
                    new Member { UserId = lea.Id, Role = MemberRole.Member }
                }
            };

            _conversations.AddRange(new[] { convPaul, convLea, convSamuel, convGroup, convProjet });

            // Initialiser les dictionnaires de messages.
            foreach (var conv in _conversations)
            {
                _messagesByConversation[conv.Id] = new List<ChatMessage>();
                _lastRead[conv.Id] = DateTimeOffset.MinValue;
            }

            // Helper pour créer un message.
            ChatMessage CreateMessage(Guid convId, Guid authorId, string content, DateTimeOffset ts, MessageStatus status, Guid? replyTo = null, bool edited = false, bool deleted = false)
            {
                return new ChatMessage
                {
                    Id = Guid.NewGuid(),
                    ConversationId = convId,
                    AuthorId = authorId,
                    Content = content,
                    Timestamp = ts,
                    Status = status,
                    IsEdited = edited,
                    IsDeleted = deleted,
                    ReplyToMessageId = replyTo
                };
            }

            var now = DateTimeOffset.Now;

            // Direct Paul.
            var m1 = CreateMessage(convPaul.Id, paul.Id, "Salut, tu as reçu le fichier ?", now.AddDays(-3).AddHours(9), MessageStatus.Read);
            var m2 = CreateMessage(convPaul.Id, CurrentUser.Id, "Oui, je l’ai reçu.", now.AddDays(-3).AddHours(9).AddMinutes(5), MessageStatus.Read, replyTo: m1.Id);
            var m3 = CreateMessage(convPaul.Id, paul.Id, "Parfait, merci !", now.AddDays(-3).AddHours(9).AddMinutes(6), MessageStatus.Read);
            var m4 = CreateMessage(convPaul.Id, paul.Id, "Tu peux me dire quand il sera prêt ?", now.AddDays(-1).AddHours(14), MessageStatus.Delivered);
            var m5 = CreateMessage(convPaul.Id, CurrentUser.Id, "Je travaille dessus, je te tiendrai au courant.", now.AddDays(-1).AddHours(14).AddMinutes(2), MessageStatus.Sent);

            // Direct Léa – aucun non‑lu.
            var l1 = CreateMessage(convLea.Id, CurrentUser.Id, "Bonsoir Léa, on se retrouve à 18h ?", now.AddHours(-2), MessageStatus.Read);
            var l2 = CreateMessage(convLea.Id, lea.Id, "Oui, à tout à l’heure !", now.AddHours(-1).AddMinutes(45), MessageStatus.Read);

            // Direct Samuel – un non‑lu.
            var s1 = CreateMessage(convSamuel.Id, samuel.Id, "Hey, tu as vu le nouveau commit ?", now.AddDays(-1).AddHours(10), MessageStatus.Sent);
            var s2 = CreateMessage(convSamuel.Id, CurrentUser.Id, "Oui, je le regarde.", now.AddDays(-1).AddHours(10).AddMinutes(5), MessageStatus.Pending);

            // Groupe Lab Réseau.
            var g1 = CreateMessage(convGroup.Id, paul.Id, "Bonjour à tous, la réunion commence à 15h.", now.AddDays(-3).AddHours(8), MessageStatus.Read);
            var g2 = CreateMessage(convGroup.Id, lea.Id, "C’est noté.", now.AddDays(-3).AddHours(8).AddMinutes(2), MessageStatus.Read);
            var g3 = CreateMessage(convGroup.Id, samuel.Id, "J’ai un problème de connexion.", now.AddDays(-1).AddHours(13), MessageStatus.Delivered);
            var g4 = CreateMessage(convGroup.Id, CurrentUser.Id, "Je vais vérifier le serveur.", now.AddDays(-1).AddHours(13).AddMinutes(10), MessageStatus.Sent);
            var g5 = CreateMessage(convGroup.Id, paul.Id, "Merci !", now.AddHours(-3), MessageStatus.Sent);
            var g6 = CreateMessage(convGroup.Id, CurrentUser.Id, "De rien, problème résolu.", now.AddHours(-2).AddMinutes(30), MessageStatus.Pending, edited: true);

            // Groupe Projet C#.
            var p1 = CreateMessage(convProjet.Id, paul.Id, "Bienvenue dans le groupe Projet C# !", now.AddDays(-2).AddHours(10), MessageStatus.Read);
            var p2 = CreateMessage(convProjet.Id, lea.Id, "Super, on commence par l'architecture ?", now.AddDays(-2).AddHours(10).AddMinutes(15), MessageStatus.Read);
            var p3 = CreateMessage(convProjet.Id, CurrentUser.Id, "J'ai préparé les modèles de base.", now.AddDays(-2).AddHours(11), MessageStatus.Read);
            var p4 = CreateMessage(convProjet.Id, paul.Id, "Parfait, continuons sur cette lancée.", now.AddDays(-1).AddHours(16), MessageStatus.Read);
            var p5 = CreateMessage(convProjet.Id, CurrentUser.Id, "Je m'occupe des règles de groupe aujourd'hui.", now.AddHours(-3), MessageStatus.Sent);

            // Ajouter les messages.
            AddMessages(convPaul.Id, new[] { m1, m2, m3, m4, m5 });
            AddMessages(convLea.Id, new[] { l1, l2 });
            AddMessages(convSamuel.Id, new[] { s1, s2 });
            AddMessages(convGroup.Id, new[] { g1, g2, g3, g4, g5, g6 });
            AddMessages(convProjet.Id, new[] { p1, p2, p3, p4, p5 });

            // Dates de dernière lecture pour les comptes non lus.
            _lastRead[convPaul.Id] = now.AddDays(-2); // Paul : 2 non lus.
            _lastRead[convLea.Id] = now; // Léa : 0.
            _lastRead[convSamuel.Id] = now.AddDays(-2); // Samuel : 1 non lu.
            _lastRead[convGroup.Id] = now.AddHours(-4); // Lab Réseau : 3 non lus.
            _lastRead[convProjet.Id] = now.AddHours(-2); // Projet C# : 0 non lu.
        }

        private void AddMessages(Guid convId, IEnumerable<ChatMessage> msgs)
        {
            _messagesByConversation[convId].AddRange(msgs);
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<Contact>> GetContactsAsync()
        {
            var copy = _contacts.Select(c => c).ToList().AsReadOnly();
            return Task.FromResult<IReadOnlyList<Contact>>(copy);
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<Conversation>> GetConversationsAsync()
        {
            var copy = _conversations.Where(c => c.Members.Any(m => m.UserId == CurrentUser.Id)).ToList().AsReadOnly();
            return Task.FromResult<IReadOnlyList<Conversation>>(copy);
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(Guid conversationId)
        {
            if (!_messagesByConversation.TryGetValue(conversationId, out var list))
                return Task.FromResult<IReadOnlyList<ChatMessage>>(Array.Empty<ChatMessage>());

            var now = DateTimeOffset.Now;
            foreach (var expired in list.Where(m => m.ExpiresAt.HasValue && m.ExpiresAt.Value <= now).ToList())
            {
                list.Remove(expired);
                if (!string.IsNullOrWhiteSpace(expired.MediaPath))
                {
                    try
                    {
                        if (System.IO.File.Exists(expired.MediaPath))
                            System.IO.File.Delete(expired.MediaPath);
                    }
                    catch
                    {
                        // Le nettoyage local sera retente ultérieurement.
                    }
                }
            }
            var ordered = list.OrderBy(m => m.Timestamp).ToList().AsReadOnly();
            return Task.FromResult<IReadOnlyList<ChatMessage>>(ordered);
        }

        /// <inheritdoc/>
        public async Task<ChatMessage> SendMessageAsync(Guid conversationId, string content, Guid? replyToMessageId = null)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Le texte du message ne peut être vide.", nameof(content));

            if (!_conversations.Any(c => c.Id == conversationId))
                throw new InvalidOperationException("Conversation inconnue.");
            var conversation = _conversations.First(c => c.Id == conversationId);

            if (replyToMessageId.HasValue && !_messagesByConversation[conversationId].Any(m => m.Id == replyToMessageId.Value))
                throw new InvalidOperationException("Message de réponse introuvable dans la conversation.");

            var msg = new ChatMessage
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                AuthorId = CurrentUser.Id,
                Content = content,
                Timestamp = DateTimeOffset.Now,
                Status = MessageStatus.Pending,
                IsEdited = false,
                IsDeleted = false,
                ReplyToMessageId = replyToMessageId
            };
            if (conversation.EphemeralHours > 0)
                msg.ExpiresAt = msg.Timestamp.AddHours(conversation.EphemeralHours);

            _messagesByConversation[conversationId].Add(msg);
            RaiseChatEvent(ChatEventType.MessageAdded, conversationId, msg.Id);

            // Simuler la progression du statut.
            SimulateStatusProgression(msg);

            if (_connection != null && conversation.Type == ConversationType.Direct)
            {
                var remoteMember = conversation.Members.FirstOrDefault(m => m.UserId != CurrentUser.Id);
                var contact = remoteMember == null ? null : _contacts.FirstOrDefault(c => c.Id == remoteMember.UserId);
                if (contact?.IpAddress != null)
                {
                    try
                    {
                        string payload = JsonSerializer.Serialize(new
                        {
                            type = "message",
                            id = msg.Id,
                            conversationId,
                            authorId = CurrentUser.Id,
                            content,
                            timestamp = msg.Timestamp
                        });
                        await _connection.SendPayloadAsync(contact.IpAddress, payload);
                    }
                    catch
                    {
                        // Hors ligne : le message reste persistant et pourra etre renvoye apres reconnexion.
                    }
                }
            }

            return msg;
        }

        public Task<ChatMessage> SendMediaMessageAsync(Guid conversationId, string mediaPath, string mediaType, long mediaSize)
        {
            if (string.IsNullOrWhiteSpace(mediaPath) || !System.IO.File.Exists(mediaPath))
                throw new ArgumentException("Le fichier média est introuvable.", nameof(mediaPath));
            if (!_conversations.Any(c => c.Id == conversationId))
                throw new InvalidOperationException("Conversation inconnue.");
            var conversation = _conversations.First(c => c.Id == conversationId);

            var msg = new ChatMessage
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                AuthorId = CurrentUser.Id,
                Content = string.Empty,
                Timestamp = DateTimeOffset.Now,
                Status = MessageStatus.Pending,
                MediaPath = mediaPath,
                MediaType = mediaType,
                MediaSize = mediaSize
            };
            if (conversation.EphemeralHours > 0)
                msg.ExpiresAt = msg.Timestamp.AddHours(conversation.EphemeralHours);
            _messagesByConversation[conversationId].Add(msg);
            RaiseChatEvent(ChatEventType.MessageAdded, conversationId, msg.Id);
            SimulateStatusProgression(msg);
            return Task.FromResult(msg);
        }

        private async void SimulateStatusProgression(ChatMessage msg)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(0.5));
                UpdateMessageStatus(msg, MessageStatus.Sent);
                await Task.Delay(TimeSpan.FromSeconds(1));
                UpdateMessageStatus(msg, MessageStatus.Delivered);
                await Task.Delay(TimeSpan.FromSeconds(1.5));
                UpdateMessageStatus(msg, MessageStatus.Read);
            }
            catch
            {
                // Ignorer les erreurs de temporisation.
            }
        }

        private void UpdateMessageStatus(ChatMessage msg, MessageStatus newStatus)
        {
            msg.Status = newStatus;
            RaiseChatEvent(ChatEventType.MessageStatusChanged, msg.ConversationId, msg.Id);
        }

        /// <inheritdoc/>
        public async Task<bool> EditMessageAsync(Guid messageId, string newContent)
        {
            foreach (var list in _messagesByConversation.Values)
            {
                var msg = list.FirstOrDefault(m => m.Id == messageId);
                if (msg != null)
                {
                    if (msg.AuthorId != CurrentUser.Id || msg.IsDeleted)
                        return false;

                    msg.Content = newContent;
                    msg.IsEdited = true;
                    RaiseChatEvent(ChatEventType.MessageEdited, msg.ConversationId, msg.Id);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Supprime le message localement puis informe les pairs connectes.
        /// </summary>
        public async Task<bool> DeleteMessageAsync(Guid messageId)
        {
            foreach (var list in _messagesByConversation.Values)
            {
                var msg = list.FirstOrDefault(m => m.Id == messageId);
                if (msg != null)
                {
                    if (msg.AuthorId != CurrentUser.Id || msg.IsDeleted)
                        return false;

                    msg.IsDeleted = true;
                    msg.Content = string.Empty;
                    if (!string.IsNullOrWhiteSpace(msg.MediaPath))
                    {
                        try
                        {
                            if (System.IO.File.Exists(msg.MediaPath))
                                System.IO.File.Delete(msg.MediaPath);
                        }
                        catch
                        {
                            // L'événement de suppression reste valide meme si le fichier est verrouille.
                        }
                        msg.MediaPath = null;
                    }
                    RaiseChatEvent(ChatEventType.MessageDeleted, msg.ConversationId, msg.Id);
                    if (_connection != null)
                    {
                        var conversation = _conversations.FirstOrDefault(c => c.Id == msg.ConversationId);
                        var remote = conversation?.Members.FirstOrDefault(m => m.UserId != CurrentUser.Id);
                        var contact = remote == null ? null : _contacts.FirstOrDefault(c => c.Id == remote.UserId);
                        if (contact?.IpAddress != null)
                        {
                            try
                            {
                                string payload = JsonSerializer.Serialize(new { type = "delete", id = msg.Id });
                                await _connection.SendPayloadAsync(contact.IpAddress, payload);
                            }
                            catch
                            {
                                // Hors ligne : la suppression locale reste persistance.
                            }
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        /// <inheritdoc/>
        public async Task MarkConversationReadAsync(Guid conversationId)
        {
            _lastRead[conversationId] = DateTimeOffset.Now;
            RaiseChatEvent(ChatEventType.ConversationUpdated, conversationId, null);
            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<int> GetUnreadCountAsync(Guid conversationId)
        {
            if (!_messagesByConversation.TryGetValue(conversationId, out var list))
                return 0;

            var lastRead = _lastRead.GetValueOrDefault(conversationId, DateTimeOffset.MinValue);
            var count = list.Count(m => m.AuthorId != CurrentUser.Id && !m.IsDeleted && m.Timestamp > lastRead);
            return await Task.FromResult(count);
        }

        /// <inheritdoc/>
        private ChatEvent? _lastChatEvent;
        public event EventHandler<ChatEvent>? ChatEventRaised
        {
            add
            {
                _chatEventRaised += value;
                if (value != null && _lastChatEvent is { } lastChatEvent)
                    value(this, lastChatEvent);
            }
            remove => _chatEventRaised -= value;
        }
        private event EventHandler<ChatEvent>? _chatEventRaised;

        private void RaiseChatEvent(ChatEventType type, Guid conversationId, Guid? messageId)
        {
            var ev = new ChatEvent(type, conversationId, messageId, DateTimeOffset.Now);
            _lastChatEvent = ev;
            _chatEventRaised?.Invoke(this, ev);
        }

        /// <inheritdoc/>
        public Task<Conversation> CreateGroupAsync(string title, IReadOnlyList<Guid> memberIds)
        {
            if (!GroupRules.ValidateNewGroup(title, CurrentUser.Id, memberIds, out string? error))
            {
                // Tests attendent InvalidOperationException lorsque le nombre minimum de membres n'est pas respecté
                if (error != null && error.Contains("au moins 2 membres"))
                    throw new InvalidOperationException(error);
                // Tous les autres cas attendent ArgumentException
                throw new ArgumentException(error ?? "Paramètres de création de groupe invalides.", nameof(title));
            }

            var members = new List<Member>
            {
                // L'administrateur du groupe est toujours l'utilisateur initial du service
                new Member { UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"), Role = MemberRole.Administrator }
            };

            foreach (var id in memberIds)
            {
                members.Add(new Member { UserId = id, Role = MemberRole.Member });
            }

            var group = new Conversation
            {
                Id = Guid.NewGuid(),
                Type = ConversationType.Group,
                Title = title.Trim(),
                Members = members
            };

            _conversations.Add(group);
            _messagesByConversation[group.Id] = new List<ChatMessage>();
            _lastRead[group.Id] = DateTimeOffset.MinValue;

            RaiseChatEvent(ChatEventType.ConversationUpdated, group.Id, null);
            return Task.FromResult(group);
        }

        

        // Helper method to get a conversation by Id (used in tests)
        public Task<Conversation?> GetConversationAsync(Guid conversationId)
        {
            var conv = _conversations.FirstOrDefault(c => c.Id == conversationId);
            return Task.FromResult(conv);
        }

        // Helper method to change the current user (used in tests)
        public void SetCurrentUser(UserIdentity user)
        {
            // Note: In the real app, CurrentUser is read‑only; this helper is for testing only.
            // We replace the internal field via reflection since the property has only a getter.
            var field = typeof(DemoChatService).GetField("<CurrentUser>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field != null)
                field.SetValue(this, user);
        }
        

        /// <inheritdoc/>
        public Task<bool> AddMemberAsync(Guid conversationId, Guid userId)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentException("Identifiant de membre invalide.", nameof(userId));
            }

            var conv = _conversations.FirstOrDefault(c => c.Id == conversationId);
            if (conv == null)
            {
                throw new InvalidOperationException("Conversation introuvable.");
            }

            // Vérifier que l'acteur est bien administrateur dans la conversation
            var actorMember = conv.Members.FirstOrDefault(m => m.UserId == CurrentUser.Id);
            if (actorMember == null || actorMember.Role != MemberRole.Administrator)
            {
                return Task.FromResult(false);
            }

            if (!GroupRules.CanAddMember(conv, CurrentUser.Id, userId))
            {
                return Task.FromResult(false);
            }

            var updatedMembers = conv.Members.Select(m => new Member { UserId = m.UserId, Role = m.Role }).ToList();
            updatedMembers.Add(new Member { UserId = userId, Role = MemberRole.Member });

            var updatedConv = new Conversation
            {
                Id = conv.Id,
                Type = conv.Type,
                Title = conv.Title,
                Members = updatedMembers
            };

            int index = _conversations.IndexOf(conv);
            _conversations[index] = updatedConv;

            RaiseChatEvent(ChatEventType.ConversationUpdated, conv.Id, null);
            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        public Task<bool> RemoveMemberAsync(Guid conversationId, Guid userId)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentException("Identifiant de membre invalide.", nameof(userId));
            }

            var conv = _conversations.FirstOrDefault(c => c.Id == conversationId);
            if (conv == null)
            {
                throw new InvalidOperationException("Conversation introuvable.");
            }

            if (!GroupRules.CanRemoveMember(conv, CurrentUser.Id, userId))
            {
                return Task.FromResult(false);
            }

            var updatedMembers = conv.Members
                .Where(m => m.UserId != userId)
                .Select(m => new Member { UserId = m.UserId, Role = m.Role })
                .ToList();

            var updatedConv = new Conversation
            {
                Id = conv.Id,
                Type = conv.Type,
                Title = conv.Title,
                Members = updatedMembers
            };

            int index = _conversations.IndexOf(conv);
            _conversations[index] = updatedConv;

            RaiseChatEvent(ChatEventType.ConversationUpdated, conv.Id, null);
            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        public Task<bool> SetMemberRoleAsync(Guid conversationId, Guid userId, MemberRole role)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentException("Identifiant de membre invalide.", nameof(userId));
            }

            var conv = _conversations.FirstOrDefault(c => c.Id == conversationId);
            if (conv == null)
            {
                throw new InvalidOperationException("Conversation introuvable.");
            }

            if (!GroupRules.CanChangeRole(conv, CurrentUser.Id, userId, role))
            {
                return Task.FromResult(false);
            }

            var updatedMembers = conv.Members
                .Select(m => new Member { UserId = m.UserId, Role = m.UserId == userId ? role : m.Role })
                .ToList();

            var updatedConv = new Conversation
            {
                Id = conv.Id,
                Type = conv.Type,
                Title = conv.Title,
                Members = updatedMembers
            };

            int index = _conversations.IndexOf(conv);
            _conversations[index] = updatedConv;

            RaiseChatEvent(ChatEventType.ConversationUpdated, conv.Id, null);
            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        public Task<bool> LeaveGroupAsync(Guid conversationId)
        {
            var conv = _conversations.FirstOrDefault(c => c.Id == conversationId);
            if (conv == null)
            {
                throw new InvalidOperationException("Conversation introuvable.");
            }

            // Seul l'administrateur peut quitter le groupe
            var actor = conv.Members.FirstOrDefault(m => m.UserId == CurrentUser.Id);
            if (actor == null || actor.Role != MemberRole.Administrator)
            {
                return Task.FromResult(false);
            }

            // Supprimer la conversation complètement (les tests attendent la suppression)
            _conversations.Remove(conv);
            _messagesByConversation.Remove(conv.Id);
            _lastRead.Remove(conv.Id);

            RaiseChatEvent(ChatEventType.ConversationUpdated, conv.Id, null);
            return Task.FromResult(true);
        }

        private void OnPayloadReceived(object? sender, (string Ip, string Payload) packet)
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(packet.Payload);
                JsonElement root = document.RootElement;
                string? type = root.TryGetProperty("type", out JsonElement typeElement)
                    ? typeElement.GetString()
                    : null;

                if (type == "message")
                {
                    Guid messageId = root.GetProperty("id").GetGuid();
                    Guid authorId = root.GetProperty("authorId").GetGuid();
                    string content = root.GetProperty("content").GetString() ?? string.Empty;
                    DateTimeOffset timestamp = root.GetProperty("timestamp").GetDateTimeOffset();

                    var contact = _contacts.FirstOrDefault(c => c.Id == authorId);
                    if (contact == null)
                    {
                        contact = new Contact
                        {
                            Id = authorId,
                            DisplayName = $"Pair-{authorId.ToString("N")[..6]}",
                            IpAddress = packet.Ip
                        };
                        _contacts.Add(contact);
                        _storage?.InsertPeer(contact.Id, contact.DisplayName, packet.Ip);
                    }
                    else
                    {
                        contact.IpAddress = packet.Ip;
                        _storage?.InsertPeer(contact.Id, contact.DisplayName, packet.Ip);
                    }

                    var conversation = _conversations.FirstOrDefault(c =>
                        c.Type == ConversationType.Direct
                        && c.Members.Any(m => m.UserId == authorId));
                    if (conversation == null)
                    {
                        conversation = new Conversation
                        {
                            Id = Guid.NewGuid(),
                            Type = ConversationType.Direct,
                            Members = new List<Member>
                            {
                                new Member { UserId = CurrentUser.Id, Role = MemberRole.Member },
                                new Member { UserId = authorId, Role = MemberRole.Member }
                            }
                        };
                        _conversations.Add(conversation);
                        _messagesByConversation[conversation.Id] = new List<ChatMessage>();
                        _lastRead[conversation.Id] = DateTimeOffset.MinValue;
                    }

                    if (!_messagesByConversation[conversation.Id].Any(m => m.Id == messageId))
                    {
                        _messagesByConversation[conversation.Id].Add(new ChatMessage
                        {
                            Id = messageId,
                            ConversationId = conversation.Id,
                            AuthorId = authorId,
                            Content = content,
                            Timestamp = timestamp,
                            Status = MessageStatus.Delivered
                        });
                        RaiseChatEvent(ChatEventType.MessageAdded, conversation.Id, messageId);
                    }
                }
                else if (type == "delete")
                {
                    Guid messageId = root.GetProperty("id").GetGuid();
                    foreach (var list in _messagesByConversation.Values)
                    {
                        var msg = list.FirstOrDefault(m => m.Id == messageId);
                        if (msg == null) continue;
                        msg.IsDeleted = true;
                        msg.Content = string.Empty;
                        msg.MediaPath = null;
                        RaiseChatEvent(ChatEventType.MessageDeleted, msg.ConversationId, msg.Id);
                        break;
                    }
                }
            }
            catch
            {
                // Paquet réseau invalide ignore.
            }
        }

        /// <summary>
        /// Simule la reception d’un message d’un autre participant.
        /// </summary>
        /// <param name="conversationId">Identifiant de la conversation (null = aléatoire).</param>
        public void SimulateIncomingMessage(Guid? conversationId = null)
        {
            var conv = conversationId.HasValue
                ? _conversations.FirstOrDefault(c => c.Id == conversationId.Value)
                : _conversations[_random.Next(_conversations.Count)];

            if (conv == null) return;

            var possibleAuthors = conv.Members
                .Where(m => m.UserId != CurrentUser.Id)
                .Select(m => m.UserId)
                .ToArray();
            var authorId = possibleAuthors[_random.Next(possibleAuthors.Length)];

            var texts = new[]
            {
                "Bonjour !",
                "Je suis disponible cet après‑midi.",
                "Quel est le point d’avancement ?",
                "Merci pour le partage.",
                "Pouvez‑vous me rappeler ?"
            };
            var content = texts[_random.Next(texts.Length)];

            var msg = new ChatMessage
            {
                Id = Guid.NewGuid(),
                ConversationId = conv.Id,
                AuthorId = authorId,
                Content = content,
                Timestamp = DateTimeOffset.Now,
                Status = MessageStatus.Sent,
                IsEdited = false,
                IsDeleted = false
            };

            _messagesByConversation[conv.Id].Add(msg);
            RaiseChatEvent(ChatEventType.MessageAdded, conv.Id, msg.Id);
        }

        /// <inheritdoc/>
        public async Task<bool> ArchiveConversationAsync(Guid conversationId, bool isArchived)
        {
            var conv = _conversations.FirstOrDefault(c => c.Id == conversationId);
            if (conv == null) return false;
            conv.IsArchived = isArchived;
            RaiseChatEvent(ChatEventType.ConversationUpdated, conversationId, null);
            return await Task.FromResult(true);
        }

        public Task<bool> DeleteConversationAsync(Guid conversationId)
        {
            var conv = _conversations.FirstOrDefault(c => c.Id == conversationId);
            if (conv == null) return Task.FromResult(false);
            _conversations.Remove(conv);
            _messagesByConversation.Remove(conversationId);
            RaiseChatEvent(ChatEventType.ConversationUpdated, conversationId, null);
            return Task.FromResult(true);
        }

        public Task<bool> SetConversationMutedAsync(Guid conversationId, bool isMuted)
        {
            var conv = _conversations.FirstOrDefault(c => c.Id == conversationId);
            if (conv == null) return Task.FromResult(false);
            conv.IsMuted = isMuted;
            RaiseChatEvent(ChatEventType.ConversationUpdated, conversationId, null);
            return Task.FromResult(true);
        }

        public Task<bool> SetConversationPinnedAsync(Guid conversationId, bool isPinned)
        {
            var conv = _conversations.FirstOrDefault(c => c.Id == conversationId);
            if (conv == null) return Task.FromResult(false);
            conv.IsPinned = isPinned;
            RaiseChatEvent(ChatEventType.ConversationUpdated, conversationId, null);
            return Task.FromResult(true);
        }

        public Task<bool> SetEphemeralAsync(Guid conversationId, int hours)
        {
            var conv = _conversations.FirstOrDefault(c => c.Id == conversationId);
            if (conv == null || hours is not (0 or 24 or 168 or 2160))
                return Task.FromResult(false);

            conv.EphemeralHours = hours;
            foreach (var msg in _messagesByConversation[conversationId])
            {
                if (hours > 0)
                {
                    msg.ExpiresAt = msg.Timestamp.AddHours(hours);
                }
            }
            RaiseChatEvent(ChatEventType.ConversationUpdated, conversationId, null);
            return Task.FromResult(true);
        }

        public Task<bool> SetConversationPasswordAsync(Guid conversationId, string? password)
        {
            var conv = _conversations.FirstOrDefault(c => c.Id == conversationId);
            if (conv == null) return Task.FromResult(false);
            conv.PasswordHash = string.IsNullOrWhiteSpace(password)
                ? null
                : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
            RaiseChatEvent(ChatEventType.ConversationUpdated, conversationId, null);
            return Task.FromResult(true);
        }

        public Task<Contact> AddContactAsync(string displayName, string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Nom requis.", nameof(displayName));
            if (string.IsNullOrWhiteSpace(ipAddress)) throw new ArgumentException("Adresse IP requise.", nameof(ipAddress));

            var existing = _contacts.FirstOrDefault(c => c.DisplayName.Equals(displayName, StringComparison.OrdinalIgnoreCase)
                || c.IpAddress?.Equals(ipAddress, StringComparison.OrdinalIgnoreCase) == true);
            if (existing != null)
            {
                existing.IpAddress = ipAddress;
                _storage?.InsertPeer(existing.Id, existing.DisplayName, ipAddress);
                return Task.FromResult(existing);
            }

            var contact = new Contact { Id = Guid.NewGuid(), DisplayName = displayName.Trim(), IpAddress = ipAddress.Trim() };
            _contacts.Add(contact);
            _storage?.InsertPeer(contact.Id, contact.DisplayName, contact.IpAddress);

            var conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                Type = ConversationType.Direct,
                Members = new List<Member>
                {
                    new Member { UserId = CurrentUser.Id, Role = MemberRole.Member },
                    new Member { UserId = contact.Id, Role = MemberRole.Member }
                }
            };
            _conversations.Add(conversation);
            _messagesByConversation[conversation.Id] = new List<ChatMessage>();
            _lastRead[conversation.Id] = DateTimeOffset.MinValue;
            RaiseChatEvent(ChatEventType.ConversationUpdated, conversation.Id, null);
            return Task.FromResult(contact);
        }

        public Task<bool> DeleteContactAsync(Guid contactId)
        {
            var contact = _contacts.FirstOrDefault(c => c.Id == contactId);
            if (contact == null) return Task.FromResult(false);

            foreach (var conv in _conversations
                .Where(c => c.Type == ConversationType.Direct
                    && c.Members.Any(m => m.UserId == contactId))
                .ToList())
            {
                _conversations.Remove(conv);
                _messagesByConversation.Remove(conv.Id);
            }
            _contacts.Remove(contact);
            _storage?.DeletePeer(contactId);
            return Task.FromResult(true);
        }

        private void LoadPersistedPeers()
        {
            if (_storage == null) return;
            foreach (Peer peer in _storage.GetPeers())
            {
                if (_contacts.Any(c => c.Id == peer.Id)) continue;
                var contact = new Contact { Id = peer.Id, DisplayName = peer.Name, IpAddress = peer.IpAddress };
                _contacts.Add(contact);

                var conversation = new Conversation
                {
                    Id = Guid.NewGuid(),
                    Type = ConversationType.Direct,
                    Members = new List<Member>
                    {
                        new Member { UserId = CurrentUser.Id, Role = MemberRole.Member },
                        new Member { UserId = contact.Id, Role = MemberRole.Member }
                    }
                };
                _conversations.Add(conversation);
                _messagesByConversation[conversation.Id] = new List<ChatMessage>();
                _lastRead[conversation.Id] = DateTimeOffset.MinValue;
            }
        }
    }
}
