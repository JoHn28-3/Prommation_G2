#nullable enable

using System;
using System.Collections.Generic;
using System.IO;

namespace WHATSAPP_CHAPT.App.SelfTest
{
    /// <summary>
    /// Exécuteur des autotests pour valider chaque phase.
    /// </summary>
    public static class SelfTestRunner
    {
        /// <summary>
        /// Exécute les autotests pour une phase donnée ou pour toutes les phases.
        /// </summary>
        /// <param name="phase">Phase à exécuter (« A », « B », « C », « D », « E » ou « all »).</param>
        /// <returns>Nombre total d'échecs.</returns>
        public static int Run(string phase)
        {
            var results = new List<(string Name, bool Success, string? Detail)>();

            string normalizedPhase = phase.Trim().ToLowerInvariant().TrimStart('7');

            if (normalizedPhase == "a" || normalizedPhase == "all")
            {
                Phase7ATests.Run(results);
            }

            if (normalizedPhase == "c" || normalizedPhase == "all")
            {
                Phase7CTests.Run(results);
            }

            int total = results.Count;
            int passed = 0;
            var lines = new List<string>();

            foreach (var r in results)
            {
                if (r.Success)
                {
                    passed++;
                    lines.Add($"[PASS] {r.Name}");
                }
                else
                {
                    lines.Add($"[FAIL] {r.Name} : {r.Detail}");
                }
            }

            lines.Add($"TOTAL {passed}/{total}");

            string reportContent = string.Join(Environment.NewLine, lines);

            // Écriture à côté de l'exécutable
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string reportFileExe = Path.Combine(exeDir, "selftest-report.txt");
            File.WriteAllText(reportFileExe, reportContent);

            // Écriture également dans le dossier de travail courant si différent
            string currentDir = Directory.GetCurrentDirectory();
            string reportFileCurrent = Path.Combine(currentDir, "selftest-report.txt");
            if (!string.Equals(reportFileExe, reportFileCurrent, StringComparison.OrdinalIgnoreCase))
            {
                File.WriteAllText(reportFileCurrent, reportContent);
            }

            // Affichage console
            Console.WriteLine(reportContent);

            int failures = total - passed;
            return failures;
        }
    }
}
