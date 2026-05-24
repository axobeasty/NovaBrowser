using NovaBrowser.Models;

namespace NovaBrowser.Services;

public sealed class BrowserImportService
{
    private readonly BookmarkService _bookmarkService;
    private readonly HistoryService _historyService;

    public BrowserImportService(BookmarkService bookmarkService, HistoryService historyService)
    {
        _bookmarkService = bookmarkService;
        _historyService = historyService;
    }

    public void ImportBookmarksHtml(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        _bookmarkService.ImportFromHtml(File.ReadAllText(filePath));
    }

    public int ImportChromeBookmarks(string? chromeProfilePath = null)
    {
        chromeProfilePath ??= Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Google", "Chrome", "User Data", "Default", "Bookmarks");

        if (!File.Exists(chromeProfilePath))
        {
            return 0;
        }

        var before = _bookmarkService.Entries.Count;
        var json = File.ReadAllText(chromeProfilePath);
        ImportChromeBookmarkJson(json);
        return _bookmarkService.Entries.Count - before;
    }

    public int ImportEdgeBookmarks()
    {
        var edgePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "Edge", "User Data", "Default", "Bookmarks");

        if (!File.Exists(edgePath))
        {
            return 0;
        }

        var before = _bookmarkService.Entries.Count;
        ImportChromeBookmarkJson(File.ReadAllText(edgePath));
        return _bookmarkService.Entries.Count - before;
    }

    private void ImportChromeBookmarkJson(string json)
    {
        using var document = System.Text.Json.JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("roots", out var roots))
        {
            return;
        }

        foreach (var root in roots.EnumerateObject())
        {
            if (root.Value.TryGetProperty("children", out var children))
            {
                ImportChromeNodes(children);
            }
        }
    }

    private void ImportChromeNodes(System.Text.Json.JsonElement nodes)
    {
        foreach (var node in nodes.EnumerateArray())
        {
            if (node.TryGetProperty("type", out var typeElement) &&
                typeElement.GetString() == "url" &&
                node.TryGetProperty("url", out var urlElement))
            {
                var url = urlElement.GetString() ?? string.Empty;
                var title = node.TryGetProperty("name", out var nameElement)
                    ? nameElement.GetString() ?? url
                    : url;

                if (!string.IsNullOrWhiteSpace(url) && !_bookmarkService.IsBookmarked(url))
                {
                    _bookmarkService.AddBookmark(title, url);
                }
            }

            if (node.TryGetProperty("children", out var children))
            {
                ImportChromeNodes(children);
            }
        }
    }
}
