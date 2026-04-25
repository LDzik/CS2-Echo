using System;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CS2_Echo.Infrastructure.Services;

namespace CS2_Echo.UI.ViewModels;

public partial class QuickTranslateViewModel : ObservableObject
{
    private readonly TranslationService _translationService;
    private readonly ConfigurationService _configService;

    public event Action? RequestHide;

    [ObservableProperty] public partial string TargetLanguage { get; set; }
    [ObservableProperty] public partial string InputText { get; set; }
    [ObservableProperty] public partial bool IsTranslating { get; set; }
    [ObservableProperty] public partial bool IsSuccess { get; set; }
    [ObservableProperty] public partial bool IsError { get; set; }
    public QuickTranslateViewModel(TranslationService translationService, ConfigurationService configService)
    {
        _translationService = translationService;
        _configService = configService;

        TargetLanguage = _configService.Current.LastQuickTranslateLang ?? "en";
        InputText = string.Empty;
    }

    [RelayCommand]
    public async Task TranslateAndCopyAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText) || string.IsNullOrWhiteSpace(TargetLanguage)) return;

        IsTranslating = true;
        IsSuccess = false;
        IsError = false;

        if (!_configService.Current.LastQuickTranslateLang.Equals(TargetLanguage, StringComparison.OrdinalIgnoreCase))
        {
            _configService.Update(config => config with
            {
                LastQuickTranslateLang = TargetLanguage.ToLower()
            });
        }

        try
        {
            var result = await _translationService.TranslateAsync(InputText, TargetLanguage);

            if (result.SourceLang == "error")
            {
                await HandleErrorStateAsync();
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                Clipboard.SetText(result.TranslatedText);
            });

            IsTranslating = false;
            IsSuccess = true;

            await Task.Delay(800);

            InputText = string.Empty;

            RequestHide?.Invoke();
        }
        catch (Exception)
        {
            await HandleErrorStateAsync();
        }
        finally
        {
            IsSuccess = false;
        }
    }

    private async Task HandleErrorStateAsync()
    {
        IsTranslating = false;
        IsError = true;

        await Task.Delay(800);

        IsError = false;
    }
}

