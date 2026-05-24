namespace NovaBrowser.Models;

public sealed class HistoryEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime VisitedAt { get; set; } = DateTime.UtcNow;
}

public sealed class BookmarkEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ParentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class DownloadEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long TotalBytes { get; set; }
    public long ReceivedBytes { get; set; }
    public DownloadState State { get; set; } = DownloadState.InProgress;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

public enum DownloadState
{
    InProgress,
    Completed,
    Failed,
    Cancelled,
}

public sealed class SessionTabEntry
{
    public string Url { get; set; } = BrowserSettings.NewTabPage;
    public bool IsPinned { get; set; }
    public string? GroupId { get; set; }
    public string? GroupColor { get; set; }
    public bool IsMuted { get; set; }
}

public sealed class UserProfile
{
    public string Id { get; set; } = "default";
    public string Name { get; set; } = "Default";
    public string AvatarColor { get; set; } = "#6C5CE7";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class UserScript
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string MatchPattern { get; set; } = "*://*/*";
    public string Script { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}

public sealed class SavedPasswordEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Site { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string ResourceKey { get; set; } = string.Empty;
}

public sealed class ClosedTabEntry
{
    public string Url { get; set; } = BrowserSettings.NewTabPage;
    public string Title { get; set; } = string.Empty;
}

public sealed class SyncPayload
{
    public string ProfileId { get; set; } = "default";
    public AppSettings? Settings { get; set; }
    public List<BookmarkEntry> Bookmarks { get; set; } = [];
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
}
