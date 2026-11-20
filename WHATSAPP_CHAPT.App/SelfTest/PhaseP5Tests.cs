#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WHATSAPP_CHAPT.App.Services;

namespace WHATSAPP_CHAPT.App.SelfTest
{
    public static class PhaseP5Tests
    {
        public static void Run(List<(string Name, bool Success, string? Detail)> results)
        {
            Test("P5_Wav_EntetePCM", () =>
            {
                using var stream = new MemoryStream();
                byte[] audio = { 0, 0, 255, 127 };
                WaveInRecorder.WriteWave(stream, audio);
                stream.Position = 0;
                using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
                if (new string(reader.ReadChars(4)) != "RIFF"
                    || reader.ReadInt32() != 40
                    || new string(reader.ReadChars(4)) != "WAVE"
                    || new string(reader.ReadChars(4)) != "fmt "
                    || reader.ReadInt32() != 16
                    || reader.ReadInt16() != 1
                    || reader.ReadInt16() != 1
                    || reader.ReadInt32() != 16000
                    || reader.ReadInt32() != 32000
                    || reader.ReadInt16() != 2
                    || reader.ReadInt16() != 16
                    || new string(reader.ReadChars(4)) != "data"
                    || reader.ReadInt32() != audio.Length)
                {
                    throw new Exception("En-tête WAV PCM incorrect.");
                }
            }, results);

            Test("P5_Audio_CalculCrete", () =>
            {
                if (WaveInRecorder.CalculatePeak(new byte[] { 0, 0, 0, 0 }, 4) != 0
                    || WaveInRecorder.CalculatePeak(BitConverter.GetBytes(short.MinValue), 2) != 32768)
                {
                    throw new Exception("Calcul de crête PCM incorrect.");
                }
            }, results);

            Test("P5_Groq_MultipartEtCache", () =>
                RunAsync(TestMultipartAndCacheAsync), results);
            Test("P5_Groq_Erreurs401429", () =>
                RunAsync(TestHttpErrorsAsync), results);
            Test("P5_Groq_CleAbsenteEtFormat", () =>
                RunAsync(TestMissingKeyAndFormatAsync), results);
        }

        private static async Task TestMultipartAndCacheAsync()
        {
            string path = CreateAudioFile();
            int calls = 0;
            var requestFields = new HashSet<string>(StringComparer.Ordinal);
            try
            {
                using var client = new HttpClient(new StubHandler(request =>
                {
                    calls++;
                    if (request.Content is not MultipartFormDataContent form
                        || form.Headers.ContentType?.MediaType != "multipart/form-data")
                    {
                        throw new Exception("La requête audio n'est pas multipart/form-data.");
                    }
                    foreach (var part in form)
                    {
                        string? name = part.Headers.ContentDisposition?.Name?.Trim('"');
                        if (!string.IsNullOrEmpty(name))
                        {
                            requestFields.Add(name);
                        }
                    }
                    return Task.FromResult(JsonResponse(HttpStatusCode.OK, "{\"text\":\"transcription de test\"}"));
                }));
                using var groq = new GroqAiService(client, "test-key", "https://example.test/openai/v1");
                var cache = new AudioTranscriptionCacheService(groq);
                var first = await cache.TranscribeAsync(path);
                var second = await cache.TranscribeAsync(path);

                if (first.Transcription != "transcription de test"
                    || second.Transcription != first.Transcription
                    || calls != 1
                    || !requestFields.Contains("file")
                    || !requestFields.Contains("model")
                    || !requestFields.Contains("language"))
                {
                    throw new Exception(
                        "Requête multipart ou cache de transcription incorrect.");
                }
            }
            finally
            {
                File.Delete(path);
            }
        }

        private static async Task TestHttpErrorsAsync()
        {
            string path = CreateAudioFile();
            try
            {
                using var unauthorizedClient = new HttpClient(new StubHandler(_ =>
                    Task.FromResult(JsonResponse(HttpStatusCode.Unauthorized, "{}"))));
                using var unauthorizedGroq = new GroqAiService(
                    unauthorizedClient, "test-key", "https://example.test/openai/v1");
                var unauthorized = await unauthorizedGroq.TranscribeAudioAsync(
                    path, GroqAiService.DefaultTranscriptionModel, "fr");
                if (unauthorized.ErrorMessage == null || !unauthorized.ErrorMessage.Contains("401", StringComparison.Ordinal))
                {
                    throw new Exception("Erreur HTTP 401 non gérée.");
                }

                using var limitedClient = new HttpClient(new StubHandler(_ =>
                {
                    var response = JsonResponse((HttpStatusCode)429, "{}");
                    response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(7));
                    return Task.FromResult(response);
                }));
                using var limitedGroq = new GroqAiService(
                    limitedClient, "test-key", "https://example.test/openai/v1");
                var limited = await limitedGroq.TranscribeAudioAsync(
                    path, GroqAiService.DefaultTranscriptionModel, "fr");
                if (limited.ErrorMessage == null
                    || !limited.ErrorMessage.Contains("429", StringComparison.Ordinal)
                    || !limited.ErrorMessage.Contains("7", StringComparison.Ordinal))
                {
                    throw new Exception("Erreur HTTP 429 ou Retry-After non gérée.");
                }
            }
            finally
            {
                File.Delete(path);
            }
        }

        private static async Task TestMissingKeyAndFormatAsync()
        {
            string path = CreateAudioFile(".wav");
            string unsupportedPath = CreateAudioFile(".txt");
            try
            {
                using var client = new HttpClient(new StubHandler(_ =>
                    throw new Exception("Une requête réseau ne devait pas être envoyée.")));
                using var groq = new GroqAiService(client, string.Empty, "https://example.test/openai/v1");
                var missingKey = await groq.TranscribeAudioAsync(
                    path, GroqAiService.DefaultTranscriptionModel, "fr");
                if (missingKey.ErrorMessage == null || !missingKey.ErrorMessage.Contains("manquante", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception("Absence de clé API non signalée.");
                }

                using var configuredGroq = new GroqAiService(
                    client, "test-key", "https://example.test/openai/v1");
                var cache = new AudioTranscriptionCacheService(configuredGroq);
                var unsupported = await cache.TranscribeAsync(unsupportedPath);
                if (unsupported.Status != AudioTranscriptionCacheService.TranscriptionStatus.Error
                    || unsupported.Error == null
                    || !unsupported.Error.Contains("Format non pris en charge", StringComparison.Ordinal))
                {
                    throw new Exception("Format audio non pris en charge non signalé.");
                }
            }
            finally
            {
                File.Delete(path);
                File.Delete(unsupportedPath);
            }
        }

        private static string CreateAudioFile(string extension = ".wav")
        {
            string path = Path.Combine(Path.GetTempPath(), $"whatsapp_p5_{Guid.NewGuid():N}{extension}");
            File.WriteAllBytes(path, Guid.NewGuid().ToByteArray());
            return path;
        }

        private static HttpResponseMessage JsonResponse(HttpStatusCode status, string json)
        {
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }

        private static void RunAsync(Func<Task> body) => body().GetAwaiter().GetResult();

        private static void Test(
            string name,
            Action body,
            List<(string Name, bool Success, string? Detail)> results)
        {
            try
            {
                body();
                results.Add((name, true, null));
            }
            catch (Exception ex)
            {
                results.Add((name, false, ex.Message));
            }
        }

        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _responseFactory;

            public StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory)
            {
                _responseFactory = responseFactory;
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                return _responseFactory(request);
            }
        }
    }
}
