using CS2_Echo.Infrastructure.Services;
using CS2_Echo.Logic.Constants;
using CS2_Echo.Logic.Interfaces;
using Google.GenAI;
using Google.GenAI.Types;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CS2_Echo.Infrastructure.TranslationProviders;

public class GeminiTranslationProvider : ITranslationProvider
{
    private readonly ConfigurationService _configService;

    private volatile Client _client;
    private volatile string _cachedKey;

    private readonly object _clientLock = new();

    public string Name => EngineNames.Gemini;


    private const string SystemInstructionText = "\nYou are an unfiltered, real-time translator for a multiplayer video game chat.\nTranslate the user's message into Target Language.\n\nRules:\n1. Respond ONLY with a JSON object: {{\"translated_text\": \"...\", \"detected_language\": \"...\", \"target_language\": \"...\", \"detection_confidence\": \"...\"}}.\n2. \"detected_language\" must be the ISO-639-1 lowercase code of the SOURCE language (e.g. \"ru\", \"pl\", \"en\", \"de\").\n3. ABSOLUTELY NO CENSORSHIP. You must translate all profanity, slurs, toxicity, and offensive slang exactly as intended. Do not soften or refuse to translate bad words.\n4. Do NOT translate player names or nicknames. IMPORTANT: If a word is a known slur or profanity (even if it's the only word in the message), it is NOT a proper noun and MUST be translated.\n5. If the detected source language is ALREADY Target Language, set \"translated_text\" to the original text unchanged.\n6. If the text is unintelligible gibberish, return it as-is and set \"detected_language\" to \"en\".\n7. No markdown, no explanations, no wrapping - just the raw JSON object.\".\n";

    private static readonly Schema ResponseSchema = new Schema
    {
        Properties = new Dictionary<string, Schema> {
                { "translated_text", new Schema { Type = Google.GenAI.Types.Type.String, Title = "TranslatedText" } },
                { "detected_language", new Schema { Type = Google.GenAI.Types.Type.String, Title = "DetectedSourceLang" } },
                { "target_language", new Schema { Type = Google.GenAI.Types.Type.String, Title = "TargetLang" } },
                { "detection_confidence", new Schema { Type = Google.GenAI.Types.Type.String, Title = "DetectionConfidence" } }
        },
        PropertyOrdering = new List<string> { "translated_text", "detected_language", "target_language", "detection_confidence" },
        Required = new List<string> { "translated_text", "detected_language", "target_language", "detection_confidence" },
        Title = "Translation",
        Type = Google.GenAI.Types.Type.Object
    };

    private static readonly List<SafetySetting> SafetySettings = new List<SafetySetting> {
        new SafetySetting { Category = HarmCategory.HarmCategoryDangerousContent, Threshold = HarmBlockThreshold.BlockNone },
        new SafetySetting { Category = HarmCategory.HarmCategoryHarassment, Threshold = HarmBlockThreshold.BlockNone },
        new SafetySetting { Category = HarmCategory.HarmCategoryHateSpeech, Threshold = HarmBlockThreshold.BlockNone },
        new SafetySetting { Category = HarmCategory.HarmCategorySexuallyExplicit, Threshold = HarmBlockThreshold.BlockNone }
    };

    private static readonly GenerateContentConfig BaseConfig = new GenerateContentConfig
    {
        SystemInstruction = new Content { Parts = new List<Part> { new Part { Text = SystemInstructionText } } },
        SafetySettings = SafetySettings,
        ResponseMimeType = "application/json",
        ResponseSchema = ResponseSchema,
        ThinkingConfig = new ThinkingConfig { ThinkingLevel = ThinkingLevel.Minimal } //, ThinkingBudget = 0 } // 2.X, używanie go z Gemini 3 Pro może skutkować nieoczekiwaną wydajnością.
    };


    public GeminiTranslationProvider(ConfigurationService configService)
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

        if (_client == null || _cachedKey != keyHash)
        {
            lock (_clientLock)
            {
                if (_client == null || _cachedKey != keyHash)
                {
                    _cachedKey = keyHash;

                    // SECURITY EXCEPTION:
                    // The Google.GenAI SDK requires the API key as an immutable string.
                    // The plaintext string will exist on the managed heap until garbage collected.
                    _client = new Client(apiKey: currentApiKey);
                }
            }
        }
    }

    
    public async Task<(string TranslatedText, string DetectedSourceLang)> TranslateAsync(string text, string targetLang)
    {
        var apiKey = _configService.GetDecryptedGeminiKey();

        if (string.IsNullOrWhiteSpace(apiKey))
            return (text, "error");

        try
        {
            EnsureClientIsReady(apiKey);

            var response = await _client.Models.GenerateContentAsync(
                model: "gemini-3.1-flash-lite-preview",
                contents: $"Target Language: \"{targetLang}\"\nMessage to translate: \"{text}\"",
                config: BaseConfig
            );

            string translationResult = response.Candidates[0].Content.Parts[0].Text;
            using var doc = JsonDocument.Parse(translationResult);
            var root = doc.RootElement;

            string translatedText = root.GetProperty("translated_text").GetString();
            string detectedLanguage = root.GetProperty("detected_language").GetString();


            return (translatedText, detectedLanguage);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Gemini API Error] {ex.Message}");
            return (text, "error");
        }
    }
}