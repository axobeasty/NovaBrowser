using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using NovaBrowser.Controls;
using NovaBrowser.Services;
using NovaBrowser.ViewModels;

namespace NovaBrowser;

public sealed partial class MainPage : Page
{
    private readonly Dictionary<BrowserTabViewModel, BrowserTabView> _tabViews = [];
    private BrowserTabStrip? _tabStrip;

    public MainPageViewModel ViewModel { get; }

    public string AppVersionLabel => $"v{AppVersionService.CurrentVersionLabel}";

    public MainPage()
    {
        ViewModel = ((App)Application.Current).MainViewModel;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (App.Window is not MainWindow mainWindow)
        {
            return;
        }

        _tabStrip = mainWindow.TabStripControl;
        _tabStrip.AddTabRequested += OnAddTabRequested;
        _tabStrip.TabCloseRequested += OnTabCloseRequested;
        _tabStrip.TabSelected += OnTabSelected;

        SyncTabsFromViewModel();
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        ViewModel.Tabs.CollectionChanged += (_, _) => SyncTabsFromViewModel();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainPageViewModel.ActiveTab))
        {
            UpdateActiveTab();
        }
    }

    private void SyncTabsFromViewModel()
    {
        if (_tabStrip is null)
        {
            return;
        }

        var existingIds = ViewModel.Tabs.Select(t => t.Id).ToHashSet();

        foreach (var removed in _tabViews.Keys.Where(k => !existingIds.Contains(k.Id)).ToList())
        {
            if (_tabViews.Remove(removed, out var view))
            {
                TabContentHost.Children.Remove(view);
            }
        }

        foreach (var tab in ViewModel.Tabs)
        {
            if (_tabViews.ContainsKey(tab))
            {
                continue;
            }

            var tabView = new BrowserTabView { ViewModel = tab };
            _tabViews[tab] = tabView;
            TabContentHost.Children.Add(tabView);

            tab.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName is nameof(BrowserTabViewModel.CanGoBack) or nameof(BrowserTabViewModel.CanGoForward))
                {
                    ViewModel.NotifyNavigationCommands();
                }
            };
        }

        UpdateActiveTab();
    }

    private void UpdateActiveTab()
    {
        if (_tabStrip is null)
        {
            return;
        }

        var active = ViewModel.ActiveTab;
        _tabStrip.Refresh(ViewModel.Tabs, active);

        if (active is null)
        {
            return;
        }

        foreach (var pair in _tabViews)
        {
            pair.Value.Visibility = pair.Key == active ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void OnAddTabRequested(object? sender, EventArgs e)
    {
        ViewModel.NewTabCommand.Execute(null);
    }

    private void OnTabCloseRequested(object? sender, BrowserTabViewModel tab)
    {
        ViewModel.CloseTabCommand.Execute(tab);
    }

    private void OnTabSelected(object? sender, BrowserTabViewModel tab)
    {
        ViewModel.ActiveTab = tab;
        UpdateActiveTab();
    }

    private void OnAddressBarKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            ViewModel.NavigateCommand.Execute(null);
            e.Handled = true;
        }
    }

    private async void OnCheckUpdatesClick(object sender, RoutedEventArgs e)
    {
        if (Application.Current is App app)
        {
            await app.UpdateCoordinator.CheckManuallyAsync(XamlRoot);
        }
    }
}
