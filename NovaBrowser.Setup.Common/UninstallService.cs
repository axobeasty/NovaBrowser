using NovaBrowser.Setup.Common.Models;

namespace NovaBrowser.Setup.Common;

public static class UninstallService
{
    public static async Task<InstallResult> UninstallAsync(
        string installPath,
        IProgress<InstallProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var removalPaths = InstallPaths.GetRemovalPaths(installPath);
        var normalizedInstallPath = removalPaths[0];

        if (!Directory.Exists(normalizedInstallPath) &&
            removalPaths.Skip(1).All(path => !Directory.Exists(path)))
        {
            UninstallRegistryService.Unregister();
            ShortcutService.RemoveDesktopShortcut();
            ShortcutService.RemoveStartMenuShortcuts();
            return new InstallResult { Succeeded = true, InstallPath = normalizedInstallPath };
        }

        try
        {
            progress?.Report(new InstallProgressReport { Progress = 0.1, Status = "Закрытие NovaBrowser..." });
            await Task.Run(() => TryCloseRelatedProcesses(normalizedInstallPath), cancellationToken);

            progress?.Report(new InstallProgressReport { Progress = 0.25, Status = "Удаление ярлыков..." });
            ShortcutService.RemoveDesktopShortcut();
            ShortcutService.RemoveStartMenuShortcuts();

            progress?.Report(new InstallProgressReport { Progress = 0.4, Status = "Удаление файлов..." });

            var runningFromInstallFolder = IsRunningFromDirectory(normalizedInstallPath);
            var deferredCleanup = false;

            if (runningFromInstallFolder)
            {
                ScheduleDeferredCleanup(removalPaths);
                deferredCleanup = true;
            }
            else
            {
                var deleted = await Task.Run(
                    () => DeletePathsSynchronously(removalPaths),
                    cancellationToken);

                if (!deleted)
                {
                    ScheduleDeferredCleanup(removalPaths);
                    deferredCleanup = true;
                }
            }

            UninstallRegistryService.Unregister();

            progress?.Report(new InstallProgressReport
            {
                Progress = deferredCleanup ? 0.95 : 1,
                Status = deferredCleanup ? "Завершение очистки..." : "Готово",
            });

            if (deferredCleanup)
            {
                await Task.Delay(300, cancellationToken);
            }

            progress?.Report(new InstallProgressReport { Progress = 1, Status = "Готово" });
            return new InstallResult { Succeeded = true, InstallPath = normalizedInstallPath };
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

    private static void TryCloseRelatedProcesses(string installPath)
    {
        var normalizedInstallPath = NormalizeDirectory(installPath);

        foreach (var process in System.Diagnostics.Process.GetProcesses())
        {
            try
            {
                if (process.Id == Environment.ProcessId || process.HasExited)
                {
                    continue;
                }

                var shouldKill = process.ProcessName.Equals("NovaBrowser", StringComparison.OrdinalIgnoreCase);

                if (!shouldKill)
                {
                    var modulePath = TryGetProcessPath(process);
                    if (!string.IsNullOrWhiteSpace(modulePath))
                    {
                        var processDirectory = NormalizeDirectory(Path.GetDirectoryName(modulePath)!);
                        shouldKill = processDirectory.StartsWith(normalizedInstallPath, StringComparison.OrdinalIgnoreCase);
                    }
                }

                if (shouldKill)
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

        Thread.Sleep(500);
    }

    private static string? TryGetProcessPath(System.Diagnostics.Process process)
    {
        try
        {
            return process.MainModule?.FileName;
        }
        catch
        {
            return null;
        }
    }

    private static bool DeletePathsSynchronously(IReadOnlyList<string> paths)
    {
        var allRemoved = true;

        foreach (var path in paths.OrderByDescending(p => p.Length))
        {
            if (!Directory.Exists(path))
            {
                continue;
            }

            ClearAttributesRecursively(path);

            for (var attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    Directory.Delete(path, recursive: true);
                    break;
                }
                catch when (attempt < 4)
                {
                    Thread.Sleep(400);
                    ClearAttributesRecursively(path);
                }
                catch
                {
                    allRemoved = false;
                    break;
                }
            }

            if (Directory.Exists(path))
            {
                allRemoved = false;
            }
        }

        return allRemoved;
    }

    private static void ScheduleDeferredCleanup(IReadOnlyList<string> paths)
    {
        var scriptPath = Path.Combine(Path.GetTempPath(), $"NovaBrowser-Cleanup-{Guid.NewGuid():N}.ps1");
        var currentProcessId = Environment.ProcessId;
        var escapedPaths = paths
            .Select(path => $"'{path.Replace("'", "''", StringComparison.Ordinal)}'")
            .ToArray();

        var script = $@"
$ErrorActionPreference = 'SilentlyContinue'
$targets = @({string.Join(", ", escapedPaths)})
$currentPid = {currentProcessId}

function Clear-TreeAttributes([string]$Path) {{
  if (-not (Test-Path -LiteralPath $Path)) {{ return }}
  Get-ChildItem -LiteralPath $Path -Force -Recurse -ErrorAction SilentlyContinue | ForEach-Object {{
    if ($_.Attributes -band [IO.FileAttributes]::ReadOnly) {{
      $_.Attributes = $_.Attributes -band (-bnot [IO.FileAttributes]::ReadOnly)
    }}
  }}
}}

function Stop-InstallProcesses([string[]]$InstallPaths) {{
  foreach ($installPath in $InstallPaths) {{
    if (-not (Test-Path -LiteralPath $installPath)) {{ continue }}

    Get-Process -Name 'NovaBrowser' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

    Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
      Where-Object {{
        $_.Name -eq 'msedgewebview2.exe' -and
        $_.CommandLine -and
        ($_.CommandLine -like ""*$installPath*"" -or $_.CommandLine -like '*NovaBrowser*')
      }} |
      ForEach-Object {{ Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }}
  }}
}}

function Remove-DirectoryForce([string]$Path) {{
  if (-not (Test-Path -LiteralPath $Path)) {{ return $true }}

  Clear-TreeAttributes $Path
  Remove-Item -LiteralPath $Path -Recurse -Force -ErrorAction SilentlyContinue
  if (-not (Test-Path -LiteralPath $Path)) {{ return $true }}

  $empty = Join-Path $env:TEMP ([Guid]::NewGuid().ToString('N'))
  New-Item -ItemType Directory -Path $empty -Force | Out-Null
  & robocopy $empty $Path /MIR /R:1 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
  Remove-Item -LiteralPath $empty -Force -ErrorAction SilentlyContinue
  Remove-Item -LiteralPath $Path -Recurse -Force -ErrorAction SilentlyContinue
  return -not (Test-Path -LiteralPath $Path)
}}

$deadline = (Get-Date).AddSeconds(60)
while ((Get-Process -Id $currentPid -ErrorAction SilentlyContinue) -and (Get-Date) -lt $deadline) {{
  Start-Sleep -Milliseconds 250
}}

for ($pass = 0; $pass -lt 120; $pass++) {{
  Stop-InstallProcesses $targets

  $remaining = $false
  foreach ($target in ($targets | Sort-Object {{ $_.Length }} -Descending)) {{
    if (-not (Test-Path -LiteralPath $target)) {{ continue }}
    if (-not (Remove-DirectoryForce $target)) {{ $remaining = $true }}
  }}

  Get-ChildItem -LiteralPath $env:TEMP -Filter 'NovaBrowser-*' -ErrorAction SilentlyContinue |
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
  Get-ChildItem -LiteralPath $env:TEMP -Filter 'novabrowser-*' -ErrorAction SilentlyContinue |
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

  if (-not $remaining) {{ break }}
  Start-Sleep -Milliseconds 500
}}

Remove-Item -LiteralPath $PSCommandPath -Force -ErrorAction SilentlyContinue
";

        File.WriteAllText(scriptPath, script);

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{scriptPath}\"",
            CreateNoWindow = true,
            UseShellExecute = true,
        });
    }

    private static void ClearAttributesRecursively(string directoryPath)
    {
        foreach (var file in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
        {
            try
            {
                var attributes = File.GetAttributes(file);
                if (attributes.HasFlag(FileAttributes.ReadOnly))
                {
                    File.SetAttributes(file, attributes & ~FileAttributes.ReadOnly);
                }
            }
            catch
            {
                // Best effort.
            }
        }
    }

    private static bool IsRunningFromDirectory(string installPath)
    {
        if (string.IsNullOrWhiteSpace(Environment.ProcessPath))
        {
            return File.Exists(Path.Combine(installPath, InstallPaths.UninstallExecutableName));
        }

        var currentDirectory = Path.GetDirectoryName(Path.GetFullPath(Environment.ProcessPath));
        if (string.IsNullOrWhiteSpace(currentDirectory))
        {
            return false;
        }

        return NormalizeDirectory(currentDirectory)
            .Equals(installPath, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeDirectory(string path) =>
        Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
