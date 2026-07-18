using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CS2_Echo.Logic.Interfaces;
using CS2_Echo.UI.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace CS2_Echo.UI.ViewModels;

public partial class InfoViewModel : ObservableObject
{

    [ObservableProperty] public partial string ReleaseNotes { get; set; } = "Checking for notes...";

    private const string GitHubRepoUrl = "https://github.com/LDzik/CS2-Echo";
    private const string BugReportUrl = "https://github.com/LDzik/CS2-Echo/issues";

    private readonly IUpdateService _updateService;
    private readonly ISnackbarService _snackbarService;

    [ObservableProperty] public partial bool IsUpdateAvailable { get; set; }

    [RelayCommand]
    private void OpenGitHub()
    {
        OpenUrl(GitHubRepoUrl);
    }

    [RelayCommand]
    private void ReportBug()
    {
        OpenUrl(BugReportUrl);
    }

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            // blocked
        }
    }

    public InfoViewModel(IUpdateService updateService, ISnackbarService snackbarService)
    {
        _updateService = updateService;
        _snackbarService = snackbarService;

#if DEBUG
        IsUpdateAvailable = true;
        ReleaseNotes = @"## What's New in v1.3.0
### New Features
* **Steam Auto-Start Integration:** Tested working!
* **Auto-Detect Folder:** Also working!
* [Link test (Click me)](https://github.com/LDzik/CS2-Echo)
";
#endif
    }

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        IsUpdateAvailable = await _updateService.CheckForUpdatesAsync();

        if (IsUpdateAvailable)
        {
            ReleaseNotes = _updateService.LatestReleaseNotes;

            _snackbarService.Show("Update Found",
                "A new version of CS2 Echo is available!",
                ControlAppearance.Success,
                new SymbolIcon(SymbolRegular.ArrowDownload24),
                new System.TimeSpan(0, 0, 4));
        }
        else
        {
            _snackbarService.Show("Up to date",
                "You are running the latest version.",
                ControlAppearance.Info,
                new SymbolIcon(SymbolRegular.CheckmarkCircle24),
                new System.TimeSpan(0, 0, 3));
        }
    }

    [RelayCommand]
    private async Task ApplyUpdateAsync()
    {
        _snackbarService.Show("Updating...",
            "Downloading and applying update. The app will restart automatically.",
            ControlAppearance.Info,
            new SymbolIcon(SymbolRegular.ArrowSync24),
            new System.TimeSpan(0, 0, 5));
        await _updateService.DownloadAndApplyUpdateAsync();
    }
}

