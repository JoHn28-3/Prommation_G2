#nullable enable

using System;
using System.Drawing;
using System.Windows.Forms;
using WHATSAPP_CHAPT.App.Drawing;

namespace WHATSAPP_CHAPT.App.Controls
{
    public sealed class AttachmentMenu : IDisposable
    {
        private readonly ThemedMenu _menu;

        public event EventHandler<string>? AttachmentRequested;

        public AttachmentMenu()
        {
            var catalog = new IconCatalog();
            _menu = new ThemedMenu(new[]
            {
                new ThemedMenuItem
                {
                    Text = "Document",
                    Icon = catalog.Load("document", 20, 20, Theme.TextPrimary),
                    Action = () => AttachmentRequested?.Invoke(this, "document")
                },
                new ThemedMenuItem
                {
                    Text = "Photos et vidéos",
                    Icon = catalog.Load("photo", 20, 20, Theme.TextPrimary),
                    Action = () => AttachmentRequested?.Invoke(this, "media")
                },
                new ThemedMenuItem
                {
                    Text = "Appareil photo",
                    Icon = catalog.Load("video", 20, 20, Theme.TextPrimary),
                    Action = () => AttachmentRequested?.Invoke(this, "photo")
                },
                new ThemedMenuItem
                {
                    Text = "Capture d’écran",
                    Icon = catalog.Load("photo", 20, 20, Theme.TextPrimary),
                    Action = () => AttachmentRequested?.Invoke(this, "screenshot")
                },
                new ThemedMenuItem
                {
                    Text = "Sticker",
                    Icon = catalog.Load("sticker", 20, 20, Theme.TextPrimary),
                    Action = () => AttachmentRequested?.Invoke(this, "sticker")
                },
                new ThemedMenuItem
                {
                    Text = "Contact",
                    Icon = catalog.Load("contact", 20, 20, Theme.TextPrimary),
                    Action = () => AttachmentRequested?.Invoke(this, "contact")
                }
            });
        }

        public void Show(Control owner, Point screenPoint)
        {
            _menu.ShowAt(owner, screenPoint);
        }

        public void Dispose()
        {
            _menu.Dispose();
        }
    }
}
