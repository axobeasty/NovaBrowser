using CommunityToolkit.Mvvm.ComponentModel;
using NovaBrowser.Helpers;
using NovaBrowser.Models;
using NovaBrowser.Services;

namespace NovaBrowser.ViewModels;

public partial class BrowserTabViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

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

    [ObservableProperty]
    private bool _isPinned;

    [ObservableProperty]
    private bool _isMuted;

    [ObservableProperty]
    private string? _groupId;

    [ObservableProperty]
    private string? _groupColor;

    [ObservableProperty]
    private string? _faviconUri;

    [ObservableProperty]
    private double _zoomFactor = 1.0;

    public Guid Id { get; } = Guid.NewGuid();

    public event Action<string>? NavigationRequested;
    public event Action? GoBackRequested;
    public event Action? GoForwardRequested;
    public event Action? ReloadRequested;
    public event Action? DevToolsRequested;
    public event Action? PrintRequested;
    public event Action<double>? ZoomRequested;
    public event Action<string, bool, bool>? FindRequested;
    public event Action? FindStopRequested;
    public event Action? ReadingModeRequested;
    public event Action? TranslateRequested;

    public BrowserTabViewModel()
    {
        Title = L.Get("NewTabTitle");
    }

    public SessionTabEntry ToSessionEntry() => new()
    {
        Url = Url,
        IsPinned = IsPinned,
        GroupId = GroupId,
        GroupColor = GroupColor,
        IsMuted = IsMuted,
    };

    public void ApplySessionEntry(SessionTabEntry entry)
    {
        Url = entry.Url;
        IsPinned = entry.IsPinned;
        GroupId = entry.GroupId;
        GroupColor = entry.GroupColor;
        IsMuted = entry.IsMuted;
        AddressBarText = UrlNormalizer.GetDisplayUrl(Url);
        if (Url.Equals(BrowserSettings.NewTabPage, StringComparison.OrdinalIgnoreCase))
        {
            Title = L.Get("NewTabTitle");
        }
    }

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

    public void RequestDevTools() => DevToolsRequested?.Invoke();

    public void RequestPrint() => PrintRequested?.Invoke();

    public void RequestZoom(double factor) => ZoomRequested?.Invoke(factor);

    public void RequestFind(string text, bool forward, bool matchCase) =>
        FindRequested?.Invoke(text, forward, matchCase);

    public void RequestFindStop() => FindStopRequested?.Invoke();

    public void RequestReadingMode() => ReadingModeRequested?.Invoke();

    public void RequestTranslate() => TranslateRequested?.Invoke();

    public void UpdateFromWebView(string title, string url, bool canGoBack, bool canGoForward, bool isSecure)
    {
        Title = string.IsNullOrWhiteSpace(title) ? L.Get("AppFallbackTitle") : title;
        Url = url;
        AddressBarText = UrlNormalizer.GetDisplayUrl(url);
        CanGoBack = canGoBack;
        CanGoForward = canGoForward;
        IsSecure = isSecure;
    }

    public void RefreshLocalizedTitle()
    {
        if (Url.Equals(BrowserSettings.NewTabPage, StringComparison.OrdinalIgnoreCase))
        {
            Title = L.Get("NewTabTitle");
        }
    }
}
