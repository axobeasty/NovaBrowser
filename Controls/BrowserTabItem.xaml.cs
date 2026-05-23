using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using NovaBrowser.ViewModels;

namespace NovaBrowser.Controls;

public sealed partial class BrowserTabItem : UserControl
{
    private BrowserTabViewModel? _tab;
    private bool _isSelected;
    private bool _showCloseButton;
    private bool _isPointerOver;

    public event EventHandler? CloseRequested;
    public event EventHandler? Selected;

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
            or nameof(BrowserTabViewModel.IsSecure))
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

        TitleText.Text = _tab.Title;

        if (_tab.IsLoading)
        {
            LoadingRing.Visibility = Visibility.Visible;
            LoadingRing.IsActive = true;
            SiteIcon.Visibility = Visibility.Collapsed;
        }
        else
        {
            LoadingRing.IsActive = false;
            LoadingRing.Visibility = Visibility.Collapsed;
            SiteIcon.Visibility = Visibility.Visible;
            SiteIcon.Glyph = _tab.IsSecure ? "\uE72E" : "\uE774";
        }
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
            TabRoot.MinWidth = 148;
            TabRoot.Padding = new Thickness(12, 0, 8, 0);
            TabRoot.CornerRadius = new CornerRadius(10, 10, 0, 0);
            TabRoot.BorderThickness = new Thickness(1, 1, 1, 0);
            TabRoot.Background = activeBg;
            TabRoot.BorderBrush = activeBorder;

            AccentBar.Opacity = 1;
            TitleText.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
            TitleText.Opacity = 1;
            SiteIcon.Opacity = 1;

            CloseButton.Visibility = _showCloseButton ? Visibility.Visible : Visibility.Collapsed;
            return;
        }

        TabRoot.Height = 32;
        TabRoot.MinWidth = 140;
        TabRoot.Padding = new Thickness(10, 0, 6, 0);
        TabRoot.CornerRadius = new CornerRadius(8);
        TabRoot.BorderThickness = new Thickness(1);
        AccentBar.Opacity = 0;
        TitleText.FontWeight = Microsoft.UI.Text.FontWeights.Normal;
        TitleText.Opacity = 0.78;
        SiteIcon.Opacity = 0.72;

        if (_isPointerOver)
        {
            TabRoot.Background = hoverBg;
            TabRoot.BorderBrush = hoverBorder;
            TitleText.Opacity = 0.92;
            CloseButton.Visibility = _showCloseButton ? Visibility.Visible : Visibility.Collapsed;
        }
        else
        {
            TabRoot.Background = inactiveBg;
            TabRoot.BorderBrush = border;
            CloseButton.Visibility = Visibility.Collapsed;
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
