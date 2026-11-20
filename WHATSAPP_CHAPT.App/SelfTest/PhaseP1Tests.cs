#nullable enable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using WHATSAPP_CHAPT.App.Drawing;

namespace WHATSAPP_CHAPT.App.SelfTest
{
    public static class PhaseP1Tests
    {
        public static void Run(List<(string Name, bool Success, string? Detail)> results)
        {
            Test("P1_IconCatalog_AucunDoublon", () =>
            {
                var catalog = new IconCatalog();
                var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string file in catalog.Files.Values)
                {
                    if (!used.Add(file))
                        throw new Exception($"Deux fonctions utilisent le même fichier : {file}");
                }
            }, results);

            Test("P1_IconCatalog_TousFichiersExistants", () =>
            {
                var catalog = new IconCatalog();
                foreach (string file in catalog.Files.Values)
                {
                    if (!File.Exists(catalog.GetPath(GetFunction(catalog, file))))
                        throw new Exception($"Fichier absent : {file}");
                }
            }, results);

            Test("P1_SvgLecture_BoiteEnglobante", () =>
            {
                foreach (string file in new[]
                {
                    "icons8-contacts.svg",
                    "icons8-document.svg",
                    "icons8-photo.svg"
                })
                {
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Icons", file);
                    if (!File.Exists(path))
                        throw new Exception($"SVG absent : {file}");
                    using Bitmap bitmap = SvgIconReader.Read(path, 24, 24);
                    if (bitmap.Width != 24 || bitmap.Height != 24)
                        throw new Exception($"SVG invalide : {file}");
                }
            }, results);

            Test("P1_Theme_ChangementCouleurs", () =>
            {
                Color dark = Theme.AppBackground;
                Theme.SetThemeMode(ThemeMode.Light);
                Color light = Theme.AppBackground;
                Theme.SetThemeMode(ThemeMode.Dark);
                if (dark == light)
                    throw new Exception("Le changement de thème ne change pas les couleurs.");
            }, results);

            Test("P1_Theme_SystemDefini", () =>
            {
                Theme.SetThemeMode(ThemeMode.System);
                if (Theme.Mode != ThemeMode.System)
                    throw new Exception("Mode système non enregistré.");
                Theme.SetThemeMode(ThemeMode.Dark);
            }, results);
        }

        private static string GetFunction(IconCatalog catalog, string file)
        {
            foreach (var pair in catalog.Files)
                if (pair.Value.Equals(file, StringComparison.OrdinalIgnoreCase))
                    return pair.Key;
            throw new KeyNotFoundException(file);
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
