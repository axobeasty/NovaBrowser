using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using NovaBrowser.Models;
using NovaBrowser.Services;
using NovaBrowser.ViewModels;

namespace NovaBrowser.Controls;

public sealed partial class BrowserTabView : UserControl
{
    private bool _isInitialized;
    private BrowserTabViewModel? _viewModel;

    public event EventHandler<(string Title, string Url)>? PageVisited;

    public BrowserTabViewModel ViewModel
    {
        get => _viewModel ?? throw new InvalidOperationException("ViewModel is not set.");
        set
        {
            if (_viewModel == value)
            {
                return;
            }

            UnsubscribeFromViewModel(_viewModel);
            _viewModel = value;
            SubscribeToViewModel(_viewModel);
        }
    }

    public BrowserTabView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isInitialized)
        {
            return;
        }

        if (Application.Current is App app)
        {
            app.ThemeService.ThemeChanged += OnAppThemeChanged;
            app.Localization.LanguageChanged += OnLanguageChanged;
        }

        await InitializeWebViewAsync();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (Application.Current is App app)
        {
            app.ThemeService.ThemeChanged -= OnAppThemeChanged;
            app.Localization.LanguageChanged -= OnLanguageChanged;
        }

        UnsubscribeFromViewModel(_viewModel);
    }

    private void OnAppThemeChanged(object? sender, BrowserTheme e)
    {
        ApplyWebViewColorScheme();
        ApplyStartPageThemeIfNeeded();
    }

    private void OnLanguageChanged(object? sender, EventArgs e) =>
        RefreshStartPageIfNeeded();

    public void RefreshStartPageIfNeeded()
    {
        if (!_isInitialized)
        {
            return;
        }

        if (ViewModel.Url.Equals(BrowserSettings.NewTabPage, StringComparison.OrdinalIgnoreCase))
        {
            NavigateInternal(BrowserSettings.NewTabPage);
        }
    }

    private async Task InitializeWebViewAsync()
    {
        if (Application.Current is not App app)
        {
            return;
        }

        var environment = await app.Services.WebViewEnvironmentService.GetEnvironmentAsync(app.Services.ProfileService);
        await WebView.EnsureCoreWebView2Async(environment);
        _isInitialized = true;

        var core = WebView.CoreWebView2;
        core.Settings.AreDevToolsEnabled = true;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.AreDefaultContextMenusEnabled = true;
        core.Settings.AreBrowserAcceleratorKeysEnabled = true;

        core.NavigationStarting += OnNavigationStarting;
        core.NavigationCompleted += OnNavigationCompleted;
        core.DocumentTitleChanged += OnDocumentTitleChanged;
        core.SourceChanged += OnSourceChanged;
        core.HistoryChanged += OnHistoryChanged;
        core.NewWindowRequested += OnNewWindowRequested;
        core.FaviconChanged += OnFaviconChanged;
        core.DownloadStarting += OnDownloadStarting;
        core.ProcessFailed += OnProcessFailed;

        if (app.Services.AdBlockService.IsEnabled)
        {
            core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
            core.WebResourceRequested += OnWebResourceRequested;
        }

        ApplyWebViewColorScheme();
        await InjectUserScriptsAsync(core);

        if (_viewModel is not null && !string.IsNullOrEmpty(_viewModel.Url))
        {
            NavigateInternal(_viewModel.Url);
        }
    }

    private async Task InjectUserScriptsAsync(CoreWebView2 core)
    {
        if (Application.Current is not App app)
        {
            return;
        }

        foreach (var script in app.Services.UserScriptService.Scripts.Where(script => script.IsEnabled))
        {
            await core.AddScriptToExecuteOnDocumentCreatedAsync(script.Script);
        }
    }

    private void SubscribeToViewModel(BrowserTabViewModel? viewModel)
    {
        if (viewModel is null)
        {
            return;
        }

        viewModel.NavigationRequested += NavigateInternal;
        viewModel.GoBackRequested += GoBackInternal;
        viewModel.GoForwardRequested += GoForwardInternal;
        viewModel.ReloadRequested += ReloadInternal;
        viewModel.DevToolsRequested += OpenDevToolsInternal;
        viewModel.PrintRequested += PrintInternal;
        viewModel.ZoomRequested += ZoomInternal;
        viewModel.FindRequested += FindInternal;
        viewModel.FindStopRequested += FindStopInternal;
        viewModel.ReadingModeRequested += ReadingModeInternal;
        viewModel.TranslateRequested += TranslateInternal;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void UnsubscribeFromViewModel(BrowserTabViewModel? viewModel)
    {
        if (viewModel is null)
        {
            return;
        }

        viewModel.NavigationRequested -= NavigateInternal;
        viewModel.GoBackRequested -= GoBackInternal;
        viewModel.GoForwardRequested -= GoForwardInternal;
        viewModel.ReloadRequested -= ReloadInternal;
        viewModel.DevToolsRequested -= OpenDevToolsInternal;
        viewModel.PrintRequested -= PrintInternal;
        viewModel.ZoomRequested -= ZoomInternal;
        viewModel.FindRequested -= FindInternal;
        viewModel.FindStopRequested -= FindStopInternal;
        viewModel.ReadingModeRequested -= ReadingModeInternal;
        viewModel.TranslateRequested -= TranslateInternal;
        viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BrowserTabViewModel.IsLoading))
        {
            LoadingBar.Visibility = ViewModel.IsLoading ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void NavigateInternal(string url)
    {
        if (!_isInitialized)
        {
            return;
        }

        if (url.Equals(BrowserSettings.NewTabPage, StringComparison.OrdinalIgnoreCase))
        {
            var startPagePath = GetStartPagePath();
            WebView.CoreWebView2.Navigate($"file:///{startPagePath.Replace('\\', '/')}");
            return;
        }

        WebView.CoreWebView2.Navigate(url);
    }

    private static string GetStartPagePath()
    {
        var fileName = Application.Current is App app
            ? app.Localization.GetStartPageFileName()
            : "start.html";

        return Path.Combine(AppContext.BaseDirectory, "Assets", fileName);
    }

    private static bool IsStartPageSource(string source) =>
        source.Contains("start.html", StringComparison.OrdinalIgnoreCase) ||
        source.Contains("start.en.html", StringComparison.OrdinalIgnoreCase);

    private void GoBackInternal()
    {
        if (_isInitialized && WebView.CoreWebView2.CanGoBack)
        {
            WebView.CoreWebView2.GoBack();
        }
    }

    private void GoForwardInternal()
    {
        if (_isInitialized && WebView.CoreWebView2.CanGoForward)
        {
            WebView.CoreWebView2.GoForward();
        }
    }

    private void ReloadInternal()
    {
        if (!_isInitialized)
        {
            return;
        }

        if (ViewModel.Url.Equals(BrowserSettings.NewTabPage, StringComparison.OrdinalIgnoreCase))
        {
            NavigateInternal(BrowserSettings.NewTabPage);
            return;
        }

        WebView.CoreWebView2.Reload();
    }

    private void OpenDevToolsInternal() => WebView.CoreWebView2?.OpenDevToolsWindow();

    private void PrintInternal() => WebView.CoreWebView2?.ShowPrintUI(CoreWebView2PrintDialogKind.System);

    private void ZoomInternal(double factor) =>
        _ = WebView.CoreWebView2?.ExecuteScriptAsync($"document.body.style.zoom = '{factor.ToString(System.Globalization.CultureInfo.InvariantCulture)}';");

    private void FindInternal(string text, bool forward, bool matchCase)
    {
        if (!_isInitialized || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var escaped = System.Text.Json.JsonSerializer.Serialize(text);
        var script = $"window.find({escaped}, {matchCase.ToString().ToLowerInvariant()}, false, {forward.ToString().ToLowerInvariant()});";
        _ = WebView.CoreWebView2.ExecuteScriptAsync(script);
    }

    private void FindStopInternal() =>
        _ = WebView.CoreWebView2?.ExecuteScriptAsync("window.getSelection()?.removeAllRanges();");

    private async void ReadingModeInternal()
    {
        if (_isInitialized)
        {
            _ = await WebView.CoreWebView2.ExecuteScriptAsync(ReadingModeService.InjectScript);
        }
    }

    private void TranslateInternal()
    {
        if (!_isInitialized)
        {
            return;
        }

        var source = WebView.CoreWebView2.Source;
        if (!string.IsNullOrWhiteSpace(source) &&
            !source.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
        {
            NavigateInternal(TranslationService.BuildTranslateUrl(source));
        }
    }

    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e) =>
        ViewModel.IsLoading = true;

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        ViewModel.IsLoading = false;
        SyncViewModelFromWebView();
        ApplyStartPageThemeIfNeeded();
        ApplyStartPageSearchEngineIfNeeded();

        if (e.IsSuccess)
        {
            PageVisited?.Invoke(this, (ViewModel.Title, ViewModel.Url));
        }
    }

    private void OnDocumentTitleChanged(object? sender, object e) => SyncViewModelFromWebView();

    private void OnSourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e) => SyncViewModelFromWebView();

    private void OnHistoryChanged(object? sender, object e) => SyncViewModelFromWebView();

    private void OnFaviconChanged(object? sender, object e)
    {
        if (!_isInitialized)
        {
            return;
        }

        ViewModel.FaviconUri = WebView.CoreWebView2.FaviconUri;
    }

    private void OnDownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
    {
        if (Application.Current is not App app)
        {
            return;
        }

        var download = e.DownloadOperation;
        var fileName = string.IsNullOrWhiteSpace(download.ResultFilePath)
            ? Path.GetFileName(new Uri(download.Uri).LocalPath)
            : Path.GetFileName(download.ResultFilePath);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = "download.bin";
        }

        var downloadDirectory = app.Services.SettingsService.Current.DownloadDirectory;
        if (string.IsNullOrWhiteSpace(downloadDirectory))
        {
            downloadDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads");
        }

        Directory.CreateDirectory(downloadDirectory);
        var destination = Path.Combine(downloadDirectory, fileName);
        e.ResultFilePath = destination;

        var entry = app.Services.DownloadService.StartDownload(download.Uri, fileName, destination, download.TotalBytesToReceive);
        download.BytesReceivedChanged += (_, _) =>
        {
            app.Services.DownloadService.UpdateProgress(entry.Id, (long)download.BytesReceived, download.TotalBytesToReceive);
        };

        download.StateChanged += (_, _) =>
        {
            if (download.State == CoreWebView2DownloadState.Completed)
            {
                app.Services.DownloadService.CompleteDownload(entry.Id);
            }
            else if (download.State == CoreWebView2DownloadState.Interrupted)
            {
                app.Services.DownloadService.FailDownload(entry.Id);
            }
        };
    }

    private void OnProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
    {
        if (Application.Current is App app)
        {
            app.Services.TelemetryService.TrackCrash(e.ProcessFailedKind.ToString());
        }

        ReloadInternal();
    }

    private void OnWebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        if (Application.Current is not App app || !app.Services.AdBlockService.ShouldBlock(e.Request.Uri))
        {
            return;
        }

        e.Response = WebView.CoreWebView2.Environment.CreateWebResourceResponse(null, 404, "Blocked", "Content-Type: text/plain");
    }

    private void SyncViewModelFromWebView()
    {
        if (!_isInitialized)
        {
            return;
        }

        var core = WebView.CoreWebView2;
        var source = core.Source;

        if (source.StartsWith("file:///", StringComparison.OrdinalIgnoreCase) &&
            IsStartPageSource(source))
        {
            source = BrowserSettings.NewTabPage;
        }

        ViewModel.UpdateFromWebView(
            core.DocumentTitle,
            source,
            core.CanGoBack,
            core.CanGoForward,
            source.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

        ViewModel.FaviconUri = core.FaviconUri;
    }

    private void OnNewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        e.Handled = true;
        if (Application.Current is App app)
        {
            app.MainViewModel.OpenUrlInNewTab(e.Uri);
        }
    }

    private void ApplyStartPageThemeIfNeeded()
    {
        if (!_isInitialized)
        {
            return;
        }

        var source = WebView.CoreWebView2.Source;
        if (!IsStartPageSource(source))
        {
            return;
        }

        if (Application.Current is not App app)
        {
            return;
        }

        var script = app.ThemeService.BuildStartPageThemeScript(app.ThemeService.CurrentTheme);
        _ = WebView.CoreWebView2.ExecuteScriptAsync(script);
    }

    private void ApplyStartPageSearchEngineIfNeeded()
    {
        if (!_isInitialized || Application.Current is not App app)
        {
            return;
        }

        var source = WebView.CoreWebView2.Source;
        if (!IsStartPageSource(source))
        {
            return;
        }

        var searchUrl = app.BrowserPreferences.SearchTemplateUrl;
        var escaped = searchUrl.Replace("'", "\\'", StringComparison.Ordinal);
        var script = $"window.__novaSearchUrl = '{escaped}';";
        _ = WebView.CoreWebView2.ExecuteScriptAsync(script);
    }

    private void ApplyWebViewColorScheme()
    {
        if (!_isInitialized || Application.Current is not App app)
        {
            return;
        }

        var isLight = ThemeService.IsLightTheme(app.ThemeService.CurrentTheme);
        WebView.CoreWebView2.Profile.PreferredColorScheme = isLight
            ? CoreWebView2PreferredColorScheme.Light
            : CoreWebView2PreferredColorScheme.Dark;

        WebView.DefaultBackgroundColor = isLight
            ? global::Windows.UI.Color.FromArgb(255, 255, 255, 255)
            : global::Windows.UI.Color.FromArgb(255, 18, 21, 28);
    }
}
