#nullable enable

using System;
using System.Drawing;
using System.Windows.Forms;
using WHATSAPP_CHAPT.App.Drawing;

namespace WHATSAPP_CHAPT.App.Pages
{
    public abstract class AppPage : UserControl
    {
        protected readonly Label TitleLabel = new();

        protected AppPage(string title)
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Panel;
            DoubleBuffered = true;

            TitleLabel.Text = title;
            TitleLabel.Dock = DockStyle.Top;
            TitleLabel.Height = LogicalToDeviceUnits(60);
            TitleLabel.TextAlign = ContentAlignment.MiddleLeft;
            TitleLabel.Padding = new Padding(LogicalToDeviceUnits(20), 0, 0, 0);
            TitleLabel.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
            TitleLabel.ForeColor = Theme.TextPrimary;
            TitleLabel.BackColor = Theme.Header;
            Controls.Add(TitleLabel);

            Theme.ThemeChanged += OnThemeChanged;
        }

        public event Action? BackRequested;

        protected void AddBackButton()
        {
            var back = new Button
            {
                Text = "←",
                Size = new Size(LogicalToDeviceUnits(40), LogicalToDeviceUnits(40)),
                Location = new Point(LogicalToDeviceUnits(8),
                    (LogicalToDeviceUnits(60) - LogicalToDeviceUnits(40)) / 2),
                BackColor = Theme.Header,
                ForeColor = Theme.TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TabStop = false
            };
            back.FlatAppearance.BorderSize = 0;
            back.Click += (s, e) => BackRequested?.Invoke();
            Controls.Add(back);
            back.BringToFront();
        }

        protected Panel CreateEmptyState(string message)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.AppBackground
            };
            var label = new Label
            {
                Dock = DockStyle.Fill,
                Text = message,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 11f)
            };
            panel.Controls.Add(label);
            return panel;
        }

        private void OnThemeChanged(object? sender, EventArgs e)
        {
            BackColor = Theme.Panel;
            TitleLabel.BackColor = Theme.Header;
            TitleLabel.ForeColor = Theme.TextPrimary;
            Invalidate(true);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                Theme.ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }
}
