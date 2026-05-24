using System.IO.Compression;
using System.Reflection;
using NovaBrowser.Setup.Common.Models;

namespace NovaBrowser.Setup.Common;

public static class EmbeddedSetupBundle
{
    public const string BundleResourceSuffix = "SetupBundle.zip";

    public static bool IsAvailable(Assembly hostAssembly) =>
        OpenBundleStream(hostAssembly) is not null;

    public static Stream? OpenBundleStream(Assembly hostAssembly)
    {
        foreach (var resourceName in hostAssembly.GetManifestResourceNames())
        {
            if (resourceName.EndsWith(BundleResourceSuffix, StringComparison.OrdinalIgnoreCase))
            {
                return hostAssembly.GetManifestResourceStream(resourceName);
            }
        }

        return null;
    }

    public static Version ReadBundleVersion(Assembly hostAssembly)
    {
        using var archive = OpenArchive(hostAssembly);
        if (archive is null)
        {
            return hostAssembly.GetName().Version ?? new Version(0, 4, 0);
        }

        var entry = archive.GetEntry("NovaBrowser.dll")
            ?? archive.Entries.FirstOrDefault(item =>
                item.FullName.EndsWith("/NovaBrowser.dll", StringComparison.OrdinalIgnoreCase) ||
                item.FullName.Equals("NovaBrowser.dll", StringComparison.OrdinalIgnoreCase));

        if (entry is null)
        {
            return hostAssembly.GetName().Version ?? new Version(0, 4, 0);
        }

        var tempPath = Path.Combine(Path.GetTempPath(), $"NovaBrowser-Version-{Guid.NewGuid():N}.dll");
        try
        {
            entry.ExtractToFile(tempPath, overwrite: true);
            return AssemblyName.GetAssemblyName(tempPath).Version ?? new Version(0, 4, 0);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    public static async Task ExtractToAsync(
        Assembly hostAssembly,
        string targetDirectory,
        IProgress<InstallProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        using var archive = OpenArchive(hostAssembly)
            ?? throw new InvalidOperationException("Встроенный архив установки не найден.");

        var entries = archive.Entries
            .Where(entry => !string.IsNullOrEmpty(entry.Name))
            .ToList();

        Directory.CreateDirectory(targetDirectory);
        var total = Math.Max(entries.Count, 1);

        for (var index = 0; index < entries.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var entry = entries[index];
            var relativePath = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
            var destinationPath = Path.Combine(targetDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

            if (File.Exists(destinationPath))
            {
                ClearReadOnly(destinationPath);
            }

            entry.ExtractToFile(destinationPath, overwrite: true);

            progress?.Report(new InstallProgressReport
            {
                Progress = (index + 1) / (double)total,
                Status = $"Установка: {relativePath}",
            });

            if (index % 8 == 0)
            {
                await Task.Yield();
            }
        }
    }

    public static bool BundleContainsUninstaller(Assembly hostAssembly)
    {
        using var archive = OpenArchive(hostAssembly);
        if (archive is null)
        {
            return false;
        }

        return archive.Entries.Any(entry =>
            entry.Name.Equals(InstallPaths.UninstallExecutableName, StringComparison.OrdinalIgnoreCase));
    }

    private static ZipArchive? OpenArchive(Assembly hostAssembly)
    {
        var stream = OpenBundleStream(hostAssembly);
        if (stream is null)
        {
            return null;
        }

        return new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
    }

    private static void ClearReadOnly(string filePath)
    {
        var attributes = File.GetAttributes(filePath);
        if (attributes.HasFlag(FileAttributes.ReadOnly))
        {
            File.SetAttributes(filePath, attributes & ~FileAttributes.ReadOnly);
        }
    }
}
