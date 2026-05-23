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

    public List<BrowserTheme> CustomThemes { get; set; } = [];
}
