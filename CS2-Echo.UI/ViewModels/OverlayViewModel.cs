using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CS2_Echo.Infrastructure.Services;
using CS2_Echo.Logic.Interfaces;
using CS2_Echo.UI.Models;
using CS2_Echo.UI.Services;
using CS2_Echo.UI.Views;
using System.Collections.ObjectModel;
using System.Windows;
using System.Linq;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace CS2_Echo.UI.ViewModels;


public record OverlaySettingsChangedMessage();

public partial class OverlayViewModel : ObservableObject
{
    private readonly ConfigurationService _configService;
    private readonly ChatFeedService _chatFeedService;
    private readonly ISnackbarService _snackbarService;
    private readonly IOverlayService _overlayService;

    public ObservableCollection<TranslationCard> ChatFeed => _chatFeedService.ChatFeed;

    [ObservableProperty] public partial string HotkeyLetter { get; set; } = "O";
    [ObservableProperty] public partial string QuickTranslateHotkeyLetter { get; set; } = "T";
    [ObservableProperty] public partial double LockedOpacity { get; set; }
    [ObservableProperty] public partial int FontSize { get; set; }

    public OverlayViewModel(
            ConfigurationService configService,
            ChatFeedService chatFeedService,
            ISnackbarService snackbarService,
            IOverlayService overlayService
        )
    {
        _configService = configService;
        _chatFeedService = chatFeedService;
        _snackbarService = snackbarService;
        _overlayService = overlayService;

        LockedOpacity = _configService.Current.OverlayLockedOpacity;
        FontSize = _configService.Current.OverlayFontSize;
        HotkeyLetter = _configService.Current.OverlayHotkey ?? "O";
        QuickTranslateHotkeyLetter = _configService.Current.QuickTranslateHotkey ?? "T";
    }

    [RelayCommand]
    private void LaunchOverlay()
    {
        _configService.Update(config => config with
        {
            OverlayLockedOpacity = LockedOpacity,
            OverlayFontSize = FontSize,
        });

        _overlayService.ShowOverlay();
    }

    [RelayCommand]
    private void SaveSettings()
    {
        _configService.Update(config => config with
        {
            OverlayLockedOpacity = LockedOpacity,
            OverlayFontSize = FontSize,
            OverlayHotkey = HotkeyLetter.ToUpper(),
            QuickTranslateHotkey = QuickTranslateHotkeyLetter.ToUpper()
        });

        WeakReferenceMessenger.Default.Send(new OverlaySettingsChangedMessage());

        _snackbarService.Show(
                "Settings Saved",
                $"Overlay setting saved.",
                ControlAppearance.Success,
                new SymbolIcon(SymbolRegular.Checkmark24),
                new System.TimeSpan(0, 0, 3));
    }

}



