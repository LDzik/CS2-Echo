using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CS2_Echo.UI.ViewModels;
using Wpf.Ui.Controls;

namespace CS2_Echo.UI.Views.Pages;

/// <summary>
/// Logika interakcji dla klasy SettingsPage.xaml
/// </summary>
public partial class SettingsPage : Page
{

    private readonly SettingsViewModel _viewModel;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        Loaded += SettingsPage_Loaded;
    }


    // Needs to be done this way because WPF is trash
    private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        DeepLKeyBox.PasswordChanged -= DeepLKeyBox_PasswordChanged;
        GeminiKeyBox.PasswordChanged -= GeminiKeyBox_PasswordChanged;

        DeepLKeyBox.Password = _viewModel.DeepLApiKey ?? string.Empty;
        GeminiKeyBox.Password = _viewModel.GeminiApiKey ?? string.Empty;

        DeepLKeyBox.PasswordChanged += DeepLKeyBox_PasswordChanged;
        GeminiKeyBox.PasswordChanged += GeminiKeyBox_PasswordChanged;
    }

    private void DeepLKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.DeepLApiKey = DeepLKeyBox.Password;
    }

    private void GeminiKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.GeminiApiKey = GeminiKeyBox.Password;
    }
}

