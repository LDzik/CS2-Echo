using CommunityToolkit.Mvvm.Messaging;
using CS2_Echo.Infrastructure;
using CS2_Echo.Infrastructure.Services;
using CS2_Echo.UI.Services;
using CS2_Echo.UI.ViewModels;
using CS2_Echo.UI.Views;
using CS2_Echo.UI.Views.Pages;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Controls;
using CS2_Echo.Logic.Interfaces;


namespace CS2_Echo.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : FluentWindow
{

    private readonly ConfigurationService _configService;
    private readonly OverlayViewModel _overlayViewModel;
    private readonly IOverlayService _overlayService;
    private QuickTranslateWindow _quickTranslateWindow;

    private const int HOTKEY_QUICKTRANS_ID = 9001;
    private IntPtr _windowHandle;
    private string _quickTranslateHotkeyLetter;

    public MainWindow(MainViewModel viewModel,
        INavigationService navigationService,
        INavigationViewPageProvider pageService,
        ISnackbarService snackbarService,
        ConfigurationService configService,
        OverlayViewModel overlayViewModel,
        QuickTranslateViewModel quickTranslateViewModel,
        IOverlayService overlayService
        )
    {
        InitializeComponent();
        DataContext = viewModel;

        _configService = configService;
        _overlayViewModel = overlayViewModel;
        _overlayService = overlayService;

        Wpf.Ui.Appearance.ApplicationThemeManager.Apply(Wpf.Ui.Appearance.ApplicationTheme.Dark);

        RootNavigation.SetPageProviderService(pageService);
        navigationService.SetNavigationControl(RootNavigation);

        snackbarService.SetSnackbarPresenter(SnackbarPresenter);

        Loaded += (sender, args) =>
        {
            navigationService.Navigate(typeof(TranslationsPage));

            if (_configService.Current.AutoLaunchOverlay)
            {
                _overlayService.ShowOverlay(startLocked: true);
            }
        };

        _quickTranslateWindow = new QuickTranslateWindow(quickTranslateViewModel);
        _quickTranslateHotkeyLetter = configService.Current.QuickTranslateHotkey ?? "T";

        WeakReferenceMessenger.Default.Register<OverlaySettingsChangedMessage>(this, (recipient, message) =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ReloadHotkeys(_configService.Current.QuickTranslateHotkey ?? "T");
            });
        });
    }


    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        _windowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        System.Windows.Interop.HwndSource source = System.Windows.Interop.HwndSource.FromHwnd(_windowHandle);
        source?.AddHook(HwndHook);

        RegisterQuickTranslateHotkey();
    }

    public void ReloadHotkeys(string newLetter)
    {
        NativeMethods.UnregisterHotKey(_windowHandle, HOTKEY_QUICKTRANS_ID);
        _quickTranslateHotkeyLetter = newLetter ?? "T";
        RegisterQuickTranslateHotkey();
    }

    private void RegisterQuickTranslateHotkey()
    {
        if (_windowHandle == IntPtr.Zero) return;

        uint modifiers = NativeMethods.MOD_CTRL | NativeMethods.MOD_SHIFT;
        if (Enum.TryParse(_quickTranslateHotkeyLetter, out Key wpfKey))
        {
            uint vKey = (uint)KeyInterop.VirtualKeyFromKey(wpfKey);

            bool success = NativeMethods.RegisterHotKey(_windowHandle, HOTKEY_QUICKTRANS_ID, modifiers, vKey);

            //if (!success)
            //{
            //    System.Windows.MessageBox.Show(
            //        $"Failed to register the Quick Translate Hotkey (Ctrl+Alt+{_quickTranslateHotkeyLetter}). Another program is already using this shortcut.",
            //        "Hotkey Error",
            //        System.Windows.MessageBoxButton.OK,
            //        MessageBoxImage.Warning);
            //}
        }
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && wParam.ToInt32() == HOTKEY_QUICKTRANS_ID)
        {
            _quickTranslateWindow.Show();

            _quickTranslateWindow.Topmost = false;
            _quickTranslateWindow.Topmost = true;

            _quickTranslateWindow.Activate();
            _quickTranslateWindow.Focus();

            handled = true;
        }
        return IntPtr.Zero;
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.Unregister<OverlaySettingsChangedMessage>(this);
        NativeMethods.UnregisterHotKey(_windowHandle, HOTKEY_QUICKTRANS_ID);
        _quickTranslateWindow?.Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_configService.Current.MinimizeToTray)
        {
            e.Cancel = true;
            this.Hide();
        }
        else
        {
            AppNotifyIcon.Unregister();
            Application.Current.Shutdown();
        }

        base.OnClosing(e);
    }

    private void AppNotifyIcon_LeftClick(object sender, RoutedEventArgs e)
    {
        RestoreWindow();
    }

    private void TrayOpen_Click(object sender, RoutedEventArgs e)
    {
        RestoreWindow();
    }

    private void TrayExit_Click(object sender, RoutedEventArgs e)
    {

        AppNotifyIcon.Unregister();
        Application.Current.Shutdown();
    }

    private void TrayLaunchOverlay_Click(object sender, RoutedEventArgs e)
    {
        if (_overlayViewModel.LaunchOverlayCommand.CanExecute(null))
        {
            _overlayViewModel.LaunchOverlayCommand.Execute(null);
        }
    }

    private void TrayContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        bool isOpen = _overlayService.IsOverlayOpen;
        bool isLocked = _overlayService.IsOverlayLocked;

        TrayMenuHidden.IsEnabled = isOpen;
        TrayMenuUnlocked.IsEnabled = !isOpen || isLocked;
        TrayMenuLocked.IsEnabled = !isOpen || !isLocked;
    }

    private void TrayMenuHidden_Click(object sender, RoutedEventArgs e)
    {
        _overlayService.CloseOverlay();
    }

    private void TrayMenuUnlocked_Click(object sender, RoutedEventArgs e)
    {
        _overlayService.ShowOverlay(startLocked: false);
    }

    private void TrayMenuLocked_Click(object sender, RoutedEventArgs e)
    {
        _overlayService.ShowOverlay(startLocked: true);
    }


    private void RestoreWindow()
    {
        this.Show();
        this.WindowState = WindowState.Normal;
        this.Activate();
    }
}
