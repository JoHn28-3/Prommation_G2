using System;
using System.Windows.Forms;
using WHATSAPP_CHAPT.App.Forms;
using WHATSAPP_CHAPT.App.Services;

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
    static void Main()
    {
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

        var chatService = new DemoChatService();
        var avatarStore = new LocalAvatarStore();
        Application.Run(new MainForm(chatService, avatarStore, () => chatService.SimulateIncomingMessage()));
    }    
}
