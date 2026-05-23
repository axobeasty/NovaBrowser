using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using NovaBrowser.Helpers;
using NovaBrowser.ViewModels;

namespace NovaBrowser.Controls;

public sealed partial class BrowserTabStrip : UserControl
{
    private readonly Button _addTabButton;
    private readonly FontIcon _addTabIcon;
    private readonly List<BrowserTabItem> _tabItems = [];
    private IReadOnlyList<BrowserTabViewModel> _tabs = [];
    private BrowserTabViewModel? _selectedTab;

    public event EventHandler<BrowserTabViewModel>? TabSelected;
    public event EventHandler<BrowserTabViewModel>? TabCloseRequested;
    public event EventHandler? AddTabRequested;

    public BrowserTabStrip()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;

        _addTabIcon = new FontIcon
        {
            Glyph = "\uE710",
            FontSize = 10,
        };

        _addTabButton = new Button
        {
            Width = 28,
            Height = 28,
            Margin = new Thickness(4, 0, 0, 4),
            VerticalAlignment = VerticalAlignment.Bottom,
            Style = (Style)Application.Current.Resources["NovaTabAddButtonStyle"],
            Content = _addTabIcon,
        };

        _addTabButton.Click += (_, _) => AddTabRequested?.Invoke(this, EventArgs.Empty);
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
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (Application.Current is App app)
        {
            app.ThemeService.ThemeChanged -= OnThemeChanged;
            app.Localization.LanguageChanged -= OnLanguageChanged;
        }
    }

    private void OnThemeChanged(object? sender, Models.BrowserTheme e) => ApplyStripTheme();

    private void OnLanguageChanged(object? sender, EventArgs e) => RefreshLocalizedStrings();

    public void RefreshLocalizedStrings() =>
        ToolTipService.SetToolTip(_addTabButton, L.Get("NewTab"));

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

        foreach (var tab in tabs)
        {
            var tabItem = new BrowserTabItem();
            tabItem.Bind(tab, tab == selectedTab, showCloseButton);
            tabItem.CloseRequested += (_, _) => TabCloseRequested?.Invoke(this, tab);
            tabItem.Selected += (_, _) => TabSelected?.Invoke(this, tab);

            _tabItems.Add(tabItem);
            TabHost.Children.Add(tabItem);
        }

        TabHost.Children.Add(_addTabButton);
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

    private void ApplyStripTheme()
    {
        StripRoot.Background = (Brush)Application.Current.Resources["NovaTabStripBackgroundBrush"];
        _addTabButton.BorderBrush = (Brush)Application.Current.Resources["NovaTabBorderBrush"];
        _addTabIcon.Foreground = (Brush)Application.Current.Resources["NovaIconForegroundBrush"];
    }
}
