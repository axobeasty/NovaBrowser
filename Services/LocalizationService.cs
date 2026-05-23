using System.Globalization;
using System.Text.Json;
using NovaBrowser.Models;
using Windows.ApplicationModel;
using Windows.Globalization;

namespace NovaBrowser.Services;

public sealed class LocalizationService
{
    public const string SystemLanguage = "system";
    public const string Russian = "ru-RU";
    public const string English = "en-US";

    public event EventHandler? LanguageChanged;

    public string CurrentLanguage { get; private set; } = English;

    private SettingsService? _settingsService;
    private Dictionary<string, string> _strings = new(StringComparer.Ordinal);
    private Dictionary<string, string> _fallbackStrings = new(StringComparer.Ordinal);

    public void Initialize(SettingsService settingsService)
    {
        _settingsService = settingsService;
        _fallbackStrings = LoadLocaleFile(English);
    }

    public void ApplySavedLanguage(bool persist = false)
    {
        var preference = _settingsService?.Current.UiLanguage ?? SystemLanguage;
        ApplyLanguage(preference, persist);
    }

    public void SetLanguage(string language, bool persist = true)
    {
        ApplyLanguage(language, persist);
    }

    public string GetString(string key) =>
        _strings.TryGetValue(key, out var value)
            ? value
            : _fallbackStrings.TryGetValue(key, out var fallback)
                ? fallback
                : key;

    public string Format(string key, params object[] args) =>
        string.Format(CultureInfo.CurrentCulture, GetString(key), args);

    public string GetStartPageFileName() =>
        CurrentLanguage.StartsWith("ru", StringComparison.OrdinalIgnoreCase)
            ? "start.html"
            : "start.en.html";

    public IReadOnlyList<LanguageOption> GetLanguageOptions() =>
    [
        new(SystemLanguage, GetString("LanguageSystem")),
        new(Russian, GetString("LanguageRussian")),
        new(English, GetString("LanguageEnglish")),
    ];

    private void ApplyLanguage(string preference, bool persist)
    {
        CurrentLanguage = ResolveLanguageCode(preference);
        _strings = LoadLocaleFile(CurrentLanguage);

        ApplicationLanguages.PrimaryLanguageOverride =
            preference == SystemLanguage ? string.Empty : CurrentLanguage;

        if (persist && _settingsService is not null)
        {
            _settingsService.Current.UiLanguage = preference;
            _settingsService.Save();
        }

        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    private static string ResolveLanguageCode(string preference)
    {
        if (preference is Russian or English)
        {
            return preference;
        }

        var systemLanguage = ApplicationLanguages.Languages.FirstOrDefault()
            ?? CultureInfo.CurrentUICulture.Name;

        return systemLanguage.StartsWith("ru", StringComparison.OrdinalIgnoreCase)
            ? Russian
            : English;
    }

    private static Dictionary<string, string> LoadLocaleFile(string language)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Locale", $"{language}.json");
        if (!File.Exists(path))
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                ?? new Dictionary<string, string>(StringComparer.Ordinal);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }
    }
}

public sealed record LanguageOption(string Code, string Title);
