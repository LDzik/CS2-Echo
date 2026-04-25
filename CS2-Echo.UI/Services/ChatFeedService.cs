using CS2_Echo.Infrastructure;
using CS2_Echo.Infrastructure.Services;
using CS2_Echo.UI.Models;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
namespace CS2_Echo.UI.Services;

public class ChatFeedService : IDisposable
{
    private readonly LogMonitorService _logMonitor;
    private readonly ChatParser _chatParser;
    private readonly FilterService _filterService;
    private readonly TranslationService _translationService;
    private readonly DatabaseService _databaseService;
    private readonly ConfigurationService _configService;
    private readonly IHostApplicationLifetime _appLifetime;

    private bool _disposed;

    public ObservableCollection<TranslationCard> ChatFeed { get; } = new();

    public ChatFeedService(
        LogMonitorService logMonitor,
        ChatParser chatParser,
        FilterService filterService,
        TranslationService translationService,
        DatabaseService databaseService,
        ConfigurationService configService,
        IHostApplicationLifetime appLifetime)
    {
        _logMonitor = logMonitor;
        _chatParser = chatParser;
        _filterService = filterService;
        _translationService = translationService;
        _databaseService = databaseService;
        _configService = configService;
        _appLifetime = appLifetime;

        _logMonitor.OnNewLineRead += ProcessLiveLogLine;

        _appLifetime.ApplicationStopping.Register(() =>
        {
            _logMonitor.OnNewLineRead -= ProcessLiveLogLine;
        });
    }

    private async void ProcessLiveLogLine(string rawLine)
    {
        try
        {
            var chatMsg = _chatParser.ParseLine(rawLine);
            if (chatMsg == null) return;

            if (!_filterService.ShouldTranslate(chatMsg, out string detectedLang, out string confidenceLog)) return;

            var result = await _translationService.TranslateAsync(chatMsg.Message, _configService.Current.TargetLanguage);

            string finalLang = result.SourceLang switch { "cached" => detectedLang, null => detectedLang, _ => result.SourceLang };

            await _databaseService.LogPlayerLanguageAsync(chatMsg.PlayerName, finalLang);
            string currentPrimaryLang = await _databaseService.GetTopLanguageForPlayerAsync(chatMsg.PlayerName);

            if (Application.Current == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (ChatFeed.Count >= 100) ChatFeed.RemoveAt(0);

                bool isPlayerIgnored = _filterService.GetIgnoredUsers().Contains(chatMsg.PlayerName);

                ChatFeed.Add(new TranslationCard
                {
                    Channel = chatMsg.Channel,
                    PlayerName = chatMsg.PlayerName,
                    OriginalText = chatMsg.Message,
                    TranslatedText = result.TranslatedText,
                    SourceLang = finalLang,
                    PrimaryLang = currentPrimaryLang,
                    ConfidenceLog = confidenceLog,
                    CanIgnorePlayer = !isPlayerIgnored,
                });
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ChatFeedService] Error processing live log line: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _logMonitor.OnNewLineRead -= ProcessLiveLogLine;
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}

