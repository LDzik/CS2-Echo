using CS2_Echo.Infrastructure.Services;
using CS2_Echo.Logic.Interfaces;
using CS2_Echo.Logic.Constants;
using DeepL;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CS2_Echo.Infrastructure.TranslationProviders;

public class DeepLTranslationProvider : ITranslationProvider
{
    
    private readonly ConfigurationService _configService;

    private volatile DeepLClient _deepLClient;
    private volatile string _cachedKey;

    private readonly object _clientLock = new();

    public string Name => EngineNames.DeepL;

    public DeepLTranslationProvider(ConfigurationService configService)
    {
        _configService = configService;
    }

    private void EnsureClientIsReady(string currentApiKey)
    {
        int maxByteCount = Encoding.UTF8.GetMaxByteCount(currentApiKey.Length);
        Span<byte> keyBytes = maxByteCount <= 1024 ? stackalloc byte[maxByteCount] : new byte[maxByteCount];
        int actualByteCount = Encoding.UTF8.GetBytes(currentApiKey, keyBytes);

        Span<byte> hashBytes = stackalloc byte[32];
        SHA256.HashData(keyBytes[..actualByteCount], hashBytes);

        keyBytes.Clear();

        string keyHash = Convert.ToBase64String(hashBytes);

        if (_deepLClient == null || _cachedKey != keyHash)
        {
            lock (_clientLock)
            {
                if (_deepLClient == null || _cachedKey != keyHash)
                {
                    var oldClient = _deepLClient;

                    _cachedKey = keyHash;

                    // SECURITY EXCEPTION:
                    // The DeepL SDK requires the API key as an immutable string.
                    // The plaintext string will exist on the managed heap until garbage collected.
                    _deepLClient = new DeepLClient(currentApiKey);

                    oldClient?.Dispose();
                }
            }
        }
    }

    public async Task<(string TranslatedText, string DetectedSourceLang)> TranslateAsync(string text, string targetLang)
    {
        var apiKey = _configService.GetDecryptedDeepLKey();

        if (string.IsNullOrWhiteSpace(apiKey))
            return (text, "error");

        try
        {

            EnsureClientIsReady(apiKey);


            //var usage = await _deepLClient.GetUsageAsync();
            //if (usage.AnyLimitReached)
            //{
            //    Console.WriteLine("Translation limit exceeded.");
            //}
            //else if (usage.Character != null)
            //{
            //    Console.WriteLine($"Character usage: {usage.Character}");
            //}
            //else
            //{
            //    Console.WriteLine($"{usage}");
            //}



            var translationResult = await _deepLClient.TranslateTextAsync(new[] { text }, null, targetLang.ToUpper());

            var translatedText = translationResult[0].Text;
            var detectedSourceLang = translationResult[0].DetectedSourceLanguageCode.ToLower();
            //var billedCharacters = translationResult[0].BilledCharacters;

            return (translatedText, detectedSourceLang);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DeepL API Error] {ex.Message}");
            return (text, "error");
        }
    }
}
