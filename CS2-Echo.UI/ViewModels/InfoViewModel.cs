using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CS2_Echo.UI.ViewModels;

public partial class InfoViewModel : ObservableObject
{
    private const string GitHubRepoUrl = "https://github.com/LDzik/CS2-Echo";
    private const string BugReportUrl = "https://github.com/LDzik/CS2-Echo/issues";

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
}

