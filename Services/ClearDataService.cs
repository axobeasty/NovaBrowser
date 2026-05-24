namespace NovaBrowser.Services;

public sealed class ClearDataService
{
    private readonly HistoryService _historyService;
    private readonly DownloadService _downloadService;
    private readonly SessionService _sessionService;
    private readonly ProfileService _profileService;

    public ClearDataService(
        HistoryService historyService,
        DownloadService downloadService,
        SessionService sessionService,
        ProfileService profileService)
    {
        _historyService = historyService;
        _downloadService = downloadService;
        _sessionService = sessionService;
        _profileService = profileService;
    }

    public void ClearHistory(TimeSpan? olderThan = null) => _historyService.Clear(olderThan);

    public void ClearDownloads() => _downloadService.ClearCompleted();

    public void ClearSession() => _sessionService.ClearSession();

    public void ClearWebViewData()
    {
        var webViewDirectory = Path.Combine(_profileService.ProfileRootDirectory, "WebView2");
        if (Directory.Exists(webViewDirectory))
        {
            try
            {
                Directory.Delete(webViewDirectory, recursive: true);
            }
            catch
            {
                // Best effort.
            }
        }
    }

    public void ClearAllBrowsingData()
    {
        ClearHistory();
        ClearDownloads();
        ClearSession();
        ClearWebViewData();
    }
}
