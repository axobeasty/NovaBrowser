using Microsoft.Win32;

namespace NovaBrowser.Installer.Services;

public static class UninstallRegistryService
{
    public static void Register(string installPath, Version version, string uninstallCommand)
    {
        using var key = Registry.CurrentUser.CreateSubKey(InstallPaths.UninstallRegistryKey);
        key.SetValue("DisplayName", "NovaBrowser");
        key.SetValue("DisplayVersion", version.ToString(3));
        key.SetValue("Publisher", "NovaBrowser");
        key.SetValue("InstallLocation", installPath);
        key.SetValue("UninstallString", uninstallCommand);
        key.SetValue("DisplayIcon", InstallPaths.GetBrowserExecutable(installPath));
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
    }

    public static void Unregister()
    {
        Registry.CurrentUser.DeleteSubKeyTree(InstallPaths.UninstallRegistryKey, throwOnMissingSubKey: false);
    }
}
