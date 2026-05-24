namespace NovaBrowser.Models;

public static class UpdateSettings
{
    public const string RepositoryOwner = "axobeasty";
    public const string RepositoryName = "NovaBrowser";
    public const string UserAgent = "NovaBrowser-Updater";

    public static string GetAssetName(string architecture) => $"NovaBrowser-win-{architecture}.zip";

    public static string GetLatestReleaseApiUrl() =>
        $"https://api.github.com/repos/{RepositoryOwner}/{RepositoryName}/releases/latest";

    public static TimeSpan InitialCheckDelay { get; } = TimeSpan.FromSeconds(3);

    public static TimeSpan BackgroundCheckInterval { get; } = TimeSpan.FromMinutes(30);
}
