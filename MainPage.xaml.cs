using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using NovaBrowser.Controls;
using NovaBrowser.Helpers;
using NovaBrowser.Models;
using NovaBrowser.Services;
using NovaBrowser.ViewModels;
using Windows.System;

namespace NovaBrowser;

public sealed partial class MainPage : Page
{
    private readonly Dictionary<BrowserTabViewModel, BrowserTabView> _tabViews = [];
    private BrowserTabStrip? _tabStrip;

    public MainPageViewModel ViewModel { get; }

    public string AppVersionLabel => $"v{AppVersionService.CurrentVersionLabel}";

    public MainPage()
    {
        ViewModel = App.Window is MainWindow mainWindow
            ? mainWindow.ViewModel
            : ((App)Application.Current).MainViewModel;
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public BrowserTabView? ReleaseTabView(BrowserTabViewModel tab)
    {
        if (!_tabViews.Remove(tab, out var view))
        {
            return null;
        }

        TabContentHost.Children.Remove(view);
        view.PageVisited -= OnTabPageVisited;
        return view;
    }

    public void PrepareTabAdoption(BrowserTabViewModel tab, BrowserTabView? tabView)
    {
        if (tabView is null)
        {
            return;
        }

        tabView.ViewModel = tab;
        tabView.PageVisited -= OnTabPageVisited;
        tabView.PageVisited += OnTabPageVisited;
        _tabViews[tab] = tabView;

        if (!TabContentHost.Children.Contains(tabView))
        {
            TabContentHost.Children.Add(tabView);
        }

        HookTabPropertyChanged(tab);
    }

    public void FocusAddressBar()
    {
        AddressBar.Focus(FocusState.Programmatic);
        AddressBar.SelectAll();
    }

    public void RefreshAfterSettingsChanged()
    {
        ApplyBookmarkBarVisibility();
        RefreshBookmarkBar();
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
        _tabStrip.TabReorderRequested += OnTabReorderRequested;
        _tabStrip.TabDetachRequested += OnTabDetachRequested;
        _tabStrip.TabPinRequested += (_, tab) => ViewModel.PinTab(tab);
        _tabStrip.TabDuplicateRequested += (_, tab) => ViewModel.DuplicateTab(tab);
        _tabStrip.TabCloseOthersRequested += (_, tab) => ViewModel.CloseOtherTabs(tab);
        _tabStrip.TabCloseToRightRequested += (_, tab) => ViewModel.CloseTabsToRight(tab);
        _tabStrip.TabMuteRequested += (_, tab) => ViewModel.MuteTab(tab);

        FindBarControl.ViewModel = ViewModel;
        FindBarControl.CloseRequested += OnFindBarClose;
        BookmarkBarControl.BookmarkActivated += OnBookmarkActivated;

        SyncTabsFromViewModel();
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        ViewModel.Tabs.CollectionChanged += (_, _) => SyncTabsFromViewModel();

        if (Application.Current is App appInstance)
        {
            appInstance.ThemeService.ThemeChanged += OnAppThemeChanged;
            appInstance.Localization.LanguageChanged += OnLanguageChanged;
            appInstance.UpdateCoordinator.UpdateAvailabilityChanged += OnUpdateAvailabilityChanged;
            var dispatcherQueue = App.DispatcherQueue
                ?? Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            appInstance.UpdateCoordinator.StartBackgroundMonitoring(dispatcherQueue);
            ApplyPageTheme();
            ApplyLocalizedStrings();
            ApplyUpdateIndicator();
            ApplyBookmarkBarVisibility();
            RefreshBookmarkBar();
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.SaveSession();

        if (Application.Current is App app)
        {
            app.ThemeService.ThemeChanged -= OnAppThemeChanged;
            app.Localization.LanguageChanged -= OnLanguageChanged;
            app.UpdateCoordinator.UpdateAvailabilityChanged -= OnUpdateAvailabilityChanged;
            app.UpdateCoordinator.StopBackgroundMonitoring();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainPageViewModel.ActiveTab))
        {
            UpdateActiveTab();
            UpdateSecurityIndicator();
        }
        else if (e.PropertyName == nameof(MainPageViewModel.IsFindBarOpen))
        {
            FindBarControl.Visibility = ViewModel.IsFindBarOpen ? Visibility.Visible : Visibility.Collapsed;
            if (ViewModel.IsFindBarOpen)
            {
                FindBarControl.FocusQuery();
            }
        }
    }

    private void OnUpdateAvailabilityChanged(object? sender, EventArgs e) => ApplyUpdateIndicator();

    private void OnAppThemeChanged(object? sender, Models.BrowserTheme e) => ApplyPageTheme();

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
        SetButtonLocalization(BookmarkButton, "BookmarkButton");
        SetButtonLocalization(DownloadsButton, "DownloadsButton");
        SetButtonLocalization(LibraryButton, "LibraryButton");
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
        StatusTextBlock.Foreground = GetThemeBrush("NovaTextSecondaryBrush");
    }

    private static Microsoft.UI.Xaml.Media.Brush GetThemeBrush(string key) =>
        (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources[key];

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
            tabView.PageVisited += OnTabPageVisited;
            _tabViews[tab] = tabView;
            TabContentHost.Children.Add(tabView);
            HookTabPropertyChanged(tab);
        }

