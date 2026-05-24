using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using NovaBrowser.Helpers;
using NovaBrowser.Models;
using NovaBrowser.Services;

namespace NovaBrowser.Controls;

public sealed partial class FeaturePage : Page
{
    private FeatureWindowKind _kind;
    private BrowserServiceHost? _services;
    private List<FeatureListItem> _allItems = [];

    public event EventHandler<string>? NavigateRequested;

    public FeaturePage() => InitializeComponent();

    public void Initialize(FeatureWindowKind kind, BrowserServiceHost services)
    {
        _kind = kind;
        _services = services;
        ApplyLocalizedStrings();
        ConfigureActions();
        ReloadItems();
    }

    public void ReloadItems()
    {
        if (_services is null)
        {
            return;
        }

        _allItems = _kind switch
        {
            FeatureWindowKind.Bookmarks => _services.BookmarkService.Entries
                .OrderBy(entry => entry.SortOrder)
                .Select(entry => new FeatureListItem(entry.Id, entry.Title, entry.Url, entry.Url))
                .ToList(),
            FeatureWindowKind.History => _services.HistoryService.Entries
                .Take(500)
                .Select(entry => new FeatureListItem(entry.Id, entry.Title, entry.Url, entry.VisitedAt.ToLocalTime().ToString("g")))
                .ToList(),
            FeatureWindowKind.Downloads => _services.DownloadService.Items
                .Select(entry => new FeatureListItem(
                    entry.Id,
                    entry.FileName,
                    entry.FilePath,
                    $"{entry.State} • {FormatBytes(entry.ReceivedBytes)} / {FormatBytes(entry.TotalBytes)}"))
                .ToList(),
            FeatureWindowKind.UserScripts => _services.UserScriptService.Scripts
                .Select(entry => new FeatureListItem(entry.Id, entry.Name, entry.MatchPattern, entry.IsEnabled ? L.Get("FeatureEnabled") : L.Get("FeatureDisabled")))
                .ToList(),
            FeatureWindowKind.Passwords => _services.PasswordService.Entries
                .Select(entry => new FeatureListItem(entry.Id, entry.Site, entry.Username, entry.Username))
                .ToList(),
            _ => [],
        };

        ApplyFilter(SearchBox.Text);
    }

    private void ConfigureActions()
    {
        SearchBox.Visibility = _kind is FeatureWindowKind.Bookmarks or FeatureWindowKind.History
            ? Visibility.Visible
            : Visibility.Collapsed;

        PrimaryActionButton.Visibility = Visibility.Visible;
        SecondaryActionButton.Visibility = _kind switch
        {
            FeatureWindowKind.Bookmarks or FeatureWindowKind.History or FeatureWindowKind.Downloads or FeatureWindowKind.UserScripts => Visibility.Visible,
            _ => Visibility.Collapsed,
        };

        switch (_kind)
        {
            case FeatureWindowKind.Bookmarks:
                PrimaryActionButton.Content = L.Get("FeatureRefresh");
                SecondaryActionButton.Content = L.Get("FeatureDeleteSelected");
                break;
            case FeatureWindowKind.History:
                PrimaryActionButton.Content = L.Get("FeatureClearAll");
                SecondaryActionButton.Content = L.Get("FeatureDeleteSelected");
                break;
            case FeatureWindowKind.Downloads:
                PrimaryActionButton.Content = L.Get("FeatureOpenFolder");
                SecondaryActionButton.Content = L.Get("FeatureClearCompleted");
                break;
            case FeatureWindowKind.UserScripts:
                PrimaryActionButton.Content = L.Get("FeatureAddSampleScript");
                SecondaryActionButton.Content = L.Get("FeatureDeleteSelected");
                break;
            case FeatureWindowKind.Passwords:
                PrimaryActionButton.Content = L.Get("FeatureRefresh");
                SecondaryActionButton.Visibility = Visibility.Collapsed;
                break;
        }
    }

    public void ApplyLocalizedStrings()
    {
        TitleText.Text = _kind switch
        {
            FeatureWindowKind.Bookmarks => L.Get("WindowBookmarksTitle"),
            FeatureWindowKind.History => L.Get("WindowHistoryTitle"),
            FeatureWindowKind.Downloads => L.Get("WindowDownloadsTitle"),
            FeatureWindowKind.UserScripts => L.Get("WindowScriptsTitle"),
            FeatureWindowKind.Passwords => L.Get("WindowPasswordsTitle"),
            _ => string.Empty,
        };

        SearchBox.PlaceholderText = L.Get("FeatureSearchPlaceholder");
        ConfigureActions();
    }

