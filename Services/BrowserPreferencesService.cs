using NovaBrowser.Models;

namespace NovaBrowser.Services;

public sealed class BrowserPreferencesService
{
    private readonly SettingsService _settingsService;

    public BrowserPreferencesService(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public string HomePage
    {
        get
        {
            var value = _settingsService.Current.HomePage.Trim();
            return string.IsNullOrEmpty(value) ? BrowserSettings.HomePage : value;
        }
    }

    public string BuildSearchUrl(string query)
    {
        var template = ResolveSearchTemplate();
        return template.Contains("{0}", StringComparison.Ordinal)
            ? string.Format(template, Uri.EscapeDataString(query))
            : template + Uri.EscapeDataString(query);
    }

    private string ResolveSearchTemplate()
    {
        var settings = _settingsService.Current;
        if (settings.SearchEngineId == SearchEngineCatalog.CustomId)
        {
            var custom = settings.CustomSearchEngineUrl.Trim();
            if (!string.IsNullOrEmpty(custom))
            {
                return custom;
            }
        }

        var preset = SearchEngineCatalog.GetById(settings.SearchEngineId);
        return preset?.QueryUrl ?? BrowserSettings.SearchEngine;
    }
}
