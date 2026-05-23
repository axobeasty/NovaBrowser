using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NovaBrowser.Controls;
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

        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(25, 255, 255, 255);
            titleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(45, 255, 255, 255);
        }
    }
}
