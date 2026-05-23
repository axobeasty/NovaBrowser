using System.Reflection;

namespace NovaBrowser.Services;

public static class AppVersionService
{
    public static Version CurrentVersion { get; } =
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 1, 0);

    public static string CurrentVersionLabel => CurrentVersion.ToString(3);
}
