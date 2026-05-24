using CommunityToolkit.Mvvm.ComponentModel;
using NovaBrowser.Helpers;
using NovaBrowser.Models;
using NovaBrowser.Services;

namespace NovaBrowser.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly LocalizationService _localizationService;
    private readonly BrowserServiceHost _services;

    public ThemeSettingsViewModel Theme { get; }

    [ObservableProperty]
    private string _homePage = string.Empty;

    [ObservableProperty]
    private string _searchEngineId = "bing";

    [ObservableProperty]
    private string _customSearchEngineUrl = string.Empty;

    [ObservableProperty]
    private string _uiLanguage = LocalizationService.SystemLanguage;

    [ObservableProperty]
    private bool _isCustomSearchEngineVisible;

    [ObservableProperty]
    private SessionRestoreMode _sessionRestore = SessionRestoreMode.Continue;

    [ObservableProperty]
    private bool _showBookmarkBar;

    [ObservableProperty]
    private bool _adBlockEnabled = true;

    [ObservableProperty]
    private bool _telemetryEnabled;

    [ObservableProperty]
    private string _downloadDirectory = string.Empty;

    [ObservableProperty]
    private string _activeProfileId = "default";

    public SettingsViewModel(
        SettingsService settingsService,
        ThemeService themeService,
        LocalizationService localizationService,
        BrowserServiceHost services)
    {
        _settingsService = settingsService;
        _localizationService = localizationService;
        _services = services;
        Theme = new ThemeSettingsViewModel(settingsService, themeService);
        LoadFromSettings();
    }

    public IReadOnlyList<LanguageOption> GetLanguageOptions() =>
        _localizationService.GetLanguageOptions();

    public IReadOnlyList<SearchEngineOption> GetSearchEngineOptions()
    {
        var options = SearchEngineCatalog.Presets
            .Select(preset => new SearchEngineOption(preset.Id, L.Get(preset.NameKey)))
            .ToList();

        options.Add(new SearchEngineOption(SearchEngineCatalog.CustomId, L.Get("SearchEngineCustom")));
        return options;
    }

    public IReadOnlyList<SessionRestoreOption> GetSessionRestoreOptions() =>
    [
        new SessionRestoreOption(SessionRestoreMode.Continue, L.Get("SessionRestoreContinue")),
        new SessionRestoreOption(SessionRestoreMode.HomePage, L.Get("SessionRestoreHome")),
        new SessionRestoreOption(SessionRestoreMode.Ask, L.Get("SessionRestoreAsk")),
    ];

    public IReadOnlyList<UserProfile> Profiles => _services.ProfileService.Profiles;

    public void LoadFromSettings()
    {
        var settings = _settingsService.Current;
        HomePage = settings.HomePage;
        SearchEngineId = SearchEngineCatalog.NormalizeId(settings.SearchEngineId);
        CustomSearchEngineUrl = settings.CustomSearchEngineUrl;
        UiLanguage = settings.UiLanguage;
        SessionRestore = settings.SessionRestore;
        ShowBookmarkBar = settings.ShowBookmarkBar;
        AdBlockEnabled = settings.AdBlockEnabled;
        TelemetryEnabled = settings.TelemetryEnabled;
        DownloadDirectory = settings.DownloadDirectory;
        ActiveProfileId = settings.ActiveProfileId;
        UpdateCustomSearchVisibility();
    }

    public void SaveAllSettings()
    {
        SaveGeneralSettings();
        SaveProfileSettings();
    }

    public void SaveProfileSettings()
    {
        _services.ProfileService.SwitchProfile(ActiveProfileId);
        _settingsService.Current.ActiveProfileId = ActiveProfileId;
        _settingsService.Save();
    }

    public void SaveGeneralSettings()
    {
        var settings = _settingsService.Current;
        var previousLanguage = settings.UiLanguage;

        settings.HomePage = HomePage.Trim();
        settings.SearchEngineId = SearchEngineCatalog.NormalizeId(SearchEngineId);
        settings.CustomSearchEngineUrl = CustomSearchEngineUrl.Trim();
        settings.UiLanguage = UiLanguage;
        settings.SessionRestore = SessionRestore;
        settings.ShowBookmarkBar = ShowBookmarkBar;
        settings.AdBlockEnabled = AdBlockEnabled;
        settings.TelemetryEnabled = TelemetryEnabled;
        settings.DownloadDirectory = DownloadDirectory.Trim();
        settings.ActiveProfileId = ActiveProfileId;
        _settingsService.Save();

        _services.AdBlockService.IsEnabled = AdBlockEnabled;
        _services.TelemetryService.IsEnabled = TelemetryEnabled;

        if (!string.Equals(previousLanguage, UiLanguage, StringComparison.Ordinal))
        {
            _localizationService.SetLanguage(UiLanguage);
        }
    }

    public void ClearBrowsingData() => _services.ClearDataService.ClearAllBrowsingData();

    public void ClearHistoryOnly() => _services.ClearDataService.ClearHistory();

    public void ClearDownloadsOnly() => _services.ClearDataService.ClearDownloads();

    public void ImportChromeBookmarks() => _services.BrowserImportService.ImportChromeBookmarks();

    public void ImportEdgeBookmarks() => _services.BrowserImportService.ImportEdgeBookmarks();

    public void ExportSync(string filePath) => _services.SyncService.ExportToFile(_services.ProfileService.ActiveProfile.Id, filePath);

    public void ImportSync(string filePath) => _services.SyncService.ImportFromFile(filePath, mergeBookmarks: true);

    public void OpenDefaultAppsSettings() => DefaultBrowserService.OpenDefaultAppsSettings();

    public void SwitchProfile(string profileId) => _services.ProfileService.SwitchProfile(profileId);

    public UserProfile CreateProfile(string name) => _services.ProfileService.CreateProfile(name);

    partial void OnSearchEngineIdChanged(string value) =>
        UpdateCustomSearchVisibility();

    private void UpdateCustomSearchVisibility() =>
        IsCustomSearchEngineVisible = SearchEngineId == SearchEngineCatalog.CustomId;
}

public sealed record SearchEngineOption(string Id, string Title);

public sealed record SessionRestoreOption(SessionRestoreMode Mode, string Title);
