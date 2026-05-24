using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NovaBrowser.Helpers;
using NovaBrowser.Models;
using NovaBrowser.Services;
using NovaBrowser.ViewModels;

namespace NovaBrowser.Controls;

public sealed partial class SidePanel : UserControl
{
    private BrowserServiceHost? _services;
    private MainPageViewModel? _viewModel;
    private SidePanelSection _section = SidePanelSection.Bookmarks;

    public event EventHandler<string>? NavigateRequested;

    public SidePanel() => InitializeComponent();

    public void Initialize(BrowserServiceHost services, MainPageViewModel viewModel)
    {
        _services = services;
        _viewModel = viewModel;
        ApplyLocalizedStrings();
        ShowSection(_section);
    }

    public void ShowSection(SidePanelSection section)
    {
        _section = section;
        RefreshItems();
    }

    private void RefreshItems()
    {
        if (_services is null)
        {
            return;
        }

        ItemsList.Items.Clear();
        switch (_section)
        {
            case SidePanelSection.Bookmarks:
                foreach (var item in _services.BookmarkService.Entries.OrderBy(entry => entry.SortOrder))
                {
                    ItemsList.Items.Add(new SidePanelListItem(item.Title, item.Url));
                }

                break;
            case SidePanelSection.History:
                foreach (var item in _services.HistoryService.Entries.Take(200))
                {
                    ItemsList.Items.Add(new SidePanelListItem(item.Title, item.Url));
                }

                break;
            case SidePanelSection.Downloads:
                foreach (var item in _services.DownloadService.Items)
                {
                    ItemsList.Items.Add(new SidePanelListItem($"{item.FileName} ({item.State})", item.FilePath));
                }

                break;
            case SidePanelSection.Scripts:
                foreach (var item in _services.UserScriptService.Scripts)
                {
                    ItemsList.Items.Add(new SidePanelListItem(item.Name, item.MatchPattern));
                }

                break;
            case SidePanelSection.Passwords:
                foreach (var item in _services.PasswordService.Entries)
                {
                    ItemsList.Items.Add(new SidePanelListItem($"{item.Site} ({item.Username})", item.Site));
                }

                break;
        }
    }

    private void OnItemSelected(object sender, SelectionChangedEventArgs e)
    {
        if (ItemsList.SelectedItem is SidePanelListItem item &&
            !string.IsNullOrWhiteSpace(item.Url) &&
            (_section is SidePanelSection.Bookmarks or SidePanelSection.History))
        {
            NavigateRequested?.Invoke(this, item.Url);
        }
    }

    private void OnBookmarksTabClick(object sender, RoutedEventArgs e) => ShowSection(SidePanelSection.Bookmarks);

    private void OnHistoryTabClick(object sender, RoutedEventArgs e) => ShowSection(SidePanelSection.History);

    private void OnDownloadsTabClick(object sender, RoutedEventArgs e) => ShowSection(SidePanelSection.Downloads);

    private void OnScriptsTabClick(object sender, RoutedEventArgs e) => ShowSection(SidePanelSection.Scripts);

    public void ApplyLocalizedStrings()
    {
        BookmarksTabButton.Content = L.Get("SidePanelBookmarks");
        HistoryTabButton.Content = L.Get("SidePanelHistory");
        DownloadsTabButton.Content = L.Get("SidePanelDownloads");
        ScriptsTabButton.Content = L.Get("SidePanelScripts");
    }

    private sealed record SidePanelListItem(string Title, string Url)
    {
        public override string ToString() => Title;
    }
}
