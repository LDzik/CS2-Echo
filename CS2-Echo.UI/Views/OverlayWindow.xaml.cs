using CS2_Echo.Infrastructure;
using CS2_Echo.Infrastructure.Services;
using CS2_Echo.UI.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace CS2_Echo.UI.Views;

/// <summary>
/// Logika interakcji dla klasy OverlayWindow.xaml
/// </summary>
public partial class OverlayWindow : Window
{
    private bool _isLocked = false;
    private IntPtr _windowHandle;
    private const int HOTKEY_ID = 9000;

    private readonly ConfigurationService _configService;
    private double _lockedOpacity;
    private string _hotkeyLetter;

    private readonly OverlayViewModel _viewModel;

    private bool _startLocked;

    public bool IsLocked => _isLocked;

    public void SetLockState(bool lockOverlay)
    {
        if (_isLocked != lockOverlay)
        {
            ToggleLockState();
        }
    }

    public OverlayWindow(OverlayViewModel viewModel, ConfigurationService configService, bool startLocked = false)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        _configService = configService;

        _startLocked = startLocked;

        _viewModel.ChatFeed.CollectionChanged += ChatFeed_CollectionChanged;

        WeakReferenceMessenger.Default.Register<OverlaySettingsChangedMessage>(this, (r, m) =>
        {
            ApplyNewSettings();
        });

        LoadSettingsFromConfig();

    }

    private void LoadSettingsFromConfig()
    {
        _lockedOpacity = _configService.Current.OverlayLockedOpacity;
        _hotkeyLetter = _configService.Current.OverlayHotkey ?? "O";

        Dispatcher.Invoke(() =>
        {
            ChatItemsControl.FontSize = _configService.Current.OverlayFontSize;
            ShortcutHintText.Text = $"Press Ctrl+Shift+{_hotkeyLetter} to Lock";
        });
    }

    private void ApplyNewSettings()
    {
        NativeMethods.UnregisterHotKey(_windowHandle, HOTKEY_ID);

        LoadSettingsFromConfig();

        RegisterGlobalHotkey();

        Dispatcher.Invoke(() =>
        {
            if (_isLocked) SetLockedState();
            else SetUnlockedState();
        });
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _windowHandle = new WindowInteropHelper(this).Handle;
        HwndSource source = HwndSource.FromHwnd(_windowHandle);
        source.AddHook(HwndHook);

        int extendedStyle = NativeMethods.GetWindowLong(_windowHandle, NativeMethods.GWL_EXSTYLE);
        NativeMethods.SetWindowLong(_windowHandle, NativeMethods.GWL_EXSTYLE, extendedStyle | NativeMethods.WS_EX_TOOLWINDOW);

        RegisterGlobalHotkey();

        if (_startLocked)
        {
            _isLocked = true;
            SetLockedState();
        }
        else
        {
            _isLocked = false;
            SetUnlockedState();
        }

        if (!double.IsNaN(_configService.Current.OverlayX) && !double.IsNaN(_configService.Current.OverlayY))
        {
            this.Left = _configService.Current.OverlayX;
            this.Top = _configService.Current.OverlayY;
        }

        ChatScrollViewer.ScrollToEnd();
    }

    private void RegisterGlobalHotkey()
    {
        uint modifiers = NativeMethods.MOD_CTRL | NativeMethods.MOD_SHIFT;
        if (Enum.TryParse(_hotkeyLetter, out Key wpfKey))
        {
            uint vKey = (uint)KeyInterop.VirtualKeyFromKey(wpfKey);
            NativeMethods.RegisterHotKey(_windowHandle, HOTKEY_ID, modifiers, vKey);
        }
    }

    private void ChatFeed_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            ChatScrollViewer.Dispatcher.InvokeAsync(() =>
            {
                ChatScrollViewer.ScrollToEnd();
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            ToggleLockState();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void ToggleLockState()
    {
        _isLocked = !_isLocked;
        if (_isLocked) SetLockedState();
        else SetUnlockedState();
    }

    private void SetLockedState()
    {
        int extendedStyle = NativeMethods.GetWindowLong(_windowHandle, NativeMethods.GWL_EXSTYLE);
        NativeMethods.SetWindowLong(_windowHandle, NativeMethods.GWL_EXSTYLE, extendedStyle | NativeMethods.WS_EX_TRANSPARENT);

        HeaderBar.Visibility = Visibility.Collapsed;

        byte alpha = (byte)(_lockedOpacity * 255);
        OverlayContainer.Background = new SolidColorBrush(Color.FromArgb(alpha, 0, 0, 0));

        SavePositionToConfig();
    }

    private void SetUnlockedState()
    {
        int extendedStyle = NativeMethods.GetWindowLong(_windowHandle, NativeMethods.GWL_EXSTYLE);
        NativeMethods.SetWindowLong(_windowHandle, NativeMethods.GWL_EXSTYLE, extendedStyle & ~NativeMethods.WS_EX_TRANSPARENT);

        HeaderBar.Visibility = Visibility.Visible;
        OverlayContainer.Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0));
    }

    private void HeaderBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            this.DragMove();
        }
    }

    private void SavePositionToConfig()
    {
        if (double.IsNaN(this.Left) || double.IsNaN(this.Top)) return;

        if (_configService.Current.OverlayX != this.Left || _configService.Current.OverlayY != this.Top)
        {
            _configService.Update(config => config with
            {
                OverlayX = this.Left,
                OverlayY = this.Top
            });
        }
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        SavePositionToConfig();

        NativeMethods.UnregisterHotKey(_windowHandle, HOTKEY_ID);
        _viewModel.ChatFeed.CollectionChanged -= ChatFeed_CollectionChanged;

        WeakReferenceMessenger.Default.Unregister<OverlaySettingsChangedMessage>(this);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}

