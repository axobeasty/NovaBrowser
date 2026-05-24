using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using NovaBrowser.Controls;
using NovaBrowser.Models;
using NovaBrowser.Services;
using NovaBrowser.ViewModels;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using WinRT.Interop;

namespace NovaBrowser;

public sealed partial class MainWindow : Window
{
    private AppWindow _appWindow = null!;
    private MainPage? _mainPage;

    public MainPageViewModel ViewModel { get; }

    public bool IsPrivateMode { get; }

    public MainPage? MainPage => _mainPage;

    public BrowserTabStrip TabStripControl => TabStrip;

    public MainWindow(bool isPrivate, MainPageViewModel viewModel)
    {
        IsPrivateMode = isPrivate;
        ViewModel = viewModel;
        InitializeComponent();

        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        Title = isPrivate ? "NovaBrowser — Private" : "NovaBrowser";
        ConfigureTitleBar();
        RegisterKeyboardAccelerators();
        AppWindow.SetIcon("Assets/AppIcon.ico");

        if (Application.Current is App app)
        {
            app.ThemeService.ThemeChanged += OnThemeChanged;
            ApplyTitleBarTheme(app.ThemeService.CurrentTheme);
        }
    }

    public void NavigateToMainPage()
    {
        RootFrame.Navigate(typeof(MainPage));
        _mainPage = RootFrame.Content as MainPage;
    }

    public void FocusAddressBar() => _mainPage?.FocusAddressBar();

    public void RefreshAfterSettingsChanged() => _mainPage?.RefreshAfterSettingsChanged();

    public void MoveToScreenPoint(Point screenPoint)
    {
        const int offsetX = 24;
        const int offsetY = 16;
        var x = Math.Max(0, (int)screenPoint.X - offsetX);
        var y = Math.Max(0, (int)screenPoint.Y - offsetY);
        _appWindow.Move(new Windows.Graphics.PointInt32(x, y));
    }

    private void ConfigureTitleBar()
    {
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TabStrip);

        var titleBar = _appWindow.TitleBar;
        titleBar.ExtendsContentIntoTitleBar = true;
        titleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
        titleBar.PreferredHeightOption = TitleBarHeightOption.Standard;
    }

    private void RegisterKeyboardAccelerators()
    {
        if (Content is not UIElement root)
        {
            return;
        }

        RegisterAccelerator(root, VirtualKey.T, VirtualKeyModifiers.Control, () => ViewModel.NewTabCommand.Execute(null));
        RegisterAccelerator(root, VirtualKey.N, VirtualKeyModifiers.Control, () => CreateNewWindow());
        RegisterAccelerator(root, VirtualKey.W, VirtualKeyModifiers.Control, () => ViewModel.CloseActiveTab());
        RegisterAccelerator(root, VirtualKey.Tab, VirtualKeyModifiers.Control, () => ViewModel.SelectRelativeTab(1));
        RegisterAccelerator(
            root,
            VirtualKey.Tab,
            VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
            () => ViewModel.SelectRelativeTab(-1));
        RegisterAccelerator(
            root,
            VirtualKey.T,
            VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
            () => ViewModel.ReopenClosedTab());
        RegisterAccelerator(root, VirtualKey.D, VirtualKeyModifiers.Control, () => ViewModel.ToggleBookmarkForActiveTab());
        RegisterAccelerator(root, VirtualKey.F, VirtualKeyModifiers.Control, () => ViewModel.ToggleFindBar());
        RegisterAccelerator(root, VirtualKey.H, VirtualKeyModifiers.Control, () => OpenFeatureWindow(FeatureWindowKind.History));
        RegisterAccelerator(root, VirtualKey.J, VirtualKeyModifiers.Control, () => OpenFeatureWindow(FeatureWindowKind.Downloads));
        RegisterAccelerator(root, VirtualKey.B, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, () => OpenFeatureWindow(FeatureWindowKind.Bookmarks));
        RegisterAccelerator(root, VirtualKey.L, VirtualKeyModifiers.Control, () => FocusAddressBar());
        RegisterAccelerator(root, VirtualKey.Add, VirtualKeyModifiers.Control, () => ViewModel.ZoomActiveTab(0.1));
        RegisterAccelerator(root, VirtualKey.Subtract, VirtualKeyModifiers.Control, () => ViewModel.ZoomActiveTab(-0.1));
        RegisterAccelerator(root, VirtualKey.Number0, VirtualKeyModifiers.Control, () => ViewModel.ResetZoomActiveTab());
        RegisterAccelerator(root, VirtualKey.F5, VirtualKeyModifiers.None, () => ViewModel.ReloadCommand.Execute(null));
        RegisterAccelerator(root, VirtualKey.F6, VirtualKeyModifiers.None, () => FocusAddressBar());
        RegisterAccelerator(root, VirtualKey.F12, VirtualKeyModifiers.None, () => ViewModel.ActiveTab?.RequestDevTools());
        RegisterAccelerator(root, VirtualKey.P, VirtualKeyModifiers.Control, () => ViewModel.ActiveTab?.RequestPrint());

        for (var i = 0; i < 9; i++)
        {
            var tabNumber = i + 1;
            RegisterAccelerator(
                root,
                (VirtualKey)((int)VirtualKey.Number1 + i),
                VirtualKeyModifiers.Control,
                () => ViewModel.SelectTabByNumber(tabNumber));
        }
    }

    private static void OpenFeatureWindow(FeatureWindowKind kind)
    {
        if (Application.Current is App app)
        {
            app.OpenFeatureWindow(kind);
        }
    }

    private static void CreateNewWindow()
    {
        if (Application.Current is App app)
        {
            app.CreateMainWindow(app.Services.ProfileService.IsPrivateMode);
        }
    }

    private static void RegisterAccelerator(
        UIElement target,
        VirtualKey key,
        VirtualKeyModifiers modifiers,
        Action handler)
    {
        var accelerator = new KeyboardAccelerator
        {
            Key = key,
            Modifiers = modifiers,
        };

        accelerator.Invoked += (_, args) =>
        {
            handler();
            args.Handled = true;
        };

        target.KeyboardAccelerators.Add(accelerator);
    }

    private void OnThemeChanged(object? sender, BrowserTheme theme) =>
        ApplyTitleBarTheme(theme);

    private void ApplyTitleBarTheme(BrowserTheme theme)
    {
        if (!AppWindowTitleBar.IsCustomizationSupported())
        {
            return;
        }

        var background = ThemeService.GetTitleBarBackgroundColor(theme);
        var foreground = ThemeService.GetTitleBarForegroundColor(theme);
        var (hover, pressed) = ThemeService.GetTitleBarButtonColors(theme);
        var titleBar = _appWindow.TitleBar;

        titleBar.BackgroundColor = IsPrivateMode ? Color.FromArgb(255, 36, 18, 36) : background;
        titleBar.InactiveBackgroundColor = titleBar.BackgroundColor;
        titleBar.ForegroundColor = foreground;
        titleBar.ButtonBackgroundColor = Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        titleBar.ButtonForegroundColor = foreground;
        titleBar.ButtonInactiveForegroundColor = Color.FromArgb(160, foreground.R, foreground.G, foreground.B);
        titleBar.ButtonHoverBackgroundColor = hover;
        titleBar.ButtonPressedBackgroundColor = pressed;
    }
}
