using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WHATSAPP_CHAPT.App.Controls
{
    /// <summary>
    /// Renderer sombre personnalisé pour tous les ContextMenuStrip de l'application.
    /// Applique le thème sombre cohérent (fond, hover, séparateurs) et peut arrondir les coins.
    /// </summary>
    public sealed class ThemeContextMenuRenderer : ToolStripProfessionalRenderer
    {
        public ThemeContextMenuRenderer() : base(new DarkColorTable()) { }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using var brush = new SolidBrush(Color.FromArgb(30, 30, 30));
            e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var rect = new Rectangle(Point.Empty, e.Item.Size);
            if (e.Item.Selected)
            {
                using var brush = new SolidBrush(Color.FromArgb(55, 55, 55));
                e.Graphics.FillRectangle(brush, rect);
            }
            else
            {
                using var brush = new SolidBrush(Color.FromArgb(30, 30, 30));
                e.Graphics.FillRectangle(brush, rect);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = Color.FromArgb(220, 220, 220);
            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.Height / 2;
            using var pen = new Pen(Color.FromArgb(80, 80, 80));
            e.Graphics.DrawLine(pen, 4, y, e.Item.Width - 4, y);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using var pen = new Pen(Color.FromArgb(70, 70, 70));
            var rect = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
            e.Graphics.DrawRectangle(pen, rect);
        }

        /// <summary>
        /// Applique des coins arrondis à un ContextMenuStrip via sa Region.
        /// À appeler dans l'événement Opened du menu.
        /// </summary>
        public static void ApplyRoundedCorners(ContextMenuStrip menu, int radius = 8)
        {
            var path = new GraphicsPath();
            var r = new Rectangle(0, 0, menu.Width, menu.Height);
            path.AddArc(r.X, r.Y, radius, radius, 180, 90);
            path.AddArc(r.Right - radius, r.Y, radius, radius, 270, 90);
            path.AddArc(r.Right - radius, r.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(r.X, r.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
            menu.Region = new Region(path);
        }

        private sealed class DarkColorTable : ProfessionalColorTable
        {
            public override Color ToolStripDropDownBackground => Color.FromArgb(30, 30, 30);
            public override Color MenuBorder => Color.FromArgb(70, 70, 70);
            public override Color MenuItemBorder => Color.FromArgb(55, 55, 55);
            public override Color MenuItemSelected => Color.FromArgb(55, 55, 55);
            public override Color MenuItemSelectedGradientBegin => Color.FromArgb(55, 55, 55);
            public override Color MenuItemSelectedGradientEnd => Color.FromArgb(55, 55, 55);
            public override Color MenuItemPressedGradientBegin => Color.FromArgb(45, 45, 45);
            public override Color MenuItemPressedGradientEnd => Color.FromArgb(45, 45, 45);
            public override Color ImageMarginGradientBegin => Color.FromArgb(30, 30, 30);
            public override Color ImageMarginGradientMiddle => Color.FromArgb(30, 30, 30);
            public override Color ImageMarginGradientEnd => Color.FromArgb(30, 30, 30);
            public override Color SeparatorDark => Color.FromArgb(80, 80, 80);
            public override Color SeparatorLight => Color.FromArgb(100, 100, 100);
        }
    }
}
