#nullable enable

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WHATSAPP_CHAPT.App.Services
{
    public sealed class AudioTranscriptionCacheService
    {
        public enum TranscriptionStatus { Idle, Processing, Done, Error }

        private const long MaximumAudioBytes = 25 * 1024 * 1024;
        private static readonly string[] SupportedExtensions =
            { ".flac", ".mp3", ".mp4", ".mpeg", ".mpga", ".m4a", ".ogg", ".wav", ".webm" };
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

        private readonly GroqAiService _groq;
        private readonly string _cacheDir;

        public AudioTranscriptionCacheService(GroqAiService groq)
        {
            _groq = groq ?? throw new ArgumentNullException(nameof(groq));
            _cacheDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WHATSAPP_CHAPT",
                "TranscriptionCache");
            Directory.CreateDirectory(_cacheDir);
        }

        public async Task<(string Transcription, TranscriptionStatus Status, string? Error)> TranscribeAsync(
            string audioFilePath,
            IProgress<string>? progress = null,
            CancellationToken ct = default,
            string model = GroqAiService.DefaultTranscriptionModel,
            string language = "fr")
        {
            if (!File.Exists(audioFilePath))
            {
                return (string.Empty, TranscriptionStatus.Error, "Fichier audio introuvable.");
            }

            if (!Array.Exists(SupportedExtensions, extension =>
                    string.Equals(extension, Path.GetExtension(audioFilePath), StringComparison.OrdinalIgnoreCase)))
            {
                return (string.Empty, TranscriptionStatus.Error,
                    "Format non pris en charge. Formats acceptés : flac, mp3, mp4, mpeg, mpga, m4a, ogg, wav et webm.");
            }

            try
            {
                if (new FileInfo(audioFilePath).Length > MaximumAudioBytes)
                {
                    return (string.Empty, TranscriptionStatus.Error,
                        "Le fichier dépasse la limite de transcription de 25 Mo.");
                }

                progress?.Report("Calcul de l'empreinte…");
                string contentHash = ComputeSha256(audioFilePath);
                string optionsHash = ComputeSha256(Encoding.UTF8.GetBytes($"{model}\n{language}"));
                string cacheFile = Path.Combine(_cacheDir, $"{contentHash}_{optionsHash}.json");
                SemaphoreSlim gate = Locks.GetOrAdd(cacheFile, _ => new SemaphoreSlim(1, 1));
                await gate.WaitAsync(ct);
                try
                {
                    if (File.Exists(cacheFile))
                    {
                        try
                        {
                            CacheEntry? cached = JsonSerializer.Deserialize<CacheEntry>(
                                await File.ReadAllTextAsync(cacheFile, ct));
                            if (cached != null)
                            {
                                progress?.Report("Transcription trouvée dans le cache.");
                                return (cached.Transcription, TranscriptionStatus.Done, null);
                            }
                        }
                        catch (JsonException)
                        {
                        }
                    }

                    if (!_groq.HasKey)
                    {
                        return (string.Empty, TranscriptionStatus.Error, "Clé Groq manquante.");
                    }

                    progress?.Report("Envoi de l'audio à Groq pour transcription…");
                    var result = await _groq.TranscribeAudioAsync(audioFilePath, model, language, ct);
                    if (result.Transcription == null)
                    {
                        return (string.Empty, TranscriptionStatus.Error, result.ErrorMessage);
                    }

                    var entry = new CacheEntry
                    {
                        Transcription = result.Transcription,
                        CreatedAt = DateTime.UtcNow
                    };
                    string temporaryFile = cacheFile + "." + Guid.NewGuid().ToString("N") + ".tmp";
                    await File.WriteAllTextAsync(temporaryFile, JsonSerializer.Serialize(entry), ct);
                    File.Move(temporaryFile, cacheFile, overwrite: true);
                    return (result.Transcription, TranscriptionStatus.Done, null);
                }
                finally
                {
                    gate.Release();
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return (string.Empty, TranscriptionStatus.Error, "Transcription annulée.");
            }
            catch (IOException)
            {
                return (string.Empty, TranscriptionStatus.Error, "Impossible de lire ou mettre en cache le fichier audio.");
            }
            catch (UnauthorizedAccessException)
            {
                return (string.Empty, TranscriptionStatus.Error, "Accès au fichier audio refusé.");
            }
        }

        private static string ComputeSha256(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        }

        private static string ComputeSha256(byte[] content)
        {
            return Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        }

        private sealed class CacheEntry
        {
            public string Transcription { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
        }
    }
}
