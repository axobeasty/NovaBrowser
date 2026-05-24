using NovaBrowser.Services;
using Xunit;

namespace NovaBrowser.Tests;

public class UrlNormalizerTests
{
    [Theory]
    [InlineData("example.com", "https://example.com/")]
    [InlineData("https://example.com", "https://example.com/")]
    public void Normalize_AddsHttpsWhenMissing(string input, string expectedStart)
    {
        var result = UrlNormalizer.Normalize(input);
        Assert.StartsWith(expectedStart.TrimEnd('/'), result.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
    }
}

public class UserScriptServiceTests
{
    [Theory]
    [InlineData("https://example.com/page", "*://*/*", true)]
    [InlineData("https://example.com/page", "https://example.com/*", true)]
    [InlineData("https://other.com/page", "https://example.com/*", false)]
    public void MatchesPattern_Works(string url, string pattern, bool expected)
    {
        Assert.Equal(expected, UserScriptService.MatchesPattern(url, pattern));
    }
}
