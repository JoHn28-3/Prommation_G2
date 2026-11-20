#nullable enable

using System.Drawing;
using System.Windows.Forms;
using WHATSAPP_CHAPT.App.Drawing;

namespace WHATSAPP_CHAPT.App.Pages
{
    public sealed class CallsPage : AppPage
    {
        public CallsPage()
            : base("Appels")
        {
            AddBackButton();
            Controls.Add(CreateEmptyState("Aucun appel récent"));
        }
    }
}
