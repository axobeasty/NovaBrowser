namespace NovaBrowser.Models;

public sealed class ThemeColorProperty
{
    public required string Key { get; init; }

    public required string DisplayNameKey { get; init; }

    public required string CategoryKey { get; init; }

    public required Func<BrowserTheme, string> Getter { get; init; }

    public required Action<BrowserTheme, string> Setter { get; init; }
}

public static class ThemeColorCatalog
{
    public static IReadOnlyList<ThemeColorProperty> All { get; } =
    [
        Prop(nameof(BrowserTheme.Accent), "ColorAccent", "ColorCatMain", t => t.Accent, (t, v) => t.Accent = v),
        Prop(nameof(BrowserTheme.AccentSecondary), "ColorAccentSecondary", "ColorCatMain", t => t.AccentSecondary, (t, v) => t.AccentSecondary = v),
        Prop(nameof(BrowserTheme.TabStripBackground), "ColorTabStripBackground", "ColorCatTabs", t => t.TabStripBackground, (t, v) => t.TabStripBackground = v),
        Prop(nameof(BrowserTheme.TabStripDivider), "ColorTabStripDivider", "ColorCatTabs", t => t.TabStripDivider, (t, v) => t.TabStripDivider = v),
        Prop(nameof(BrowserTheme.TabActiveBackground), "ColorTabActiveBackground", "ColorCatTabs", t => t.TabActiveBackground, (t, v) => t.TabActiveBackground = v),
        Prop(nameof(BrowserTheme.TabActiveBorder), "ColorTabActiveBorder", "ColorCatTabs", t => t.TabActiveBorder, (t, v) => t.TabActiveBorder = v),
        Prop(nameof(BrowserTheme.TabInactiveBackground), "ColorTabInactiveBackground", "ColorCatTabs", t => t.TabInactiveBackground, (t, v) => t.TabInactiveBackground = v),
        Prop(nameof(BrowserTheme.TabHoverBackground), "ColorTabHoverBackground", "ColorCatTabs", t => t.TabHoverBackground, (t, v) => t.TabHoverBackground = v),
        Prop(nameof(BrowserTheme.TabHoverBorder), "ColorTabHoverBorder", "ColorCatTabs", t => t.TabHoverBorder, (t, v) => t.TabHoverBorder = v),
        Prop(nameof(BrowserTheme.TabBorder), "ColorTabBorder", "ColorCatTabs", t => t.TabBorder, (t, v) => t.TabBorder = v),
        Prop(nameof(BrowserTheme.TabCloseHover), "ColorTabCloseHover", "ColorCatTabs", t => t.TabCloseHover, (t, v) => t.TabCloseHover = v),
        Prop(nameof(BrowserTheme.TabClosePressed), "ColorTabClosePressed", "ColorCatTabs", t => t.TabClosePressed, (t, v) => t.TabClosePressed = v),
        Prop(nameof(BrowserTheme.TabAddHover), "ColorTabAddHover", "ColorCatTabs", t => t.TabAddHover, (t, v) => t.TabAddHover = v),
        Prop(nameof(BrowserTheme.ToolbarBackground), "ColorToolbarBackground", "ColorCatUi", t => t.ToolbarBackground, (t, v) => t.ToolbarBackground = v),
        Prop(nameof(BrowserTheme.ContentBackground), "ColorContentBackground", "ColorCatUi", t => t.ContentBackground, (t, v) => t.ContentBackground = v),
        Prop(nameof(BrowserTheme.TextPrimary), "ColorTextPrimary", "ColorCatUi", t => t.TextPrimary, (t, v) => t.TextPrimary = v),
        Prop(nameof(BrowserTheme.TextSecondary), "ColorTextSecondary", "ColorCatUi", t => t.TextSecondary, (t, v) => t.TextSecondary = v),
        Prop(nameof(BrowserTheme.IconForeground), "ColorIconForeground", "ColorCatUi", t => t.IconForeground, (t, v) => t.IconForeground = v),
        Prop(nameof(BrowserTheme.TitleBarButtonHover), "ColorTitleBarButtonHover", "ColorCatUi", t => t.TitleBarButtonHover, (t, v) => t.TitleBarButtonHover = v),
        Prop(nameof(BrowserTheme.TitleBarButtonPressed), "ColorTitleBarButtonPressed", "ColorCatUi", t => t.TitleBarButtonPressed, (t, v) => t.TitleBarButtonPressed = v),
        Prop(nameof(BrowserTheme.StartPageBackground), "ColorStartPageBackground", "ColorCatStartPage", t => t.StartPageBackground, (t, v) => t.StartPageBackground = v),
        Prop(nameof(BrowserTheme.StartPageSurface), "ColorStartPageSurface", "ColorCatStartPage", t => t.StartPageSurface, (t, v) => t.StartPageSurface = v),
        Prop(nameof(BrowserTheme.StartPageText), "ColorStartPageText", "ColorCatStartPage", t => t.StartPageText, (t, v) => t.StartPageText = v),
        Prop(nameof(BrowserTheme.StartPageMuted), "ColorStartPageMuted", "ColorCatStartPage", t => t.StartPageMuted, (t, v) => t.StartPageMuted = v),
    ];

    private static ThemeColorProperty Prop(
        string key,
        string displayNameKey,
        string categoryKey,
        Func<BrowserTheme, string> getter,
        Action<BrowserTheme, string> setter) =>
        new()
        {
            Key = key,
            DisplayNameKey = displayNameKey,
            CategoryKey = categoryKey,
            Getter = getter,
            Setter = setter,
        };
}
