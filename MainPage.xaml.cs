using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using NovaBrowser.Controls;
using NovaBrowser.Helpers;
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
        Unloaded += OnUnloaded;
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

        if (Application.Current is App app)
        {
            app.ThemeService.ThemeChanged += OnAppThemeChanged;
            app.Localization.LanguageChanged += OnLanguageChanged;
            app.UpdateCoordinator.UpdateAvailabilityChanged += OnUpdateAvailabilityChanged;
            app.UpdateCoordinator.StartBackgroundMonitoring(App.DispatcherQueue);
            ApplyPageTheme();
            ApplyLocalizedStrings();
            ApplyUpdateIndicator();
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (Application.Current is App app)
        {
            app.ThemeService.ThemeChanged -= OnAppThemeChanged;
            app.Localization.LanguageChanged -= OnLanguageChanged;
            app.UpdateCoordinator.UpdateAvailabilityChanged -= OnUpdateAvailabilityChanged;
            app.UpdateCoordinator.StopBackgroundMonitoring();
        }
    }

    private void OnUpdateAvailabilityChanged(object? sender, EventArgs e) =>
        ApplyUpdateIndicator();

    private void OnAppThemeChanged(object? sender, Models.BrowserTheme e) =>
        ApplyPageTheme();

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        ApplyLocalizedStrings();
        ViewModel.RefreshLocalization();

        if (_tabStrip is not null)
        {
            _tabStrip.RefreshLocalizedStrings();
            _tabStrip.Refresh(ViewModel.Tabs, ViewModel.ActiveTab);
        }

        foreach (var tabView in _tabViews.Values)
        {
            tabView.RefreshStartPageIfNeeded();
        }
    }

    private void ApplyLocalizedStrings()
    {
        SetButtonLocalization(NavBackButton, "NavBack");
        SetButtonLocalization(NavForwardButton, "NavForward");
        SetButtonLocalization(NavReloadButton, "NavReload");
        SetButtonLocalization(NavHomeButton, "NavHome");
        SetButtonLocalization(SettingsButton, "SettingsButton");
        ApplyUpdatesButtonLocalization();

        AddressBar.PlaceholderText = L.Get("AddressBarPlaceholder");
    }

    private void ApplyUpdateIndicator()
    {
        if (Application.Current is not App app)
        {
            return;
        }

        UpdateBadge.Visibility = app.UpdateCoordinator.HasPendingUpdate
            ? Visibility.Visible
            : Visibility.Collapsed;

        ApplyUpdatesButtonLocalization();
    }

    private void ApplyUpdatesButtonLocalization()
    {
        if (Application.Current is App app &&
            app.UpdateCoordinator.HasPendingUpdate &&
            app.UpdateCoordinator.PendingUpdate?.Update is { } update)
        {
            var text = L.Format("UpdatesButtonAvailable", update.Version);
            AutomationProperties.SetName(UpdatesButton, text);
            ToolTipService.SetToolTip(UpdatesButton, text);
            return;
        }

        SetButtonLocalization(UpdatesButton, "UpdatesButton");
    }

    private static void SetButtonLocalization(Button button, string key)
    {
        var text = L.Get(key);
        AutomationProperties.SetName(button, text);
        ToolTipService.SetToolTip(button, text);
    }

    private void ApplyPageTheme()
    {
        PageRoot.Background = GetThemeBrush("NovaContentBackgroundBrush");
        ToolbarGrid.Background = GetThemeBrush("NovaToolbarBackgroundBrush");
        ToolbarGrid.BorderBrush = GetThemeBrush("NovaTabStripDividerBrush");
        TabContentHost.Background = GetThemeBrush("NovaContentBackgroundBrush");
        VersionLabel.Foreground = GetThemeBrush("NovaTextSecondaryBrush");
    }

    private static Microsoft.UI.Xaml.Media.Brush GetThemeBrush(string key) =>
        (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources[key];

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

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        if (Application.Current is not App app)
        {
            return;
        }

        var viewModel = new SettingsViewModel(app.SettingsService, app.ThemeService, app.Localization);
        SettingsPanel.Initialize(viewModel);
        SettingsPanel.CloseRequested += OnSettingsPanelCloseRequested;
        SettingsPanel.CheckUpdatesRequested += OnSettingsCheckUpdatesRequested;
        SettingsOverlay.Visibility = Visibility.Visible;
    }

    private async void OnSettingsCheckUpdatesRequested(object? sender, EventArgs e)
    {
        if (Application.Current is App app)
        {
            await app.UpdateCoordinator.CheckManuallyAsync(XamlRoot);
        }
    }

    private void OnSettingsPanelCloseRequested(object? sender, EventArgs e)
    {
        SettingsOverlay.Visibility = Visibility.Collapsed;
        SettingsPanel.CloseRequested -= OnSettingsPanelCloseRequested;
        SettingsPanel.CheckUpdatesRequested -= OnSettingsCheckUpdatesRequested;
    }
}
