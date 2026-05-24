using Windows.System;

namespace NovaBrowser.Services;

public static class DefaultBrowserService
{
    public static void OpenDefaultAppsSettings()
    {
        try
        {
            _ = Launcher.LaunchUriAsync(new Uri("ms-settings:defaultapps"));
        }
        catch
        {
            // Best effort.
        }
    }
}
