#nullable enable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using WHATSAPP_CHAPT.App.Models;
using WHATSAPP_CHAPT.App.Services;

namespace WHATSAPP_CHAPT.App.Drawing
{
    /// <summary>
    /// Gestionnaire de cache pour les images d'avatars circulaires (initiales ou photos de profil).
    /// </summary>
    public sealed class AvatarCache : IDisposable
    {
        private readonly IAvatarProvider? _avatarProvider;
        private readonly record struct AvatarKey(string Initials, int Argb, int Size);
        private readonly record struct OwnerKey(Guid OwnerId, int Size, string ContentId);

        private readonly Dictionary<AvatarKey, Bitmap> _cache = new();
        private readonly Dictionary<OwnerKey, Bitmap> _ownerCache = new();
        private bool _disposed;

        /// <summary>
        /// Initialise une nouvelle instance de <see cref="AvatarCache"/>.
        /// </summary>
        /// <param name="avatarProvider">Fournisseur de photos de profil (optionnel).</param>
        public AvatarCache(IAvatarProvider? avatarProvider = null)
        {
            _avatarProvider = avatarProvider;
            if (_avatarProvider != null)
            {
                _avatarProvider.PhotoChanged += OnPhotoChanged;
            }
        }

        private void OnPhotoChanged(object? sender, Guid ownerId)
        {
            var keysToRemove = new List<OwnerKey>();
            foreach (var kvp in _ownerCache)
            {
                if (kvp.Key.OwnerId == ownerId)
                {
                    keysToRemove.Add(kvp.Key);
                    kvp.Value.Dispose();
                }
            }

            foreach (var key in keysToRemove)
            {
                _ownerCache.Remove(key);
            }
        }

        /// <summary>
        /// Obtient ou génère l'avatar d'un propriétaire (photo découpée en cercle ou initiales).
        /// </summary>
        /// <param name="ownerId">Identifiant du propriétaire.</param>
        /// <param name="displayName">Nom affiché utilisé pour le calcul des initiales si aucune photo n'existe.</param>
        /// <param name="sizePx">Taille de l'avatar en pixels.</param>
        /// <returns>Image bitmap de l'avatar.</returns>
        public Bitmap GetAvatar(Guid ownerId, string displayName, int sizePx)
        {
            if (sizePx <= 0)
            {
                sizePx = 48;
            }

            string? photoId = _avatarProvider?.GetPhotoId(ownerId);
            string contentId = photoId ?? "initiales";
            var key = new OwnerKey(ownerId, sizePx, contentId);

            if (_ownerCache.TryGetValue(key, out var cachedBitmap))
            {
                return cachedBitmap;
            }

            if (photoId != null)
            {
                using var photo = _avatarProvider?.OpenPhoto(photoId);
                if (photo != null)
                {
                    var photoBitmap = new Bitmap(sizePx, sizePx, PixelFormat.Format32bppPArgb);
                    using (var g = Graphics.FromImage(photoBitmap))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.Clear(Color.Transparent);

                        using var path = new GraphicsPath();
                        path.AddEllipse(0, 0, sizePx - 1, sizePx - 1);
                        g.SetClip(path);
                        g.DrawImage(photo, new Rectangle(0, 0, sizePx, sizePx));
                    }

                    _ownerCache[key] = photoBitmap;
                    return photoBitmap;
                }
            }

            // Fallback : initiales actuelles
            string initials = ConversationListItem.GetInitials(displayName);
            Color backgroundColor = ConversationListItem.GetAvatarColor(displayName);

            var fallbackBitmap = new Bitmap(sizePx, sizePx, PixelFormat.Format32bppPArgb);
            using (var g = Graphics.FromImage(fallbackBitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                using (var brush = new SolidBrush(backgroundColor))
                {
                    g.FillEllipse(brush, 0, 0, sizePx - 1, sizePx - 1);
                }

                float fontSize = Math.Max(8f, sizePx * 0.38f);
                using var font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
                using var format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                using var textBrush = new SolidBrush(Theme.TickRead);
                g.DrawString(initials, font, textBrush, new RectangleF(0, 0, sizePx, sizePx), format);
            }

            _ownerCache[key] = fallbackBitmap;
            return fallbackBitmap;
        }

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
        /// Libère l'ensemble des images bitmap du cache et se désabonne de PhotoChanged.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_avatarProvider != null)
            {
                _avatarProvider.PhotoChanged -= OnPhotoChanged;
            }

            foreach (var bmp in _ownerCache.Values)
            {
                bmp.Dispose();
            }
            _ownerCache.Clear();

            foreach (var bmp in _cache.Values)
            {
                bmp.Dispose();
            }
            _cache.Clear();

            _disposed = true;
        }
    }
}
