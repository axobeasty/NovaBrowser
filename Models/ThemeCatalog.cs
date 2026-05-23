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
        TextPrimary = "#F0F2F8",
        TextSecondary = "#A3A9BA",
        IconForeground = "#E8EBF5",
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
        Accent = "#5B4BD6",
        AccentSecondary = "#7B6CF0",
        TabStripBackground = "#C5CCD9",
        TabStripDivider = "#30000000",
        TabActiveBackground = "#FFFFFF",
        TabActiveBorder = "#45000000",
        TabInactiveBackground = "#B3BAC8",
        TabHoverBackground = "#DDE2EB",
        TabHoverBorder = "#50000000",
        TabBorder = "#28000000",
        TabCloseHover = "#22000000",
        TabClosePressed = "#44E74C3C",
        TabAddHover = "#18000000",
        ToolbarBackground = "#FFFFFF",
        ContentBackground = "#E8ECF3",
        TextPrimary = "#12151C",
        TextSecondary = "#5C6575",
        IconForeground = "#2B3140",
        TitleBarButtonHover = "#22000000",
        TitleBarButtonPressed = "#38000000",
        StartPageBackground = "#E8ECF3",
        StartPageSurface = "#FFFFFF",
        StartPageText = "#12151C",
        StartPageMuted = "#5C6575",
    };

    public static IEnumerable<BrowserTheme> BuiltInThemes => [Dark, Light];
}
