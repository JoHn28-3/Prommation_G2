#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using WHATSAPP_CHAPT.App.Services;
using WHATSAPP_CHAPT.Core.Models;
using WHATSAPP_CHAPT.Core.Security;

namespace WHATSAPP_CHAPT.App.SelfTest
{
    /// <summary>
    /// Autotests pour la phase I (Sécurité, Chiffrement des messages et Archivage).
    /// </summary>
    public static class Phase7ITests
    {
        public static void Run(List<(string Name, bool Success, string? Detail)> results)
        {
            Test("MessageCrypto_DerivationCle_32Octets", () =>
            {
                byte[] secret = new byte[] { 10, 20, 30, 40 };
                byte[] key = MessageCrypto.DeriveKey(secret);
                if (key.Length != 32) throw new Exception($"Taille attendue 32 mais reçu {key.Length}");
            }, results);

            Test("MessageCrypto_ChiffrementDechiffrement_Roundtrip", () =>
            {
                byte[] key = MessageCrypto.DeriveKey(new byte[] { 1, 2, 3 });
                string original = "Message confidentiel en pair-à-pair 🔒";

                string encrypted = MessageCrypto.Encrypt(original, key);
                if (string.IsNullOrEmpty(encrypted)) throw new Exception("Texte chiffré vide");
                if (encrypted == original) throw new Exception("Le texte chiffré est identique au texte clair");

                string decrypted = MessageCrypto.Decrypt(encrypted, key);
                if (decrypted != original) throw new Exception($"Déchiffré incorrect: '{decrypted}' vs '{original}'");
            }, results);

            Test("MessageCrypto_MauvaiseCleEchoue", () =>
            {
                byte[] key1 = MessageCrypto.DeriveKey(new byte[] { 1, 1, 1 });
                byte[] key2 = MessageCrypto.DeriveKey(new byte[] { 2, 2, 2 });

                string encrypted = MessageCrypto.Encrypt("Top secret", key1);
                try
                {
                    string decrypted = MessageCrypto.Decrypt(encrypted, key2);
                    if (decrypted == "Top secret") throw new Exception("Déchiffrement réussi avec une mauvaise clé !");
                }
                catch (CryptographicException)
                {
                    // Comportement attendu
                }
            }, results);

            Test("Conversation_ArchivageViaService", () =>
            {
                var service = new DemoChatService();
                var convs = service.GetConversationsAsync().GetAwaiter().GetResult();
                if (convs.Count == 0) throw new Exception("Aucune conversation disponible");

                var targetId = convs[0].Id;

                // Archiver
                bool ok = service.ArchiveConversationAsync(targetId, true).GetAwaiter().GetResult();
                if (!ok) throw new Exception("ArchiveConversationAsync(true) a échoué");

                var updatedConvs = service.GetConversationsAsync().GetAwaiter().GetResult();
                var updated = updatedConvs.FirstOrDefault(c => c.Id == targetId);
                if (updated == null || !updated.IsArchived) throw new Exception("La conversation devrait être archivée");

                // Désarchiver
                ok = service.ArchiveConversationAsync(targetId, false).GetAwaiter().GetResult();
                if (!ok) throw new Exception("ArchiveConversationAsync(false) a échoué");

                updatedConvs = service.GetConversationsAsync().GetAwaiter().GetResult();
                updated = updatedConvs.FirstOrDefault(c => c.Id == targetId);
                if (updated == null || updated.IsArchived) throw new Exception("La conversation ne devrait plus être archivée");
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
