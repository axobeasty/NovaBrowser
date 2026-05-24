namespace NovaBrowser.Services;

public sealed class AdBlockService
{
    private static readonly string[] BlockedDomains =
    [
        "doubleclick.net",
        "googlesyndication.com",
        "googleadservices.com",
        "adservice.google.com",
        "facebook.net/en_US/fbevents.js",
        "scorecardresearch.com",
        "taboola.com",
        "outbrain.com",
        "ads.yahoo.com",
        "adnxs.com",
        "rubiconproject.com",
        "pubmatic.com",
        "moatads.com",
        "quantserve.com",
    ];

    public bool IsEnabled { get; set; } = true;

    public bool ShouldBlock(string uri)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(uri))
        {
            return false;
        }

        return BlockedDomains.Any(domain =>
            uri.Contains(domain, StringComparison.OrdinalIgnoreCase));
    }
}
