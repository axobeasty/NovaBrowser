using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using NovaBrowser.Models;
using Windows.UI;

namespace NovaBrowser.Services;

public sealed class ThemeService
{
    public event EventHandler<BrowserTheme>? ThemeChanged;

    public BrowserTheme CurrentTheme { get; private set; } = ThemeCatalog.Dark.Clone();

    private SettingsService? _settingsService;

    public void Initialize(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public void ApplySavedSelection(ElementTheme? systemTheme = null)
    {
        var settings = _settingsService?.Current ?? new AppSettings();
        var theme = ResolveTheme(settings, systemTheme);
        ApplyTheme(theme, persist: false);
    }

    public void ApplyTheme(BrowserTheme theme, bool persist = true)
    {
        CurrentTheme = theme.Clone();
        EnsureReadableTextColors(CurrentTheme);
        UpdateResourceDictionary(CurrentTheme);
        ApplyWindowTheme(CurrentTheme);
        ThemeChanged?.Invoke(this, CurrentTheme);

        if (persist && _settingsService is not null)
        {
            _settingsService.Save();
        }
    }

    public void SetSelection(ThemeSelectionType selection, BrowserTheme? customTheme = null, bool persist = true)
    {
        if (_settingsService is null)
        {
            return;
        }

        _settingsService.Current.ThemeSelection = selection;

        if (selection == ThemeSelectionType.Custom && customTheme is not null)
        {
            _settingsService.Current.ActiveCustomThemeId = customTheme.Id;
        }

        if (persist)
        {
            _settingsService.Save();
        }

        ApplyTheme(ResolveTheme(_settingsService.Current), persist: false);
    }

    public BrowserTheme ResolveTheme(AppSettings settings, ElementTheme? systemTheme = null)
    {
        return settings.ThemeSelection switch
        {
            ThemeSelectionType.Light => ThemeCatalog.Light.Clone(),
            ThemeSelectionType.Dark => ThemeCatalog.Dark.Clone(),
            ThemeSelectionType.Custom => ResolveCustomTheme(settings),
            _ => IsLightTheme(systemTheme)
                ? ThemeCatalog.Light.Clone()
                : ThemeCatalog.Dark.Clone(),
        };
    }

    public BrowserTheme ResolveCustomTheme(AppSettings settings)
    {
        var custom = settings.CustomThemes.FirstOrDefault(t => t.Id == settings.ActiveCustomThemeId);
        return custom?.Clone() ?? ThemeCatalog.Dark.Clone();
    }

    public void SaveCustomTheme(BrowserTheme theme)
    {
        if (_settingsService is null)
        {
            return;
        }

        theme.IsBuiltIn = false;
        var existing = _settingsService.Current.CustomThemes.FindIndex(t => t.Id == theme.Id);
        if (existing >= 0)
        {
            _settingsService.Current.CustomThemes[existing] = theme.Clone();
        }
        else
        {
            _settingsService.Current.CustomThemes.Add(theme.Clone());
        }

        _settingsService.Current.ThemeSelection = ThemeSelectionType.Custom;
        _settingsService.Current.ActiveCustomThemeId = theme.Id;
        _settingsService.Save();
        ApplyTheme(theme, persist: false);
    }

    public void DeleteCustomTheme(string themeId)
    {
        if (_settingsService is null)
        {
            return;
        }

        _settingsService.Current.CustomThemes.RemoveAll(t => t.Id == themeId);

        if (_settingsService.Current.ActiveCustomThemeId == themeId)
        {
            _settingsService.Current.ThemeSelection = ThemeSelectionType.Dark;
            _settingsService.Current.ActiveCustomThemeId = string.Empty;
            ApplyTheme(ThemeCatalog.Dark.Clone(), persist: false);
        }

        _settingsService.Save();
    }

    public static bool IsLightTheme(ElementTheme? theme) =>
        theme == ElementTheme.Light;

    public static bool IsLightTheme(BrowserTheme theme) =>
        ThemeColorHelper.IsLightColor(ThemeColorHelper.ParseColor(theme.ToolbarBackground));

    public string BuildStartPageThemeScript(BrowserTheme theme)
    {
        var isLight = IsLightTheme(theme);
        return $$"""
            (function() {
              const root = document.documentElement;
              root.style.setProperty('--bg', '{{theme.StartPageBackground}}');
              root.style.setProperty('--surface', '{{theme.StartPageSurface}}');
              root.style.setProperty('--accent', '{{theme.Accent}}');
              root.style.setProperty('--accent-soft', '{{theme.AccentSecondary}}');
              root.style.setProperty('--text', '{{theme.StartPageText}}');
              root.style.setProperty('--muted', '{{theme.StartPageMuted}}');
              root.style.colorScheme = '{{(isLight ? "light" : "dark")}}';
              document.body.style.color = '{{theme.StartPageText}}';
              const card = document.querySelector('.card');
              if (card) {
                card.style.background = '{{theme.StartPageSurface}}';
                card.style.borderColor = '{{(isLight ? "#28000000" : "#18FFFFFF")}}';
              }
              const input = document.querySelector('input');
              if (input) {
                input.style.background = '{{(isLight ? "#F5F7FB" : "#FFFFFF0A")}}';
                input.style.borderColor = '{{(isLight ? "#35000000" : "#1FFFFFFF")}}';
                input.style.color = '{{theme.StartPageText}}';
              }
              document.querySelectorAll('.shortcut').forEach((el) => {
                el.style.background = '{{(isLight ? "#F5F7FB" : "#FFFFFF0A")}}';
                el.style.borderColor = '{{(isLight ? "#28000000" : "#14FFFFFF")}}';
                el.style.color = '{{theme.StartPageText}}';
              });
            })();
            """;
    }

    private static void EnsureReadableTextColors(BrowserTheme theme)
    {
        var backgroundIsLight = IsLightTheme(theme);
        var textIsLight = ThemeColorHelper.IsLightColor(ThemeColorHelper.ParseColor(theme.TextPrimary));

        if (backgroundIsLight == textIsLight)
        {
            theme.TextPrimary = backgroundIsLight ? "#12151C" : "#F0F2F8";
            theme.TextSecondary = backgroundIsLight ? "#5C6575" : "#A3A9BA";
            theme.IconForeground = backgroundIsLight ? "#2B3140" : "#E8EBF5";
        }
    }

    private static void ApplyWindowTheme(BrowserTheme theme)
    {
        var elementTheme = IsLightTheme(theme) ? ElementTheme.Light : ElementTheme.Dark;

        if (App.Window?.Content is FrameworkElement root)
        {
            root.RequestedTheme = elementTheme;
        }

        if (App.Window is MainWindow window)
        {
            window.TabStripControl.RequestedTheme = elementTheme;
        }
    }

    private static void UpdateResourceDictionary(BrowserTheme theme)
    {
        var resources = Application.Current.Resources;
        SetBrush(resources, "NovaAccentBrush", theme.Accent);
        SetBrush(resources, "NovaAccentSecondaryBrush", theme.AccentSecondary);
        SetBrush(resources, "NovaTabStripBackgroundBrush", theme.TabStripBackground);
        SetBrush(resources, "NovaTabStripDividerBrush", theme.TabStripDivider);
        SetBrush(resources, "NovaTabActiveBackgroundBrush", theme.TabActiveBackground);
        SetBrush(resources, "NovaTabActiveBorderBrush", theme.TabActiveBorder);
        SetBrush(resources, "NovaTabInactiveBackgroundBrush", theme.TabInactiveBackground);
        SetBrush(resources, "NovaTabHoverBackgroundBrush", theme.TabHoverBackground);
        SetBrush(resources, "NovaTabHoverBorderBrush", theme.TabHoverBorder);
        SetBrush(resources, "NovaTabBorderBrush", theme.TabBorder);
        SetBrush(resources, "NovaTabCloseHoverBrush", theme.TabCloseHover);
        SetBrush(resources, "NovaTabClosePressedBrush", theme.TabClosePressed);
        SetBrush(resources, "NovaTabAddHoverBrush", theme.TabAddHover);
        SetBrush(resources, "NovaToolbarBackgroundBrush", theme.ToolbarBackground);
        SetBrush(resources, "NovaContentBackgroundBrush", theme.ContentBackground);
        SetBrush(resources, "NovaTextPrimaryBrush", theme.TextPrimary);
        SetBrush(resources, "NovaTextSecondaryBrush", theme.TextSecondary);
        SetBrush(resources, "NovaIconForegroundBrush", theme.IconForeground);
    }

    public static (Color Hover, Color Pressed) GetTitleBarButtonColors(BrowserTheme theme) =>
        (ThemeColorHelper.ParseColor(theme.TitleBarButtonHover),
         ThemeColorHelper.ParseColor(theme.TitleBarButtonPressed));

    private static void SetBrush(ResourceDictionary resources, string key, string hex)
    {
        resources[key] = new SolidColorBrush(ThemeColorHelper.ParseColor(hex));
    }
}
