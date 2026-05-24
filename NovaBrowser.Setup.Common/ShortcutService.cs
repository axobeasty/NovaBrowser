namespace NovaBrowser.Setup.Common;

public static class ShortcutService
{
    public static void CreateDesktopShortcut(string targetPath, string workingDirectory)
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        CreateShortcut(Path.Combine(desktop, "NovaBrowser.lnk"), targetPath, workingDirectory);
    }

    public static void CreateStartMenuShortcut(string targetPath, string workingDirectory)
    {
        var startMenu = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
        var folder = Path.Combine(startMenu, "Programs", "NovaBrowser");
        Directory.CreateDirectory(folder);
        CreateShortcut(Path.Combine(folder, "NovaBrowser.lnk"), targetPath, workingDirectory);
    }

    public static void RemoveDesktopShortcut()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "NovaBrowser.lnk");
        DeleteIfExists(path);
    }

    public static void RemoveStartMenuShortcuts()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            "Programs",
            "NovaBrowser");

        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string workingDirectory)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("WScript.Shell недоступен.");

        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetPath;
        shortcut.WorkingDirectory = workingDirectory;
        shortcut.Description = "NovaBrowser";
        shortcut.Save();
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