        UpdateActiveTab();
    }

    private void HookTabPropertyChanged(BrowserTabViewModel tab)
    {
        tab.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(BrowserTabViewModel.CanGoBack) or nameof(BrowserTabViewModel.CanGoForward))
            {
                ViewModel.NotifyNavigationCommands();
            }
        };
    }

    private void OnTabPageVisited(object? sender, (string Title, string Url) visit)
    {
        if (sender is BrowserTabView tabView)
        {
            ViewModel.RecordHistory(tabView.ViewModel);
        }
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

    private void UpdateSecurityIndicator()
    {
        if (ViewModel.ActiveTab is null)
        {
            return;
        }

        SecurityIcon.Glyph = ViewModel.ActiveTab.IsSecure ? "\uE72E" : "\uE785";
    }

    private void OnAddTabRequested(object? sender, EventArgs e) =>
        ViewModel.NewTabCommand.Execute(null);

    private void OnTabCloseRequested(object? sender, BrowserTabViewModel tab) =>
        ViewModel.CloseTabCommand.Execute(tab);

    private void OnTabSelected(object? sender, BrowserTabViewModel tab)
    {
        ViewModel.ActiveTab = tab;
        UpdateActiveTab();
    }

    private void OnTabReorderRequested(object? sender, (int OldIndex, int NewIndex) args) =>
        ViewModel.MoveTab(args.OldIndex, args.NewIndex);

    private void OnTabDetachRequested(object? sender, (BrowserTabViewModel Tab, Windows.Foundation.Point ScreenPosition) args)
    {
        if (Application.Current is not App app || App.Window is not MainWindow sourceWindow)
        {
            return;
        }

        app.DetachTabToNewWindow(sourceWindow, args.Tab, args.ScreenPosition);
    }

    private void OnAddressBarKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            ViewModel.NavigateCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnBookmarkActivated(object sender, string url) =>
        ViewModel.ActiveTab?.RequestNavigation(url);

    private void OnLibraryClick(object sender, RoutedEventArgs e)
    {
        var flyout = new MenuFlyout();
        AddMenuItem(flyout, L.Get("WindowBookmarksTitle"), () => OpenFeature(FeatureWindowKind.Bookmarks));
        AddMenuItem(flyout, L.Get("WindowHistoryTitle"), () => OpenFeature(FeatureWindowKind.History));
        AddMenuItem(flyout, L.Get("WindowDownloadsTitle"), () => OpenFeature(FeatureWindowKind.Downloads));
        flyout.Items.Add(new MenuFlyoutSeparator());
        AddMenuItem(flyout, L.Get("WindowScriptsTitle"), () => OpenFeature(FeatureWindowKind.UserScripts));
        AddMenuItem(flyout, L.Get("WindowPasswordsTitle"), () => OpenFeature(FeatureWindowKind.Passwords));
        flyout.ShowAt(LibraryButton, new FlyoutShowOptions { Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft });
    }

    private static void AddMenuItem(MenuFlyout flyout, string text, Action handler)
    {
        var item = new MenuFlyoutItem { Text = text };
        item.Click += (_, _) => handler();
        flyout.Items.Add(item);
    }

    private static void OpenFeature(FeatureWindowKind kind)
    {
        if (Application.Current is App app)
        {
            app.OpenFeatureWindow(kind);
        }
    }

    private void OnBookmarkClick(object sender, RoutedEventArgs e) =>
        ViewModel.ToggleBookmarkForActiveTab();

    private void OnDownloadsClick(object sender, RoutedEventArgs e) =>
        OpenFeature(FeatureWindowKind.Downloads);

    private void OnFindBarClose(object sender, EventArgs e) =>
        ViewModel.ToggleFindBar();

    private async void OnSecurityClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.ActiveTab is null)
        {
            return;
        }

        var dialog = new ContentDialog
        {
            Title = L.Get("SiteInfoTitle"),
            Content = new TextBlock
            {
                Text = $"{ViewModel.ActiveTab.Title}\n{ViewModel.ActiveTab.Url}\n{(ViewModel.ActiveTab.IsSecure ? L.Get("SiteSecure") : L.Get("SiteNotSecure"))}",
                TextWrapping = TextWrapping.Wrap,
            },
            CloseButtonText = L.Get("Close"),
            XamlRoot = Content.XamlRoot,
        };

        await dialog.ShowAsync();
    }

    private async void OnCheckUpdatesClick(object sender, RoutedEventArgs e)
    {
        if (Application.Current is App app)
        {
            await app.UpdateCoordinator.CheckManuallyAsync();
        }
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        if (Application.Current is App app)
        {
            app.OpenSettings();
        }
    }

    private void ApplyBookmarkBarVisibility()
    {
        if (Application.Current is not App app)
        {
            return;
        }

        BookmarkBarControl.Visibility = app.SettingsService.Current.ShowBookmarkBar
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void RefreshBookmarkBar()
    {
        ViewModel.RefreshBookmarkBar();
        BookmarkBarControl.Bind(ViewModel.BookmarkBarItems);
    }
}
