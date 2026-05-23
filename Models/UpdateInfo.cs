namespace NovaBrowser.Models;

public sealed class UpdateInfo
{
    public required Version Version { get; init; }

    public required string TagName { get; init; }

    public required string ReleaseNotes { get; init; }

    public required Uri DownloadUrl { get; init; }

    public required string AssetName { get; init; }

    public long AssetSize { get; init; }
}

public enum UpdateCheckStatus
{
    UpToDate,
    UpdateAvailable,
    Skipped,
    Failed,
}

public sealed class UpdateCheckResult
{
    public required UpdateCheckStatus Status { get; init; }

    public required Version CurrentVersion { get; init; }

    public UpdateInfo? Update { get; init; }

    public string? ErrorMessage { get; init; }
}

public sealed class UpdateDownloadResult
{
    public required bool Succeeded { get; init; }

    public string? PackagePath { get; init; }

    public string? ErrorMessage { get; init; }
}
