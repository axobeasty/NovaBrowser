namespace NovaBrowser.Models;

public static class UpdateSettings
{
    public const string RepositoryOwner = "axobeasty";
    public const string RepositoryName = "NovaBrowser";
    public const string UserAgent = "NovaBrowser-Updater";

    public static string GetAssetName(string architecture) => $"NovaBrowser-win-{architecture}.zip";

    public static string GetLatestReleaseApiUrl() =>
        $"https://api.github.com/repos/{RepositoryOwner}/{RepositoryName}/releases/latest";
}
