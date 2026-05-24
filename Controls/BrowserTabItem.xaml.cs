using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using NovaBrowser.Helpers;
using NovaBrowser.ViewModels;

namespace NovaBrowser.Controls;

public sealed partial class BrowserTabItem : UserControl
{
    private BrowserTabViewModel? _tab;
    private bool _isSelected;
    private bool _showCloseButton;
    private bool _isPointerOver;
    private bool _isCompact;
    private double _layoutWidth = 200;

    public event EventHandler? CloseRequested;
    public event EventHandler? Selected;
    public event EventHandler? PinRequested;
    public event EventHandler? DuplicateRequested;
    public event EventHandler? CloseOthersRequested;
    public event EventHandler? CloseToRightRequested;
    public event EventHandler? MuteRequested;

    public int TabOrderIndex { get; set; }

    public BrowserTabItem()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Application.Current is App app)
        {
            app.ThemeService.ThemeChanged += OnThemeChanged;
        }

        ApplyVisualState();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (Application.Current is App app)
        {
            app.ThemeService.ThemeChanged -= OnThemeChanged;
        }

        Unsubscribe();
    }

    private void OnThemeChanged(object? sender, Models.BrowserTheme e) => ApplyVisualState();

    public void Bind(BrowserTabViewModel tab, bool isSelected, bool showCloseButton)
    {
        Unsubscribe();

        _tab = tab;
        _isSelected = isSelected;
        _showCloseButton = showCloseButton;
        _tab.PropertyChanged += OnTabPropertyChanged;

        UpdateContent();
        ApplyVisualState();
    }

    public void SetSelected(bool isSelected)
    {
        _isSelected = isSelected;
        _isPointerOver = false;
        ApplyVisualState();
    }

    public void SetShowCloseButton(bool showCloseButton)
    {
        _showCloseButton = showCloseButton;
        ApplyVisualState();
    }

    public void SetLayoutWidth(double width)
    {
        _layoutWidth = width;
        _isCompact = width < 88;
        Width = width;
        ApplyVisualState();
    }

    public bool CanStartDrag(DependencyObject? source) =>
        !IsCloseButtonSource(source);

    public void SetDragVisual(bool isDragging, bool isDetachPreview = false)
    {
        Opacity = isDragging ? (isDetachPreview ? 0.55 : 0.72) : 1;
        TabRoot.RenderTransform = isDragging
            ? new TranslateTransform { Y = isDetachPreview ? 4 : 0 }
            : null;
    }

    public void Unsubscribe()
    {
        if (_tab is not null)
        {
            _tab.PropertyChanged -= OnTabPropertyChanged;
            _tab = null;
        }
    }

    private void OnTabPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(BrowserTabViewModel.Title)
            or nameof(BrowserTabViewModel.IsLoading)
            or nameof(BrowserTabViewModel.IsSecure)
            or nameof(BrowserTabViewModel.FaviconUri)
            or nameof(BrowserTabViewModel.IsPinned)
            or nameof(BrowserTabViewModel.IsMuted)
            or nameof(BrowserTabViewModel.GroupColor))
        {
            UpdateContent();
        }
    }

    private void UpdateContent()
    {
        if (_tab is null)
        {
            return;
        }

        TitleText.Text = _tab.IsMuted ? $"🔇 {_tab.Title}" : _tab.Title;
        ToolTipService.SetToolTip(TabRoot, _tab.Title);
        AccentBar.Background = string.IsNullOrWhiteSpace(_tab.GroupColor)
            ? GetBrush("NovaAccentBrush")
            : new SolidColorBrush(ParseColor(_tab.GroupColor));

        if (_tab.IsLoading)
        {
            LoadingRing.Visibility = Visibility.Visible;
            LoadingRing.IsActive = true;
            SiteIcon.Visibility = Visibility.Collapsed;
            FaviconImage.Visibility = Visibility.Collapsed;
        }
        else if (!string.IsNullOrWhiteSpace(_tab.FaviconUri))
        {
            LoadingRing.IsActive = false;
            LoadingRing.Visibility = Visibility.Collapsed;
            SiteIcon.Visibility = Visibility.Collapsed;
            FaviconImage.Visibility = Visibility.Visible;
            FaviconImage.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(_tab.FaviconUri));
        }
        else
        {
            LoadingRing.IsActive = false;
            LoadingRing.Visibility = Visibility.Collapsed;
            FaviconImage.Visibility = Visibility.Collapsed;
            SiteIcon.Visibility = Visibility.Visible;
            SiteIcon.Glyph = _tab.IsSecure ? "\uE72E" : "\uE774";
        }
    }

    private static global::Windows.UI.Color ParseColor(string value)
    {
        value = value.TrimStart('#');
        if (value.Length == 6 &&
            byte.TryParse(value[..2], System.Globalization.NumberStyles.HexNumber, null, out var r) &&
            byte.TryParse(value.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out var g) &&
            byte.TryParse(value.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out var b))
        {
            return global::Windows.UI.Color.FromArgb(255, r, g, b);
        }

        return global::Windows.UI.Color.FromArgb(255, 108, 92, 231);
    }

    private void ApplyVisualState()
    {
        var activeBg = GetBrush("NovaTabActiveBackgroundBrush");
        var activeBorder = GetBrush("NovaTabActiveBorderBrush");
        var inactiveBg = GetBrush("NovaTabInactiveBackgroundBrush");
        var hoverBg = GetBrush("NovaTabHoverBackgroundBrush");
        var hoverBorder = GetBrush("NovaTabHoverBorderBrush");
        var border = GetBrush("NovaTabBorderBrush");

        if (_isSelected)
        {
            TabRoot.Height = 36;
            TabRoot.Padding = _isCompact ? new Thickness(8, 0, 6, 0) : new Thickness(12, 0, 8, 0);
            TabRoot.CornerRadius = new CornerRadius(10, 10, 0, 0);
            TabRoot.BorderThickness = new Thickness(1, 1, 1, 0);
            TabRoot.Background = activeBg;
            TabRoot.BorderBrush = activeBorder;

            AccentBar.Opacity = 1;
            TitleText.Visibility = _isCompact ? Visibility.Collapsed : Visibility.Visible;
            TitleText.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
            TitleText.Opacity = 1;
            TitleText.Foreground = GetBrush("NovaTextPrimaryBrush");
            SiteIcon.Opacity = 1;
            SiteIcon.Foreground = GetBrush("NovaIconForegroundBrush");
            ApplyCloseButtonForeground();

            CloseButton.Visibility = _showCloseButton && _tab?.IsPinned != true ? Visibility.Visible : Visibility.Collapsed;
            return;
        }

        TabRoot.Height = 32;
        TabRoot.Padding = _isCompact ? new Thickness(6, 0, 4, 0) : new Thickness(10, 0, 6, 0);
        TabRoot.CornerRadius = new CornerRadius(8);
        TabRoot.BorderThickness = new Thickness(1);
        AccentBar.Opacity = 0;
        TitleText.Visibility = _isCompact ? Visibility.Collapsed : Visibility.Visible;
        TitleText.FontWeight = Microsoft.UI.Text.FontWeights.Normal;
        TitleText.Foreground = GetBrush("NovaTextPrimaryBrush");
        SiteIcon.Foreground = GetBrush("NovaIconForegroundBrush");
        ApplyCloseButtonForeground();

        if (_isPointerOver)
        {
            TabRoot.Background = hoverBg;
            TabRoot.BorderBrush = hoverBorder;
            TitleText.Opacity = 1;
            SiteIcon.Opacity = 0.92;
            CloseButton.Visibility = _showCloseButton && _tab?.IsPinned != true ? Visibility.Visible : Visibility.Collapsed;
        }
        else
        {
            TabRoot.Background = inactiveBg;
            TabRoot.BorderBrush = border;
            TitleText.Opacity = 0.88;
            SiteIcon.Opacity = 0.78;
            CloseButton.Visibility = Visibility.Collapsed;
        }
    }

    private void ApplyCloseButtonForeground()
    {
        if (CloseButton.Content is FontIcon icon)
        {
            icon.Foreground = GetBrush("NovaIconForegroundBrush");
        }
    }

    private static Brush GetBrush(string key) =>
        (Brush)Application.Current.Resources[key];

    private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _isPointerOver = true;
        if (!_isSelected)
        {
            ApplyVisualState();
        }
    }

    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        _isPointerOver = false;
        if (!_isSelected)
        {
            ApplyVisualState();
        }
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (IsCloseButtonSource(e.OriginalSource as DependencyObject))
        {
            return;
        }

        Selected?.Invoke(this, EventArgs.Empty);
    }

    private void OnRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        var flyout = new MenuFlyout();
        var isPinned = _tab?.IsPinned == true;
        var isMuted = _tab?.IsMuted == true;

        AddMenuItem(flyout, L.Get(isPinned ? "TabContextUnpin" : "TabContextPin"), () => PinRequested?.Invoke(this, EventArgs.Empty));
        AddMenuItem(flyout, L.Get("TabContextDuplicate"), () => DuplicateRequested?.Invoke(this, EventArgs.Empty));
        AddMenuItem(flyout, L.Get(isMuted ? "TabContextUnmute" : "TabContextMute"), () => MuteRequested?.Invoke(this, EventArgs.Empty));
        flyout.Items.Add(new MenuFlyoutSeparator());
        AddMenuItem(flyout, L.Get("TabContextCloseOthers"), () => CloseOthersRequested?.Invoke(this, EventArgs.Empty));
        AddMenuItem(flyout, L.Get("TabContextCloseToRight"), () => CloseToRightRequested?.Invoke(this, EventArgs.Empty));
        flyout.ShowAt(this, e.GetPosition(this));
    }

    private static void AddMenuItem(MenuFlyout flyout, string text, Action handler)
    {
        var item = new MenuFlyoutItem { Text = text };
        item.Click += (_, _) => handler();
        flyout.Items.Add(item);
    }

    private void OnTapped(object sender, TappedRoutedEventArgs e)
    {
        if (IsCloseButtonSource(e.OriginalSource as DependencyObject))
        {
            e.Handled = true;
        }
    }

    private bool IsCloseButtonSource(DependencyObject? source) =>
        FindParent<Button>(source) == CloseButton;

    private void OnCloseClick(object sender, RoutedEventArgs e) =>
        CloseRequested?.Invoke(this, EventArgs.Empty);

    private static T? FindParent<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child is not null)
        {
            if (child is T match)
            {
                return match;
            }

            child = VisualTreeHelper.GetParent(child);
        }

        return null;
    }
}
