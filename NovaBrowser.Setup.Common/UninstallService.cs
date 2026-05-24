using NovaBrowser.Setup.Common.Models;

namespace NovaBrowser.Setup.Common;

public static class UninstallService
{
    public static async Task<InstallResult> UninstallAsync(
        string installPath,
        IProgress<InstallProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(installPath))
        {
            UninstallRegistryService.Unregister();
            ShortcutService.RemoveDesktopShortcut();
            ShortcutService.RemoveStartMenuShortcuts();
            return new InstallResult { Succeeded = true, InstallPath = installPath };
        }

        try
        {
            progress?.Report(new InstallProgressReport { Progress = 0.1, Status = "Закрытие NovaBrowser..." });
            await Task.Run(TryCloseBrowserProcesses, cancellationToken);

            progress?.Report(new InstallProgressReport { Progress = 0.25, Status = "Удаление ярлыков..." });
            ShortcutService.RemoveDesktopShortcut();
            ShortcutService.RemoveStartMenuShortcuts();

            progress?.Report(new InstallProgressReport { Progress = 0.4, Status = "Удаление файлов..." });
            var deferredCleanup = await Task.Run(
                () => DeleteInstallDirectory(installPath),
                cancellationToken);

            UninstallRegistryService.Unregister();

            progress?.Report(new InstallProgressReport
            {
                Progress = deferredCleanup ? 0.95 : 1,
                Status = deferredCleanup ? "Завершение очистки..." : "Готово",
            });

            if (deferredCleanup)
            {
                await Task.Delay(500, cancellationToken);
            }

            progress?.Report(new InstallProgressReport { Progress = 1, Status = "Готово" });
            return new InstallResult { Succeeded = true, InstallPath = installPath };
        }
        catch (Exception ex)
        {
            return new InstallResult
            {
                Succeeded = false,
                InstallPath = installPath,
                ErrorMessage = ex.Message,
            };
        }
    }

    private static void TryCloseBrowserProcesses()
    {
        foreach (var process in System.Diagnostics.Process.GetProcessesByName("NovaBrowser"))
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                    process.WaitForExit(5000);
                }
            }
            catch
            {
                // Best effort.
            }
            finally
            {
                process.Dispose();
            }
        }
    }

    /// <returns>True when folder cleanup was deferred to a background script.</returns>
    private static bool DeleteInstallDirectory(string installPath)
    {
        var normalizedInstallPath = Path.GetFullPath(installPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var currentProcessPath = string.IsNullOrWhiteSpace(Environment.ProcessPath)
            ? null
            : Path.GetFullPath(Environment.ProcessPath);

        var runningFromInstallFolder = currentProcessPath is not null &&
            Path.GetDirectoryName(currentProcessPath) is { } currentDirectory &&
            Path.GetFullPath(currentDirectory).Equals(normalizedInstallPath, StringComparison.OrdinalIgnoreCase);

        var lockedFiles = new List<string>();

        foreach (var file in Directory.EnumerateFiles(normalizedInstallPath, "*", SearchOption.AllDirectories))
        {
            if (runningFromInstallFolder &&
                currentProcessPath is not null &&
                file.Equals(currentProcessPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!TryDeleteFile(file))
            {
                lockedFiles.Add(file);
            }
        }

        foreach (var directory in Directory.EnumerateDirectories(normalizedInstallPath, "*", SearchOption.AllDirectories)
                     .OrderByDescending(path => path.Length))
        {
            TryDeleteEmptyDirectory(directory);
        }

        if (!Directory.Exists(normalizedInstallPath))
        {
            return false;
        }

        if (!Directory.EnumerateFileSystemEntries(normalizedInstallPath).Any())
        {
            Directory.Delete(normalizedInstallPath, recursive: false);
            return false;
        }

        if (runningFromInstallFolder && currentProcessPath is not null)
        {
            ScheduleDirectoryCleanup(normalizedInstallPath, currentProcessPath, lockedFiles);
            return true;
        }

        try
        {
            Directory.Delete(normalizedInstallPath, recursive: true);
            return false;
        }
        catch
        {
            ScheduleDirectoryCleanup(normalizedInstallPath, currentProcessPath, lockedFiles);
            return true;
        }
    }

    private static bool TryDeleteFile(string filePath)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                ClearReadOnly(filePath);
                File.Delete(filePath);
                return true;
            }
            catch when (attempt < 2)
            {
                Thread.Sleep(250);
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    private static void TryDeleteEmptyDirectory(string directoryPath)
    {
        try
        {
            if (Directory.Exists(directoryPath) && !Directory.EnumerateFileSystemEntries(directoryPath).Any())
            {
                Directory.Delete(directoryPath, recursive: false);
            }
        }
        catch
        {
            // Best effort.
        }
    }

    private static void ScheduleDirectoryCleanup(string installPath, string? skipFilePath, IReadOnlyCollection<string> lockedFiles)
    {
        var batchPath = Path.Combine(Path.GetTempPath(), $"NovaBrowser-Cleanup-{Guid.NewGuid():N}.cmd");
        var lines = new List<string>
        {
            "@echo off",
            "ping 127.0.0.1 -n 3 >nul",
        };

        if (!string.IsNullOrWhiteSpace(skipFilePath))
        {
            lines.Add($":wait_uninstall");
            lines.Add($"tasklist /FI \"IMAGENAME eq NovaBrowser.Uninstall.exe\" 2>nul | find /I \"NovaBrowser.Uninstall.exe\" >nul");
            lines.Add("if %errorlevel%==0 (ping 127.0.0.1 -n 2 >nul & goto wait_uninstall)");
            lines.Add($"del /f /q \"{skipFilePath}\" >nul 2>&1");
        }

        foreach (var lockedFile in lockedFiles)
        {
            lines.Add($"del /f /q \"{lockedFile}\" >nul 2>&1");
        }

        lines.Add(":retry");
        lines.Add($"rmdir /s /q \"{installPath}\" >nul 2>&1");
        lines.Add($"if exist \"{installPath}\" (ping 127.0.0.1 -n 2 >nul & goto retry)");
        lines.Add("del /f /q \"%~f0\" >nul 2>&1");

        File.WriteAllText(batchPath, string.Join(Environment.NewLine, lines));

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = batchPath,
            CreateNoWindow = true,
            UseShellExecute = true,
        });
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
