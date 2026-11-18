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
        var chatService = new DemoChatService();
        Application.Run(new MainForm(chatService, () => chatService.SimulateIncomingMessage()));
    }    
}
