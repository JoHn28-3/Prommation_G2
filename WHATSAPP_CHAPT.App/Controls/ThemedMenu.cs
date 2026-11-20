#nullable enable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WHATSAPP_CHAPT.App.Drawing;

namespace WHATSAPP_CHAPT.App.Controls
{
    public sealed class ThemedMenuItem
    {
        public string Text { get; init; } = string.Empty;
        public string? Shortcut { get; init; }
        public Image? Icon { get; init; }
        public bool IsSeparator { get; init; }
        public bool Enabled { get; init; } = true;
        public Action? Action { get; init; }
    }

    public sealed class ThemedMenu : Form
    {
        private readonly List<ThemedMenuItem> _items;
        private int _hoveredIndex = -1;
        private int _selectedIndex = -1;
        private readonly IconCatalog _icons = new();

        public ThemedMenu(IEnumerable<ThemedMenuItem> items)
        {
            _items = new List<ThemedMenuItem>(items);
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            BackColor = Theme.Header;
            ForeColor = Theme.TextPrimary;
            DoubleBuffered = true;
            Size = new Size(280, LogicalToDeviceUnits(12) + _items.Count * LogicalToDeviceUnits(40));
            Paint += OnPaint;
            MouseMove += OnMouseMove;
            MouseLeave += (s, e) => { _hoveredIndex = -1; Invalidate(); };
            MouseDown += OnMouseDown;
            KeyDown += OnKeyDown;
            Deactivate += (s, e) => Close();
            Theme.ThemeChanged += OnThemeChanged;
        }

        private void OnThemeChanged(object? sender, EventArgs e)
        {
            BackColor = Theme.Header;
            ForeColor = Theme.TextPrimary;
            Invalidate();
        }

        public void ShowAt(Control owner, Point screenPoint)
        {
            int width = LogicalToDeviceUnits(280);
            int height = LogicalToDeviceUnits(12) + _items.Count * LogicalToDeviceUnits(40);
            Rectangle screen = Screen.FromControl(owner).WorkingArea;
            int x = Math.Min(screenPoint.X, screen.Right - width);
            int y = Math.Min(screenPoint.Y, screen.Bottom - height);
            Bounds = new Rectangle(Math.Max(screen.Left, x), Math.Max(screen.Top, y), width, height);
            using GraphicsPath path = RoundedRectangle(ClientRectangle, LogicalToDeviceUnits(10));
            Region = new Region(path);
            Show(owner);
            Activate();
        }

        private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void OnPaint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Theme.Header);
            int rowHeight = LogicalToDeviceUnits(40);
            int y = LogicalToDeviceUnits(6);

            for (int i = 0; i < _items.Count; i++)
            {
                ThemedMenuItem item = _items[i];
                if (item.IsSeparator)
                {
                    using var pen = new Pen(Theme.Separator);
                    g.DrawLine(pen, LogicalToDeviceUnits(12), y + rowHeight / 2,
                        Width - LogicalToDeviceUnits(12), y + rowHeight / 2);
                }
                else
                {
                    var row = new Rectangle(0, y, Width, rowHeight);
                    if (i == _hoveredIndex)
                        using (var brush = new SolidBrush(Theme.Hover))
                            g.FillRectangle(brush, row);

                    if (item.Icon != null)
                        g.DrawImage(item.Icon, LogicalToDeviceUnits(12),
                            y + (rowHeight - LogicalToDeviceUnits(20)) / 2,
                            LogicalToDeviceUnits(20), LogicalToDeviceUnits(20));

                    TextRenderer.DrawText(g, item.Text,
                        new Font("Segoe UI", 9.5f, item.Enabled ? FontStyle.Regular : FontStyle.Italic),
                        new Rectangle(LogicalToDeviceUnits(42), y, Width - LogicalToDeviceUnits(100), rowHeight),
                        item.Enabled ? Theme.TextPrimary : Theme.TextSecondary,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                    if (!string.IsNullOrEmpty(item.Shortcut))
                        TextRenderer.DrawText(g, item.Shortcut,
                            new Font("Segoe UI", 8.5f),
                            new Rectangle(Width - LogicalToDeviceUnits(58), y,
                                LogicalToDeviceUnits(46), rowHeight),
                            Theme.TextSecondary,
                            TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
                }
                y += rowHeight;
            }
        }

        private void OnMouseMove(object? sender, MouseEventArgs e)
        {
            int index = (e.Y - LogicalToDeviceUnits(6)) / LogicalToDeviceUnits(40);
            if (index < 0 || index >= _items.Count)
                index = -1;
            if (index != _hoveredIndex)
            {
                _hoveredIndex = index;
                Invalidate();
            }
        }

        private void OnMouseDown(object? sender, MouseEventArgs e)
        {
            int index = (e.Y - LogicalToDeviceUnits(6)) / LogicalToDeviceUnits(40);
            if (index >= 0 && index < _items.Count)
            {
                ThemedMenuItem item = _items[index];
                if (item.Enabled && !item.IsSeparator)
                {
                    Action? action = item.Action;
                    Close();
                    action?.Invoke();
                }
            }
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
                return;
            }

            if (_selectedIndex < 0)
                _selectedIndex = 0;

            if (e.KeyCode == Keys.Down)
                _selectedIndex = Math.Min(_items.Count - 1, _selectedIndex + 1);
            else if (e.KeyCode == Keys.Up)
                _selectedIndex = Math.Max(0, _selectedIndex - 1);
            else if (e.KeyCode == Keys.Enter && _selectedIndex >= 0)
            {
                ThemedMenuItem item = _items[_selectedIndex];
                if (item.Enabled && !item.IsSeparator)
                {
                    Action? action = item.Action;
                    Close();
                    action?.Invoke();
                }
            }
            _hoveredIndex = _selectedIndex;
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                Theme.ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }
}
