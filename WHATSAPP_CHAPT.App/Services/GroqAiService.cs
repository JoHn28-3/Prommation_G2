#nullable enable

using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WHATSAPP_CHAPT.App.Services
{
    /// <summary>
    /// Service isolé pour l'API Groq.
    /// La clé API est lue depuis Config/groq.settings.json (ignoré par .gitignore).
    /// La clé n'est jamais écrite dans le code source, les logs ou les messages d'erreur.
    /// </summary>
    public sealed class GroqAiService : IDisposable
    {
        public const string DefaultTranscriptionModel = "whisper-large-v3-turbo";

        private readonly HttpClient _http;
        private readonly bool _ownsHttpClient;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly string _baseUrl;
        private bool _disposed;

        public bool HasKey => !string.IsNullOrWhiteSpace(_apiKey) && _apiKey != "COLLER_LA_CLE_ICI";

        public GroqAiService()
        {
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _ownsHttpClient = true;
            var configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "groq.settings.json");
            if (!System.IO.File.Exists(configPath))
            {
                // Mode démo sans clé
                _apiKey = string.Empty;
                _model = "llama-3.3-70b-versatile";
                _baseUrl = "https://api.groq.com/openai/v1";
                return;
            }

            try
            {
                var json = System.IO.File.ReadAllText(configPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                _apiKey = root.GetProperty("ApiKey").GetString() ?? string.Empty;
                _model = root.GetProperty("Model").GetString() ?? "llama-3.3-70b-versatile";
                _baseUrl = root.GetProperty("BaseUrl").GetString() ?? "https://api.groq.com/openai/v1";
            }
            catch
            {
                _apiKey = string.Empty;
                _model = "llama-3.3-70b-versatile";
                _baseUrl = "https://api.groq.com/openai/v1";
            }
        }

        internal GroqAiService(HttpClient http, string apiKey, string baseUrl)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
            _model = "llama-3.3-70b-versatile";
        }

        /// <summary>
        /// Envoie un message à Groq et retourne la réponse texte.
        /// Retourne null si la clé manque ou si une erreur se produit.
        /// errorMessage contient un message lisible (sans exposer la clé).
        /// </summary>
        public async Task<(string? Response, string? ErrorMessage)> AskAsync(
            string prompt,
            CancellationToken ct = default)
        {
            if (!HasKey)
            {
                return (null, "Clé API Groq manquante. Éditez Config/groq.settings.json et remplacez COLLER_LA_CLE_ICI par votre clé.");
            }

            var payload = new
            {
                model = _model,
                messages = new[] { new { role = "user", content = prompt } },
                temperature = 0.7,
                max_tokens = 1024
            };

            var body = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            try
            {
                using var response = await _http.PostAsync(_baseUrl.TrimEnd('/') + "/chat/completions", body, ct);

                if (!response.IsSuccessStatusCode)
                {
                    return (null, $"Erreur Groq HTTP {(int)response.StatusCode} : vérifiez votre clé et votre quota.");
                }

                var respJson = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(respJson);
                var choices = doc.RootElement.GetProperty("choices");
                if (choices.GetArrayLength() > 0)
                {
                    return (choices[0].GetProperty("message").GetProperty("content").GetString(), null);
                }
                return (null, "Réponse vide de Groq.");
            }
            catch (TaskCanceledException)
            {
                return (null, "Délai d'attente dépassé (30 s). Vérifiez votre connexion Internet.");
            }
            catch (HttpRequestException)
            {
                return (null, "Impossible de joindre l'API Groq. Vérifiez votre connexion Internet.");
            }
            catch (JsonException)
            {
                return (null, "Réponse Groq illisible (JSON invalide).");
            }
        }

        public async Task<(string? Transcription, string? ErrorMessage)> TranscribeAudioAsync(
            string audioFilePath,
            string model,
            string language,
            CancellationToken ct = default)
        {
            if (!HasKey)
            {
                return (null, "Clé API Groq manquante.");
            }

            try
            {
                using var file = new FileStream(audioFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var form = new MultipartFormDataContent();
                using var fileContent = new StreamContent(file);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetAudioContentType(audioFilePath));
                form.Add(fileContent, "file", Path.GetFileName(audioFilePath));
                form.Add(new StringContent(model), "model");
                form.Add(new StringContent("json"), "response_format");
                form.Add(new StringContent(language), "language");

                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    _baseUrl.TrimEnd('/') + "/audio/transcriptions")
                {
                    Content = form
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
                if (!response.IsSuccessStatusCode)
                {
                    if ((int)response.StatusCode == 401)
                    {
                        return (null, "Clé Groq invalide ou non autorisée (HTTP 401).");
                    }

                    if ((int)response.StatusCode == 429)
                    {
                        string retryAfter = response.Headers.RetryAfter?.Delta is TimeSpan delay
                            ? $" Réessayez dans {Math.Ceiling(delay.TotalSeconds)} secondes."
                            : string.Empty;
                        return (null, $"Limite de transcription Groq atteinte (HTTP 429).{retryAfter}");
                    }

                    return (null, $"Erreur Groq HTTP {(int)response.StatusCode} pendant la transcription.");
                }

                string responseJson = await response.Content.ReadAsStringAsync(ct);
                using var document = JsonDocument.Parse(responseJson);
                if (document.RootElement.TryGetProperty("text", out JsonElement text)
                    && text.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(text.GetString()))
                {
                    return (text.GetString(), null);
                }

                return (null, "La réponse de transcription Groq ne contient aucun texte.");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return (null, "Transcription annulée.");
            }
            catch (OperationCanceledException)
            {
                return (null, "Délai de transcription dépassé. Vérifiez votre connexion Internet.");
            }
            catch (HttpRequestException)
            {
                return (null, "Impossible de joindre Groq pour la transcription. Vérifiez votre connexion Internet.");
            }
            catch (JsonException)
            {
                return (null, "Réponse de transcription Groq illisible.");
            }
            catch (IOException)
            {
                return (null, "Le fichier audio n'a pas pu être lu.");
            }
        }

        private static string GetAudioContentType(string path)
        {
            return Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".flac" => "audio/flac",
                ".mp3" => "audio/mpeg",
                ".mp4" => "audio/mp4",
                ".mpeg" or ".mpga" => "audio/mpeg",
                ".m4a" => "audio/mp4",
                ".ogg" => "audio/ogg",
                ".wav" => "audio/wav",
                ".webm" => "audio/webm",
                _ => "application/octet-stream"
            };
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_ownsHttpClient)
                {
                    _http.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
