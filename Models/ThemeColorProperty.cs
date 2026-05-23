namespace NovaBrowser.Models;

public sealed class ThemeColorProperty
{
    public required string Key { get; init; }

    public required string DisplayName { get; init; }

    public required string Category { get; init; }

    public required Func<BrowserTheme, string> Getter { get; init; }

    public required Action<BrowserTheme, string> Setter { get; init; }
}

public static class ThemeColorCatalog
{
    public static IReadOnlyList<ThemeColorProperty> All { get; } =
    [
        Prop(nameof(BrowserTheme.Accent), "Акцент", "Основные", t => t.Accent, (t, v) => t.Accent = v),
        Prop(nameof(BrowserTheme.AccentSecondary), "Вторичный акцент", "Основные", t => t.AccentSecondary, (t, v) => t.AccentSecondary = v),
        Prop(nameof(BrowserTheme.TabStripBackground), "Фон полосы вкладок", "Вкладки", t => t.TabStripBackground, (t, v) => t.TabStripBackground = v),
        Prop(nameof(BrowserTheme.TabStripDivider), "Разделитель полосы", "Вкладки", t => t.TabStripDivider, (t, v) => t.TabStripDivider = v),
        Prop(nameof(BrowserTheme.TabActiveBackground), "Активная вкладка", "Вкладки", t => t.TabActiveBackground, (t, v) => t.TabActiveBackground = v),
        Prop(nameof(BrowserTheme.TabActiveBorder), "Рамка активной вкладки", "Вкладки", t => t.TabActiveBorder, (t, v) => t.TabActiveBorder = v),
        Prop(nameof(BrowserTheme.TabInactiveBackground), "Неактивная вкладка", "Вкладки", t => t.TabInactiveBackground, (t, v) => t.TabInactiveBackground = v),
        Prop(nameof(BrowserTheme.TabHoverBackground), "Вкладка при наведении", "Вкладки", t => t.TabHoverBackground, (t, v) => t.TabHoverBackground = v),
        Prop(nameof(BrowserTheme.TabHoverBorder), "Рамка при наведении", "Вкладки", t => t.TabHoverBorder, (t, v) => t.TabHoverBorder = v),
        Prop(nameof(BrowserTheme.TabBorder), "Рамка вкладки", "Вкладки", t => t.TabBorder, (t, v) => t.TabBorder = v),
        Prop(nameof(BrowserTheme.TabCloseHover), "Кнопка закрытия (hover)", "Вкладки", t => t.TabCloseHover, (t, v) => t.TabCloseHover = v),
        Prop(nameof(BrowserTheme.TabClosePressed), "Кнопка закрытия (нажатие)", "Вкладки", t => t.TabClosePressed, (t, v) => t.TabClosePressed = v),
        Prop(nameof(BrowserTheme.TabAddHover), "Кнопка «+» (hover)", "Вкладки", t => t.TabAddHover, (t, v) => t.TabAddHover = v),
        Prop(nameof(BrowserTheme.ToolbarBackground), "Панель навигации", "Интерфейс", t => t.ToolbarBackground, (t, v) => t.ToolbarBackground = v),
        Prop(nameof(BrowserTheme.ContentBackground), "Фон контента", "Интерфейс", t => t.ContentBackground, (t, v) => t.ContentBackground = v),
        Prop(nameof(BrowserTheme.TitleBarButtonHover), "Кнопки окна (hover)", "Интерфейс", t => t.TitleBarButtonHover, (t, v) => t.TitleBarButtonHover = v),
        Prop(nameof(BrowserTheme.TitleBarButtonPressed), "Кнопки окна (нажатие)", "Интерфейс", t => t.TitleBarButtonPressed, (t, v) => t.TitleBarButtonPressed = v),
        Prop(nameof(BrowserTheme.StartPageBackground), "Стартовая: фон", "Стартовая страница", t => t.StartPageBackground, (t, v) => t.StartPageBackground = v),
        Prop(nameof(BrowserTheme.StartPageSurface), "Стартовая: карточка", "Стартовая страница", t => t.StartPageSurface, (t, v) => t.StartPageSurface = v),
        Prop(nameof(BrowserTheme.StartPageText), "Стартовая: текст", "Стартовая страница", t => t.StartPageText, (t, v) => t.StartPageText = v),
        Prop(nameof(BrowserTheme.StartPageMuted), "Стартовая: вторичный текст", "Стартовая страница", t => t.StartPageMuted, (t, v) => t.StartPageMuted = v),
    ];

    private static ThemeColorProperty Prop(
        string key,
        string displayName,
        string category,
        Func<BrowserTheme, string> getter,
        Action<BrowserTheme, string> setter) =>
        new()
        {
            Key = key,
            DisplayName = displayName,
            Category = category,
            Getter = getter,
            Setter = setter,
        };
}
