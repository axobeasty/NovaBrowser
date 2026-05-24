using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using NovaBrowser.Helpers;
using NovaBrowser.ViewModels;

namespace NovaBrowser.Controls;

public sealed partial class BrowserTabStrip : UserControl
{
    private const double CaptionButtonReserve = 142;
    private const double OuterLeftPadding = 12;
    private const double TabHostRightPadding = 8;
    private const double AddButtonSlotWidth = 36;
    private const double TabSpacing = 2;
    private const double PreferredTabWidth = 200;
    private const double MaxTabWidth = 240;
    private const double MinTabWidth = 56;

    private readonly List<BrowserTabItem> _tabItems = [];
    private IReadOnlyList<BrowserTabViewModel> _tabs = [];
    private BrowserTabViewModel? _selectedTab;
    private BrowserTabItem? _dragSource;
    private int _dragSourceIndex = -1;

    public event EventHandler<BrowserTabViewModel>? TabSelected;
    public event EventHandler<BrowserTabViewModel>? TabCloseRequested;
    public event EventHandler? AddTabRequested;
    public event EventHandler<(int OldIndex, int NewIndex)>? TabReorderRequested;
    public event EventHandler<BrowserTabViewModel>? TabPinRequested;
    public event EventHandler<BrowserTabViewModel>? TabDuplicateRequested;
    public event EventHandler<BrowserTabViewModel>? TabCloseOthersRequested;
    public event EventHandler<BrowserTabViewModel>? TabCloseToRightRequested;
    public event EventHandler<BrowserTabViewModel>? TabMuteRequested;

    public BrowserTabStrip()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        RefreshLocalizedStrings();
        ApplyStripTheme();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Application.Current is App app)
        {
            app.ThemeService.ThemeChanged += OnThemeChanged;
            app.Localization.LanguageChanged += OnLanguageChanged;
        }

        UpdateTabLayout();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (Application.Current is App app)
        {
            app.ThemeService.ThemeChanged -= OnThemeChanged;
            app.Localization.LanguageChanged -= OnLanguageChanged;
        }
    }

    private void OnStripSizeChanged(object sender, SizeChangedEventArgs e) =>
        UpdateTabLayout();

    private void OnAddTabClick(object sender, RoutedEventArgs e) =>
        AddTabRequested?.Invoke(this, EventArgs.Empty);

    private void OnThemeChanged(object? sender, Models.BrowserTheme e) => ApplyStripTheme();

    private void OnLanguageChanged(object? sender, EventArgs e) => RefreshLocalizedStrings();

    public void RefreshLocalizedStrings() =>
        ToolTipService.SetToolTip(AddTabButton, L.Get("NewTab"));

    public void Refresh(IReadOnlyList<BrowserTabViewModel> tabs, BrowserTabViewModel? selectedTab)
    {
        if (CanUpdateSelectionOnly(tabs))
        {
            _selectedTab = selectedTab;
            var showClose = tabs.Count > 1;

            for (var i = 0; i < _tabItems.Count; i++)
            {
                _tabItems[i].SetSelected(tabs[i] == selectedTab);
                _tabItems[i].SetShowCloseButton(showClose);
            }

            UpdateTabLayout();
            return;
        }

        foreach (var item in _tabItems)
        {
            item.Unsubscribe();
        }

        _tabs = tabs;
        _selectedTab = selectedTab;
        TabHost.Children.Clear();
        _tabItems.Clear();

        var showCloseButton = tabs.Count > 1;

        for (var i = 0; i < tabs.Count; i++)
        {
            var tab = tabs[i];
            var tabItem = new BrowserTabItem { TabOrderIndex = i };
            tabItem.Bind(tab, tab == selectedTab, showCloseButton);
            tabItem.CloseRequested += (_, _) => TabCloseRequested?.Invoke(this, tab);
            tabItem.Selected += (_, _) => TabSelected?.Invoke(this, tab);
            tabItem.PinRequested += (_, _) => TabPinRequested?.Invoke(this, tab);
            tabItem.DuplicateRequested += (_, _) => TabDuplicateRequested?.Invoke(this, tab);
            tabItem.CloseOthersRequested += (_, _) => TabCloseOthersRequested?.Invoke(this, tab);
            tabItem.CloseToRightRequested += (_, _) => TabCloseToRightRequested?.Invoke(this, tab);
            tabItem.MuteRequested += (_, _) => TabMuteRequested?.Invoke(this, tab);
            tabItem.PointerMoved += OnTabPointerMoved;
            tabItem.PointerReleased += OnTabPointerReleased;

            _tabItems.Add(tabItem);
            TabHost.Children.Add(tabItem);
        }

        UpdateTabLayout();
    }

    private bool CanUpdateSelectionOnly(IReadOnlyList<BrowserTabViewModel> tabs)
    {
        if (_tabItems.Count != tabs.Count || _tabs.Count != tabs.Count)
        {
            return false;
        }

        for (var i = 0; i < tabs.Count; i++)
        {
            if (!ReferenceEquals(_tabs[i], tabs[i]))
            {
                return false;
            }
        }

        _tabs = tabs;
        return true;
    }

    private void OnTabPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not BrowserTabItem source || e.Pointer.IsInContact != true)
        {
            return;
        }

        _dragSource = source;
        _dragSourceIndex = _tabItems.IndexOf(source);
    }

    private void OnTabPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_dragSource is null || _dragSourceIndex < 0)
        {
            return;
        }

        var position = e.GetCurrentPoint(TabHost).Position;
        var targetIndex = _dragSourceIndex;
        var offset = 0.0;

        for (var i = 0; i < _tabItems.Count; i++)
        {
            offset += _tabItems[i].ActualWidth + TabSpacing;
            if (position.X < offset)
            {
                targetIndex = i;
                break;
            }
        }

        if (targetIndex != _dragSourceIndex)
        {
            TabReorderRequested?.Invoke(this, (_dragSourceIndex, targetIndex));
        }

        _dragSource = null;
        _dragSourceIndex = -1;
    }

    private void UpdateTabLayout()
    {
        if (_tabItems.Count == 0)
        {
            return;
        }

        var tabWidth = CalculateTabWidth();
        foreach (var tabItem in _tabItems)
        {
            tabItem.SetLayoutWidth(tabWidth);
        }
    }

    private double CalculateTabWidth()
    {
        var stripWidth = StripRoot.ActualWidth;
        if (stripWidth <= 0)
        {
            return PreferredTabWidth;
        }

        var tabCount = _tabItems.Count;
        var available = stripWidth
            - OuterLeftPadding
            - CaptionButtonReserve
            - AddButtonSlotWidth
            - TabHostRightPadding
            - TabSpacing * Math.Max(tabCount - 1, 0);

        available = Math.Max(available, MinTabWidth * tabCount);

        var equalWidth = available / tabCount;
        var width = Math.Min(MaxTabWidth, equalWidth);

        if (tabCount * PreferredTabWidth <= available)
        {
            width = Math.Min(MaxTabWidth, PreferredTabWidth);
        }

        return Math.Clamp(width, MinTabWidth, MaxTabWidth);
    }

    private void ApplyStripTheme()
    {
        StripRoot.Background = (Brush)Application.Current.Resources["NovaTabStripBackgroundBrush"];
        AddTabButton.BorderBrush = (Brush)Application.Current.Resources["NovaTabBorderBrush"];
        AddTabIcon.Foreground = (Brush)Application.Current.Resources["NovaIconForegroundBrush"];
    }
}
