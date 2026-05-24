namespace NovaBrowser.Setup.Common.Models;

public sealed class InstallProgressReport
{
    public required double Progress { get; init; }

    public required string Status { get; init; }
}

public sealed class InstallResult
{
    public required bool Succeeded { get; init; }

    public string? ErrorMessage { get; init; }

    public string? InstallPath { get; init; }
}
