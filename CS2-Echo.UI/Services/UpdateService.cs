using CS2_Echo.Logic.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using Velopack;
using Velopack.Sources;

namespace CS2_Echo.UI.Services;

public class UpdateService : IUpdateService
{

    private UpdateInfo? _pendingUpdate;

    private GithubSource GetUpdateSource()
    {
        return new GithubSource("https://github.com/LDzik/CS2-Echo", accessToken: null, prerelease: false);
    }
    public async Task<bool> CheckForUpdatesAsync()
    {
        try
        {
            var mgr = new UpdateManager(GetUpdateSource());
            if (!mgr.IsInstalled) return false;

            _pendingUpdate = await mgr.CheckForUpdatesAsync();
            return _pendingUpdate != null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Update check failed: {ex.Message}");
            return false;
        }
    }

    public async Task DownloadAndApplyUpdateAsync()
    {
        if (_pendingUpdate == null) return;

        var mgr = new UpdateManager(GetUpdateSource());
        await mgr.DownloadUpdatesAsync(_pendingUpdate);
        mgr.ApplyUpdatesAndRestart(_pendingUpdate);
    }
}

