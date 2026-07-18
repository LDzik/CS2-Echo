using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace CS2_Echo.Infrastructure.Utilities;

public static class SteamLocator
{
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