namespace NovaBrowser.Installer.Models;

public sealed class InstallSettings
{
    public string InstallPath { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NovaBrowser");

    public bool CreateDesktopShortcut { get; set; } = true;

    public bool CreateStartMenuShortcut { get; set; } = true;

    public bool LaunchAfterInstall { get; set; } = true;
}

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
