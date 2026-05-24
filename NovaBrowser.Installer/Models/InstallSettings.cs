namespace NovaBrowser.Installer.Models;

public sealed class InstallSettings
{
    public string InstallPath { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NovaBrowser");

    public bool CreateDesktopShortcut { get; set; } = true;

    public bool CreateStartMenuShortcut { get; set; } = true;

    public bool LaunchAfterInstall { get; set; } = true;
}
