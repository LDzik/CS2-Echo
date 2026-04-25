using System;
using System.Linq;
using System.Windows;
using CS2_Echo.Infrastructure.Services;
using CS2_Echo.Logic.Interfaces;
using CS2_Echo.UI.ViewModels;
using CS2_Echo.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace CS2_Echo.UI.Services;

public class OverlayService : IOverlayService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConfigurationService _configService;

    private OverlayWindow? _activeOverlay;

    public OverlayService(IServiceProvider serviceProvider, ConfigurationService configService)
    {
        _serviceProvider = serviceProvider;
        _configService = configService;
    }

    public bool IsOverlayOpen => _activeOverlay != null;
    public bool IsOverlayLocked => _activeOverlay != null && _activeOverlay.IsLocked;

    public void ShowOverlay(bool startLocked = false)
    {
        if (_activeOverlay != null)
        {
            _activeOverlay.SetLockState(startLocked);
            _activeOverlay.Activate();
            return;
        }

        var viewModel = _serviceProvider.GetRequiredService<OverlayViewModel>();

        _activeOverlay = new OverlayWindow(viewModel, _configService, startLocked);

        _activeOverlay.Closed += (s, e) => _activeOverlay = null;

        _activeOverlay.Show();
    }

    public void CloseOverlay()
    {
        _activeOverlay?.Close();
    }

    public void SetLockState(bool isLocked)
    {
        _activeOverlay?.SetLockState(isLocked);
    }
}
