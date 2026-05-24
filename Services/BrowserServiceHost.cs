using NovaBrowser.Models;

namespace NovaBrowser.Services;

public sealed class BrowserServiceHost
{
    public BrowserServiceHost(SettingsService settingsService)
    {
        SettingsService = settingsService;

        var rootDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            BrowserSettings.AppName);

        var rootStore = new DataStoreService(rootDirectory);
        ProfileService = new ProfileService(rootStore);
        ProfileStore = ProfileService.CreateProfileStore();

        HistoryService = new HistoryService(ProfileStore);
        BookmarkService = new BookmarkService(ProfileStore);
        DownloadService = new DownloadService(ProfileStore);
        SessionService = new SessionService(ProfileStore);
        UserScriptService = new UserScriptService(ProfileStore);
        PasswordService = new PasswordService(ProfileStore);
        TelemetryService = new TelemetryService(ProfileStore);
        SyncService = new SyncService(settingsService, BookmarkService, ProfileStore);
        BrowserImportService = new BrowserImportService(BookmarkService, HistoryService);
        CrashRecoveryService = new CrashRecoveryService(SessionService);
        ClearDataService = new ClearDataService(HistoryService, DownloadService, SessionService, ProfileService);
        WebViewEnvironmentService = new WebViewEnvironmentService();
        AdBlockService = new AdBlockService { IsEnabled = settingsService.Current.AdBlockEnabled };
    }

    public SettingsService SettingsService { get; }

    public ProfileService ProfileService { get; }

    public DataStoreService ProfileStore { get; }

    public HistoryService HistoryService { get; }

    public BookmarkService BookmarkService { get; }

    public DownloadService DownloadService { get; }

    public SessionService SessionService { get; }

    public UserScriptService UserScriptService { get; }

    public PasswordService PasswordService { get; }

    public TelemetryService TelemetryService { get; }

    public SyncService SyncService { get; }

    public BrowserImportService BrowserImportService { get; }

    public CrashRecoveryService CrashRecoveryService { get; }

    public ClearDataService ClearDataService { get; }

    public WebViewEnvironmentService WebViewEnvironmentService { get; }

    public AdBlockService AdBlockService { get; }
}
