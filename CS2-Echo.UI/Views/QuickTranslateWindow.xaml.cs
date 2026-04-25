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
using System.Windows.Shapes;
using CS2_Echo.UI.ViewModels;

namespace CS2_Echo.UI.Views;

/// <summary>
/// Logika interakcji dla klasy QuickTranslateWindow.xaml
/// </summary>
public partial class QuickTranslateWindow : Window
{
    private readonly QuickTranslateViewModel _viewModel;

    public QuickTranslateWindow(QuickTranslateViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.RequestHide += () => this.Hide();
    }

    private void Window_Activated(object sender, System.EventArgs e)
    {
        MessageBox.Focus();
        MessageBox.SelectAll();
    }

    private void Window_Deactivated(object sender, System.EventArgs e)
    {
        if (this.IsVisible)
        {
            this.Hide();
        }
    }

    private void MessageBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;

            if (_viewModel.TranslateAndCopyCommand.CanExecute(null))
            {
                _viewModel.TranslateAndCopyCommand.Execute(null);
            }
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            this.Hide();
        }
    }
}

