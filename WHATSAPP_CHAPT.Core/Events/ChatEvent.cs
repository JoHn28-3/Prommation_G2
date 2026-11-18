#nullable enable

namespace WHATSAPP_CHAPT.Core.Events
{
    /// <summary>
    /// Types d'événements de chat.
    /// </summary>
    public enum ChatEventType
    {
        MessageAdded,
        MessageEdited,
        MessageDeleted,
        MessageStatusChanged,
        ConversationUpdated
    }

    /// <summary>
    /// Représente un événement de chat.
    /// </summary>
    public sealed record ChatEvent(ChatEventType Type, Guid ConversationId, Guid? MessageId, DateTimeOffset OccurredAt);
}
