#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WHATSAPP_CHAPT.Core.Models;
using WHATSAPP_CHAPT.Core.Events;

namespace WHATSAPP_CHAPT.Core.Services
{
    /// <summary>
    /// Service de chat gérant les conversations et les messages.
    /// </summary>
    public interface IChatService
    {
        /// <summary>
        /// Identité de l'utilisateur actuel.
        /// </summary>
        UserIdentity CurrentUser { get; }

        /// <summary>
        /// Obtient la liste des contacts.
        /// </summary>
        Task<IReadOnlyList<Contact>> GetContactsAsync();

        /// <summary>
        /// Obtient la liste des conversations.
        /// </summary>
        Task<IReadOnlyList<Conversation>> GetConversationsAsync();

        /// <summary>
        /// Obtient les messages d'une conversation.
        /// </summary>
        Task<IReadOnlyList<Message>> GetMessagesAsync(Guid conversationId);

        /// <summary>
        /// Envoie un nouveau message dans une conversation.
        /// </summary>
        Task<Message> SendMessageAsync(Guid conversationId, string content, Guid? replyToMessageId = null);

        /// <summary>
        /// Ajoute un fichier média local à une conversation.
        /// </summary>
        Task<Message> SendMediaMessageAsync(Guid conversationId, string mediaPath, string mediaType, long mediaSize);

        /// <summary>
        /// Modifie le texte d'un propre message.
        /// </summary>
        Task<bool> EditMessageAsync(Guid messageId, string newContent);

        /// <summary>
        /// Supprime un propre message.
        /// </summary>
        Task<bool> DeleteMessageAsync(Guid messageId);

        /// <summary>
        /// Marque une conversation comme lue.
        /// </summary>
        Task MarkConversationReadAsync(Guid conversationId);

        /// <summary>
        /// Retourne le nombre de messages non lus dans une conversation.
        /// </summary>
        Task<int> GetUnreadCountAsync(Guid conversationId);

        /// <summary>
        /// Crée un nouveau groupe avec les membres spécifiés.
        /// </summary>
        Task<Conversation> CreateGroupAsync(string title, IReadOnlyList<Guid> memberIds);

        /// <summary>
        /// Ajoute un membre à une conversation de groupe.
        /// </summary>
        Task<bool> AddMemberAsync(Guid conversationId, Guid userId);

        /// <summary>
        /// Retire un membre d'une conversation de groupe.
        /// </summary>
        Task<bool> RemoveMemberAsync(Guid conversationId, Guid userId);

        /// <summary>
        /// Modifie le rôle d'un membre dans un groupe.
        /// </summary>
        Task<bool> SetMemberRoleAsync(Guid conversationId, Guid userId, MemberRole role);

        /// <summary>
        /// Quitte une conversation de groupe.
        /// </summary>
        Task<bool> LeaveGroupAsync(Guid conversationId);

        /// <summary>
        /// Archive ou désarchive une conversation (Phase I).
        /// </summary>
        Task<bool> ArchiveConversationAsync(Guid conversationId, bool isArchived);

        /// <summary>
        /// Événement levé lorsqu'un événement de chat se produit.
        /// </summary>
        event EventHandler<ChatEvent>? ChatEventRaised;
    }
}
