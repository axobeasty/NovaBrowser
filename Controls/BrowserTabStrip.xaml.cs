using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using NovaBrowser.Helpers;
using NovaBrowser.ViewModels;
using Windows.Foundation;
using WinRT.Interop;

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
    private const double DragThreshold = 6;

    private readonly List<BrowserTabItem> _tabItems = [];
    private IReadOnlyList<BrowserTabViewModel> _tabs = [];
    private BrowserTabViewModel? _selectedTab;

    private BrowserTabItem? _dragTab;
    private int _dragSourceIndex = -1;
    private Point _dragStartPosition;
    private bool _dragStarted;
    private int _insertIndex = -1;
    private uint _capturedPointerId;

    public event EventHandler<BrowserTabViewModel>? TabSelected;
    public event EventHandler<BrowserTabViewModel>? TabCloseRequested;
    public event EventHandler? AddTabRequested;
    public event EventHandler<(int OldIndex, int NewIndex)>? TabReorderRequested;
    public event EventHandler<(BrowserTabViewModel Tab, Point ScreenPosition)>? TabDetachRequested;
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

        ResetDragState();
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

        ResetDragState();

        foreach (var item in _tabItems)
        {
            item.Unsubscribe();
            UnhookDragEvents(item);
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
            HookDragEvents(tabItem);

            _tabItems.Add(tabItem);
            TabHost.Children.Add(tabItem);
        }

        UpdateTabLayout();
    }

    private bool CanUpdateSelectionOnly(IReadOnlyList<BrowserTabViewModel> tabs)
    {
        if (_dragTab is not null || _tabItems.Count != tabs.Count || _tabs.Count != tabs.Count)
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

    private void HookDragEvents(BrowserTabItem tabItem)
    {
        tabItem.PointerPressed += OnTabPointerPressed;
        tabItem.PointerMoved += OnTabPointerMoved;
        tabItem.PointerReleased += OnTabPointerReleased;
        tabItem.PointerCanceled += OnTabPointerCanceled;
    }

    private void UnhookDragEvents(BrowserTabItem tabItem)
    {
        tabItem.PointerPressed -= OnTabPointerPressed;
        tabItem.PointerMoved -= OnTabPointerMoved;
        tabItem.PointerReleased -= OnTabPointerReleased;
        tabItem.PointerCanceled -= OnTabPointerCanceled;
    }

    private void OnTabPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not BrowserTabItem tabItem ||
            !tabItem.CanStartDrag(e.OriginalSource as DependencyObject) ||
            !e.GetCurrentPoint(tabItem).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _dragTab = tabItem;
        _dragSourceIndex = _tabItems.IndexOf(tabItem);
        _dragStartPosition = e.GetCurrentPoint(StripRoot).Position;
        _dragStarted = false;
        _insertIndex = -1;
        _capturedPointerId = e.Pointer.PointerId;
        tabItem.CapturePointer(e.Pointer);
    }

    private void OnTabPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_dragTab is null || sender is not BrowserTabItem tabItem || !ReferenceEquals(_dragTab, tabItem))
        {
            return;
        }

        if (e.Pointer.PointerId != _capturedPointerId)
        {
            return;
        }

        var position = e.GetCurrentPoint(StripRoot).Position;
        var deltaX = position.X - _dragStartPosition.X;
        var deltaY = position.Y - _dragStartPosition.Y;
        var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

        if (!_dragStarted && distance < DragThreshold)
        {
            return;
        }

        if (!_dragStarted)
        {
            _dragStarted = true;
            tabItem.SetDragVisual(true);
        }

        if (IsOutsideTabStrip(position) || IsOutsideWindow(e))
        {
            HideInsertIndicator();
            tabItem.SetDragVisual(true, isDetachPreview: true);
            return;
        }

        tabItem.SetDragVisual(true, isDetachPreview: false);
        _insertIndex = CalculateInsertIndex(position.X);
        UpdateInsertIndicator(_insertIndex);
    }

    private void OnTabPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_dragTab is null || sender is not BrowserTabItem tabItem || !ReferenceEquals(_dragTab, tabItem))
        {
            return;
        }

        if (e.Pointer.PointerId != _capturedPointerId)
        {
            return;
        }

        try
        {
            if (_dragStarted)
            {
                var position = e.GetCurrentPoint(StripRoot).Position;
                var screenPosition = e.GetCurrentPoint(null).Position;

                if (IsOutsideTabStrip(position) || IsOutsideWindow(e))
                {
                    if (_dragSourceIndex >= 0 && _dragSourceIndex < _tabs.Count)
                    {
                        TabDetachRequested?.Invoke(this, (_tabs[_dragSourceIndex], screenPosition));
                    }
                }
                else
                {
                    var targetIndex = CalculateInsertIndex(position.X);
                    if (targetIndex != _dragSourceIndex)
                    {
                        TabReorderRequested?.Invoke(this, (_dragSourceIndex, targetIndex));
                    }
                }
            }
        }
        finally
        {
            ResetDragState();
        }
    }

    private void OnTabPointerCanceled(object sender, PointerRoutedEventArgs e) =>
        ResetDragState();

    private void ResetDragState()
    {
        if (_dragTab is not null)
        {
            _dragTab.SetDragVisual(false);
            _dragTab.ReleasePointerCaptures();
        }

        _dragTab = null;
        _dragSourceIndex = -1;
        _dragStarted = false;
        _insertIndex = -1;
        _capturedPointerId = 0;
        HideInsertIndicator();
    }

    private int CalculateInsertIndex(double pointerX)
    {
        if (_tabItems.Count == 0)
        {
            return 0;
        }

        var offset = 0.0;
        for (var i = 0; i < _tabItems.Count; i++)
        {
            var tabCenter = offset + _tabItems[i].ActualWidth / 2;
            if (pointerX < tabCenter)
            {
                return i;
            }

            offset += _tabItems[i].ActualWidth + TabSpacing;
        }

        return _tabItems.Count - 1;
    }

    private void UpdateInsertIndicator(int index)
    {
        if (index < 0 || index >= _tabItems.Count)
        {
            HideInsertIndicator();
            return;
        }

        var left = 0.0;
        for (var i = 0; i < index; i++)
        {
            left += _tabItems[i].ActualWidth + TabSpacing;
        }

        if (index > _dragSourceIndex)
        {
            left -= TabSpacing;
        }

        InsertIndicator.Margin = new Thickness(left, 0, 0, 4);
        InsertIndicator.Visibility = Visibility.Visible;
    }

    private void HideInsertIndicator() =>
        InsertIndicator.Visibility = Visibility.Collapsed;

    private bool IsOutsideTabStrip(Point positionInStripRoot)
    {
        if (TabHost.ActualWidth <= 0 || TabHost.ActualHeight <= 0)
        {
            return false;
        }

        var transform = TabHost.TransformToVisual(StripRoot);
        var bounds = transform.TransformBounds(new Rect(0, 0, TabHost.ActualWidth, TabHost.ActualHeight));
        const double margin = 12;
        bounds = new Rect(
            bounds.X - margin,
            bounds.Y - margin,
            bounds.Width + margin * 2,
            bounds.Height + margin * 2);

        return !bounds.Contains(positionInStripRoot);
    }

    private bool IsOutsideWindow(PointerRoutedEventArgs e)
    {
        var window = GetHostWindow();
        if (window is null)
        {
            return false;
        }

        var hwnd = WindowNative.GetWindowHandle(window);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        var windowRect = appWindow.Position;
        var windowSize = appWindow.Size;
        var screenPoint = e.GetCurrentPoint(null).Position;

        return screenPoint.X < windowRect.X ||
               screenPoint.Y < windowRect.Y ||
               screenPoint.X > windowRect.X + windowSize.Width ||
               screenPoint.Y > windowRect.Y + windowSize.Height;
    }

    private MainWindow? GetHostWindow() =>
        App.Window as MainWindow;

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
        InsertIndicator.Background = (Brush)Application.Current.Resources["NovaAccentBrush"];
    }
}
