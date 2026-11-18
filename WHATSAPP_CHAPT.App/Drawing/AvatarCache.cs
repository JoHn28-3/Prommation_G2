#nullable enable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace WHATSAPP_CHAPT.App.Drawing
{
    /// <summary>
    /// Gestionnaire de cache pour les images d'avatars circulaires.
    /// </summary>
    public sealed class AvatarCache : IDisposable
    {
        private readonly record struct AvatarKey(string Initials, int Argb, int Size);
        private readonly Dictionary<AvatarKey, Bitmap> _cache = new();
        private bool _disposed;

        /// <summary>
        /// Obtient ou génère un avatar rond contenant les initiales spécifiées.
        /// </summary>
        /// <param name="initials">Initiales à afficher.</param>
        /// <param name="backgroundColor">Couleur de fond de l'avatar.</param>
        /// <param name="size">Taille de l'avatar en pixels.</param>
        /// <returns>Image bitmap de l'avatar.</returns>
        public Bitmap GetAvatar(string initials, Color backgroundColor, int size)
        {
            if (size <= 0)
            {
                size = 48;
            }

            var cleanInitials = string.IsNullOrWhiteSpace(initials) ? "?" : initials.Trim();
            var key = new AvatarKey(cleanInitials, backgroundColor.ToArgb(), size);

            if (_cache.TryGetValue(key, out var cachedBitmap))
            {
                return cachedBitmap;
            }

            var bitmap = new Bitmap(size, size, PixelFormat.Format32bppPArgb);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                using (var brush = new SolidBrush(backgroundColor))
                {
                    g.FillEllipse(brush, 0, 0, size - 1, size - 1);
                }

                float fontSize = Math.Max(8f, size * 0.38f);
                using var font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
                using var format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                using var textBrush = new SolidBrush(Theme.TickRead);
                g.DrawString(cleanInitials, font, textBrush, new RectangleF(0, 0, size, size), format);
            }

            _cache[key] = bitmap;
            return bitmap;
        }

        /// <summary>
        /// Libère l'ensemble des images bitmap du cache.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            foreach (var bmp in _cache.Values)
            {
                bmp.Dispose();
            }

            _cache.Clear();
            _disposed = true;
        }
    }
}
