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
            Console.WriteLine("[Google API] Currently in 429 cooldown period. Skipping translation.");
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

                string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl={targetLang}&dt=t&q={Uri.EscapeDataString(text)}";


                using var response = await _httpClient.GetAsync(url);
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    _cooldownUntil = DateTimeOffset.UtcNow.AddSeconds(30);
                    Console.WriteLine("[Google API] Rate limited (HTTP 429). Triggering 30s global cooldown.");
                    return (text, "error");
                }

                response.EnsureSuccessStatusCode();

                string content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);

                var translatedText = doc.RootElement[0][0][0].GetString() ?? string.Empty;
                var detectedSourceLang = doc.RootElement[2].GetString() ?? "unknown";

                return (translatedText, detectedSourceLang);
            }
            catch (HttpRequestException ex) when (retry < maxRetries - 1 && ex.StatusCode != HttpStatusCode.TooManyRequests)
            {
                Console.WriteLine($"[Google API] Transient error ({ex.StatusCode}). Retrying in {delayMs}ms...");
                await Task.Delay(delayMs);
                delayMs *= 2;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Google API Error] {ex.Message}");
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