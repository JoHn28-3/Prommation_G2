#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WHATSAPP_CHAPT.Core.Services;
using WHATSAPP_CHAPT.Core.Events;
using WHATSAPP_CHAPT.Core.Models;
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

        /// <summary>
        /// Initialise les données de démonstration.
        /// </summary>
        public DemoChatService()
        {
            // Utilisateur actuel – GUID fixe.
            CurrentUser = new UserIdentity
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                DisplayName = "Moi"
            };

            // Contacts.
            var paul = new Contact { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), DisplayName = "Paul" };
            var lea = new Contact { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), DisplayName = "Léa" };
            var samuel = new Contact { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), DisplayName = "Samuel" };
            _contacts.AddRange(new[] { paul, lea, samuel });

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

            _conversations.AddRange(new[] { convPaul, convLea, convSamuel, convGroup });

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

            // Ajouter les messages.
            AddMessages(convPaul.Id, new[] { m1, m2, m3, m4, m5 });
            AddMessages(convLea.Id, new[] { l1, l2 });
            AddMessages(convSamuel.Id, new[] { s1, s2 });
            AddMessages(convGroup.Id, new[] { g1, g2, g3, g4, g5, g6 });

            // Dates de dernière lecture pour les comptes non lus.
            _lastRead[convPaul.Id] = now.AddDays(-2); // Paul : 2 non lus.
            _lastRead[convLea.Id] = now; // Léa : 0.
            _lastRead[convSamuel.Id] = now.AddDays(-2); // Samuel : 1 non lu.
            _lastRead[convGroup.Id] = now.AddHours(-4); // Lab Réseau : 3 non lus.
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
            var copy = _conversations.Select(c => c).ToList().AsReadOnly();
            return Task.FromResult<IReadOnlyList<Conversation>>(copy);
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(Guid conversationId)
        {
            if (!_messagesByConversation.TryGetValue(conversationId, out var list))
                return Task.FromResult<IReadOnlyList<ChatMessage>>(Array.Empty<ChatMessage>());

            var ordered = list.OrderBy(m => m.Timestamp).ToList().AsReadOnly();
            return Task.FromResult<IReadOnlyList<ChatMessage>>(ordered);
        }

        /// <inheritdoc/>
        public Task<ChatMessage> SendMessageAsync(Guid conversationId, string content, Guid? replyToMessageId = null)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Le texte du message ne peut être vide.", nameof(content));

            if (!_conversations.Any(c => c.Id == conversationId))
                throw new InvalidOperationException("Conversation inconnue.");

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

            _messagesByConversation[conversationId].Add(msg);
            RaiseChatEvent(ChatEventType.MessageAdded, conversationId, msg.Id);

            // Simuler la progression du statut.
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
        /// Suppression locale : à ne jamais synchroniser avec les autres utilisateurs.
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
                    RaiseChatEvent(ChatEventType.MessageDeleted, msg.ConversationId, msg.Id);
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
        public event EventHandler<ChatEvent>? ChatEventRaised;

        private void RaiseChatEvent(ChatEventType type, Guid conversationId, Guid? messageId)
        {
            var ev = new ChatEvent(type, conversationId, messageId, DateTimeOffset.Now);
            ChatEventRaised?.Invoke(this, ev);
        }

        /// <summary>
        /// Simule la réception d’un message d’un autre participant.
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
    }
}
