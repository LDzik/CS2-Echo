using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace CS2_Echo.Infrastructure.Utilities;

public static class SteamLocator
{
    private static string GetActiveSteamAccountId(string steamPath)
    {
        try
        {
            object? activeUserObj = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam\ActiveProcess", "ActiveUser", 0);
            if (activeUserObj is int activeUser && activeUser > 0)
            {
                return activeUser.ToString();
            }

            string loginUsersPath = Path.Combine(steamPath, "config", "loginusers.vdf");
            if (File.Exists(loginUsersPath))
            {
                string currentSteamId64 = string.Empty;
                foreach (var line in File.ReadLines(loginUsersPath))
                {
                    string trimmed = line.Trim();

                    var idMatch = Regex.Match(trimmed, "^\"(7656119[0-9]+)\"");
                    if (idMatch.Success)
                    {
                        currentSteamId64 = idMatch.Groups[1].Value;
                    }

                    if (trimmed.Contains("\"MostRecent\"") && trimmed.Contains("\"1\"") && !string.IsNullOrEmpty(currentSteamId64))
                    {
                        if (ulong.TryParse(currentSteamId64, out ulong steamId64))
                        {
                            uint accountId = (uint)(steamId64 & 0xFFFFFFFF);
                            return accountId.ToString();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SteamLocator] Error finding active account: {ex.Message}");
        }

        return string.Empty;
    }

    public static string GetCS2LaunchOptions()
    {
        if (!OperatingSystem.IsWindows()) return string.Empty;

        try
        {
            string? steamPath = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string;
            if (string.IsNullOrEmpty(steamPath)) return string.Empty;

            string userdataDir = Path.Combine(steamPath, "userdata");
            if (!Directory.Exists(userdataDir)) return string.Empty;

            string targetAccountId = GetActiveSteamAccountId(steamPath);

            List<string> foldersToScan = new();
            if (!string.IsNullOrEmpty(targetAccountId))
            {
                string targetDir = Path.Combine(userdataDir, targetAccountId);
                if (Directory.Exists(targetDir))
                {
                    System.Diagnostics.Debug.WriteLine($"[SteamLocator] Targeting active account: {targetAccountId}");
                    foldersToScan.Add(targetDir);
                }
            }

            if (foldersToScan.Count == 0)
            {
                foldersToScan.AddRange(Directory.GetDirectories(userdataDir));
            }

            foreach (var userDir in foldersToScan)
            {
                string localConfig = Path.Combine(userDir, "config", "localconfig.vdf");
                if (!File.Exists(localConfig)) continue;

                bool in730Block = false;
                int braceDepth = 0;
                int targetDepth = -1;

                foreach (var line in File.ReadLines(localConfig))
                {
                    string trimmed = line.Trim();

                    if (trimmed == "{") braceDepth++;
                    if (trimmed == "}") braceDepth--;

                    if (trimmed.StartsWith("\"730\""))
                    {
                        in730Block = true;
                        targetDepth = braceDepth + 1;
                        continue;
                    }

                    if (in730Block && braceDepth < targetDepth)
                    {
                        in730Block = false;
                    }

                    if (in730Block && trimmed.StartsWith("\"LaunchOptions\"", StringComparison.OrdinalIgnoreCase))
                    {
                        var match = Regex.Match(trimmed, "\"LaunchOptions\"\\s+\"(.*)\"", RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            string cleanOptions = match.Groups[1].Value.Replace("\\\"", "\"").Replace("\\\\", "\\");

                            System.Diagnostics.Debug.WriteLine($"[SteamLocator] CLEANED options:  {cleanOptions}");
                            return cleanOptions;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to read VDF launch options: {ex.Message}");
        }

        return string.Empty;
    }

    public static string? FindCS2InstallPath()
    {
        if (!OperatingSystem.IsWindows()) return null;

        try
        {
            string? steamPath = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string;

            if (string.IsNullOrEmpty(steamPath) || !Directory.Exists(steamPath))
            {

                steamPath = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath", null) as string;
            }

            if (string.IsNullOrEmpty(steamPath) || !Directory.Exists(steamPath))
                return null;

            steamPath = Path.GetFullPath(steamPath);

            string vdfPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(vdfPath))
            {
                string defaultCs2 = Path.Combine(steamPath, "steamapps", "common", "Counter-Strike Global Offensive");
                return Directory.Exists(defaultCs2) ? defaultCs2 : null;
            }

            string currentLibraryPath = steamPath;

            foreach (var line in File.ReadLines(vdfPath))
            {
                string trimmed = line.Trim();

                var pathMatch = Regex.Match(trimmed, "\"path\"\\s+\"([^\"]+)\"", RegexOptions.IgnoreCase);
                if (pathMatch.Success)
                {
                    currentLibraryPath = pathMatch.Groups[1].Value.Replace("\\\\", "\\");
                }

                if (trimmed.StartsWith("\"730\"", StringComparison.OrdinalIgnoreCase))
                {
                    string cs2Path = Path.Combine(currentLibraryPath, "steamapps", "common", "Counter-Strike Global Offensive");
                    if (Directory.Exists(cs2Path))
                    {
                        return cs2Path;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to auto-detect CS2 path: {ex.Message}");
        }

        return null;
    }
}