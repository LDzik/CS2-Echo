using CS2_Echo.UI.ViewModels;
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

namespace CS2_Echo.UI.Views.Pages;

/// <summary>
/// Logika interakcji dla klasy StatsPage.xaml
/// </summary>
public partial class StatsPage : Page
{
    private readonly StatsViewModel _viewModel;

    public StatsPage(StatsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.RefreshDataAsync();
    }
}

