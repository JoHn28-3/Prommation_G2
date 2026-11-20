using System;
using System.Windows.Forms;
using WHATSAPP_CHAPT.App.Forms;
using WHATSAPP_CHAPT.App.SelfTest;
using WHATSAPP_CHAPT.App.Services;
using WHATSAPP_CHAPT.App.Drawing;
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

        string logDirectory = System.IO.Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            "WHATSAPP_CHAPT", "logs");
        System.IO.Directory.CreateDirectory(logDirectory);
        string logPath = System.IO.Path.Combine(logDirectory, "app.log");

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (s, e) =>
        {
            System.IO.File.AppendAllText(logPath, $"[{DateTime.Now:O}] ThreadException\n");
            MessageBox.Show("Une erreur inattendue s'est produite.", "WHATSAPP", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            System.IO.File.AppendAllText(logPath, $"[{DateTime.Now:O}] UnhandledException\n");
            MessageBox.Show("Une erreur inattendue s'est produite.", "WHATSAPP", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            System.IO.File.WriteAllText(logPath, $"[UnobservedTask]\n{e.Exception}");
        };

        string dataFolder = DataFolder.GetPath(args);
        var identity = new IdentityManager(dataFolder);
        var storage = new StorageContext(dataFolder);
        storage.SetLocalIdentity(identity.UserId, identity.PublicKey);
        var profileStore = new ProfileStore(dataFolder);
        var profile = profileStore.Load();
        Theme.SetThemeMode(profile.ThemeMode);
        string? registrationPhotoPath = null;

        if (!profileStore.Exists)
        {
            profile = new UserProfile { IpAddress = ProfileStore.DetectLocalIp() };
        }

        var connection = new ConnectionService(storage);
        var registeredIdentity = new UserIdentity
        {
            Id = identity.UserId,
            DisplayName = profile.DisplayName
        };
        var chatService = new DemoChatService(
            registeredIdentity,
            storage,
            connection);
        _ = connection.StartTcpListenerAsync();
        _ = connection.StartBroadcastListenerAsync();
        var avatarStore = new LocalAvatarStore();
        if (registrationPhotoPath != null)
        {
            avatarStore.SetPhoto(identity.UserId, registrationPhotoPath);
        }
        Application.Run(new MainForm(chatService, avatarStore, profileStore: profileStore));
    }    
}
