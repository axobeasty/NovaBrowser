using System.Collections.ObjectModel;
using NovaBrowser.Models;

namespace NovaBrowser.Services;

public sealed class DownloadService
{
    private const string DownloadsFile = "downloads.json";

    private readonly DataStoreService _store;

    public ObservableCollection<DownloadEntry> Items { get; } = [];

    public event EventHandler<DownloadEntry>? DownloadUpdated;

    public DownloadService(DataStoreService store)
    {
        _store = store;
        foreach (var entry in store.Load(DownloadsFile, new List<DownloadEntry>()))
        {
            Items.Add(entry);
        }
    }

    public DownloadEntry StartDownload(string sourceUrl, string fileName, string destinationPath, long totalBytes)
    {
        var entry = new DownloadEntry
        {
            SourceUrl = sourceUrl,
            FileName = fileName,
            FilePath = destinationPath,
            TotalBytes = totalBytes,
            State = DownloadState.InProgress,
        };

        Items.Insert(0, entry);
        Save();
        DownloadUpdated?.Invoke(this, entry);
        return entry;
    }

    public void UpdateProgress(Guid id, long receivedBytes, long? totalBytes = null)
    {
        var entry = Items.FirstOrDefault(item => item.Id == id);
        if (entry is null)
        {
            return;
        }

        entry.ReceivedBytes = receivedBytes;
        if (totalBytes is > 0)
        {
            entry.TotalBytes = totalBytes.Value;
        }

        DownloadUpdated?.Invoke(this, entry);
    }

    public void CompleteDownload(Guid id)
    {
        var entry = Items.FirstOrDefault(item => item.Id == id);
        if (entry is null)
        {
            return;
        }

        entry.State = DownloadState.Completed;
        entry.CompletedAt = DateTime.UtcNow;
        Save();
        DownloadUpdated?.Invoke(this, entry);
    }

    public void FailDownload(Guid id)
    {
        var entry = Items.FirstOrDefault(item => item.Id == id);
        if (entry is null)
        {
            return;
        }

        entry.State = DownloadState.Failed;
        entry.CompletedAt = DateTime.UtcNow;
        Save();
        DownloadUpdated?.Invoke(this, entry);
    }

    public void ClearCompleted()
    {
        var completed = Items.Where(item => item.State is DownloadState.Completed or DownloadState.Failed or DownloadState.Cancelled).ToList();
        foreach (var entry in completed)
        {
            Items.Remove(entry);
        }

        Save();
    }

    private void Save() => _store.Save(DownloadsFile, Items.ToList());
}
