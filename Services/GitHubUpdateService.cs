using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text.Json;
using NovaBrowser.Helpers;
using NovaBrowser.Models;

namespace NovaBrowser.Services;

public sealed class GitHubUpdateService
{
    private static readonly HttpClient HttpClient = CreateHttpClient();

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var currentVersion = AppVersionService.CurrentVersion;

        try
        {
            using var response = await HttpClient.GetAsync(
                UpdateSettings.GetLatestReleaseApiUrl(),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return Failed(currentVersion, L.Format("GitHubStatusCode", (int)response.StatusCode));
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;

            var tagName = root.GetProperty("tag_name").GetString() ?? string.Empty;
            var latestVersion = ParseVersion(tagName);
            if (latestVersion is null)
            {
                return Failed(currentVersion, L.Format("ParseVersionFailed", tagName));
            }

            var assetName = UpdateSettings.GetAssetName(GetRuntimeArchitecture());
            var asset = FindAsset(root, assetName);
            if (asset is null)
            {
                return Failed(currentVersion, L.Format("AssetNotFound", tagName, assetName));
            }

            var update = new UpdateInfo
            {
                Version = latestVersion,
                TagName = tagName,
                ReleaseNotes = root.GetProperty("body").GetString()?.Trim() ?? L.Get("NoReleaseNotes"),
                DownloadUrl = new Uri(asset.Value.GetProperty("browser_download_url").GetString()!),
                AssetName = assetName,
                AssetSize = asset.Value.GetProperty("size").GetInt64(),
            };

            if (latestVersion <= currentVersion)
            {
                return new UpdateCheckResult
                {
                    Status = UpdateCheckStatus.UpToDate,
                    CurrentVersion = currentVersion,
                };
            }

            return new UpdateCheckResult
            {
                Status = UpdateCheckStatus.UpdateAvailable,
                CurrentVersion = currentVersion,
                Update = update,
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return Failed(currentVersion, ex.Message);
        }
    }

    public async Task<UpdateDownloadResult> DownloadUpdateAsync(
        UpdateInfo update,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var destination = Path.Combine(
            Path.GetTempPath(),
            $"NovaBrowser-update-{update.Version}-{Guid.NewGuid():N}.zip");

        try
        {
            using var response = await HttpClient.GetAsync(
                update.DownloadUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new UpdateDownloadResult
                {
                    Succeeded = false,
                    ErrorMessage = L.Format("DownloadHttpFailed", (int)response.StatusCode),
                };
            }

            var totalBytes = response.Content.Headers.ContentLength ?? update.AssetSize;
            await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var output = File.Create(destination);

            var buffer = new byte[81920];
            long downloaded = 0;
            int read;

            while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                downloaded += read;

                if (totalBytes > 0)
                {
                    progress?.Report(Math.Clamp(downloaded / (double)totalBytes, 0, 1));
                }
            }

            progress?.Report(1);

            return new UpdateDownloadResult
            {
                Succeeded = true,
                PackagePath = destination,
            };
        }
        catch (Exception ex) when (ex is IOException or HttpRequestException or TaskCanceledException)
        {
            if (File.Exists(destination))
            {
                File.Delete(destination);
            }

            return new UpdateDownloadResult
            {
                Succeeded = false,
                ErrorMessage = ex.Message,
            };
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(10),
        };

        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue(UpdateSettings.UserAgent, AppVersionService.CurrentVersionLabel));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

        return client;
    }

    private static UpdateCheckResult Failed(Version currentVersion, string message) =>
        new()
        {
            Status = UpdateCheckStatus.Failed,
            CurrentVersion = currentVersion,
            ErrorMessage = message,
        };

    private static JsonElement? FindAsset(JsonElement root, string assetName)
    {
        if (!root.TryGetProperty("assets", out var assets))
        {
            return null;
        }

        foreach (var asset in assets.EnumerateArray())
        {
            if (asset.TryGetProperty("name", out var name) &&
                string.Equals(name.GetString(), assetName, StringComparison.OrdinalIgnoreCase))
            {
                return asset;
            }
        }

        return null;
    }

    private static Version? ParseVersion(string tagName)
    {
        var normalized = tagName.Trim();
        if (normalized.StartsWith('v') || normalized.StartsWith('V'))
        {
            normalized = normalized[1..];
        }

        return Version.TryParse(normalized, out var version) ? version : null;
    }

    private static string GetRuntimeArchitecture() =>
        RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm64 => "arm64",
            _ => "x64",
        };
}
