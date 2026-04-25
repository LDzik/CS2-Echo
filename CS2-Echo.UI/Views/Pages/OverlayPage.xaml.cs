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
/// Logika interakcji dla klasy OverlayPage.xaml
/// </summary>
public partial class OverlayPage : Page
{
    private readonly OverlayViewModel _viewModel;

    public OverlayPage(OverlayViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }
}

