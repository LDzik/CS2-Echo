using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CS2_Echo.Infrastructure.Services;
using CS2_Echo.UI.Models;
using CS2_Echo.UI.Services;
using DeepL.Model;
using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace CS2_Echo.UI.ViewModels;



public partial class TranslationsViewModel : ObservableObject
{
    private readonly ChatFeedService _chatFeedService;
    private readonly FilterService _filterService;
    private readonly TranslationService _translationService;
    private readonly ISnackbarService _snackbarService;
    private readonly ConfigurationService _configService;

    public ObservableCollection<TranslationCard> ChatFeed => _chatFeedService.ChatFeed;

    [ObservableProperty] public partial string ManualInputText { get; set; }
    [ObservableProperty] public partial string ManualInputTargetLang { get; set; }
    [ObservableProperty] public partial string ManualOutputText { get; set; }
    [ObservableProperty] public partial bool IsTranslating { get; set; }
    [ObservableProperty] public partial bool ShowPlayerStats { get; set; }
    [ObservableProperty] public partial bool IsCondebugMissing { get; set; }

    public TranslationsViewModel(
        ChatFeedService chatFeedService,
        FilterService filterService,
        TranslationService translationService,
        ISnackbarService snackbarService,
        ConfigurationService configService
        )
    {
        _chatFeedService = chatFeedService;
        _filterService = filterService;
        _translationService = translationService;
        _snackbarService = snackbarService;
        _configService = configService;

        ShowPlayerStats = _configService.Current.EnablePlayerStats;
        ManualInputTargetLang = _configService.Current.LastQuickTranslateLang ?? "en";

        _configService.OnConfigurationChanged += () =>
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                ShowPlayerStats = _configService.Current.EnablePlayerStats;

                var latestLang = _configService.Current.LastQuickTranslateLang ?? "en";
                if (ManualInputTargetLang != latestLang)
                {
                    ManualInputTargetLang = latestLang;
                }
            });
        };


        string currentOptions = CS2_Echo.Infrastructure.Utilities.SteamLocator.GetCS2LaunchOptions();
        IsCondebugMissing = !currentOptions.Contains("-condebug", StringComparison.OrdinalIgnoreCase);
    }

    [RelayCommand]
    private async Task TranslateManualAsync()
    {
        if (string.IsNullOrWhiteSpace(ManualInputText)) return;


        if (string.IsNullOrWhiteSpace(ManualInputTargetLang)) {
            _snackbarService.Show("Error", "Please specify a target language code (e.g., 'en' for English).", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), TimeSpan.FromSeconds(3));
            return;
        }

        if (!_configService.Current.LastQuickTranslateLang.Equals(ManualInputTargetLang, StringComparison.OrdinalIgnoreCase))
        {
            _configService.Update(config => config with 
            { 
                LastQuickTranslateLang = ManualInputTargetLang.ToLower()
            });
        }

        IsTranslating = true;
        ManualOutputText = string.Empty;


        try
        {
            var result = await _translationService.TranslateAsync(ManualInputText, ManualInputTargetLang);
            ManualOutputText = result.TranslatedText;
        } 
        finally
        {
            IsTranslating = false;
            ManualInputText = string.Empty;
        }
    }

    [RelayCommand]
    private void CopyToClipboard(string textToCopy)
    {
        if (!string.IsNullOrEmpty(textToCopy))
        {
            Clipboard.SetText(textToCopy);
            _snackbarService.Show("Copied", "Text copied to clipboard.", ControlAppearance.Info, new SymbolIcon(SymbolRegular.Copy24), TimeSpan.FromSeconds(2));
        }
    }

    [RelayCommand]
    private async Task IgnorePlayerAsync(string playerName)
    {
        if (string.IsNullOrEmpty(playerName)) return;

        try
        {
            await _filterService.AddIgnoredUserAsync(playerName);

            foreach (var card in ChatFeed.Where(c => c.PlayerName == playerName))
            {
                card.CanIgnorePlayer = false;
            }

            _snackbarService.Show("Player Ignored", $"Messages from {playerName} will no longer be translated.", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.PersonDelete24), TimeSpan.FromSeconds(3));
        }
        catch (Exception)
        {
            _snackbarService.Show("Error", $"Failed to ignore {playerName}. Database write failed.", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), TimeSpan.FromSeconds(3));
        }
    }

    [RelayCommand]
    private async Task IgnoreMultiplePlayersAsync(IList selectedItems)
    {
        if (selectedItems == null || selectedItems.Count == 0) return;

        var cards = selectedItems.Cast<TranslationCard>().ToList();
        int count = 0;

        try
        {
            foreach (var card in cards)
            {
                await _filterService.AddIgnoredUserAsync(card.PlayerName);

                foreach (var feedCard in ChatFeed.Where(c => c.PlayerName == card.PlayerName))
                {
                    feedCard.CanIgnorePlayer = false;
                }

                count++;
            }

            _snackbarService.Show("Players Ignored", $"{count} players added to the ignore list.", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.PersonDelete24), TimeSpan.FromSeconds(3));
        }
        catch (Exception)
        {
            _snackbarService.Show("Error", $"Operation interrupted. Only {count} players were ignored.", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), TimeSpan.FromSeconds(3));
        }
    }


    public void RefreshUIState()
    {

        var ignoredPlayers = _filterService.GetIgnoredUsers();

        foreach (var card in ChatFeed)
        {
            card.CanIgnorePlayer = !ignoredPlayers.Contains(card.PlayerName);
        }
    }

}

