#nullable enable

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using WHATSAPP_CHAPT.App.Services;

namespace WHATSAPP_CHAPT.App.SelfTest
{
    public static class PhaseP3Tests
    {
        public static void Run(List<(string Name, bool Success, string? Detail)> results)
        {
            Test("P3_Pseudo_FormatValide", () =>
            {
                if (!Regex.IsMatch("alice_42", @"^[a-z0-9_.]{3,24}$", RegexOptions.IgnoreCase))
                    throw new Exception("Pseudo valide refusé.");
                if (Regex.IsMatch("a!", @"^[a-z0-9_.]{3,24}$", RegexOptions.IgnoreCase))
                    throw new Exception("Pseudo invalide accepté.");
            }, results);

            Test("P3_IP_LoopbackRefuse", () =>
            {
                if (!IPAddress.TryParse("127.0.0.1", out IPAddress? ip))
                    throw new Exception("IP valide non analysée.");
                if (!IPAddress.IsLoopback(ip))
                    throw new Exception("Loopback non détecté.");
            }, results);

            Test("P3_LongText_DecoupeSansPerte", () =>
            {
                string text = "Bonjour. " + new string('x', 900);
                var policy = new MessageLongTextPolicy(500, true);
                var chunks = policy.Split(text);
                string joined = string.Concat(chunks);
                string normalizedOriginal = text.Replace(" ", "");
                string normalizedJoined = joined.Replace(" ", "");
                if (chunks.Count < 2)
                    throw new Exception($"Long texte non découpé ({text.Length} caractères -> {chunks.Count} morceau).");
                if (normalizedJoined != normalizedOriginal)
                    throw new Exception("Du texte est perdu.");
            }, results);

            Test("P3_LongText_SeuilMaximum", () =>
            {
                string text = new string('x', 4000);
                var policy = new MessageLongTextPolicy(500, true);
                foreach (string chunk in policy.Split(text))
                    if (chunk.Length > 500)
                        throw new Exception("Un morceau dépasse le seuil.");
            }, results);

            Test("P3_ProfileStore_ModeTheme", () =>
            {
                var profile = new UserProfile
                {
                    DisplayName = "Alice",
                    Username = "alice_42",
                    IpAddress = "192.168.1.20",
                    ThemeMode = "system"
                };
                if (profile.ThemeMode != "system")
                    throw new Exception("Mode système non conservé.");
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
