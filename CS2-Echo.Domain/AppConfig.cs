using System;
using System.Collections.Generic;
using System.Text;

namespace CS2_Echo.Domain;



public record AppConfig
{
    public string LogFilePath { get; init; } = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Counter-Strike Global Offensive\\";
    public string SelectedEngine { get; init; } = "Google Translate (Free)";
    public string DeepLApiKey { get; init; } = "";
    public string GeminiApiKey { get; init; } = "";
    public string TargetLanguage { get; init; } = "en";
    public int MinMessageSize { get; init; } = 3;
    public bool EnablePlayerStats { get; init; } = true;
    public bool MinimizeToTray { get; init; } = true;

    public double OverlayLockedOpacity { get; init; } = 0.4;
    public int OverlayFontSize { get; init; } = 14;
    public string OverlayHotkey { get; init; } = "O";
    public string QuickTranslateHotkey { get; init; } = "T";
    public string LastQuickTranslateLang { get; init; } = "en";

    public double OverlayX { get; init; } = double.NaN;
    public double OverlayY { get; init; } = double.NaN;
}
