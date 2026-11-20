#nullable enable

using System;
using System.Windows.Forms;

namespace WHATSAPP_CHAPT.App.Pages
{
    public sealed class NewDiscussionPage : AppPage
    {
        public NewDiscussionPage()
            : base("Nouvelle discussion")
        {
            AddBackButton();
            var panel = CreateEmptyState("Rechercher un contact");
            var search = new TextBox
            {
                Dock = DockStyle.Top,
                Height = LogicalToDeviceUnits(42),
                PlaceholderText = "Rechercher un contact",
                Margin = new Padding(LogicalToDeviceUnits(20))
            };
            panel.Controls.Add(search);
            Controls.Add(panel);
        }
    }
}
