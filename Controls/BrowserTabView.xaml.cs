using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using NovaBrowser.Models;
using NovaBrowser.ViewModels;

namespace NovaBrowser.Controls;

public sealed partial class BrowserTabView : UserControl
{
    private bool _isInitialized;
    private BrowserTabViewModel? _viewModel;

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

        await InitializeWebViewAsync();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        UnsubscribeFromViewModel(_viewModel);
    }

    private async Task InitializeWebViewAsync()
    {
        await WebView.EnsureCoreWebView2Async();
        _isInitialized = true;

        var core = WebView.CoreWebView2;
        core.Settings.AreDevToolsEnabled = true;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.AreDefaultContextMenusEnabled = true;

        core.NavigationStarting += OnNavigationStarting;
        core.NavigationCompleted += OnNavigationCompleted;
        core.DocumentTitleChanged += OnDocumentTitleChanged;
        core.SourceChanged += OnSourceChanged;
        core.HistoryChanged += OnHistoryChanged;
        core.NewWindowRequested += OnNewWindowRequested;

        if (_viewModel is not null && !string.IsNullOrEmpty(_viewModel.Url))
        {
            NavigateInternal(_viewModel.Url);
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
            var startPagePath = Path.Combine(AppContext.BaseDirectory, "Assets", "start.html");
            WebView.CoreWebView2.Navigate($"file:///{startPagePath.Replace('\\', '/')}");
            return;
        }

        WebView.CoreWebView2.Navigate(url);
    }

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

    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        ViewModel.IsLoading = true;
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        ViewModel.IsLoading = false;
        SyncViewModelFromWebView();
    }

    private void OnDocumentTitleChanged(object? sender, object e) => SyncViewModelFromWebView();

    private void OnSourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e) => SyncViewModelFromWebView();

    private void OnHistoryChanged(object? sender, object e) => SyncViewModelFromWebView();

    private void SyncViewModelFromWebView()
    {
        if (!_isInitialized)
        {
            return;
        }

        var core = WebView.CoreWebView2;
        var source = core.Source;

        if (source.StartsWith("file:///", StringComparison.OrdinalIgnoreCase) &&
            source.Contains("start.html", StringComparison.OrdinalIgnoreCase))
        {
            source = BrowserSettings.NewTabPage;
        }

        ViewModel.UpdateFromWebView(
            core.DocumentTitle,
            source,
            core.CanGoBack,
            core.CanGoForward,
            source.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
    }

    private void OnNewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        e.Handled = true;
        if (App.Current is App app && app.MainViewModel is not null)
        {
            app.MainViewModel.OpenUrlInNewTab(e.Uri);
        }
    }
}
