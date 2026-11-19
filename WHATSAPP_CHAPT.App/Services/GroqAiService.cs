#nullable enable

using System;
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
        private readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        private readonly string _apiKey;
        private readonly string _model;
        private readonly string _baseUrl;
        private bool _disposed;

        public bool HasKey => !string.IsNullOrWhiteSpace(_apiKey) && _apiKey != "COLLER_LA_CLE_ICI";

        public GroqAiService()
        {
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

        public void Dispose()
        {
            if (!_disposed)
            {
                _http.Dispose();
                _disposed = true;
            }
        }
    }
}
