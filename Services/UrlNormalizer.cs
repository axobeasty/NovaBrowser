using Microsoft.UI.Xaml;
using NovaBrowser.Models;
using NovaBrowser.Services;

namespace NovaBrowser.Services;

public static class UrlNormalizer
{
    public static string Normalize(string input)
    {
        var trimmed = input.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return BrowserSettings.NewTabPage;
        }

        if (trimmed.Equals("nova://start", StringComparison.OrdinalIgnoreCase))
        {
            return BrowserSettings.NewTabPage;
        }

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var absolute) &&
            (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
        {
            return absolute.AbsoluteUri;
        }

        if (trimmed.Contains('.') && !trimmed.Contains(' '))
        {
            return $"https://{trimmed}";
        }

        return GetPreferences().BuildSearchUrl(trimmed);
    }

    public static string GetDisplayUrl(string url)
    {
        if (url.Equals(BrowserSettings.NewTabPage, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return url;
    }

    private static BrowserPreferencesService GetPreferences()
    {
        if (Application.Current is App app)
        {
            return app.BrowserPreferences;
        }

        return new BrowserPreferencesService(new SettingsService());
    }
}
