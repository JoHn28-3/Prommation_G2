#nullable enable

using System.Windows.Forms;

namespace WHATSAPP_CHAPT.App.Pages
{
    public sealed class ArchivedPage : AppPage
    {
        public ArchivedPage()
            : base("Archivées")
        {
            AddBackButton();
            Controls.Add(CreateEmptyState("Aucune discussion archivée"));
        }
    }
}
