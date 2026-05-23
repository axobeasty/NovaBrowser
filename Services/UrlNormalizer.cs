using NovaBrowser.Models;

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

        return $"{BrowserSettings.SearchEngine}{Uri.EscapeDataString(trimmed)}";
    }

    public static string GetDisplayUrl(string url)
    {
        if (url.Equals(BrowserSettings.NewTabPage, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return url;
    }
}
