namespace NovaBrowser.Services;

public static class ReadingModeService
{
    public const string InjectScript = """
        (function () {
          if (window.__novaReadingMode) {
            document.body.style.maxWidth = '';
            document.body.style.margin = '';
            document.body.style.fontSize = '';
            document.body.style.lineHeight = '';
            window.__novaReadingMode = false;
            return;
          }

          document.body.style.maxWidth = '720px';
          document.body.style.margin = '0 auto';
          document.body.style.fontSize = '18px';
          document.body.style.lineHeight = '1.7';
          window.__novaReadingMode = true;
        })();
        """;
}

public static class TranslationService
{
    public static string BuildTranslateUrl(string pageUrl, string targetLanguage = "en") =>
        $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl={Uri.EscapeDataString(targetLanguage)}&dt=t&q={Uri.EscapeDataString(pageUrl)}";
}
