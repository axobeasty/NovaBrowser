using System.Reflection;
using Microsoft.Win32;

namespace NovaBrowser.Setup.Common;

public static class InstallPaths
{
    public const string AppExecutableName = "NovaBrowser.exe";
    public const string SetupExecutableName = "NovaBrowser.Setup.exe";
    public const string UninstallExecutableName = "NovaBrowser.Uninstall.exe";
    public const string UninstallRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\NovaBrowser";

    public static string PayloadDirectory => Path.Combine(AppContext.BaseDirectory, "payload");

    public static string GetBrowserExecutable(string installPath) =>
        Path.Combine(installPath, AppExecutableName);

    public static string GetSetupExecutable(string installPath) =>
        Path.Combine(installPath, SetupExecutableName);

    public static string GetUninstallExecutable(string installPath) =>
        Path.Combine(installPath, UninstallExecutableName);

    public static void CopyBundledApplicationFiles(string sourceDirectory, string targetDirectory, string applicationBaseName)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            return;
        }

        Directory.CreateDirectory(targetDirectory);

        foreach (var file in Directory.GetFiles(sourceDirectory, $"{applicationBaseName}.*"))
        {
            var destination = Path.Combine(targetDirectory, Path.GetFileName(file));
            File.Copy(file, destination, overwrite: true);
        }
    }

    public static bool IsPayloadAvailable() =>
        Directory.Exists(PayloadDirectory) &&
        File.Exists(Path.Combine(PayloadDirectory, AppExecutableName));

    public static Version ReadPayloadVersion()
    {
        var dllPath = Path.Combine(PayloadDirectory, "NovaBrowser.dll");
        if (File.Exists(dllPath))
        {
            return AssemblyName.GetAssemblyName(dllPath).Version ?? new Version(0, 4, 0);
        }

        return Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 4, 0);
    }

    public static Version ReadInstalledVersion(string installPath)
    {
        var dllPath = Path.Combine(installPath, "NovaBrowser.dll");
        if (File.Exists(dllPath))
        {
            return AssemblyName.GetAssemblyName(dllPath).Version ?? new Version(0, 4, 0);
        }

        return ReadPayloadVersion();
    }

    public static string? ReadInstalledPath()
    {
        using var key = Registry.CurrentUser.OpenSubKey(UninstallRegistryKey);
        return key?.GetValue("InstallLocation") as string;
    }

    public static string? InferInstallPathFromProcess(string? processPath = null)
    {
        processPath ??= Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(processPath))
        {
            return null;
        }

        var directory = Path.GetDirectoryName(processPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return null;
        }

        return File.Exists(Path.Combine(directory, AppExecutableName)) ? directory : null;
    }

    public static string GetDefaultUserDataDirectory() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NovaBrowser");

    public static IReadOnlyList<string> GetRemovalPaths(string installPath)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalizedInstall = NormalizeDirectory(installPath);
        paths.Add(normalizedInstall);
        paths.Add(Path.Combine(normalizedInstall, $"{AppExecutableName}.WebView2"));
        paths.Add(GetDefaultUserDataDirectory());

        if (Directory.Exists(normalizedInstall))
        {
            foreach (var webViewFolder in Directory.EnumerateDirectories(
                         normalizedInstall,
                         "*.WebView2",
                         SearchOption.TopDirectoryOnly))
            {
                paths.Add(webViewFolder);
            }
        }

        return paths.ToList();
    }

    private static string NormalizeDirectory(string path) =>
        Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
