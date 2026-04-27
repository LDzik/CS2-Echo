using CS2_Echo.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
/// Logika interakcji dla klasy InfoPage.xaml
/// </summary>
public partial class InfoPage : Page
{

    private readonly InfoViewModel _viewModel;
    public InfoPage(InfoViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        ReleaseNotesDialog.DialogHostEx = RootDialogHost;
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private async void ViewReleaseNotes_Click(object sender, RoutedEventArgs e)
    {
        NotesScrollViewer.ScrollToTop();

        await ReleaseNotesDialog.ShowAsync();
    }
}

