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
                cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var message = string.IsNullOrWhiteSpace(errorBody)
                    ? L.Format("GitHubStatusCode", (int)response.StatusCode)
                    : $"{L.Format("GitHubStatusCode", (int)response.StatusCode)}: {errorBody}";
                return Failed(currentVersion, message);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            var root = document.RootElement;

            if (!root.TryGetProperty("tag_name", out var tagNameElement))
            {
                return Failed(currentVersion, L.Get("UpdatesCheckFailedMessage"));
            }

            var tagName = tagNameElement.GetString() ?? string.Empty;
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

            if (!asset.Value.TryGetProperty("browser_download_url", out var downloadUrlElement) ||
                string.IsNullOrWhiteSpace(downloadUrlElement.GetString()))
            {
                return Failed(currentVersion, L.Format("AssetNotFound", tagName, assetName));
            }

            var update = new UpdateInfo
            {
                Version = latestVersion,
                TagName = tagName,
                ReleaseNotes = root.TryGetProperty("body", out var bodyElement)
                    ? bodyElement.GetString()?.Trim() ?? L.Get("NoReleaseNotes")
                    : L.Get("NoReleaseNotes"),
                DownloadUrl = new Uri(downloadUrlElement.GetString()!),
                AssetName = assetName,
                AssetSize = asset.Value.TryGetProperty("size", out var sizeElement)
                    ? sizeElement.GetInt64()
                    : 0,
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
        catch (Exception ex)
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
                cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return new UpdateDownloadResult
                {
                    Succeeded = false,
                    ErrorMessage = L.Format("DownloadHttpFailed", (int)response.StatusCode),
                };
            }

            var totalBytes = response.Content.Headers.ContentLength ?? update.AssetSize;
            await using var input = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using var output = File.Create(destination);

            var buffer = new byte[81920];
            long downloaded = 0;
            int read;

            while ((read = await input.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
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
