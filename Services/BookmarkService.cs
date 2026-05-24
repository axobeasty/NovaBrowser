using System.Text.RegularExpressions;
using NovaBrowser.Models;

namespace NovaBrowser.Services;

public sealed class BookmarkService
{
    private const string BookmarksFile = "bookmarks.json";

    private readonly DataStoreService _store;
    private List<BookmarkEntry> _entries = [];

    public BookmarkService(DataStoreService store)
    {
        _store = store;
        _entries = store.Load(BookmarksFile, new List<BookmarkEntry>());
    }

    public IReadOnlyList<BookmarkEntry> Entries => _entries;

    public BookmarkEntry AddBookmark(string title, string url, Guid? parentId = null)
    {
        var bookmark = new BookmarkEntry
        {
            Title = title.Trim(),
            Url = url.Trim(),
            ParentId = parentId,
            SortOrder = _entries.Count,
        };

        _entries.Add(bookmark);
        Save();
        return bookmark;
    }

    public void RemoveBookmark(Guid id)
    {
        var toRemove = new HashSet<Guid> { id };
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var entry in _entries)
            {
                if (entry.ParentId is Guid parentId && toRemove.Contains(parentId) && toRemove.Add(entry.Id))
                {
                    changed = true;
                }
            }
        }

        _entries.RemoveAll(entry => toRemove.Contains(entry.Id));
        Save();
    }

    public bool IsBookmarked(string url) =>
        _entries.Any(entry => entry.Url.Equals(url, StringComparison.OrdinalIgnoreCase));

    public BookmarkEntry? FindByUrl(string url) =>
        _entries.FirstOrDefault(entry => entry.Url.Equals(url, StringComparison.OrdinalIgnoreCase));

    public void ImportFromHtml(string htmlContent)
    {
        var linkPattern = new Regex(
            @"<A\s+HREF=""([^""]+)""[^>]*>(.*?)</A>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        foreach (Match match in linkPattern.Matches(htmlContent))
        {
            var url = match.Groups[1].Value.Trim();
            var title = Regex.Replace(match.Groups[2].Value, "<.*?>", string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(url) || url.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (IsBookmarked(url))
            {
                continue;
            }

            AddBookmark(string.IsNullOrWhiteSpace(title) ? url : title, url);
        }
    }

    public string ExportToHtml()
    {
        var lines = new List<string>
        {
            "<!DOCTYPE NETSCAPE-Bookmark-file-1>",
            "<META HTTP-EQUIV=\"Content-Type\" CONTENT=\"text/html; charset=UTF-8\">",
            "<TITLE>Bookmarks</TITLE>",
            "<H1>Bookmarks</H1>",
            "<DL><p>",
        };

        foreach (var entry in _entries.OrderBy(entry => entry.SortOrder))
        {
            lines.Add($"    <DT><A HREF=\"{entry.Url}\">{entry.Title}</A>");
        }

        lines.Add("</DL><p>");
        return string.Join(Environment.NewLine, lines);
    }

    private void Save() => _store.Save(BookmarksFile, _entries);
}
