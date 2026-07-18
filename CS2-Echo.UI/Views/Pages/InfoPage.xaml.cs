using CS2_Echo.UI.ViewModels;
using Markdig.Wpf;
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
using Wpf.Ui.Controls;

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
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private async void ViewReleaseNotes_Click(object sender, RoutedEventArgs e)
    {
        var textColor = (SolidColorBrush)new BrushConverter().ConvertFrom("#E0E0E0")!;

        var markdownViewer = new MarkdownViewer
        {
            Markdown = _viewModel.ReleaseNotes,
            Margin = new Thickness(0, 10, 15, 10),
            Foreground = textColor
        };

        var headingKeys = new[]
        {
            Markdig.Wpf.Styles.Heading1StyleKey,
            Markdig.Wpf.Styles.Heading2StyleKey,
            Markdig.Wpf.Styles.Heading3StyleKey,
            Markdig.Wpf.Styles.Heading4StyleKey,
            Markdig.Wpf.Styles.Heading5StyleKey,
            Markdig.Wpf.Styles.Heading6StyleKey
        };

        foreach (var key in headingKeys)
        {
            if (markdownViewer.TryFindResource(key) is Style originalStyle)
            {
                var darkStyle = new Style(typeof(System.Windows.Documents.Paragraph), originalStyle);
                darkStyle.Setters.Add(new Setter(System.Windows.Documents.TextElement.ForegroundProperty, textColor));
                markdownViewer.Resources[key] = darkStyle;
            }
        }

        markdownViewer.CommandBindings.Add(new CommandBinding(Commands.Hyperlink, MarkdownHyperlink_Executed));

        var scrollViewer = new ScrollViewer
        {
            MaxHeight = 400,
            MaxWidth = 500,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = markdownViewer
        };

        var messageBox = new Wpf.Ui.Controls.MessageBox
        {
            Title = "Release Notes",
            Content = scrollViewer,
            CloseButtonText = "Close"
        };

        await messageBox.ShowDialogAsync();
    }

    private void MarkdownHyperlink_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch
            {
                // blocked
            }
        }
    }
}

