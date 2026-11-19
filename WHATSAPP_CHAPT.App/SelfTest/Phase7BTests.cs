#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using WHATSAPP_CHAPT.Storage;

namespace WHATSAPP_CHAPT.App.SelfTest
{
    /// <summary>
    /// Autotests pour la phase B (stockage & identité).
    /// </summary>
    public static class Phase7BTests
    {
        public static void Run(List<(string Name, bool Success, string? Detail)> results)
        {
            Test("DataFolder_DefaultPath", () =>
            {
                var path = DataFolder.GetPath(Array.Empty<string>());
                if (string.IsNullOrWhiteSpace(path)) throw new Exception("Chemin vide");
                if (!Directory.Exists(path)) throw new Exception("Dossier n'existe pas");
            }, results);

            Test("IdentityManager_Generation", () =>
            {
                var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(folder);
                var im = new IdentityManager(folder);
                if (im.UserId == Guid.Empty) throw new Exception("UserId vide");
                if (im.PublicKey == null || im.PublicKey.Length == 0) throw new Exception("Clé publique vide");
                // Exporter la carte de contact et vérifier le préfixe
                var card = im.ExportContactCard("TestUser");
                if (!card.StartsWith("WCARD1:")) throw new Exception("Préfixe manquant");
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
