using CS2_Echo.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
/// Logika interakcji dla klasy TranslationsPage.xaml
/// </summary>
public partial class TranslationsPage : Page
{
    private readonly TranslationsViewModel _viewModel;
    public TranslationsPage(TranslationsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        this.Loaded += Page_Loaded;

        CollectionChangedEventManager.AddHandler(_viewModel.ChatFeed, ChatFeed_CollectionChanged);
    }

    private void Page_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        _viewModel.RefreshUIState();

        _viewModel.ChatFeed.CollectionChanged -= ChatFeed_CollectionChanged;
        _viewModel.ChatFeed.CollectionChanged += ChatFeed_CollectionChanged;
    }

    private void Page_Unloaded(object sender, System.Windows.RoutedEventArgs e)
    {
        _viewModel.ChatFeed.CollectionChanged -= ChatFeed_CollectionChanged;
    }

    private void ChatFeed_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (!this.IsLoaded) return;

        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            ChatListView.Dispatcher.InvokeAsync(() =>
            {
                if (ChatListView.Items.Count > 0)
                {
                    var lastItem = ChatListView.Items[ChatListView.Items.Count - 1];
                    ChatListView.ScrollIntoView(lastItem);
                }
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }
}

