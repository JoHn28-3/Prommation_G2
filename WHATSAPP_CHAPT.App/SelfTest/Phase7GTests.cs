#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WHATSAPP_CHAPT.Storage;

namespace WHATSAPP_CHAPT.App.SelfTest
{
    /// <summary>
    /// Autotests pour la phase G (P2P Networking & Storage persistant des pairs et archives).
    /// </summary>
    public static class Phase7GTests
    {
        public static void Run(List<(string Name, bool Success, string? Detail)> results)
        {
            Test("Storage_InsertPeerEtGetPeers", () =>
            {
                var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(folder);
                using var storage = new StorageContext(folder);

                var peerId = Guid.NewGuid();
                storage.InsertPeer(peerId, "Bob Peer", "192.168.1.50");

                var peers = storage.GetPeers();
                var bob = peers.FirstOrDefault(p => p.Id == peerId);
                if (bob == null) throw new Exception("Peer inséré introuvable");
                if (bob.Name != "Bob Peer") throw new Exception($"Nom incorrect: {bob.Name}");
                if (bob.IpAddress != "192.168.1.50") throw new Exception($"IP incorrecte: {bob.IpAddress}");
            }, results);

            Test("Storage_ArchivedContacts_ArchiveEtUnarchive", () =>
            {
                var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(folder);
                using var storage = new StorageContext(folder);

                var contactId = Guid.NewGuid();
                storage.InsertContact(contactId, "Charlie", new byte[] { 1, 2, 3 });

                // Archiver
                storage.ArchiveContact(contactId);
                var archives = storage.GetArchivedContacts();
                if (!archives.Contains("Charlie")) throw new Exception("Le contact archivé n'apparaît pas dans ArchivedContacts");

                // Désarchiver
                storage.UnarchiveContact(contactId);
                archives = storage.GetArchivedContacts();
                if (archives.Contains("Charlie")) throw new Exception("Le contact ne devrait plus être dans ArchivedContacts après désarchivage");
            }, results);

            Test("P2P_ProtocoleHandshake_FormatValide", () =>
            {
                var localId = Guid.NewGuid();
                string payload = $"HELLO:{localId}";
                if (!payload.StartsWith("HELLO:")) throw new Exception("Préfixe de handshake manquant");

                string extracted = payload.Substring(6);
                if (!Guid.TryParse(extracted, out var parsedGuid) || parsedGuid != localId)
                    throw new Exception("Le GUID distant extrait du handshake est invalide");
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
