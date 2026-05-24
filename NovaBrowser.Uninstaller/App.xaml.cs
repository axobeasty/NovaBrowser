using Microsoft.UI.Xaml;
using NovaBrowser.Setup.Common;

namespace NovaBrowser.Uninstaller;

public partial class App : Application
{
    public static Window Window { get; private set; } = null!;

    public static string? InstallPathOverride { get; private set; }

    public App()
    {
        InitializeComponent();
        ParseCommandLine();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var installPath = ResolveInstallPath()
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NovaBrowser");

        Window = new MainWindow(installPath);
        Window.Activate();
    }

    private static string? ResolveInstallPath()
    {
        if (!string.IsNullOrWhiteSpace(InstallPathOverride))
        {
            return InstallPathOverride;
        }

        return InstallPaths.ReadInstalledPath() ?? InstallPaths.InferInstallPathFromProcess();
    }

    private static void ParseCommandLine()
    {
        var args = Environment.GetCommandLineArgs();
        for (var i = 1; i < args.Length; i++)
        {
            if (args[i].Equals("--path", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                InstallPathOverride = args[++i];
            }
        }
    }
}
