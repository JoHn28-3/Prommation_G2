// NavigationButton.cs - custom button for vertical navigation bar
// Simple implementation: inherits Button, displays an Image and optional tooltip.
// Hover and selected states are shown via BackColor changes.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WHATSAPP_CHAPT.App.Drawing;

namespace WHATSAPP_CHAPT.App.Controls
{
    public class NavigationButton : Button
    {
        private readonly ToolTip _toolTip;
        private bool _isSelected;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                UpdateAppearance();
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string ToolTipText
        {
            get => _toolTip.GetToolTip(this);
            set => _toolTip.SetToolTip(this, value ?? string.Empty);
        }

        public NavigationButton()
        {
            _toolTip = new ToolTip();
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Width = 52; // approximate width
            Height = 52; // approximate height
            BackColor = Theme.Panel; // default background
            ForeColor = Theme.TextPrimary;
            Cursor = Cursors.Hand;
            // Enable owner draw for custom background on hover/selection
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.DoubleBuffer, true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            if (!IsSelected)
                BackColor = Theme.Hover;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (!IsSelected)
                BackColor = Theme.Panel;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            // Fill background based on selection/hover state
            using (var brush = new SolidBrush(BackColor))
            {
                pe.Graphics.FillRectangle(brush, ClientRectangle);
            }

            // Draw image centered
            if (Image != null)
            {
                var imgSize = Math.Min(Image.Width, Image.Height);
                var rect = new Rectangle(
                    (ClientSize.Width - imgSize) / 2,
                    (ClientSize.Height - imgSize) / 2,
                    imgSize,
                    imgSize);
                pe.Graphics.DrawImage(Image, rect);
            }

            // Optionally draw a selection indicator (e.g., a left border)
            if (IsSelected)
            {
                using var selBrush = new SolidBrush(Theme.Accent);
                pe.Graphics.FillRectangle(selBrush, 0, 0, 4, Height);
            }
        }

        private void UpdateAppearance()
        {
            BackColor = IsSelected ? Theme.Selected : Theme.Panel;
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _toolTip.Dispose();
            base.Dispose(disposing);
        }
    }
}
