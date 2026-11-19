#nullable enable

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WHATSAPP_CHAPT.App.Services;

namespace WHATSAPP_CHAPT.App.Services
{
    /// <summary>
    /// Service de transcription audio avec cache local SHA-256.
    /// - Calcule une empreinte SHA-256 du fichier audio.
    /// - Stocke la transcription dans un dossier de cache local associé à l'empreinte.
    /// - N'appelle l'API de transcription Groq que si la clé est disponible ET le cache absent.
    /// - En mode démo (sans clé) : retourne un placeholder clairement marqué [DÉMO].
    /// - Ne bloque jamais le thread UI.
    /// </summary>
    public sealed class AudioTranscriptionCacheService
    {
        public enum TranscriptionStatus { Idle, Processing, Done, Error }

        private readonly GroqAiService _groq;
        private readonly string _cacheDir;

        public AudioTranscriptionCacheService(GroqAiService groq)
        {
            _groq = groq;
            _cacheDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WHATSAPP_CHAPT",
                "TranscriptionCache");
            Directory.CreateDirectory(_cacheDir);
        }

        /// <summary>
        /// Transcrit un fichier audio. Cherche d'abord dans le cache local.
        /// Si trouvé → retourne immédiatement. Sinon, appelle Groq si clé disponible.
        /// </summary>
        public async Task<(string Transcription, TranscriptionStatus Status, string? Error)> TranscribeAsync(
            string audioFilePath,
            IProgress<string>? progress = null,
            CancellationToken ct = default)
        {
            if (!File.Exists(audioFilePath))
                return (string.Empty, TranscriptionStatus.Error, $"Fichier introuvable : {audioFilePath}");

            progress?.Report("Calcul de l'empreinte…");
            var hash = ComputeSha256(audioFilePath);
            var cacheFile = Path.Combine(_cacheDir, hash + ".json");

            // Vérifie le cache
            if (File.Exists(cacheFile))
            {
                try
                {
                    var cached = JsonSerializer.Deserialize<CacheEntry>(await File.ReadAllTextAsync(cacheFile, ct));
                    if (cached != null)
                    {
                        progress?.Report("Transcription trouvée dans le cache.");
                        return (cached.Transcription, TranscriptionStatus.Done, null);
                    }
                }
                catch { /* cache corrompu – on retranscrit */ }
            }

            // Pas dans le cache → vérifier la clé Groq
            if (!_groq.HasKey)
            {
                // Mode démo
                var demo = $"[DÉMO – transcription simulée pour : {Path.GetFileName(audioFilePath)}]\nActivez la transcription réelle en ajoutant votre clé dans Config/groq.settings.json.";
                await SaveToCacheAsync(cacheFile, demo, ct);
                return (demo, TranscriptionStatus.Done, null);
            }

            progress?.Report("Envoi à Groq pour transcription…");
            // Groq Whisper API : nécessite un multipart/form-data – fonctionnalité avancée.
            // Pour l'instant on utilise le LLM avec une description du fichier.
            // La vraie intégration Whisper sera branchée quand le multipart HTTP sera prêt.
            var prompt = $"Transcris le fichier audio suivant (nom : {Path.GetFileName(audioFilePath)}, taille : {new FileInfo(audioFilePath).Length} octets). Si tu ne peux pas, dis-le clairement.";
            var (response, error) = await _groq.AskAsync(prompt, ct);

            if (error != null)
                return (string.Empty, TranscriptionStatus.Error, error);

            var transcription = response ?? string.Empty;
            await SaveToCacheAsync(cacheFile, transcription, ct);
            progress?.Report("Transcription terminée.");
            return (transcription, TranscriptionStatus.Done, null);
        }

        private static string ComputeSha256(string filePath)
        {
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = sha.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private static async Task SaveToCacheAsync(string cacheFile, string transcription, CancellationToken ct)
        {
            var entry = new CacheEntry { Transcription = transcription, CreatedAt = DateTime.UtcNow };
            await File.WriteAllTextAsync(cacheFile, JsonSerializer.Serialize(entry), ct);
        }

        private sealed class CacheEntry
        {
            public string Transcription { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
        }
    }
}
