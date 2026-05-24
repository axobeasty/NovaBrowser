using Microsoft.UI.Xaml;

namespace NovaBrowser.Helpers;

public static class DialogHost
{
    public static XamlRoot? ResolveXamlRoot(XamlRoot? preferred = null)
    {
        if (preferred is not null)
        {
            return preferred;
        }

        if (App.Window?.Content is FrameworkElement element && element.XamlRoot is not null)
        {
            return element.XamlRoot;
        }

        return null;
    }
}
