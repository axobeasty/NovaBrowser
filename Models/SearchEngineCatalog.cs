namespace NovaBrowser.Models;

public sealed record SearchEnginePreset(string Id, string NameKey, string QueryUrl, string DefaultHomePage);

public static class SearchEngineCatalog
{
    public const string CustomId = "custom";

    public static IReadOnlyList<SearchEnginePreset> Presets { get; } =
    [
        new("bing", "SearchEngineBing", "https://www.bing.com/search?q=", "https://www.bing.com"),
        new("google", "SearchEngineGoogle", "https://www.google.com/search?q=", "https://www.google.com"),
        new("duckduckgo", "SearchEngineDuckDuckGo", "https://duckduckgo.com/?q=", "https://duckduckgo.com"),
        new("yandex", "SearchEngineYandex", "https://yandex.ru/search/?text=", "https://yandex.ru"),
    ];

    public static SearchEnginePreset? GetById(string? id) =>
        Presets.FirstOrDefault(preset => preset.Id == id);

    public static string NormalizeId(string? id) =>
        GetById(id) is not null || id == CustomId ? id! : Presets[0].Id;
}
