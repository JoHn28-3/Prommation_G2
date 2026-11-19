// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WHATSAPP_CHAPT.App.Drawing;

namespace WHATSAPP_CHAPT.App.Controls
{
    /// <summary>
    /// Context menu for attachment options used in the message composition bar.
    /// </summary>
    public class AttachmentMenu : ContextMenuStrip
    {
        public AttachmentMenu()
        {
            BackColor = Theme.Header;
            ForeColor = Theme.TextPrimary;
            Renderer = new ThemeContextMenuRenderer();
            BuildItems();
        }

        private void BuildItems()
        {
            var items = new List<(string text, string iconPath, EventHandler handler)>
            {
                ("Document", "Assets/Icons/document.png", OnDocument),
                ("Photo", "Assets/Icons/photo.png", OnPhoto),
                ("Image", "Assets/Icons/image.png", OnImage),
                ("Video", "Assets/Icons/video.png", OnVideo),
                ("Audio", "Assets/Icons/audio.png", OnAudio),
                ("Enregistrement vocal", "Assets/Icons/voice.png", OnVoiceRecord),
                ("Sticker", "Assets/Icons/sticker.png", OnSticker)
            };

            foreach (var (text, iconPath, handler) in items)
            {
                var item = new ToolStripMenuItem(text);
                // Load icon if exists
                try
                {
                    var full = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, iconPath);
                    if (System.IO.File.Exists(full))
                        item.Image = Image.FromFile(full);
                }
                catch { /* ignore icon load failures */ }
                item.Click += handler;
                Items.Add(item);
            }
        }

        // Placeholder handlers – actual implementation will be wired up by the host control.
        private void OnDocument(object? sender, EventArgs e) => AttachmentRequested?.Invoke(this, "document");
        private void OnPhoto(object? sender, EventArgs e) => AttachmentRequested?.Invoke(this, "photo");
        private void OnImage(object? sender, EventArgs e) => AttachmentRequested?.Invoke(this, "image");
        private void OnVideo(object? sender, EventArgs e) => AttachmentRequested?.Invoke(this, "video");
        private void OnAudio(object? sender, EventArgs e) => AttachmentRequested?.Invoke(this, "audio");
        private void OnVoiceRecord(object? sender, EventArgs e) => AttachmentRequested?.Invoke(this, "voicerecord");
        private void OnSticker(object? sender, EventArgs e) => AttachmentRequested?.Invoke(this, "sticker");

        /// <summary>
        /// Event raised when a menu item is selected. The payload is a short string indicating the type.
        /// </summary>
        public event EventHandler<string>? AttachmentRequested;
    }
}
