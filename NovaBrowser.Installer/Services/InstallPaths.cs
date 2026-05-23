using System.Reflection;
using Microsoft.Win32;

namespace NovaBrowser.Installer.Services;

public static class InstallPaths
{
    public const string AppExecutableName = "NovaBrowser.exe";
    public const string SetupExecutableName = "NovaBrowser.Setup.exe";
    public const string UninstallRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\NovaBrowser";

    public static string PayloadDirectory => Path.Combine(AppContext.BaseDirectory, "payload");

    public static string GetBrowserExecutable(string installPath) =>
        Path.Combine(installPath, AppExecutableName);

    public static string GetSetupExecutable(string installPath) =>
        Path.Combine(installPath, SetupExecutableName);

    public static bool IsPayloadAvailable() =>
        Directory.Exists(PayloadDirectory) &&
        File.Exists(Path.Combine(PayloadDirectory, AppExecutableName));

    public static Version ReadPayloadVersion()
    {
        var dllPath = Path.Combine(PayloadDirectory, "NovaBrowser.dll");
        if (File.Exists(dllPath))
        {
            return AssemblyName.GetAssemblyName(dllPath).Version ?? new Version(0, 2, 0);
        }

        return Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 2, 0);
    }

    public static string? ReadInstalledPath()
    {
        using var key = Registry.CurrentUser.OpenSubKey(InstallPaths.UninstallRegistryKey);
        return key?.GetValue("InstallLocation") as string;
    }
}
