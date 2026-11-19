#nullable enable

using System;
using System.Collections.Generic;
using WHATSAPP_CHAPT.App.Models;

namespace WHATSAPP_CHAPT.App.Logic
{
    /// <summary>
    /// Actions possibles sur un message affiché.
    /// </summary>
    public enum MessageAction
    {
        Reply,
        Copy,
        Edit,
        Delete
    }

    /// <summary>
    /// Règles d'éligibilité des actions sur les messages.
    /// </summary>
    public static class MessageActions
    {
        /// <summary>
        /// Détermine les actions autorisées pour un élément de message.
        /// </summary>
        /// <param name="item">Message cible.</param>
        /// <returns>Liste en lecture seule des actions disponibles.</returns>
        public static IReadOnlyList<MessageAction> GetAvailableActions(MessageViewItem item)
        {
            if (item == null || item.IsDeleted)
            {
                return Array.Empty<MessageAction>();
            }

            if (item.IsMine)
            {
                return new[]
                {
                    MessageAction.Reply,
                    MessageAction.Copy,
                    MessageAction.Edit,
                    MessageAction.Delete
                };
            }

            return new[]
            {
                MessageAction.Reply,
                MessageAction.Copy
            };
        }
    }
}
