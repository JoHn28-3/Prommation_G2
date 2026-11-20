#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using WHATSAPP_CHAPT.App.Services;

namespace WHATSAPP_CHAPT.App.SelfTest
{
    public static class PhaseP4Tests
    {
        public static void Run(List<(string Name, bool Success, string? Detail)> results)
        {
            Test("P4_Emoji_ConversionHexa", () =>
            {
                if (!EmojiCatalog.TryConvertSequence("1f600", out string? unicode)
                    || unicode != "😀")
                    throw new Exception("Emoji 1f600 mal converti.");
            }, results);

            Test("P4_Emoji_NomInvalideIgnore", () =>
            {
                if (EmojiCatalog.TryConvertSequence("xxx", out _))
                    throw new Exception("Nom d'emoji invalide accepté.");
            }, results);

            Test("P4_Media_NomNettoye", () =>
            {
                string cleaned = MediaPolicy.CleanFileName(" photo:été?.jpg ");
                if (cleaned.Contains(':') || cleaned.Contains('?'))
                    throw new Exception("Nom de fichier non nettoyé.");
            }, results);

            Test("P4_Media_Classifications", () =>
            {
                if (MediaPolicy.Classify("photo.jpg") != "image"
                    || MediaPolicy.Classify("clip.mp4") != "video"
                    || MediaPolicy.Classify("song.mp3") != "audio"
                    || MediaPolicy.Classify("rapport.pdf") != "document")
                    throw new Exception("Classification incorrecte.");
            }, results);

            Test("P4_Media_AvertissementExecutables", () =>
            {
                if (!MediaPolicy.RequiresExecutableWarning("setup.exe")
                    || MediaPolicy.RequiresExecutableWarning("photo.jpg"))
                    throw new Exception("Décision d'avertissement incorrecte.");
            }, results);

            Test("P4_Emoji_CatalogueTemporaire", () =>
            {
                string folder = Path.Combine(Path.GetTempPath(), "whatsapp_p4_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(folder);
                try
                {
                    File.WriteAllBytes(Path.Combine(folder, "1f600.png"), new byte[] { 137, 80, 78, 71 });
                    var catalog = new EmojiCatalog();
                    catalog.Load(folder);
                    if (catalog.Entries.Count != 1)
                        throw new Exception("Ajout d'un emoji non détecté.");
                }
                finally
                {
                    Directory.Delete(folder, true);
                }
            }, results);
        }

        private static void Test(string name, Action body,
            List<(string Name, bool Success, string? Detail)> results)
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
