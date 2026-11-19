#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using WHATSAPP_CHAPT.App.Services;
using WHATSAPP_CHAPT.Core.Models;
using WHATSAPP_CHAPT.Storage;

namespace WHATSAPP_CHAPT.App.SelfTest
{
    /// <summary>
    /// Autotests pour la phase J (Finalisation, Intégration globale et Contrôle qualité).
    /// </summary>
    public static class Phase7JTests
    {
        public static void Run(List<(string Name, bool Success, string? Detail)> results)
        {
            Test("Integration_ServicesEtStockageComplets", () =>
            {
                var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(folder);

                using var storage = new StorageContext(folder);
                var connService = new ConnectionService(storage);
                var callService = new CallService(connService);
                var chatService = new DemoChatService();

                if (chatService.CurrentUser.Id == Guid.Empty)
                    throw new Exception("CurrentUser invalide dans DemoChatService");

                var convs = chatService.GetConversationsAsync().GetAwaiter().GetResult();
                if (convs == null || convs.Count == 0)
                    throw new Exception("Conversations de démonstration absentes");
            }, results);

            Test("Integration_TablesSQLiteCreesAutomatiquement", () =>
            {
                var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(folder);
                using var storage = new StorageContext(folder);

                // Vérifier que Peers et ArchivedContacts acceptent des insertions
                var peerId = Guid.NewGuid();
                storage.InsertPeer(peerId, "Peer J", "127.0.0.1");

                var contactId = Guid.NewGuid();
                storage.InsertContact(contactId, "Contact J", new byte[] { 1, 2, 3 });
                storage.ArchiveContact(contactId);

                var archived = storage.GetArchivedContacts();
                if (!archived.Contains("Contact J"))
                    throw new Exception("Table ArchivedContacts non fonctionnelle");
            }, results);

            Test("Integration_TousComposantsP2PValides", () =>
            {
                var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(folder);
                var im = new IdentityManager(folder);

                if (im.UserId == Guid.Empty) throw new Exception("IdentityManager UserId vide");
                if (im.PublicKey.Length == 0) throw new Exception("IdentityManager PublicKey vide");

                string card = im.ExportContactCard("Test Final");
                if (!card.StartsWith("WCARD1:")) throw new Exception("Contact card format invalide");
            }, results);
        }

        private static void Test(string name, Action body, List<(string Name, bool Success, string? Detail)> results)
        {
            try
            {
                body();
                results.Add((name, true, null));
            }
            catch (Exception ex)
            {
                results.Add((name, false, ex.Message));
            }
        }
    }
}
