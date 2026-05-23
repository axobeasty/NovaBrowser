using CommunityToolkit.Mvvm.ComponentModel;
using NovaBrowser.Models;
using NovaBrowser.Services;

namespace NovaBrowser.ViewModels;

public partial class BrowserTabViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "Новая вкладка";

    [ObservableProperty]
    private string _url = BrowserSettings.NewTabPage;

    [ObservableProperty]
    private string _addressBarText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _canGoBack;

    [ObservableProperty]
    private bool _canGoForward;

    [ObservableProperty]
    private bool _isSecure;

    public Guid Id { get; } = Guid.NewGuid();

    public event Action<string>? NavigationRequested;
    public event Action? GoBackRequested;
    public event Action? GoForwardRequested;
    public event Action? ReloadRequested;

    public void RequestNavigation(string? input = null)
    {
        var target = UrlNormalizer.Normalize(input ?? AddressBarText);
        Url = target;
        AddressBarText = UrlNormalizer.GetDisplayUrl(target);
        NavigationRequested?.Invoke(target);
    }

    public void RequestGoBack() => GoBackRequested?.Invoke();

    public void RequestGoForward() => GoForwardRequested?.Invoke();

    public void RequestReload() => ReloadRequested?.Invoke();

    public void UpdateFromWebView(string title, string url, bool canGoBack, bool canGoForward, bool isSecure)
    {
        Title = string.IsNullOrWhiteSpace(title) ? "NovaBrowser" : title;
        Url = url;
        AddressBarText = UrlNormalizer.GetDisplayUrl(url);
        CanGoBack = canGoBack;
        CanGoForward = canGoForward;
        IsSecure = isSecure;
    }
}
