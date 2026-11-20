#nullable enable

using System.Windows.Forms;

namespace WHATSAPP_CHAPT.App.Pages
{
    public sealed class StatusesPage : AppPage
    {
        public StatusesPage()
            : base("Statuts")
        {
            AddBackButton();
            Controls.Add(CreateEmptyState("Aucun statut"));
        }
    }
}
