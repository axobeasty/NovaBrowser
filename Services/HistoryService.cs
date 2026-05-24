using NovaBrowser.Models;

namespace NovaBrowser.Services;

public sealed class HistoryService
{
    private const string HistoryFile = "history.json";
    private const int MaxEntries = 5000;

    private readonly DataStoreService _store;
    private List<HistoryEntry> _entries = [];

    public HistoryService(DataStoreService store)
    {
        _store = store;
        _entries = store.Load(HistoryFile, new List<HistoryEntry>());
    }

    public IReadOnlyList<HistoryEntry> Entries => _entries;

    public void AddVisit(string title, string url)
    {
        if (string.IsNullOrWhiteSpace(url) ||
            url.Equals(BrowserSettings.NewTabPage, StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _entries.Insert(0, new HistoryEntry
        {
            Title = string.IsNullOrWhiteSpace(title) ? url : title,
            Url = url,
            VisitedAt = DateTime.UtcNow,
        });

        if (_entries.Count > MaxEntries)
        {
            _entries = _entries.Take(MaxEntries).ToList();
        }

        Save();
    }

    public IEnumerable<HistoryEntry> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return _entries;
        }

        return _entries.Where(entry =>
            entry.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            entry.Url.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    public void DeleteEntry(Guid id)
    {
        _entries.RemoveAll(entry => entry.Id == id);
        Save();
    }

    public void Clear(TimeSpan? olderThan = null)
    {
        if (olderThan is null)
        {
            _entries.Clear();
        }
        else
        {
            var threshold = DateTime.UtcNow - olderThan.Value;
            _entries.RemoveAll(entry => entry.VisitedAt < threshold);
        }

        Save();
    }

    private void Save() => _store.Save(HistoryFile, _entries);
}
