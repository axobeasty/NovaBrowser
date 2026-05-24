using Microsoft.UI.Xaml;
using NovaBrowser.Setup.Common;

namespace NovaBrowser.Installer;

public partial class App : Application
{
    public static Window Window { get; private set; } = null!;

    public static bool IsUninstallMode { get; private set; }

    public static string? InstallPathOverride { get; private set; }

    public App()
    {
        InitializeComponent();
        ParseCommandLine();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        if (IsUninstallMode && TryLaunchStandaloneUninstaller())
        {
            return;
        }

        Window = new MainWindow();
        Window.Activate();
    }

    private static bool TryLaunchStandaloneUninstaller()
    {
        var installPath = InstallPathOverride ?? InstallPaths.ReadInstalledPath();
        if (string.IsNullOrWhiteSpace(installPath))
        {
            return false;
        }

        var uninstallExe = InstallPaths.GetUninstallExecutable(installPath);
        if (!File.Exists(uninstallExe))
        {
            return false;
        }

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = uninstallExe,
            Arguments = $"--path \"{installPath}\"",
            UseShellExecute = true,
        });

        Environment.Exit(0);
        return true;
    }

    private static void ParseCommandLine()
    {
        var args = Environment.GetCommandLineArgs();
        for (var i = 1; i < args.Length; i++)
        {
            if (args[i].Equals("--uninstall", StringComparison.OrdinalIgnoreCase))
            {
                IsUninstallMode = true;
            }
            else if (args[i].Equals("--path", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                InstallPathOverride = args[++i];
            }
        }
    }
}
