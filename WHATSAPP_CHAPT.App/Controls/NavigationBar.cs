using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WHATSAPP_CHAPT.App.Drawing;

namespace WHATSAPP_CHAPT.App.Controls
{
    /// <summary>
    /// Vertical navigation bar used on the left side of the main window.
    /// Contains a column of <see cref="NavigationButton"/> instances for the main sections.
    /// </summary>
    public class NavigationBar : Panel
    {
        public event Action<string>? SectionSelected; // name of the section clicked

        private readonly List<NavigationButton> _buttons = new();
        private readonly Dictionary<string, NavigationButton> _byKey = new();

        public NavigationBar()
        {
            Dock = DockStyle.Left;
            Width = LogicalToDeviceUnits(60);
            BackColor = Theme.Panel;
            AutoSize = false;
            BuildButtons();
        }

        private new int LogicalToDeviceUnits(int logical)
        {
            // Override hides inherited Control.LogicalToDeviceUnits with our DPI calculation
            using var g = CreateGraphics();
            return (int)(logical * g.DpiX / 96.0);
        }

        private void BuildButtons()
        {
            // Define sections – order matters (top to bottom). Bottom button will be anchored later.
            var sections = new List<(string key, string tooltip, string iconPath)>
            {
                ("discussions", "Discussions", "Assets/Icons/discussions.png"),
                ("calls", "Appels", "Assets/Icons/calls.png"),
                ("status", "Statuts", "Assets/Icons/status.png"),
                ("contacts", "Contacts", "Assets/Icons/contacts.png"),
                ("settings", "Paramètres", "Assets/Icons/settings.png"),
                ("metaai", "Meta AI", "Assets/Icons/metaai.png")
            };

            int top = LogicalToDeviceUnits(12);
            foreach (var (key, tip, icon) in sections)
            {
                var btn = new NavigationButton
                {
                    Name = $"nav_{key}",
                    Image = LoadIcon(icon),
                    ToolTipText = tip,
                    Location = new Point(0, top),
                };
                btn.Click += (s, e) => OnButtonClicked(key);
                _buttons.Add(btn);
                _byKey[key] = btn;
                Controls.Add(btn);
                top += btn.Height + LogicalToDeviceUnits(8);
            }

            // Ensure the last button (metaai) stays at the bottom.
            var metaBtn = _byKey["metaai"];
            metaBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            metaBtn.Location = new Point(0, Height - metaBtn.Height - LogicalToDeviceUnits(12));
        }

        private Image? LoadIcon(string relativePath)
        {
            try
            {
                var full = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
                if (System.IO.File.Exists(full))
                    return Image.FromFile(full);
            }
            catch
            {
                // ignore – fallback to null icon
            }
            return null;
        }

        private void OnButtonClicked(string key)
        {
            // Deselect all
            foreach (var b in _buttons)
                b.IsSelected = false;
            // Select the clicked one
            if (_byKey.TryGetValue(key, out var btn))
                btn.IsSelected = true;

            SectionSelected?.Invoke(key);
        }

        /// <summary>
        /// Allows the host to programmatically select a section.
        /// </summary>
        public void Select(string key)
        {
            OnButtonClicked(key);
        }
    }
}