    private void ApplyFilter(string? query)
    {
        var filtered = string.IsNullOrWhiteSpace(query)
            ? _allItems
            : _allItems.Where(item =>
                item.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                item.Subtitle.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

        ItemsList.ItemsSource = filtered.Select(item => new FeatureListDisplayItem(item.Title, item.Subtitle)).ToList();
        ItemsList.Tag = filtered;
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs e) => ApplyFilter(SearchBox.Text);

    private void OnItemSelected(object sender, SelectionChangedEventArgs e)
    {
        if (_kind is not (FeatureWindowKind.Bookmarks or FeatureWindowKind.History))
        {
            return;
        }

        if (GetSelectedItem() is { } item && !string.IsNullOrWhiteSpace(item.NavigateUrl))
        {
            NavigateRequested?.Invoke(this, item.NavigateUrl);
        }
    }

    private void OnItemDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (_kind == FeatureWindowKind.Downloads && GetSelectedItem() is { NavigateUrl: var path } && File.Exists(path))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
            });
        }
    }

    private FeatureListItem? GetSelectedItem()
    {
        if (ItemsList.Tag is not List<FeatureListItem> items || ItemsList.SelectedIndex < 0 || ItemsList.SelectedIndex >= items.Count)
        {
            return null;
        }

        return items[ItemsList.SelectedIndex];
    }

    private void OnPrimaryActionClick(object sender, RoutedEventArgs e)
    {
        if (_services is null)
        {
            return;
        }

        switch (_kind)
        {
            case FeatureWindowKind.Bookmarks:
            case FeatureWindowKind.Passwords:
                ReloadItems();
                break;
            case FeatureWindowKind.History:
                _services.HistoryService.Clear();
                ReloadItems();
                break;
            case FeatureWindowKind.Downloads:
                OpenDownloadsFolder();
                break;
            case FeatureWindowKind.UserScripts:
                _services.UserScriptService.AddScript(
                    L.Get("FeatureSampleScriptName"),
                    "*://*/*",
                    "console.log('NovaBrowser userscript');");
                ReloadItems();
                break;
        }
    }

    private void OnSecondaryActionClick(object sender, RoutedEventArgs e)
    {
        if (_services is null)
        {
            return;
        }

        var selected = GetSelectedItem();
        switch (_kind)
        {
            case FeatureWindowKind.Bookmarks when selected is not null:
                _services.BookmarkService.RemoveBookmark(selected.Id);
                ReloadItems();
                break;
            case FeatureWindowKind.History when selected is not null:
                _services.HistoryService.DeleteEntry(selected.Id);
                ReloadItems();
                break;
            case FeatureWindowKind.Downloads:
                _services.DownloadService.ClearCompleted();
                ReloadItems();
                break;
            case FeatureWindowKind.UserScripts when selected is not null:
                _services.UserScriptService.RemoveScript(selected.Id);
                ReloadItems();
                break;
        }
    }

    private void OpenDownloadsFolder()
    {
        var directory = Application.Current is App app
            ? app.SettingsService.Current.DownloadDirectory
            : string.Empty;

        if (string.IsNullOrWhiteSpace(directory))
        {
            directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        }

        Directory.CreateDirectory(directory);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = directory,
            UseShellExecute = true,
        });
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0)
        {
            return "0 B";
        }

        string[] units = ["B", "KB", "MB", "GB"];
        var order = (int)Math.Log(bytes, 1024);
        order = Math.Clamp(order, 0, units.Length - 1);
        return $"{bytes / Math.Pow(1024, order):0.#} {units[order]}";
    }

    private sealed record FeatureListItem(Guid Id, string Title, string NavigateUrl, string Subtitle);

    private sealed class FeatureListDisplayItem
    {
        public FeatureListDisplayItem(string title, string subtitle)
        {
            Title = title;
            Subtitle = subtitle;
        }

        public string Title { get; }
        public string Subtitle { get; }
    }
}
