using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using NovaBrowser.ViewModels;

namespace NovaBrowser.Controls;

public sealed partial class BrowserTabStrip : UserControl
{
    private readonly Button _addTabButton;
    private IReadOnlyList<BrowserTabViewModel> _tabs = [];
    private BrowserTabViewModel? _selectedTab;

    public event EventHandler<BrowserTabViewModel>? TabSelected;
    public event EventHandler<BrowserTabViewModel>? TabCloseRequested;
    public event EventHandler? AddTabRequested;

    public BrowserTabStrip()
    {
        InitializeComponent();

        _addTabButton = new Button
        {
            Width = 32,
            Height = 32,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(2, 0, 0, 2),
            Style = (Style)Application.Current.Resources["NovaTabAddButtonStyle"],
            Content = new FontIcon
            {
                Glyph = "\uE710",
                FontSize = 11,
            },
        };

        _addTabButton.Click += (_, _) => AddTabRequested?.Invoke(this, EventArgs.Empty);
        ToolTipService.SetToolTip(_addTabButton, "Новая вкладка");
    }

    public void Refresh(IReadOnlyList<BrowserTabViewModel> tabs, BrowserTabViewModel? selectedTab)
    {
        _tabs = tabs;
        _selectedTab = selectedTab;

        TabHost.Children.Clear();

        foreach (var tab in tabs)
        {
            TabHost.Children.Add(CreateTabElement(tab, tab == selectedTab));
        }

        TabHost.Children.Add(_addTabButton);
    }

    private UIElement CreateTabElement(BrowserTabViewModel tab, bool isSelected)
    {
        var isDark = ActualTheme != ElementTheme.Light;

        var title = new TextBlock
        {
            Text = tab.Title,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 12,
            Opacity = isSelected ? 1 : 0.82,
        };

        tab.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(BrowserTabViewModel.Title))
            {
                title.Text = tab.Title;
            }
        };

        var closeButton = new Button
        {
            Width = 22,
            Height = 22,
            Padding = new Thickness(0),
            Margin = new Thickness(6, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Style = (Style)Application.Current.Resources["NovaTabCloseButtonStyle"],
            Visibility = _tabs.Count > 1 ? Visibility.Visible : Visibility.Collapsed,
            Opacity = 0.75,
            Content = new FontIcon
            {
                Glyph = "\uE711",
                FontSize = 8,
            },
        };

        closeButton.Click += (_, _) => TabCloseRequested?.Invoke(this, tab);

        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Auto },
            },
            Children =
            {
                title,
                closeButton,
            },
        };

        Grid.SetColumn(title, 0);
        Grid.SetColumn(closeButton, 1);

        var accentLine = new Border
        {
            Height = 2,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = (Brush)Application.Current.Resources["NovaAccentBrush"],
            CornerRadius = new CornerRadius(2, 2, 0, 0),
            Visibility = isSelected ? Visibility.Visible : Visibility.Collapsed,
        };

        var content = new Grid
        {
            Children =
            {
                header,
                accentLine,
            },
        };

        var root = new Border
        {
            MinWidth = 128,
            MaxWidth = 220,
            Height = 34,
            Padding = new Thickness(14, 6, 8, 4),
            CornerRadius = new CornerRadius(10, 10, 0, 0),
            Background = GetTabBackground(isSelected, isDark, hovered: false),
            BorderBrush = (Brush)Application.Current.Resources["NovaTabBorderBrush"],
            BorderThickness = isSelected ? new Thickness(1, 0, 1, 0) : new Thickness(1),
            Child = content,
            Tag = tab,
        };

        root.PointerPressed += (_, e) =>
        {
            if (e.GetCurrentPoint(root).Properties.IsLeftButtonPressed)
            {
                TabSelected?.Invoke(this, tab);
            }
        };

        if (!isSelected)
        {
            root.PointerEntered += (_, _) => root.Background = GetTabBackground(false, isDark, hovered: true);
            root.PointerExited += (_, _) => root.Background = GetTabBackground(false, isDark, hovered: false);
        }

        return root;
    }

    private static SolidColorBrush GetTabBackground(bool isSelected, bool isDark, bool hovered)
    {
        if (isSelected)
        {
            return new SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 42, 45, 58) : Windows.UI.Color.FromArgb(255, 255, 255, 255));
        }

        if (hovered)
        {
            return new SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 34, 37, 48) : Windows.UI.Color.FromArgb(255, 245, 245, 245));
        }

        return new SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(180, 26, 29, 40) : Windows.UI.Color.FromArgb(140, 240, 240, 240));
    }
}
