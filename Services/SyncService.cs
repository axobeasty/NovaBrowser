using NovaBrowser.Models;

namespace NovaBrowser.Services;

public sealed class SyncService
{
    private readonly SettingsService _settingsService;
    private readonly BookmarkService _bookmarkService;
    private readonly DataStoreService _store;

    public SyncService(SettingsService settingsService, BookmarkService bookmarkService, DataStoreService store)
    {
        _settingsService = settingsService;
        _bookmarkService = bookmarkService;
        _store = store;
    }

    public string ExportToJson(string profileId)
    {
        var payload = new SyncPayload
        {
            ProfileId = profileId,
            Settings = _settingsService.Current,
            Bookmarks = _bookmarkService.Entries.ToList(),
            ExportedAt = DateTime.UtcNow,
        };

        return System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
        });
    }

    public void ExportToFile(string profileId, string filePath) =>
        File.WriteAllText(filePath, ExportToJson(profileId));

    public void ImportFromFile(string filePath, bool mergeBookmarks)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        var payload = System.Text.Json.JsonSerializer.Deserialize<SyncPayload>(File.ReadAllText(filePath));
        if (payload?.Settings is not null)
        {
            _settingsService.Current.HomePage = payload.Settings.HomePage;
            _settingsService.Current.SearchEngineId = payload.Settings.SearchEngineId;
            _settingsService.Current.CustomSearchEngineUrl = payload.Settings.CustomSearchEngineUrl;
            _settingsService.Current.UiLanguage = payload.Settings.UiLanguage;
            _settingsService.Save();
        }

        if (mergeBookmarks)
        {
            foreach (var bookmark in payload?.Bookmarks ?? [])
            {
                if (!_bookmarkService.IsBookmarked(bookmark.Url))
                {
                    _bookmarkService.AddBookmark(bookmark.Title, bookmark.Url);
                }
            }
        }
    }

    public void SaveLocalBackup(string profileId) =>
        _store.Save($"sync-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json", ExportToJson(profileId));
}
