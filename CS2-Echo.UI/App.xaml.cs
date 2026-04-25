using CS2_Echo.Infrastructure;
using CS2_Echo.Infrastructure.Services;
using CS2_Echo.Infrastructure.TranslationProviders;
using CS2_Echo.Logic.Interfaces;
using CS2_Echo.UI.Services;
using CS2_Echo.UI.ViewModels;
using CS2_Echo.UI.Views.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Configuration;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Controls;
using Wpf.Ui.DependencyInjection;
using Velopack;

namespace CS2_Echo.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{

    private readonly IHost _host;

    public App()
    {
        VelopackApp.Build().Run();

        _host = Host.CreateDefaultBuilder().
            ConfigureServices((context, services) =>
            {
                

                services.AddSingleton<ConfigurationService>();

                services.AddSingleton<DatabaseService>();
                services.AddSingleton<FilterService>();
                services.AddSingleton<ChatParser>();
                services.AddSingleton<LogMonitorService>();
                services.AddHostedService(sp => sp.GetRequiredService<LogMonitorService>());

                services.AddHttpClient<GoogleTranslationProvider>(client =>
                {
                    //client.DefaultRequestHeaders.UserAgent.ParseAdd("CS2-Echo/1.0");
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Macintosh; Intel Mac OS X 11_0_0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/88.0.4324.96 Safari/537.36");
                });
                services.AddSingleton<DeepLTranslationProvider>();
                services.AddSingleton<GeminiTranslationProvider>();

                services.AddSingleton<ITranslationProvider>(sp => sp.GetRequiredService<GoogleTranslationProvider>());
                services.AddSingleton<ITranslationProvider>(sp => sp.GetRequiredService<DeepLTranslationProvider>());
                services.AddSingleton<ITranslationProvider>(sp => sp.GetRequiredService<GeminiTranslationProvider>());

                services.AddSingleton<TranslationService>();

                services.AddSingleton<IUpdateService, UpdateService>();

                services.AddNavigationViewPageProvider();
                services.AddSingleton<INavigationService, NavigationService>();


                services.AddSingleton<ISnackbarService, SnackbarService>();
                services.AddSingleton<IOverlayService, OverlayService>();

                services.AddSingleton<ChatFeedService>();

                services.AddSingleton<Views.Pages.TranslationsPage>();
                services.AddSingleton<TranslationsViewModel>();

                services.AddSingleton<Views.Pages.InfoPage>();
                services.AddSingleton<InfoViewModel>();

                services.AddSingleton<Views.Pages.SettingsPage>();
                services.AddSingleton<SettingsViewModel>();

                services.AddSingleton<Views.Pages.StatsPage>();
                services.AddSingleton<StatsViewModel>();

                services.AddSingleton<Views.Pages.OverlayPage>();
                services.AddSingleton<ViewModels.OverlayViewModel>();

                services.AddSingleton<QuickTranslateViewModel>();


                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;


        base.OnStartup(e);

        await _host.StartAsync();

        var databaseService = _host.Services.GetRequiredService<DatabaseService>();
        await databaseService.InitializeAsync();

        var filterService = _host.Services.GetRequiredService<FilterService>();
        await filterService.InitializeAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
        
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        HandleException(e.Exception, "UI Thread Exception");
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            HandleException(ex, "AppDomain Unhandled Exception");
        }
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        HandleException(e.Exception, "Unobserved Task Exception");
        e.SetObserved();
    }

    private void HandleException(Exception ex, string source)
    {
        try
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appDir = Path.Combine(appData, "CS2_Echo");
            Directory.CreateDirectory(appDir);
            string logFile = Path.Combine(appDir, "crash.log");

            string errorMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}]\r\n{ex}\r\n--------------------------------------------------\r\n";
            File.AppendAllText(logFile, errorMessage);

            System.Windows.MessageBox.Show(
                $"A critical error occurred ({source}).\n\nThe application will attempt to keep running but may be unstable.\n\nError details have been saved to:\n{logFile}",
                "CS2 Echo - Fatal Error",
                System.Windows.MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch
        {
            
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            try
            {
                await _host.StopAsync(TimeSpan.FromSeconds(10));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Host StopAsync exception: {ex.Message}");
            }
            finally
            {

                if (_host.Services.GetService<FilterService>() is { } filterService)
                {
                    filterService.Dispose();
                }

                _host.Dispose();
            }
        }
        base.OnExit(e);
    }
}

