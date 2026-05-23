using System.Diagnostics;
using Microsoft.UI.Xaml;
using NovaBrowser.Models;

namespace NovaBrowser.Services;

public static class UpdateInstallerService
{
    public static void ScheduleInstall(string packagePath)
    {
        var installDirectory = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var executablePath = Path.Combine(installDirectory, "NovaBrowser.exe");
        var processId = Environment.ProcessId;
        var scriptPath = Path.Combine(Path.GetTempPath(), $"novabrowser-update-{Guid.NewGuid():N}.ps1");

        var script = $$"""
            $ErrorActionPreference = 'Stop'

            Wait-Process -Id {{processId}} -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 2

            $extractDir = Join-Path $env:TEMP 'NovaBrowser-update-extract'
            if (Test-Path $extractDir) {
                Remove-Item $extractDir -Recurse -Force
            }

            New-Item -ItemType Directory -Path $extractDir | Out-Null
            Expand-Archive -LiteralPath '{{EscapePowerShellSingleQuoted(packagePath)}}' -DestinationPath $extractDir -Force

            robocopy "$extractDir" '{{EscapePowerShellSingleQuoted(installDirectory)}}' /E /R:3 /W:1 /NFL /NDL /NJH /NJS | Out-Null
            if ($LASTEXITCODE -ge 8) {
                exit $LASTEXITCODE
            }

            Remove-Item $extractDir -Recurse -Force -ErrorAction SilentlyContinue
            Remove-Item '{{EscapePowerShellSingleQuoted(packagePath)}}' -Force -ErrorAction SilentlyContinue
            Start-Process '{{EscapePowerShellSingleQuoted(executablePath)}}'
            Remove-Item -LiteralPath $MyInvocation.MyCommand.Path -Force
            """;

        File.WriteAllText(scriptPath, script);

        Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{scriptPath}\"",
            UseShellExecute = true,
            CreateNoWindow = true,
        });

        if (Application.Current is App)
        {
            Application.Current.Exit();
        }
    }

    private static string EscapePowerShellSingleQuoted(string value) =>
        value.Replace("'", "''", StringComparison.Ordinal);
}
