using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CS2_Echo.Infrastructure.Services;

public class LogMonitorService : BackgroundService
{
    private readonly ConfigurationService _configService;

    private string CurrentLogPath =>
        Path.Combine(_configService.Current.LogFilePath ?? "", "game", "csgo", "console.log");

    public event Action<string> OnNewLineRead;

    public LogMonitorService(ConfigurationService configService)
    {
        _configService = configService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();


        long lastPosition = -1; // do we want to load everything at start? no? -1 yes? 0
        string activePath = CurrentLogPath;
        int consecutiveErrors = 0;

        while (!stoppingToken.IsCancellationRequested)
        {

            try
            {
                string latestPathFromConfig = CurrentLogPath;

                if (activePath != latestPathFromConfig)
                {
                    activePath = latestPathFromConfig;
                    lastPosition = -1;
                    Console.WriteLine($"[LogMonitor] Path changed to: {activePath}");
                }

                if (string.IsNullOrWhiteSpace(activePath) || !File.Exists(activePath))
                {
                    await Task.Delay(2000, stoppingToken);
                    continue;
                }

                using var fileStream = new FileStream(activePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                if (lastPosition == -1)
                {
                    lastPosition = fileStream.Length;
                }

                if (fileStream.Length < lastPosition)
                {
                    lastPosition = 0;
                }

                if (fileStream.Length > lastPosition)
                {
                    fileStream.Position = lastPosition;
                    using var reader = new StreamReader(fileStream, Encoding.UTF8);

                    string line;
                    while ((line = await reader.ReadLineAsync(stoppingToken)) != null)
                    {
                        OnNewLineRead?.Invoke(line);
                    }

                    lastPosition = fileStream.Position;
                }

                consecutiveErrors = 0;
                await Task.Delay(250, stoppingToken);


            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (IOException ex)
            {
                consecutiveErrors++;
                await HandleErrorBackoffAsync(ex, consecutiveErrors, stoppingToken);
            
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LogMonitorService] Error reading log file: {ex.Message}");
                consecutiveErrors++;
                await HandleErrorBackoffAsync(ex, consecutiveErrors, stoppingToken);
            }
        }
    }

    private async Task HandleErrorBackoffAsync(Exception ex, int errorCount, CancellationToken stoppingToken)
    {
        int delayMilliseconds = Math.Min(30000, 1000 * (int)Math.Pow(2, Math.Min(errorCount - 1, 5)));

        Console.WriteLine($"[LogMonitorService] Error reading log file (Count: {errorCount}). Backing off for {delayMilliseconds}ms. Error: {ex.Message}");

        try
        {
            await Task.Delay(delayMilliseconds, stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }
    }
}
