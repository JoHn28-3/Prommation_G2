#nullable enable

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text.RegularExpressions;

namespace WHATSAPP_CHAPT.App.Drawing
{
    public sealed class SvgIconReader
    {
        public static Bitmap Read(string path, int width, int height)
        {
            Bitmap bitmap = new(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using Graphics graphics = Graphics.FromImage(bitmap);
            graphics.Clear(Color.Transparent);
            graphics.SmoothingMode = SmoothingMode.HighQuality;

            if (!File.Exists(path))
                return bitmap;

            string svg = File.ReadAllText(path);
            Match viewBox = Regex.Match(svg, @"viewBox=[""']([^""]+)[""']", RegexOptions.IgnoreCase);
            float scale = 1f;
            if (viewBox.Success)
            {
                string[] values = viewBox.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (values.Length == 4
                    && float.TryParse(values[2], out float vbWidth)
                    && vbWidth > 0)
                    scale = width / vbWidth;
            }
            graphics.ScaleTransform(scale, scale);

            foreach (Match pathMatch in Regex.Matches(svg,
                @"<path\b[^>]*\bd=[""']([^""]+)[""'][^>]*>", RegexOptions.IgnoreCase))
            {
                string d = pathMatch.Groups[1].Value;
                using GraphicsPath gp = ParsePath(d);
                graphics.FillPath(Brushes.Black, gp);
            }

            return bitmap;
        }

        private static GraphicsPath ParsePath(string d)
        {
            GraphicsPath path = new();
            float x = 0;
            float y = 0;
            MatchCollection commands = Regex.Matches(d, @"([MLHVCSQZmlhvcsqz])([^MLHVCSQZmlhvcsqz]*)");
            foreach (Match command in commands)
            {
                char letter = command.Groups[1].Value[0];
                string[] parts = command.Groups[2].Value.Split(
                    new[] { ' ', ',', '\t', '\r', '\n' },
                    StringSplitOptions.RemoveEmptyEntries);
                if ((letter == 'M' || letter == 'm') && parts.Length >= 2)
                {
                    float px = float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
                    float py = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                    if (char.IsLower(letter)) { px += x; py += y; }
                    x = px;
                    y = py;
                }
                else if ((letter == 'L' || letter == 'l') && parts.Length >= 2)
                {
                    float px = float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
                    float py = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                    if (char.IsLower(letter)) { px += x; py += y; }
                    path.AddLine(x, y, px, py);
                    x = px;
                    y = py;
                }
                else if ((letter == 'H' || letter == 'h') && parts.Length >= 1)
                {
                    float px = float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
                    if (char.IsLower(letter)) px += x;
                    path.AddLine(x, y, px, y);
                    x = px;
                }
                else if ((letter == 'V' || letter == 'v') && parts.Length >= 1)
                {
                    float py = float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
                    if (char.IsLower(letter)) py += y;
                    path.AddLine(x, y, x, py);
                    y = py;
                }
                else if (letter is 'Z' or 'z')
                {
                    path.CloseFigure();
                }
            }
            return path;
        }
    }
}
