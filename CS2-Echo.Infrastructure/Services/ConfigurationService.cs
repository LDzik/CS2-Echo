using CS2_Echo.Domain;
using CS2_Echo.Infrastructure.Security;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace CS2_Echo.Infrastructure.Services;

public class ConfigurationService
{
    private readonly string _configFilePath;
    private readonly object _configLock = new();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    private AppConfig _current;

    private volatile AppConfig _cachedSnapshot;

    public event Action? OnConfigurationChanged;

    public AppConfig Current => _cachedSnapshot;

    public ConfigurationService()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string folder = Path.Combine(appData, "CS2_Echo");

        DirectoryInfo directoryInfo = new DirectoryInfo(folder);
        if (!directoryInfo.Exists)
        {
            directoryInfo.Create();
            SecureDirectoryAcl(directoryInfo);
        }

        _configFilePath = Path.Combine(folder, "config.json");
        Load();
    }

    private void UpdateSnapshotLocked()
    {
        string json = JsonSerializer.Serialize(_current, _jsonOptions);
        _cachedSnapshot = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions) ?? new AppConfig();
    }

    private void SecureDirectoryAcl(DirectoryInfo directoryInfo)
    {
        if (!OperatingSystem.IsWindows()) return;

        try
        {
            DirectorySecurity security = directoryInfo.GetAccessControl();
            SecurityIdentifier? currentUser = WindowsIdentity.GetCurrent().User;

            if (currentUser != null)
            {
                security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

                security.AddAccessRule(new FileSystemAccessRule(
                    currentUser,
                    FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow));

                directoryInfo.SetAccessControl(security);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Config] Warning: Failed to apply directory ACLs: {ex.Message}");
        }
    }


    public void Load()
    {
        lock (_configLock)
        {
            if (File.Exists(_configFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_configFilePath);
                    var loadedConfig = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions) ?? new AppConfig();

                    _current = loadedConfig with
                    {
                        DeepLApiKey = EnsureEncrypted(loadedConfig.DeepLApiKey, "DeepL"),
                        GeminiApiKey = EnsureEncrypted(loadedConfig.GeminiApiKey, "Gemini")
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Config] Failed to load config: {ex.Message}");
                    _current = new AppConfig();
                }
            }
            else
            {
                _current = new AppConfig();
                SaveLocked();
            }

            UpdateSnapshotLocked();
        }
    }

    private string EnsureEncrypted(string storedKey, string keyName)
    {
        if (string.IsNullOrEmpty(storedKey)) return string.Empty;

        if (DpapiHelper.TryDecrypt(storedKey, out _)) return storedKey;

        Span<byte> buffer = new Span<byte>(new byte[storedKey.Length]);
        if (!Convert.TryFromBase64String(storedKey, buffer, out _))
        {
            return DpapiHelper.Encrypt(storedKey);
        }

        Console.WriteLine($"[Config] Warning: Failed to decrypt {keyName} API key. Wiping to prevent data leak.");
        return string.Empty;
    }

    public string GetDecryptedDeepLKey()
    {
        lock (_configLock)
        {
            return DpapiHelper.Decrypt(_current.DeepLApiKey);
        }
    }

    public string GetDecryptedGeminiKey()
    {
        lock (_configLock)
        {
            return DpapiHelper.Decrypt(_current.GeminiApiKey);
        }
    }

    public string EncryptSecret(string plainText)
    {
        return DpapiHelper.Encrypt(plainText);
    }

    public void Update(Func<AppConfig, AppConfig> configure)
    {
        lock (_configLock)
        {
            _current = configure(_current);
            SaveLocked();
            UpdateSnapshotLocked();
        }

        OnConfigurationChanged?.Invoke();
    }

    public void Save()
    {
        lock (_configLock)
        {
            SaveLocked();
        }
    }

    private void SaveLocked()
    {
        string jsonToSave = JsonSerializer.Serialize(_current, _jsonOptions);
        File.WriteAllText(_configFilePath, jsonToSave);
    }
}

