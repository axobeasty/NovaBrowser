using Microsoft.UI.Xaml;
using NovaBrowser.Controls;
using NovaBrowser.Models;
using NovaBrowser.Services;

namespace NovaBrowser.AppWindows;

public sealed partial class FeatureWindow : Window
{
    public event EventHandler<string>? NavigateRequested;

    public FeatureWindow(FeatureWindowKind kind)
    {
        InitializeComponent();
        Title = GetTitle(kind);
        AppWindow.Resize(new Windows.Graphics.SizeInt32(760, 640));
        AppWindow.SetIcon("Assets/AppIcon.ico");

        if (Application.Current is App app)
        {
            ContentPage.Initialize(kind, app.Services);
            ContentPage.NavigateRequested += (_, url) => NavigateRequested?.Invoke(this, url);
            app.ThemeService.ThemeChanged += OnThemeChanged;
            app.Localization.LanguageChanged += OnLanguageChanged;
            Closed += (_, _) =>
            {
                app.ThemeService.ThemeChanged -= OnThemeChanged;
                app.Localization.LanguageChanged -= OnLanguageChanged;
            };
        }
    }

    private void OnThemeChanged(object? sender, Models.BrowserTheme e) { }

    private void OnLanguageChanged(object? sender, EventArgs e) => ContentPage.ApplyLocalizedStrings();

    private static string GetTitle(FeatureWindowKind kind) => kind switch
    {
        FeatureWindowKind.Bookmarks => Helpers.L.Get("WindowBookmarksTitle"),
        FeatureWindowKind.History => Helpers.L.Get("WindowHistoryTitle"),
        FeatureWindowKind.Downloads => Helpers.L.Get("WindowDownloadsTitle"),
        FeatureWindowKind.UserScripts => Helpers.L.Get("WindowScriptsTitle"),
        FeatureWindowKind.Passwords => Helpers.L.Get("WindowPasswordsTitle"),
        _ => "NovaBrowser",
    };
}
