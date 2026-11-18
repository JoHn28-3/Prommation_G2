#nullable enable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace WHATSAPP_CHAPT.App.Services
{
    /// <summary>
    /// Stockage local sur disque des photos de profil dans %LOCALAPPDATA%\WHATSAPP_CHAPT\avatars\.
    /// </summary>
    public sealed class LocalAvatarStore : IAvatarProvider
    {
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 Mo
        private readonly string _avatarsDirectory;
        private readonly string _profilesJsonPath;
        private readonly object _lock = new();

        private Dictionary<string, string> _profiles = new();

        /// <inheritdoc/>
        public event EventHandler<Guid>? PhotoChanged;

        /// <summary>
        /// Initialise une nouvelle instance de <see cref="LocalAvatarStore"/> et charge profiles.json.
        /// </summary>
        public LocalAvatarStore()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _avatarsDirectory = Path.Combine(localAppData, "WHATSAPP_CHAPT", "avatars");
            _profilesJsonPath = Path.Combine(_avatarsDirectory, "profiles.json");

            try
            {
                if (!Directory.Exists(_avatarsDirectory))
                {
                    Directory.CreateDirectory(_avatarsDirectory);
                }

                LoadProfiles();
            }
            catch
            {
                _profiles = new Dictionary<string, string>();
            }
        }

        private void LoadProfiles()
        {
            lock (_lock)
            {
                if (File.Exists(_profilesJsonPath))
                {
                    string json = File.ReadAllText(_profilesJsonPath);
                    _profiles = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                                ?? new Dictionary<string, string>();
                }
                else
                {
                    _profiles = new Dictionary<string, string>();
                }
            }
        }

        private void SaveProfiles()
        {
            lock (_lock)
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_profiles, options);
                File.WriteAllText(_profilesJsonPath, json);
            }
        }

        /// <inheritdoc/>
        public string? GetPhotoId(Guid ownerId)
        {
            lock (_lock)
            {
                return _profiles.TryGetValue(ownerId.ToString("D"), out var hash) ? hash : null;
            }
        }

        /// <inheritdoc/>
        public Image? OpenPhoto(string photoId)
        {
            if (string.IsNullOrWhiteSpace(photoId))
            {
                return null;
            }

            try
            {
                string photoPath = Path.Combine(_avatarsDirectory, $"{photoId}.jpg");
                if (!File.Exists(photoPath))
                {
                    return null;
                }

                byte[] bytes = File.ReadAllBytes(photoPath);
                using var ms = new MemoryStream(bytes);
                using var temp = Image.FromStream(ms);
                return new Bitmap(temp);
            }
            catch
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public bool SetPhoto(Guid ownerId, string sourceFilePath)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath))
            {
                return false;
            }

            try
            {
                if (!File.Exists(sourceFilePath))
                {
                    return false;
                }

                var fileInfo = new FileInfo(sourceFilePath);
                if (fileInfo.Length > MaxFileSizeBytes || fileInfo.Length == 0)
                {
                    return false;
                }

                byte[] sourceBytes = File.ReadAllBytes(sourceFilePath);
                using var msIn = new MemoryStream(sourceBytes);
                using var sourceImage = Image.FromStream(msIn);

                int minSide = Math.Min(sourceImage.Width, sourceImage.Height);
                if (minSide <= 0)
                {
                    return false;
                }

                int cropX = (sourceImage.Width - minSide) / 2;
                int cropY = (sourceImage.Height - minSide) / 2;

                // Redimensionnement en 256x256 avec interpolation haute qualité
                using var resizedBmp = new Bitmap(256, 256, PixelFormat.Format24bppRgb);
                using (var g = Graphics.FromImage(resizedBmp))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.CompositingQuality = CompositingQuality.HighQuality;

                    g.DrawImage(
                        sourceImage,
                        new Rectangle(0, 0, 256, 256),
                        new Rectangle(cropX, cropY, minSide, minSide),
                        GraphicsUnit.Pixel);
                }

                // Encodage JPEG qualité 80
                var jpegCodec = GetJpegCodec();
                if (jpegCodec == null)
                {
                    return false;
                }

                using var encoderParameters = new EncoderParameters(1);
                encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, 80L);

                using var msOut = new MemoryStream();
                resizedBmp.Save(msOut, jpegCodec, encoderParameters);
                byte[] jpegBytes = msOut.ToArray();

                // Calcul du hash SHA-256
                byte[] hashBytes = SHA256.HashData(jpegBytes);
                string sha256 = Convert.ToHexString(hashBytes).ToLowerInvariant();

                string photoPath = Path.Combine(_avatarsDirectory, $"{sha256}.jpg");
                if (!File.Exists(photoPath))
                {
                    File.WriteAllBytes(photoPath, jpegBytes);
                }

                // Mise à jour de profiles.json
                lock (_lock)
                {
                    _profiles[ownerId.ToString("D")] = sha256;
                    SaveProfiles();
                }

                PhotoChanged?.Invoke(this, ownerId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public void RemovePhoto(Guid ownerId)
        {
            try
            {
                bool removed;
                lock (_lock)
                {
                    removed = _profiles.Remove(ownerId.ToString("D"));
                    if (removed)
                    {
                        SaveProfiles();
                    }
                }

                if (removed)
                {
                    PhotoChanged?.Invoke(this, ownerId);
                }
            }
            catch
            {
                // Ignorer les erreurs disque lors de la suppression
            }
        }

        private static ImageCodecInfo? GetJpegCodec()
        {
            var codecs = ImageCodecInfo.GetImageEncoders();
            foreach (var codec in codecs)
            {
                if (codec.FormatID == ImageFormat.Jpeg.Guid)
                {
                    return codec;
                }
            }

            return null;
        }
    }
}
