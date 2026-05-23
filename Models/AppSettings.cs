namespace NovaBrowser.Models;

public enum ThemeSelectionType
{
    System,
    Light,
    Dark,
    Custom,
}

public sealed class AppSettings
{
    public ThemeSelectionType ThemeSelection { get; set; } = ThemeSelectionType.System;

    public string ActiveCustomThemeId { get; set; } = string.Empty;

    public string UiLanguage { get; set; } = "system";

    public string HomePage { get; set; } = string.Empty;

    public string SearchEngineId { get; set; } = "bing";

    public string CustomSearchEngineUrl { get; set; } = string.Empty;

    public List<BrowserTheme> CustomThemes { get; set; } = [];
}
