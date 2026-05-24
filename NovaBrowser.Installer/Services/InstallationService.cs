using System.Reflection;
using NovaBrowser.Installer.Models;
using NovaBrowser.Setup.Common;
using NovaBrowser.Setup.Common.Models;

namespace NovaBrowser.Installer.Services;

public sealed class InstallationService
{
    private static Assembly HostAssembly => Assembly.GetExecutingAssembly();

    public async Task<InstallResult> InstallAsync(
        InstallSettings settings,
        IProgress<InstallProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!EmbeddedSetupBundle.IsAvailable(HostAssembly))
        {
            return Failed("Встроенный архив установки не найден. Соберите Setup.exe через build-installer.ps1.");
        }

        try
        {
            var target = settings.InstallPath;
            Directory.CreateDirectory(target);

            progress?.Report(new InstallProgressReport { Progress = 0.05, Status = "Подготовка..." });

            await EmbeddedSetupBundle.ExtractToAsync(
                HostAssembly,
                target,
                new Progress<InstallProgressReport>(report =>
                {
                    progress?.Report(new InstallProgressReport
                    {
                        Progress = 0.05 + report.Progress * 0.85,
                        Status = report.Status,
                    });
                }),
                cancellationToken);

            if (!File.Exists(InstallPaths.GetUninstallExecutable(target)))
            {
                return Failed("В архиве установки отсутствует NovaBrowser.Uninstall.exe.");
            }

            var setupCommonDll = Path.Combine(target, "NovaBrowser.Setup.Common.dll");
            if (!File.Exists(setupCommonDll))
            {
                return Failed("В архиве установки отсутствует NovaBrowser.Setup.Common.dll.");
            }

            progress?.Report(new InstallProgressReport { Progress = 0.92, Status = "Создание ярлыков..." });

            var browserExe = InstallPaths.GetBrowserExecutable(target);
            if (settings.CreateDesktopShortcut)
            {
                ShortcutService.CreateDesktopShortcut(browserExe, target);
            }

            if (settings.CreateStartMenuShortcut)
            {
                ShortcutService.CreateStartMenuShortcut(browserExe, target);
            }

            var version = EmbeddedSetupBundle.ReadBundleVersion(HostAssembly);
            var uninstallDestination = InstallPaths.GetUninstallExecutable(target);
            var uninstallCommand = $"\"{uninstallDestination}\" --path \"{target}\"";
            UninstallRegistryService.Register(target, version, uninstallCommand);

            progress?.Report(new InstallProgressReport { Progress = 1, Status = "Готово" });

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

    public Task<InstallResult> UninstallAsync(
        string installPath,
        IProgress<InstallProgressReport>? progress = null,
        CancellationToken cancellationToken = default) =>
        UninstallService.UninstallAsync(installPath, progress, cancellationToken);

    private static InstallResult Failed(string message) =>
        new() { Succeeded = false, ErrorMessage = message };
}
