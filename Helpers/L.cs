using Microsoft.UI.Xaml;
using NovaBrowser.Services;

namespace NovaBrowser.Helpers;

public static class L
{
    public static string Get(string key)
    {
        if (Application.Current is App app)
        {
            return app.Localization.GetString(key);
        }

        return key;
    }

    public static string Format(string key, params object[] args)
    {
        if (Application.Current is App app)
        {
            return app.Localization.Format(key, args);
        }

        return string.Format(key, args);
    }
}
