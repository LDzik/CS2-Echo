using CS2_Echo.Logic.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using CS2_Echo.Logic.Constants;

namespace CS2_Echo.Infrastructure.Services;

public class TranslationService
{
    private readonly DatabaseService _dbService;
    private readonly ConfigurationService _configService;
    private readonly IEnumerable<ITranslationProvider> _providers;

    public TranslationService(DatabaseService dbService, 
        ConfigurationService configService,
        IEnumerable<ITranslationProvider> providers)
    {
        _dbService = dbService;
        _configService = configService;
        _providers = providers;
    }

    private ITranslationProvider ActiveProvider
    {
        get
        {
            var selectedEngine = _configService.Current.SelectedEngine;

            return _providers.FirstOrDefault(p => p.Name == selectedEngine)
                ?? _providers.FirstOrDefault(p => p.Name == EngineNames.Google)
                ?? new NullTranslationProvider(); 
        }
    }

    public string CurrentProviderName => ActiveProvider.Name;

    public async Task<(string TranslatedText, string SourceLang, string ProviderName)> TranslateAsync(string text, string targetLang = "en")
    {
        if (string.IsNullOrWhiteSpace(text))
            return (text, "unknown", "none");

        var cached = await _dbService.GetCachedTranslationAsync(text, targetLang);
        if (!string.IsNullOrEmpty(cached.TranslatedText) && !string.IsNullOrEmpty(cached.SourceLang))
        {
            return (cached.TranslatedText, cached.SourceLang, "Cached");
        }

        var provider = ActiveProvider;
        var result = await provider.TranslateAsync(text, targetLang);

        if (result.DetectedSourceLang != "error" && result.DetectedSourceLang != "unknown")
        {
            await _dbService.SaveTranslationAsync(text, result.TranslatedText, result.DetectedSourceLang, targetLang);
        }

        return (result.TranslatedText, result.DetectedSourceLang, provider.Name);
    }

    private class NullTranslationProvider : ITranslationProvider
    {
        public string Name => "Error (No Providers Configured)";

        public Task<(string TranslatedText, string DetectedSourceLang)> TranslateAsync(string text, string targetLang)
        {
            return Task.FromResult((text, "error"));
        }
    }
}