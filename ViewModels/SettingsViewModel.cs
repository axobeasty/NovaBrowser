using CommunityToolkit.Mvvm.ComponentModel;
using NovaBrowser.Helpers;
using NovaBrowser.Models;
using NovaBrowser.Services;

namespace NovaBrowser.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly LocalizationService _localizationService;

    public ThemeSettingsViewModel Theme { get; }

    [ObservableProperty]
    private string _homePage = string.Empty;

    [ObservableProperty]
    private string _searchEngineId = "bing";

    [ObservableProperty]
    private string _customSearchEngineUrl = string.Empty;

    [ObservableProperty]
    private string _uiLanguage = LocalizationService.SystemLanguage;

    [ObservableProperty]
    private bool _isCustomSearchEngineVisible;

    public SettingsViewModel(
        SettingsService settingsService,
        ThemeService themeService,
        LocalizationService localizationService)
    {
        _settingsService = settingsService;
        _localizationService = localizationService;
        Theme = new ThemeSettingsViewModel(settingsService, themeService);
        LoadFromSettings();
    }

    public IReadOnlyList<LanguageOption> GetLanguageOptions() =>
        _localizationService.GetLanguageOptions();

    public IReadOnlyList<SearchEngineOption> GetSearchEngineOptions()
    {
        var options = SearchEngineCatalog.Presets
            .Select(preset => new SearchEngineOption(preset.Id, L.Get(preset.NameKey)))
            .ToList();

        options.Add(new SearchEngineOption(SearchEngineCatalog.CustomId, L.Get("SearchEngineCustom")));
        return options;
    }

    public void LoadFromSettings()
    {
        var settings = _settingsService.Current;
        HomePage = settings.HomePage;
        SearchEngineId = SearchEngineCatalog.NormalizeId(settings.SearchEngineId);
        CustomSearchEngineUrl = settings.CustomSearchEngineUrl;
        UiLanguage = settings.UiLanguage;
        UpdateCustomSearchVisibility();
    }

    public void SaveGeneralSettings()
    {
        var settings = _settingsService.Current;
        var previousLanguage = settings.UiLanguage;

        settings.HomePage = HomePage.Trim();
        settings.SearchEngineId = SearchEngineCatalog.NormalizeId(SearchEngineId);
        settings.CustomSearchEngineUrl = CustomSearchEngineUrl.Trim();
        settings.UiLanguage = UiLanguage;
        _settingsService.Save();

        if (!string.Equals(previousLanguage, UiLanguage, StringComparison.Ordinal))
        {
            _localizationService.SetLanguage(UiLanguage);
        }
    }

    partial void OnSearchEngineIdChanged(string value) =>
        UpdateCustomSearchVisibility();

    private void UpdateCustomSearchVisibility() =>
        IsCustomSearchEngineVisible = SearchEngineId == SearchEngineCatalog.CustomId;
}

public sealed record SearchEngineOption(string Id, string Title);
