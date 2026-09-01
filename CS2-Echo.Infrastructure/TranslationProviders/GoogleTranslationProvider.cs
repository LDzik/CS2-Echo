using CS2_Echo.Logic.Constants;
using CS2_Echo.Logic.Interfaces;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CS2_Echo.Infrastructure.TranslationProviders;

public class GoogleTranslationProvider : ITranslationProvider
{
    private readonly HttpClient _httpClient;

    private static DateTimeOffset _cooldownUntil = DateTimeOffset.MinValue;
    private static DateTimeOffset _lastRequestTime = DateTimeOffset.MinValue;
    private static readonly SemaphoreSlim _throttle = new(1, 1);
    private static readonly TimeSpan MinTimeBetweenRequests = TimeSpan.FromMilliseconds(500);

    public string Name => EngineNames.Google;

    public GoogleTranslationProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<(string TranslatedText, string DetectedSourceLang)> TranslateAsync(string text, string targetLang)
    {
        if (DateTimeOffset.UtcNow < _cooldownUntil)
        {
            System.Diagnostics.Debug.WriteLine("[Google API] Currently in 429 cooldown period. Skipping translation.");
            return (text, "error");
        }

        int maxRetries = 3;
        int delayMs = 1000;

        for (int retry = 0; retry < maxRetries; retry++)
        {
            await _throttle.WaitAsync();

            try
            {
                var timeSinceLast = DateTimeOffset.UtcNow - _lastRequestTime;
                if (timeSinceLast < MinTimeBetweenRequests)
                {
                    await Task.Delay(MinTimeBetweenRequests - timeSinceLast);
                }
                _lastRequestTime = DateTimeOffset.UtcNow;

                //string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl={targetLang}&dt=t&q={Uri.EscapeDataString(text)}";
                string url = $"https://clients5.google.com/translate_a/t?client=dict-chrome-ex&sl=auto&tl={targetLang}&q={Uri.EscapeDataString(text)}";

                using var request = new HttpRequestMessage(HttpMethod.Get, url)
                {
                    Version = HttpVersion.Version20
                };

                using var response = await _httpClient.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    _cooldownUntil = DateTimeOffset.UtcNow.AddSeconds(30);
                    System.Diagnostics.Debug.WriteLine("[Google API] Rate limited (HTTP 429). Triggering 30s global cooldown.");
                    return (text, "error");
                }

                response.EnsureSuccessStatusCode();

                string content = await response.Content.ReadAsStringAsync();
                // Format: ["Translated string", "detected_lang"] or [["Translated string"],"detected_lang"]
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                string translatedText = string.Empty;
                string detectedSourceLang = "unknown";

                if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                {
                    var firstElem = root[0];

                    if (firstElem.ValueKind == JsonValueKind.Array)
                    {
                        if (firstElem.GetArrayLength() > 0)
                        {
                            translatedText = firstElem[0].GetString() ?? string.Empty;
                        }

                        // Format: [["Translated string", "detected_lang"]]
                        if (firstElem.GetArrayLength() > 1 && firstElem[1].ValueKind == JsonValueKind.String)
                        {
                            detectedSourceLang = firstElem[1].GetString() ?? "unknown";
                        }
                    }
                    else if (firstElem.ValueKind == JsonValueKind.String)
                    {
                        // Format: ["Translated string", "detected_lang"]
                        translatedText = firstElem.GetString() ?? string.Empty;
                    }

                    // Format: [["Translated string"],"detected_lang"] or ["Translated string", "detected_lang"]
                    if (root.GetArrayLength() > 1 && root[1].ValueKind == JsonValueKind.String)
                    {
                        detectedSourceLang = root[1].GetString() ?? "unknown";
                    }
                }

                return (translatedText, detectedSourceLang);
            }
            catch (HttpRequestException ex) when (retry < maxRetries - 1 && ex.StatusCode != HttpStatusCode.TooManyRequests)
            {
                System.Diagnostics.Debug.WriteLine($"[Google API] Transient error ({ex.StatusCode}). Retrying in {delayMs}ms...");
                await Task.Delay(delayMs);
                delayMs *= 2;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Google API Error] {ex.Message}");
                return (text, "error");
            }
            finally
            {
                _throttle.Release();
            }
        }

        return (text, "error");
    }
}