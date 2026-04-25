using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CS2_Echo.Infrastructure.Services;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace CS2_Echo.UI.ViewModels;

public partial class StatsViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService;
    private readonly FilterService _filterService;
    private readonly ISnackbarService _snackbarService;


    public class LanguageStat
    {
        public string Language { get; set; }
        public int MessageCount { get; set; }
    }

    public class PlayerProfile
    {
        public string PlayerName { get; set; }
        public int TotalMessages { get; set; }
        public string LastActive { get; set; }
        public string PrimaryLanguage { get; set; }
        public ObservableCollection<LanguageStat> Languages { get; set; } = new();
    }

    public ObservableCollection<PlayerProfile> PlayerProfiles { get; } = new();

    [ObservableProperty]
    public partial string DbSizeText { get; set; }

    public ObservableCollection<string> IgnoredPlayers { get; } = new();

    [ObservableProperty] public partial string ManualPlayerInput { get; set; }

    public StatsViewModel(
        DatabaseService databaseService,
        FilterService filterService,
        ISnackbarService snackbarService)
    {
        _databaseService = databaseService;
        _filterService = filterService;
        _snackbarService = snackbarService;
    }

    public async Task RefreshDataAsync()
    {
        try
        {
            UpdateDbSize();

            IgnoredPlayers.Clear();
            foreach (var player in _filterService.GetIgnoredUsers()) IgnoredPlayers.Add(player);

            PlayerProfiles.Clear();
            var rawStats = await _databaseService.GetTopPlayerStatsAsync();
            var groupedStats = rawStats.GroupBy(s => s.PlayerName);


            foreach (var group in groupedStats)
            {
                var sortedLanguages = group.OrderByDescending(s => s.MessageCount).ToList();
                var topLang = sortedLanguages.First().Language.ToLower();

                var profile = new PlayerProfile
                {
                    PlayerName = group.Key,
                    TotalMessages = group.Sum(s => s.MessageCount),
                    LastActive = group.Max(s => s.LastActive).ToLocalTime().ToString("MM/dd HH:mm"),
                    PrimaryLanguage = topLang
                };

                foreach (var stat in group.OrderByDescending(s => s.MessageCount))
                {
                    profile.Languages.Add(new LanguageStat
                    {
                        Language = stat.Language.ToLower(),
                        MessageCount = stat.MessageCount
                    });
                }

                PlayerProfiles.Add(profile);
            }
        }
        catch (Exception ex)
        {
            _snackbarService.Show(
                    "Error Loading Stats",
                    ex.Message,
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24),
                    TimeSpan.FromSeconds(5)
                );
        }

    }

    private void UpdateDbSize()
    {
        double size = _databaseService.GetDatabaseSizeMB();
        DbSizeText = $"Current Cache Size: {size:F2} MB";
    }

    [RelayCommand]
    private async Task ClearCacheAsync()
    {
        await _databaseService.ClearCacheAsync();
        UpdateDbSize();
        _snackbarService.Show("Cache Cleared", "The SQLite translation memory has been wiped.", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.Delete24), new System.TimeSpan(0, 0, 3));
    }

    [RelayCommand]
    private async Task UnignorePlayerAsync(string playerName)
    {
        await _filterService.RemoveIgnoredUserAsync(playerName);
        IgnoredPlayers.Remove(playerName);
    }

    [RelayCommand]
    private async Task AddManualPlayerAsync()
    {
        if (string.IsNullOrWhiteSpace(ManualPlayerInput)) return;

        string cleanName = ManualPlayerInput.Trim();
        await _filterService.AddIgnoredUserAsync(cleanName);

        if (!IgnoredPlayers.Contains(cleanName)) IgnoredPlayers.Add(cleanName);

        ManualPlayerInput = string.Empty;
    }

    [RelayCommand]
    private async Task ClearPlayerStatsAsync()
    {
        await _databaseService.ClearPlayerStatsAsync();
        PlayerProfiles.Clear();
        _snackbarService.Show("Stats Cleared", "Player language statistics have been wiped.", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.Delete24), TimeSpan.FromSeconds(3));
    }
}

