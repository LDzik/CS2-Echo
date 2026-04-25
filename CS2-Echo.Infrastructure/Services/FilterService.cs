using CS2_Echo.Domain;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace CS2_Echo.Infrastructure.Services;


public class FilterService : IDisposable
{
    private readonly DatabaseService _databaseService;
    private readonly ConfigurationService _configService;

    private HashSet<string> _ignoredUsers = new(StringComparer.OrdinalIgnoreCase);

    private readonly ReaderWriterLockSlim _cacheLock = new();

    private bool _disposed;


    public FilterService(
        DatabaseService databaseService,
        ConfigurationService configService
        )
    {
        _databaseService = databaseService;
        _configService = configService;
    }

    public async Task InitializeAsync()
    {
        var users = await _databaseService.LoadIgnoredPlayersAsync();

        _cacheLock.EnterWriteLock();
        try
        {
            _ignoredUsers = users;
        }
        finally
        {
            _cacheLock.ExitWriteLock();
        }
    }

    public bool ShouldTranslate(ChatMessage message, out string detectedLang, out string confidenceLog)
    {
        detectedLang = "unknown";
        confidenceLog = "Disabled / Loading...";


        _cacheLock.EnterReadLock();
        try
        {
            if (_ignoredUsers.Contains(message.PlayerName)) return false;
        }
        finally
        {
            _cacheLock.ExitReadLock();
        }

        if (message.Message.Length < _configService.Current.MinMessageSize) return false;

        return true;
    }

    public async Task AddIgnoredUserAsync(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName)) return;

        playerName = playerName.Trim();

        await _databaseService.AddIgnoredPlayerAsync(playerName);

        try
        {
            _cacheLock.EnterWriteLock();
            try
            {
                _ignoredUsers.Add(playerName);
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }
        catch (Exception ex)
        {
            try
            {
                await _databaseService.RemoveIgnoredPlayerAsync(playerName);
            }
            catch
            {

            }

            System.Diagnostics.Debug.WriteLine($"[FilterService] Failed to update in-memory ignore list: {ex.Message}");
            throw;
        }
    }

    public async Task RemoveIgnoredUserAsync(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName)) return;

        playerName = playerName.Trim();

        await _databaseService.RemoveIgnoredPlayerAsync(playerName);

        try
        {
            _cacheLock.EnterWriteLock();
            try
            {
                _ignoredUsers.Remove(playerName);
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }
        catch (Exception ex)
        {
            try
            {
                await _databaseService.AddIgnoredPlayerAsync(playerName);
            }
            catch
            {

            }

            System.Diagnostics.Debug.WriteLine($"[FilterService] Failed to update in-memory ignore list: {ex.Message}");
            throw;
        }
    }

    public IEnumerable<string> GetIgnoredUsers() 
    {
        _cacheLock.EnterReadLock();
        try
        {
            return _ignoredUsers.ToList();
        }
        finally
        {
            _cacheLock.ExitReadLock();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _cacheLock.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}

