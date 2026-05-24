using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NovaBrowser.Models;
using NovaBrowser.ViewModels;

namespace NovaBrowser.Controls;

public sealed partial class BookmarkBar : UserControl
{
    public event EventHandler<string>? BookmarkActivated;

    public BookmarkBar() => InitializeComponent();

    public void Bind(IEnumerable<BookmarkEntry> bookmarks)
    {
        ItemsHost.Children.Clear();
        foreach (var bookmark in bookmarks)
        {
            var button = new Button
            {
                Content = bookmark.Title,
                Tag = bookmark.Url,
                Style = (Style)Application.Current.Resources["SubtleButtonStyle"],
            };
            button.Click += (_, _) => BookmarkActivated?.Invoke(this, bookmark.Url);
            ToolTipService.SetToolTip(button, bookmark.Url);
            ItemsHost.Children.Add(button);
        }
    }
}
