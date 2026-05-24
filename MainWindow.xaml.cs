using Microsoft.UI;

using Microsoft.UI.Windowing;

using Microsoft.UI.Xaml;

using Microsoft.UI.Xaml.Controls;

using NovaBrowser.Controls;

using NovaBrowser.Models;

using NovaBrowser.Services;

using Windows.UI;

using WinRT.Interop;



namespace NovaBrowser;



public sealed partial class MainWindow : Window

{

    private AppWindow _appWindow = null!;



    public BrowserTabStrip TabStripControl => TabStrip;



    public MainWindow()

    {

        InitializeComponent();



        var hwnd = WindowNative.GetWindowHandle(this);

        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);

        _appWindow = AppWindow.GetFromWindowId(windowId);



        ConfigureTitleBar();

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



        titleBar.BackgroundColor = background;

        titleBar.InactiveBackgroundColor = background;

        titleBar.ForegroundColor = foreground;

        titleBar.ButtonBackgroundColor = Colors.Transparent;

        titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

        titleBar.ButtonForegroundColor = foreground;

        titleBar.ButtonInactiveForegroundColor = Color.FromArgb(160, foreground.R, foreground.G, foreground.B);

        titleBar.ButtonHoverBackgroundColor = hover;

        titleBar.ButtonPressedBackgroundColor = pressed;

    }

}

