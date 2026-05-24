using Microsoft.Web.WebView2.Core;

namespace NovaBrowser.Services;

public sealed class WebViewEnvironmentService
{
    private CoreWebView2Environment? _environment;
    private string? _environmentPath;

    public async Task<CoreWebView2Environment> GetEnvironmentAsync(ProfileService profileService)
    {
        var dataDirectory = profileService.GetWebViewDataDirectory();
        if (_environment is not null && string.Equals(_environmentPath, dataDirectory, StringComparison.OrdinalIgnoreCase))
        {
            return _environment;
        }

        Directory.CreateDirectory(dataDirectory);
        _environmentPath = dataDirectory;
        _environment = await CoreWebView2Environment.CreateWithOptionsAsync(
            browserExecutableFolder: null,
            userDataFolder: dataDirectory,
            options: null);

        return _environment;
    }

    public void DisposeEnvironment()
    {
        _environment = null;
        _environmentPath = null;
    }
}
