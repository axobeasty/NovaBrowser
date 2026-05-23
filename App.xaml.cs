using Microsoft.UI.Xaml;
using NovaBrowser.Services;
using NovaBrowser.ViewModels;

namespace NovaBrowser;

public partial class App : Application
{
    public MainPageViewModel MainViewModel { get; } = new();

    public UpdateCoordinator UpdateCoordinator { get; } = new();

    public SettingsService SettingsService { get; } = new();

    public ThemeService ThemeService { get; } = new();

    public static Window Window { get; private set; } = null!;

    public static Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue { get; private set; } = null!;

    public static nint WindowHandle =>
        WinRT.Interop.WindowNative.GetWindowHandle(Window);

    public App()
    {
        InitializeComponent();
        SettingsService.Load();
        ThemeService.Initialize(SettingsService);
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        ThemeService.ApplySavedSelection();

        Window = new MainWindow();
        DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        if (Window.Content is FrameworkElement root)
        {
            root.ActualThemeChanged += OnRootActualThemeChanged;
        }

        Window.Activate();

        if (Window.Content is FrameworkElement activatedRoot)
        {
            _ = UpdateCoordinator.CheckSilentlyOnStartupAsync(activatedRoot.XamlRoot);
        }
    }

    private void OnRootActualThemeChanged(FrameworkElement sender, object args)
    {
        if (SettingsService.Current.ThemeSelection == Models.ThemeSelectionType.System)
        {
            ThemeService.ApplySavedSelection(sender.ActualTheme);
        }
    }
}
