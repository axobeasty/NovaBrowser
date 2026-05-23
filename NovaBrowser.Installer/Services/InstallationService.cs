using NovaBrowser.Installer.Models;

namespace NovaBrowser.Installer.Services;

public sealed class InstallationService
{
    public async Task<InstallResult> InstallAsync(
        InstallSettings settings,
        IProgress<InstallProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!InstallPaths.IsPayloadAvailable())
        {
            return Failed("Не найдена папка payload с файлами NovaBrowser. Сначала соберите установщик через build-installer.ps1.");
        }

        try
        {
            var source = InstallPaths.PayloadDirectory;
            var target = settings.InstallPath;
            Directory.CreateDirectory(target);

            var files = Directory.GetFiles(source, "*", SearchOption.AllDirectories);
            var total = Math.Max(files.Length, 1);

            for (var index = 0; index < files.Length; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var file = files[index];
                var relative = Path.GetRelativePath(source, file);
                var destination = Path.Combine(target, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

                File.Copy(file, destination, overwrite: true);

                progress?.Report(new InstallProgressReport
                {
                    Progress = (index + 1) / (double)total,
                    Status = $"Копирование: {relative}",
                });

                if (index % 8 == 0)
                {
                    await Task.Yield();
                }
            }

            var setupDestination = InstallPaths.GetSetupExecutable(target);
            File.Copy(Environment.ProcessPath!, setupDestination, overwrite: true);

            var browserExe = InstallPaths.GetBrowserExecutable(target);
            if (settings.CreateDesktopShortcut)
            {
                ShortcutService.CreateDesktopShortcut(browserExe, target);
            }

            if (settings.CreateStartMenuShortcut)
            {
                ShortcutService.CreateStartMenuShortcut(browserExe, target);
            }

            var version = InstallPaths.ReadPayloadVersion();
            var uninstallCommand = $"\"{setupDestination}\" --uninstall --path \"{target}\"";
            UninstallRegistryService.Register(target, version, uninstallCommand);

            if (settings.LaunchAfterInstall)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = browserExe,
                    UseShellExecute = true,
                    WorkingDirectory = target,
                });
            }

            return new InstallResult
            {
                Succeeded = true,
                InstallPath = target,
            };
        }
        catch (Exception ex)
        {
            return Failed(ex.Message);
        }
    }

    public async Task<InstallResult> UninstallAsync(
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
            progress?.Report(new InstallProgressReport { Progress = 0.2, Status = "Удаление ярлыков..." });
            ShortcutService.RemoveDesktopShortcut();
            ShortcutService.RemoveStartMenuShortcuts();
            UninstallRegistryService.Unregister();

            progress?.Report(new InstallProgressReport { Progress = 0.5, Status = "Удаление файлов..." });
            await Task.Run(() => DeleteDirectorySafe(installPath), cancellationToken);

            progress?.Report(new InstallProgressReport { Progress = 1, Status = "Готово" });
            return new InstallResult { Succeeded = true, InstallPath = installPath };
        }
        catch (Exception ex)
        {
            return Failed(ex.Message);
        }
    }

    private static void DeleteDirectorySafe(string path)
    {
        foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
        {
            var attributes = File.GetAttributes(file);
            if (attributes.HasFlag(FileAttributes.ReadOnly))
            {
                File.SetAttributes(file, attributes & ~FileAttributes.ReadOnly);
            }
        }

        Directory.Delete(path, recursive: true);
    }

    private static InstallResult Failed(string message) =>
        new() { Succeeded = false, ErrorMessage = message };
}
