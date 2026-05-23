namespace NovaBrowser.Models;

public static class ThemeCatalog
{
    public const string BuiltInDarkId = "builtin-dark";
    public const string BuiltInLightId = "builtin-light";

    public static BrowserTheme Dark { get; } = new()
    {
        Id = BuiltInDarkId,
        Name = "Тёмная",
        IsBuiltIn = true,
        Accent = "#6C5CE7",
        AccentSecondary = "#A29BFE",
        TabStripBackground = "#12141C",
        TabStripDivider = "#22FFFFFF",
        TabActiveBackground = "#1E2029",
        TabActiveBorder = "#44FFFFFF",
        TabInactiveBackground = "#1C1F28",
        TabHoverBackground = "#282B36",
        TabHoverBorder = "#55FFFFFF",
        TabBorder = "#18FFFFFF",
        TabCloseHover = "#33FFFFFF",
        TabClosePressed = "#55E74C3C",
        TabAddHover = "#22FFFFFF",
        ToolbarBackground = "#1E2029",
        ContentBackground = "#13151C",
        TitleBarButtonHover = "#19FFFFFF",
        TitleBarButtonPressed = "#2DFFFFFF",
        StartPageBackground = "#0F1117",
        StartPageSurface = "#171A24",
        StartPageText = "#F5F6FA",
        StartPageMuted = "#9AA0B5",
    };

    public static BrowserTheme Light { get; } = new()
    {
        Id = BuiltInLightId,
        Name = "Светлая",
        IsBuiltIn = true,
        Accent = "#6C5CE7",
        AccentSecondary = "#7B6CF0",
        TabStripBackground = "#ECEEF3",
        TabStripDivider = "#22000000",
        TabActiveBackground = "#FFFFFF",
        TabActiveBorder = "#33000000",
        TabInactiveBackground = "#E4E6EC",
        TabHoverBackground = "#F4F5F8",
        TabHoverBorder = "#44000000",
        TabBorder = "#18000000",
        TabCloseHover = "#18000000",
        TabClosePressed = "#33E74C3C",
        TabAddHover = "#18000000",
        ToolbarBackground = "#FFFFFF",
        ContentBackground = "#F3F4F8",
        TitleBarButtonHover = "#19000000",
        TitleBarButtonPressed = "#2D000000",
        StartPageBackground = "#F3F4F8",
        StartPageSurface = "#FFFFFF",
        StartPageText = "#1A1D26",
        StartPageMuted = "#5C6370",
    };

    public static IEnumerable<BrowserTheme> BuiltInThemes => [Dark, Light];
}
