namespace NovaBrowser.Models;

public sealed class BrowserTheme
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = "Новая тема";

    public bool IsBuiltIn { get; set; }

    public string Accent { get; set; } = "#6C5CE7";

    public string AccentSecondary { get; set; } = "#A29BFE";

    public string TabStripBackground { get; set; } = "#12141C";

    public string TabStripDivider { get; set; } = "#22FFFFFF";

    public string TabActiveBackground { get; set; } = "#1E2029";

    public string TabActiveBorder { get; set; } = "#44FFFFFF";

    public string TabInactiveBackground { get; set; } = "#1C1F28";

    public string TabHoverBackground { get; set; } = "#282B36";

    public string TabHoverBorder { get; set; } = "#55FFFFFF";

    public string TabBorder { get; set; } = "#18FFFFFF";

    public string TabCloseHover { get; set; } = "#33FFFFFF";

    public string TabClosePressed { get; set; } = "#55E74C3C";

    public string TabAddHover { get; set; } = "#22FFFFFF";

    public string ToolbarBackground { get; set; } = "#1E2029";

    public string ContentBackground { get; set; } = "#13151C";

    public string TitleBarButtonHover { get; set; } = "#19FFFFFF";

    public string TitleBarButtonPressed { get; set; } = "#2DFFFFFF";

    public string StartPageBackground { get; set; } = "#0F1117";

    public string StartPageSurface { get; set; } = "#171A24";

    public string StartPageText { get; set; } = "#F5F6FA";

    public string StartPageMuted { get; set; } = "#9AA0B5";

    public BrowserTheme Clone() =>
        new()
        {
            Id = Id,
            Name = Name,
            IsBuiltIn = IsBuiltIn,
            Accent = Accent,
            AccentSecondary = AccentSecondary,
            TabStripBackground = TabStripBackground,
            TabStripDivider = TabStripDivider,
            TabActiveBackground = TabActiveBackground,
            TabActiveBorder = TabActiveBorder,
            TabInactiveBackground = TabInactiveBackground,
            TabHoverBackground = TabHoverBackground,
            TabHoverBorder = TabHoverBorder,
            TabBorder = TabBorder,
            TabCloseHover = TabCloseHover,
            TabClosePressed = TabClosePressed,
            TabAddHover = TabAddHover,
            ToolbarBackground = ToolbarBackground,
            ContentBackground = ContentBackground,
            TitleBarButtonHover = TitleBarButtonHover,
            TitleBarButtonPressed = TitleBarButtonPressed,
            StartPageBackground = StartPageBackground,
            StartPageSurface = StartPageSurface,
            StartPageText = StartPageText,
            StartPageMuted = StartPageMuted,
        };
}
