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
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using Velopack;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Controls;
using Wpf.Ui.DependencyInjection;

namespace CS2_Echo.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{

    private readonly IHost _host;

    public static bool WasLaunchedViaSteam { get; private set; }

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


                var cookieContainer = new CookieContainer();

                services.AddHttpClient<GoogleTranslationProvider>(client =>
                {
                    client.DefaultRequestVersion = HttpVersion.Version20;
                    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;

                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36");
                    client.DefaultRequestHeaders.Add("Accept", "*/*");
                    client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9,pl;q=0.8");
                    client.DefaultRequestHeaders.Add("Sec-Ch-Ua", "\"Chromium\";v=\"128\", \"Not;A=Brand\";v=\"24\", \"Google Chrome\";v=\"128\"");
                    client.DefaultRequestHeaders.Add("Sec-Ch-Ua-Mobile", "?0");
                    client.DefaultRequestHeaders.Add("Sec-Ch-Ua-Platform", "\"Windows\"");
                    client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty");
                    client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
                    client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
                    client.DefaultRequestHeaders.Add("Referer", "https://translate.google.com/");
                })
                .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                {
                    UseCookies = true,
                    CookieContainer = cookieContainer,
                    AutomaticDecompression = DecompressionMethods.All,
                    EnableMultipleHttp2Connections = true
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

        // auto launch cs2
        if (e.Args.Length > 0)
        {
            int cs2Index = -1;
            for (int i = 0; i < e.Args.Length; i++)
            {
                if (e.Args[i].EndsWith("cs2.exe", StringComparison.OrdinalIgnoreCase))
                {
                    cs2Index = i;
                    break;
                }
            }

            if (cs2Index != -1)
            {
                WasLaunchedViaSteam = true;
                string cs2Path = e.Args[cs2Index];

                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = cs2Path,
                        UseShellExecute = true
                    };

                    for (int i = cs2Index + 1; i < e.Args.Length; i++)
                    {
                        psi.ArgumentList.Add(e.Args[i]);
                    }

                    System.Diagnostics.Process.Start(psi);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to proxy launch CS2: {ex.Message}");
                }
            }
        }

        base.OnStartup(e);

        await _host.StartAsync();

        var databaseService = _host.Services.GetRequiredService<DatabaseService>();
        await databaseService.InitializeAsync();

        var filterService = _host.Services.GetRequiredService<FilterService>();
        await filterService.InitializeAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        if (WasLaunchedViaSteam)
        {
            mainWindow.WindowState = WindowState.Minimized;
            mainWindow.Show();
        }
        else
        {
            mainWindow.Show();
        }

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

