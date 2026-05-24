using Microsoft.UI.Xaml;
using NovaBrowser.Models;
using NovaBrowser.Services;
using NovaBrowser.ViewModels;

namespace NovaBrowser;

public partial class App : Application
{
    private MainPageViewModel? _mainViewModel;
    private readonly List<MainWindow> _windows = [];

    public MainPageViewModel MainViewModel =>
        _mainViewModel ?? throw new InvalidOperationException("Main window is not initialized.");

    public BrowserServiceHost Services { get; private set; } = null!;

    public FeatureWindowService FeatureWindows { get; } = new();

    public SettingsWindowService SettingsWindow { get; } = new();

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
        Services = new BrowserServiceHost(SettingsService);
        Services.TelemetryService.IsEnabled = SettingsService.Current.TelemetryEnabled;
        Services.AdBlockService.IsEnabled = SettingsService.Current.AdBlockEnabled;
        Localization.Initialize(SettingsService);
        InitializeComponent();
        ThemeService.Initialize(SettingsService);

        FeatureWindows.NavigateRequested += url => NavigateActiveTab(url);
        SettingsWindow.SettingsApplied += OnSettingsApplied;

        UnhandledException += OnUnhandledException;
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        Localization.ApplySavedLanguage(persist: false);

        var (isPrivate, initialUrl) = ParseLaunchArguments(args.Arguments);
        Services.ProfileService.ConfigureLaunchMode(isPrivate, SettingsService.Current.ActiveProfileId);
        CreateMainWindow(isPrivate, SettingsService.Current.ActiveProfileId, initialUrl);
    }

    public MainWindow CreateMainWindow(bool isPrivate = false, string? profileId = null, string? initialUrl = null)
    {
        if (isPrivate || !string.IsNullOrWhiteSpace(profileId))
        {
            Services.ProfileService.ConfigureLaunchMode(isPrivate, profileId);
        }

        _mainViewModel = new MainPageViewModel(Services);
        var window = new MainWindow(isPrivate);
        _windows.Add(window);
        Window = window;

        window.Closed += (_, _) =>
        {
            _windows.Remove(window);
            if (_mainViewModel is not null)
            {
                _mainViewModel.SaveSession();
            }

            if (_windows.Count == 0)
            {
                FeatureWindows.CloseAll();
                if (Services.ProfileService.IsPrivateMode)
                {
                    Services.ClearDataService.ClearWebViewData();
                }
            }
        };

        window.NavigateToMainPage();
        ThemeService.ApplySavedSelection();

        if (Window.Content is FrameworkElement root)
        {
            root.ActualThemeChanged += OnRootActualThemeChanged;
        }

        MainViewModel.InitializeSession();

        if (!string.IsNullOrWhiteSpace(initialUrl))
        {
            MainViewModel.OpenUrlInNewTab(initialUrl);
        }

        JumpListService.Configure(BrowserPreferences.HomePage);
        window.Activate();
        return window;
    }

    public void OpenSettings() => SettingsWindow.Open();

    public void OpenFeatureWindow(FeatureWindowKind kind) => FeatureWindows.Open(kind);

    public void NavigateActiveTab(string url)
    {
        if (_mainViewModel?.ActiveTab is not null)
        {
            _mainViewModel.ActiveTab.RequestNavigation(url);
            Window?.Activate();
        }
    }

    private void OnSettingsApplied(object? sender, EventArgs e)
    {
        if (_mainViewModel is null || Window is not MainWindow mainWindow)
        {
            return;
        }

        mainWindow.RefreshAfterSettingsChanged();
    }

    private (bool IsPrivate, string? InitialUrl) ParseLaunchArguments(string? arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return (false, null);
        }

        var isPrivate = arguments.Contains("/private", StringComparison.OrdinalIgnoreCase) ||
                        arguments.Contains("--private", StringComparison.OrdinalIgnoreCase);

        if (arguments.Contains("/newtab", StringComparison.OrdinalIgnoreCase))
        {
            return (isPrivate, BrowserSettings.NewTabPage);
        }

        if (arguments.Contains("/home", StringComparison.OrdinalIgnoreCase))
        {
            return (isPrivate, BrowserPreferences.HomePage);
        }

        if (arguments.StartsWith("/open ", StringComparison.OrdinalIgnoreCase))
        {
            return (isPrivate, arguments[6..].Trim());
        }

        return (isPrivate, null);
    }

    private void OnRootActualThemeChanged(FrameworkElement sender, object args)
    {
        if (SettingsService.Current.ThemeSelection == Models.ThemeSelectionType.System)
        {
            ThemeService.ApplySavedSelection(sender.ActualTheme);
        }
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        Services.TelemetryService.TrackCrash(e.Message);
        if (_mainViewModel is not null)
        {
            _mainViewModel.MarkCrashRecovery();
        }
    }
}
