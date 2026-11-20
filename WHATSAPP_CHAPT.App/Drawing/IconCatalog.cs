#nullable enable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace WHATSAPP_CHAPT.App.Drawing
{
    public sealed class IconCatalog
    {
        private readonly Dictionary<string, Bitmap> _pngCache = new();

        public IReadOnlyDictionary<string, string> Files { get; } =
            new Dictionary<string, string>
            {
                ["discussions"] = "icons8-chats-24.png",
                ["calls"] = "icons8-call-50.png",
                ["status"] = "icons8-status-de-progression-50.png",
                ["attachment"] = "icons8-attachment-50.png",
                ["video"] = "icons8-video-48.png",
                ["sticker"] = "icons8-autocollant-48.png",
                ["contact"] = "icons8-contacts.svg",
                ["document"] = "icons8-document.svg",
                ["photo"] = "icons8-photo.svg",
                ["metaai"] = "icon8-metaai.png"
            };

        public string GetPath(string function)
        {
            if (!Files.TryGetValue(function, out string? file))
                throw new KeyNotFoundException($"Fonction d'icône inconnue : {function}");
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Icons", file);
        }

        public Bitmap Load(string function, int width, int height, Color tint)
        {
            string path = GetPath(function);
            string key = $"{path}|{width}|{height}|{tint.ToArgb()}";
            if (_pngCache.TryGetValue(key, out Bitmap? cached))
                return new Bitmap(cached);

            using MemoryStream stream = new();
            if (Path.GetExtension(path).Equals(".svg", StringComparison.OrdinalIgnoreCase))
            {
                using Bitmap svg = SvgIconReader.Read(path, width, height);
                svg.Save(stream, ImageFormat.Png);
            }
            else
            {
                using FileStream source = File.OpenRead(path);
                using Bitmap original = new(source);
                using Bitmap resized = new(width, height, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(resized))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.DrawImage(original, new Rectangle(0, 0, width, height));
                }
                resized.Save(stream, ImageFormat.Png);
            }
            stream.Position = 0;
            using Bitmap colored = new(stream);
            Bitmap result = TintBlackGlyph(colored, tint);
            _pngCache[key] = new Bitmap(result);
            return result;
        }

        private static Bitmap TintBlackGlyph(Bitmap source, Color tint)
        {
            Bitmap target = new(source.Width, source.Height, PixelFormat.Format32bppArgb);
            using Graphics g = Graphics.FromImage(target);
            float r = tint.R / 255f;
            float gScale = tint.G / 255f;
            float b = tint.B / 255f;
            var matrix = new ColorMatrix(new[]
            {
                new[] { r, 0f, 0f, 0f, 0f },
                new[] { 0f, gScale, 0f, 0f, 0f },
                new[] { 0f, 0f, b, 0f, 0f },
                new[] { 0f, 0f, 0f, 1f, 0f },
                new[] { 0f, 0f, 0f, 0f, 1f }
            });
            using var attributes = new ImageAttributes();
            attributes.SetColorMatrix(matrix);
            g.DrawImage(source,
                new Rectangle(0, 0, source.Width, source.Height),
                0, 0, source.Width, source.Height,
                GraphicsUnit.Pixel, attributes);
            return target;
        }
    }
}
