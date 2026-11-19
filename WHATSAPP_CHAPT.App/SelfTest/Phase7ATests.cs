#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using WHATSAPP_CHAPT.App.Drawing;
using WHATSAPP_CHAPT.App.Logic;
using WHATSAPP_CHAPT.App.Models;
using WHATSAPP_CHAPT.App.Services;
using WHATSAPP_CHAPT.Core.Events;
using WHATSAPP_CHAPT.Core.Models;

namespace WHATSAPP_CHAPT.App.SelfTest
{
    /// <summary>
    /// Autotests pour la phase 7A (Actions sur les messages).
    /// </summary>
    public static class Phase7ATests
    {
        public static void Run(List<(string Name, bool Success, string? Detail)> results)
        {
            Test("GetAvailableActions_MonMessageNormal_Renvoie4Actions", () =>
            {
                var item = new MessageViewItem
                {
                    Id = Guid.NewGuid(),
                    IsMine = true,
                    IsDeleted = false,
                    Text = "Test"
                };
                var actions = MessageActions.GetAvailableActions(item);
                if (actions.Count != 4)
                    throw new Exception($"Attendu 4 actions, obtenu {actions.Count}");
                if (!actions.Contains(MessageAction.Reply) ||
                    !actions.Contains(MessageAction.Copy) ||
                    !actions.Contains(MessageAction.Edit) ||
                    !actions.Contains(MessageAction.Delete))
                    throw new Exception("Actions attendues manquantes");
            }, results);

            Test("GetAvailableActions_MessageAutre_RenvoieReplyEtCopy", () =>
            {
                var item = new MessageViewItem
                {
                    Id = Guid.NewGuid(),
                    IsMine = false,
                    IsDeleted = false,
                    Text = "Test autre"
                };
                var actions = MessageActions.GetAvailableActions(item);
                if (actions.Count != 2)
                    throw new Exception($"Attendu 2 actions, obtenu {actions.Count}");
                if (!actions.Contains(MessageAction.Reply) || !actions.Contains(MessageAction.Copy))
                    throw new Exception("Actions attendues : Reply et Copy");
            }, results);

            Test("GetAvailableActions_MessageSupprime_RenvoieAucuneAction", () =>
            {
                var item = new MessageViewItem
                {
                    Id = Guid.NewGuid(),
                    IsMine = true,
                    IsDeleted = true,
                    Text = ""
                };
                var actions = MessageActions.GetAvailableActions(item);
                if (actions.Count != 0)
                    throw new Exception($"Attendu 0 action pour message supprimé, obtenu {actions.Count}");
            }, results);

            Test("EditMessageAsync_MonMessage_Succes", () =>
            {
                var service = new DemoChatService();
                var convs = service.GetConversationsAsync().GetAwaiter().GetResult();
                var conv = convs.First();

                var sent = service.SendMessageAsync(conv.Id, "Texte initial").GetAwaiter().GetResult();

                ChatEvent? receivedEvent = null;
                service.ChatEventRaised += (s, e) =>
                {
                    if (e.Type == ChatEventType.MessageEdited && e.MessageId == sent.Id)
                        receivedEvent = e;
                };

                bool ok = service.EditMessageAsync(sent.Id, "Texte modifié").GetAwaiter().GetResult();
                if (!ok) throw new Exception("EditMessageAsync a renvoyé false");

                var msgs = service.GetMessagesAsync(conv.Id).GetAwaiter().GetResult();
                var edited = msgs.First(m => m.Id == sent.Id);

                if (edited.Content != "Texte modifié") throw new Exception($"Contenu attendu 'Texte modifié', obtenu '{edited.Content}'");
                if (!edited.IsEdited) throw new Exception("IsEdited est false");
                if (receivedEvent == null) throw new Exception("Événement MessageEdited non reçu");
            }, results);

            Test("EditMessageAsync_MessageAutre_RenvoieFalse", () =>
            {
                var service = new DemoChatService();
                var convs = service.GetConversationsAsync().GetAwaiter().GetResult();
                var conv = convs.First();
                var msgs = service.GetMessagesAsync(conv.Id).GetAwaiter().GetResult();
                var otherMsg = msgs.First(m => m.AuthorId != service.CurrentUser.Id && !m.IsDeleted);

                bool ok = service.EditMessageAsync(otherMsg.Id, "Tentative").GetAwaiter().GetResult();
                if (ok) throw new Exception("Attendu false pour la modification du message d'un autre utilisateur");
            }, results);

            Test("EditMessageAsync_MessageSupprime_RenvoieFalse", () =>
            {
                var service = new DemoChatService();
                var convs = service.GetConversationsAsync().GetAwaiter().GetResult();
                var conv = convs.First();
                var sent = service.SendMessageAsync(conv.Id, "À supprimer").GetAwaiter().GetResult();

                service.DeleteMessageAsync(sent.Id).GetAwaiter().GetResult();

                bool ok = service.EditMessageAsync(sent.Id, "Tentative sur supprimé").GetAwaiter().GetResult();
                if (ok) throw new Exception("Attendu false pour la modification d'un message supprimé");
            }, results);

            Test("DeleteMessageAsync_MonMessage_Succes", () =>
            {
                var service = new DemoChatService();
                var convs = service.GetConversationsAsync().GetAwaiter().GetResult();
                var conv = convs.First();
                var sent = service.SendMessageAsync(conv.Id, "Texte à effacer").GetAwaiter().GetResult();

                ChatEvent? receivedEvent = null;
                service.ChatEventRaised += (s, e) =>
                {
                    if (e.Type == ChatEventType.MessageDeleted && e.MessageId == sent.Id)
                        receivedEvent = e;
                };

                bool ok = service.DeleteMessageAsync(sent.Id).GetAwaiter().GetResult();
                if (!ok) throw new Exception("DeleteMessageAsync a renvoyé false");

                var msgs = service.GetMessagesAsync(conv.Id).GetAwaiter().GetResult();
                var deleted = msgs.First(m => m.Id == sent.Id);

                if (!deleted.IsDeleted) throw new Exception("IsDeleted est false");
                if (deleted.Content != string.Empty) throw new Exception($"Contenu attendu '', obtenu '{deleted.Content}'");
                if (receivedEvent == null) throw new Exception("Événement MessageDeleted non reçu");
            }, results);

            Test("DeleteMessageAsync_MessageAutre_RenvoieFalse", () =>
            {
                var service = new DemoChatService();
                var convs = service.GetConversationsAsync().GetAwaiter().GetResult();
                var conv = convs.First();
                var msgs = service.GetMessagesAsync(conv.Id).GetAwaiter().GetResult();
                var otherMsg = msgs.First(m => m.AuthorId != service.CurrentUser.Id && !m.IsDeleted);

                bool ok = service.DeleteMessageAsync(otherMsg.Id).GetAwaiter().GetResult();
                if (ok) throw new Exception("Attendu false pour la suppression du message d'un autre utilisateur");
            }, results);

            Test("DeleteMessageAsync_DeuxiemeSuppression_RenvoieFalse", () =>
            {
                var service = new DemoChatService();
                var convs = service.GetConversationsAsync().GetAwaiter().GetResult();
                var conv = convs.First();
                var sent = service.SendMessageAsync(conv.Id, "Unique suppression").GetAwaiter().GetResult();

                bool ok1 = service.DeleteMessageAsync(sent.Id).GetAwaiter().GetResult();
                if (!ok1) throw new Exception("Premier delete a échoué");

                bool ok2 = service.DeleteMessageAsync(sent.Id).GetAwaiter().GetResult();
                if (ok2) throw new Exception("Deuxième delete attendu false mais a renvoyé true");
            }, results);

            Test("MessageLayoutCacheKey_DifferenteQuandReplyPreviewChange", () =>
            {
                var id = Guid.NewGuid();
                var msg1 = new MessageViewItem
                {
                    Id = id,
                    Text = "Contenu",
                    IsEdited = false,
                    IsDeleted = false,
                    ReplyPreview = "Aperçu 1",
                    ReplyAuthorName = "Auteur",
                    ShowAuthorName = false
                };
                var msg2 = new MessageViewItem
                {
                    Id = id,
                    Text = "Contenu",
                    IsEdited = false,
                    IsDeleted = false,
                    ReplyPreview = "Aperçu 2",
                    ReplyAuthorName = "Auteur",
                    ShowAuthorName = false
                };

                var key1 = MessageLayoutEngine.CreateCacheKey(msg1, 300);
                var key2 = MessageLayoutEngine.CreateCacheKey(msg2, 300);

                if (key1 == key2)
                    throw new Exception("Les clés de cache doivent être différentes lorsque ReplyPreview change");

                var key1Clone = MessageLayoutEngine.CreateCacheKey(msg1, 300);
                if (key1 != key1Clone)
                    throw new Exception("Deux clés identiques doivent être égales");
            }, results);
        }

        private static void Test(string name, Action action, List<(string Name, bool Success, string? Detail)> results)
        {
            try
            {
                action();
                results.Add((name, true, null));
            }
            catch (Exception ex)
            {
                results.Add((name, false, ex.Message));
            }
        }
    }
}
