using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CS2_Echo.Domain;
using CS2_Echo.Infrastructure.Services;
using CS2_Echo.Infrastructure.TranslationProviders;
using CS2_Echo.Infrastructure.Utilities;
using CS2_Echo.Logic.Interfaces;
using CS2_Echo.Logic.Constants;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace CS2_Echo.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{

    private readonly TranslationService _translationService;
    private readonly DatabaseService _databaseService;
    private readonly LogMonitorService _logMonitorService;
    private readonly ISnackbarService _snackbarService;
    private readonly ConfigurationService _configService;
    private readonly FilterService _filterService;

    [ObservableProperty] public partial string TargetLanguageCode { get; set; }
    [ObservableProperty] public partial string LogFilePath { get; set; }
    [ObservableProperty] public partial int MinMessageSize { get; set; }
    [ObservableProperty] public partial bool EnablePlayerStats { get; set; }
    [ObservableProperty] public partial string SelectedEngine { get; set; }
    [ObservableProperty] public partial bool MinimizeToTray { get; set; }
    public ObservableCollection<string> AvailableEngines { get; }

    private string? _tempDeepLKey;
    private string? _tempGeminiKey;

    public string DeepLApiKey
    {
        get => _tempDeepLKey ?? _configService.GetDecryptedDeepLKey();
        set => _tempDeepLKey = value;
    }

    public string GeminiApiKey
    {
        get => _tempGeminiKey ?? _configService.GetDecryptedGeminiKey();
        set => _tempGeminiKey = value;
    }

    public SettingsViewModel(TranslationService translationService,
        DatabaseService databaseService,
        LogMonitorService logMonitorService,
        ISnackbarService snackbarService,
        ConfigurationService configService,
        FilterService filterService,
        IEnumerable<ITranslationProvider> providers
        )
    {
        _translationService = translationService;
        _databaseService = databaseService;
        _logMonitorService = logMonitorService;
        _snackbarService = snackbarService;
        _configService = configService;
        _filterService = filterService;


        AvailableEngines = new ObservableCollection<string>(providers.Select(p => p.Name));

        LogFilePath = _configService.Current.LogFilePath;
        SelectedEngine = _configService.Current.SelectedEngine;
        MinMessageSize = _configService.Current.MinMessageSize;
        EnablePlayerStats = _configService.Current.EnablePlayerStats;
        TargetLanguageCode = _configService.Current.TargetLanguage;
        MinimizeToTray = _configService.Current.MinimizeToTray;
    }

    [RelayCommand]
    private void BrowseFolder()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            InitialDirectory = _configService.Current.LogFilePath,
            Title = "Select your 'Counter-Strike Global Offensive' Folder"
        };

        if (dialog.ShowDialog() == true)
        {
            LogFilePath = dialog.FolderName;
        }
    }

    [RelayCommand]
    private void AutoDetectFolder()
    {
        string? foundPath = SteamLocator.FindCS2InstallPath();

        if (!string.IsNullOrWhiteSpace(foundPath))
        {
            LogFilePath = foundPath;
            _snackbarService.Show(
                "CS2 Located",
                "Successfully found your Counter-Strike 2 installation directory.",
                ControlAppearance.Success,
                new SymbolIcon(SymbolRegular.Checkmark24),
                new TimeSpan(0, 0, 3));
        }
        else
        {
            _snackbarService.Show(
                "Auto-Detect Failed",
                "Could not locate CS2 in your Steam library. Please browse manually.",
                ControlAppearance.Danger,
                new SymbolIcon(SymbolRegular.Dismiss24),
                new TimeSpan(0, 0, 4));
        }
    }

    [RelayCommand]
    private void ApplySettings()
    {
        

        if (SelectedEngine == EngineNames.DeepL && string.IsNullOrWhiteSpace(DeepLApiKey))
        {
            _snackbarService.Show("Missing API Key", "DeepL requires an API key. Defaulting to Google.", ControlAppearance.Caution, new SymbolIcon(SymbolRegular.Warning24), new System.TimeSpan(0, 0, 4));
            SelectedEngine = EngineNames.Google;
        }
        else if (SelectedEngine == EngineNames.Gemini && string.IsNullOrWhiteSpace(GeminiApiKey))
        {
            _snackbarService.Show("Missing API Key", "Gemini requires an API key. Defaulting to Google.", ControlAppearance.Caution, new SymbolIcon(SymbolRegular.Warning24), new System.TimeSpan(0, 0, 4));
            SelectedEngine = EngineNames.Google;
        }
        else
        {
            _snackbarService.Show(
                "Settings Saved",
                "Your configuration has been updated successfully.",
                ControlAppearance.Success,
                new SymbolIcon(SymbolRegular.Checkmark24),
                new System.TimeSpan(0, 0, 3));
        }

        if (string.IsNullOrWhiteSpace(TargetLanguageCode)) TargetLanguageCode = "en";

        _configService.Update(config =>
        {
            var updated = config with
            {
                LogFilePath = LogFilePath,
                SelectedEngine = SelectedEngine,
                MinMessageSize = MinMessageSize,
                EnablePlayerStats = EnablePlayerStats,
                TargetLanguage = TargetLanguageCode,
                MinimizeToTray = MinimizeToTray
            };


            if (_tempDeepLKey != null)
            {
                updated = updated with { DeepLApiKey = _configService.EncryptSecret(_tempDeepLKey) };
            }

            if (_tempGeminiKey != null)
            {
                updated = updated with { GeminiApiKey = _configService.EncryptSecret(_tempGeminiKey) };
            }

            return updated;

        });

        _tempDeepLKey = null;
        _tempGeminiKey = null;
    }
}

