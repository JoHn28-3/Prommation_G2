using System;
using System.Windows.Forms;
using WHATSAPP_CHAPT.App.Forms;
using WHATSAPP_CHAPT.App.SelfTest;
using WHATSAPP_CHAPT.App.Services;
using WHATSAPP_CHAPT.Storage;
using WHATSAPP_CHAPT.Core.Models;

namespace WHATSAPP_CHAPT.App;

/// <summary>
/// Point d'entrée principal de l'application.
/// </summary>
static class Program
{
    /// <summary>
    /// Point d'entrée principal pour l'application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        string? selftestPhase = null;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("--selftest", StringComparison.OrdinalIgnoreCase))
            {
                selftestPhase = (i + 1 < args.Length && !args[i + 1].StartsWith("-")) ? args[i + 1] : "all";
                break;
            }
        }

        if (selftestPhase != null)
        {
            int failures = SelfTestRunner.Run(selftestPhase);
            Environment.Exit(failures);
            return;
        }

        ApplicationConfiguration.Initialize();

        string logPath = System.IO.Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop),
            "whatsapp_crash.log");

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (s, e) =>
        {
            System.IO.File.WriteAllText(logPath, $"[ThreadException]\n{e.Exception}");
            MessageBox.Show(e.Exception.ToString(), "CRASH ThreadException");
        };
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            System.IO.File.WriteAllText(logPath, $"[UnhandledException]\n{e.ExceptionObject}");
            MessageBox.Show(e.ExceptionObject?.ToString() ?? "null", "CRASH Unhandled");
        };
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            System.IO.File.WriteAllText(logPath, $"[UnobservedTask]\n{e.Exception}");
        };

        string dataFolder = DataFolder.GetPath(args);
        var identity = new IdentityManager(dataFolder);
        var storage = new StorageContext(dataFolder);
        storage.SetLocalIdentity(identity.UserId, identity.PublicKey);

        var connection = new ConnectionService(storage);
        var chatService = new DemoChatService(
            new UserIdentity { Id = identity.UserId, DisplayName = "Moi" },
            storage,
            connection);
        _ = connection.StartTcpListenerAsync();
        _ = connection.StartBroadcastListenerAsync();
        var avatarStore = new LocalAvatarStore();
        Application.Run(new MainForm(chatService, avatarStore, () => chatService.SimulateIncomingMessage()));
    }    
}
