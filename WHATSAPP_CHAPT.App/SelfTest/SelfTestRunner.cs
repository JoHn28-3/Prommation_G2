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

            if (normalizedPhase == "p1" || normalizedPhase == "1" || normalizedPhase == "all")
            {
                PhaseP1Tests.Run(results);
            }

            if (normalizedPhase == "p2" || normalizedPhase == "2" || normalizedPhase == "all")
            {
                PhaseP2Tests.Run(results);
            }

            if (normalizedPhase == "a" || normalizedPhase == "all")
            {
                Phase7ATests.Run(results);
            }

            if (normalizedPhase == "b" || normalizedPhase == "all")
            {
                Phase7BTests.Run(results);
            }

            if (normalizedPhase == "c" || normalizedPhase == "all")
            {
                Phase7CTests.Run(results);
            }

            if (normalizedPhase == "d" || normalizedPhase == "all")
            {
                Phase7DTests.Run(results);
            }

            if (normalizedPhase == "e" || normalizedPhase == "all")
            {
                Phase7ETests.Run(results);
            }

            if (normalizedPhase == "f" || normalizedPhase == "all")
            {
                Phase7FTests.Run(results);
            }

            if (normalizedPhase == "g" || normalizedPhase == "all")
            {
                Phase7GTests.Run(results);
            }

            if (normalizedPhase == "h" || normalizedPhase == "all")
            {
                Phase7HTests.Run(results);
            }

            if (normalizedPhase == "i" || normalizedPhase == "all")
            {
                Phase7ITests.Run(results);
            }

            if (normalizedPhase == "j" || normalizedPhase == "all")
            {
                Phase7JTests.Run(results);
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
