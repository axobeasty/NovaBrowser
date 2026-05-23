using Microsoft.UI.Xaml;
using NovaBrowser.Services;
using NovaBrowser.ViewModels;

namespace NovaBrowser;

public partial class App : Application
{
    private MainPageViewModel? _mainViewModel;

    public MainPageViewModel MainViewModel => _mainViewModel ??= new();

    public UpdateCoordinator UpdateCoordinator { get; } = new();

    public SettingsService SettingsService { get; } = new();

    public BrowserPreferencesService BrowserPreferences { get; private set; } = null!;

    public ThemeService ThemeService { get; } = new();

    public LocalizationService Localization { get; } = new();

    public static Window Window { get; private set; } = null!;

    public static Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue { get; private set; } = null!;

    public static nint WindowHandle =>
        WinRT.Interop.WindowNative.GetWindowHandle(Window);

    public App()
    {
        SettingsService.Load();
        BrowserPreferences = new BrowserPreferencesService(SettingsService);
        Localization.Initialize(SettingsService);
        Localization.ApplySavedLanguage(persist: false);
        InitializeComponent();
        ThemeService.Initialize(SettingsService);
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        Window = new MainWindow();
        DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        ThemeService.ApplySavedSelection();

        if (Window.Content is FrameworkElement root)
        {
            root.ActualThemeChanged += OnRootActualThemeChanged;
        }

        Window.Activate();
    }

    private void OnRootActualThemeChanged(FrameworkElement sender, object args)
    {
        if (SettingsService.Current.ThemeSelection == Models.ThemeSelectionType.System)
        {
            ThemeService.ApplySavedSelection(sender.ActualTheme);
        }
    }
}
