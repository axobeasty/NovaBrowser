using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using NovaBrowser.Helpers;
using NovaBrowser.Models;
using NovaBrowser.Services;

namespace NovaBrowser.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    private readonly Stack<ClosedTabEntry> _closedTabs = new();
    private readonly BrowserServiceHost _services;

    public ObservableCollection<BrowserTabViewModel> Tabs { get; } = [];

    [ObservableProperty]
    private BrowserTabViewModel? _activeTab;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private bool _isFindBarOpen;

    [ObservableProperty]
    private string _findQuery = string.Empty;

    public bool ShowBookmarkBar => _services.SettingsService.Current.ShowBookmarkBar;

    public ObservableCollection<BookmarkEntry> BookmarkBarItems { get; } = [];

    public HistoryService History => _services.HistoryService;

    public BookmarkService Bookmarks => _services.BookmarkService;

    public DownloadService Downloads => _services.DownloadService;

    public string ActiveAddressBarText
    {
        get => ActiveTab?.AddressBarText ?? string.Empty;
        set
        {
            if (ActiveTab is not null)
            {
                ActiveTab.AddressBarText = value;
            }
        }
    }

    public MainPageViewModel(BrowserServiceHost services)
    {
        _services = services;
        RefreshBookmarkBar();
    }

    public void InitializeSession()
    {
        if (Tabs.Count > 0)
        {
            return;
        }

        var settings = _services.SettingsService.Current;
        var crash = _services.CrashRecoveryService.GetPendingRecovery();
        if (crash?.Tabs.Count > 0)
        {
            RestoreSession(crash.Tabs, crash.ActiveIndex);
            _services.CrashRecoveryService.MarkHealthyShutdown();
            StatusText = L.Get("SessionRestored");
            return;
        }

        if (settings.SessionRestore == SessionRestoreMode.HomePage)
        {
            OpenUrlInNewTab(Application.Current is App app ? app.BrowserPreferences.HomePage : BrowserSettings.HomePage);
            return;
        }

        var saved = _services.SessionService.LoadSession();
        if (settings.SessionRestore == SessionRestoreMode.Continue && saved?.Tabs.Count > 0)
        {
            RestoreSession(saved.Tabs, saved.ActiveIndex);
            StatusText = L.Get("SessionRestored");
            return;
        }

        OpenUrlInNewTab(BrowserSettings.NewTabPage);
    }

    public void SaveSession()
    {
        if (_services.ProfileService.IsPrivateMode)
        {
            return;
        }

        var activeIndex = ActiveTab is null ? 0 : Math.Max(0, Tabs.IndexOf(ActiveTab));
        _services.SessionService.SaveSession(Tabs.Select(tab => tab.ToSessionEntry()), activeIndex);
        _services.CrashRecoveryService.MarkHealthyShutdown();
    }

    public void MarkCrashRecovery()
    {
        if (_services.ProfileService.IsPrivateMode)
        {
            return;
        }

        var activeIndex = ActiveTab is null ? 0 : Math.Max(0, Tabs.IndexOf(ActiveTab));
        _services.CrashRecoveryService.MarkUncleanShutdown(Tabs.Select(tab => tab.ToSessionEntry()), activeIndex);
    }

    [RelayCommand]
    private void NewTab() => OpenUrlInNewTab(BrowserSettings.NewTabPage);

    public void OpenUrlInNewTab(string url)
    {
        var tab = new BrowserTabViewModel();
        if (url.Equals(BrowserSettings.NewTabPage, StringComparison.OrdinalIgnoreCase))
        {
            tab.Title = L.Get("NewTabTitle");
        }

        Tabs.Add(tab);
        ActiveTab = tab;
        tab.RequestNavigation(url);
    }

    public void RestoreSession(IReadOnlyList<SessionTabEntry> entries, int activeIndex)
    {
        Tabs.Clear();
        foreach (var entry in entries)
        {
            var tab = new BrowserTabViewModel();
            tab.ApplySessionEntry(entry);
            Tabs.Add(tab);
        }

        if (Tabs.Count == 0)
        {
            NewTab();
            return;
        }

        ActiveTab = Tabs[Math.Clamp(activeIndex, 0, Tabs.Count - 1)];
        foreach (var tab in Tabs)
        {
            tab.RequestNavigation(tab.Url);
        }
    }

    public void RefreshLocalization()
    {
        foreach (var tab in Tabs)
        {
            tab.RefreshLocalizedTitle();
        }
    }

    [RelayCommand]
    private void CloseTab(BrowserTabViewModel? tab)
    {
        if (tab is null || !Tabs.Contains(tab))
        {
            return;
        }

        if (!tab.IsPinned)
        {
            _closedTabs.Push(new ClosedTabEntry { Url = tab.Url, Title = tab.Title });
        }

        var index = Tabs.IndexOf(tab);
        Tabs.Remove(tab);

        if (Tabs.Count == 0)
        {
            NewTab();
            return;
        }

        if (ActiveTab == tab)
        {
            ActiveTab = Tabs[Math.Min(index, Tabs.Count - 1)];
        }
    }

    public void CloseActiveTab()
    {
        if (ActiveTab is not null)
        {
            CloseTab(ActiveTab);
        }
    }

    public void ReopenClosedTab()
    {
        if (_closedTabs.Count == 0)
        {
            return;
        }

        var closed = _closedTabs.Pop();
        OpenUrlInNewTab(closed.Url);
        if (ActiveTab is not null)
        {
            ActiveTab.Title = closed.Title;
        }
    }

    public void MoveTab(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || newIndex < 0 || oldIndex >= Tabs.Count || newIndex >= Tabs.Count || oldIndex == newIndex)
        {
            return;
        }

        var tab = Tabs[oldIndex];
        Tabs.RemoveAt(oldIndex);
        Tabs.Insert(newIndex, tab);
    }

    public void PinTab(BrowserTabViewModel? tab)
    {
        tab ??= ActiveTab;
        if (tab is null)
        {
            return;
        }

        tab.IsPinned = !tab.IsPinned;
        var index = Tabs.IndexOf(tab);
        if (tab.IsPinned && index > 0)
        {
            Tabs.RemoveAt(index);
            Tabs.Insert(0, tab);
        }
    }

    public void DuplicateTab(BrowserTabViewModel? tab)
    {
        tab ??= ActiveTab;
        if (tab is null)
        {
            return;
        }

        OpenUrlInNewTab(tab.Url);
    }

    public void CloseOtherTabs(BrowserTabViewModel? tab)
    {
        tab ??= ActiveTab;
        if (tab is null)
        {
            return;
        }

        foreach (var other in Tabs.Where(t => t != tab && !t.IsPinned).ToList())
        {
            CloseTab(other);
        }
    }

    public void CloseTabsToRight(BrowserTabViewModel? tab)
    {
        tab ??= ActiveTab;
        if (tab is null)
        {
            return;
        }

        var index = Tabs.IndexOf(tab);
        foreach (var other in Tabs.Skip(index + 1).Where(t => !t.IsPinned).ToList())
        {
            CloseTab(other);
        }
    }

    public void MuteTab(BrowserTabViewModel? tab)
    {
        tab ??= ActiveTab;
        if (tab is null)
        {
            return;
        }

        tab.IsMuted = !tab.IsMuted;
    }

    public void AssignTabGroup(BrowserTabViewModel? tab, string? groupId, string? color)
    {
        tab ??= ActiveTab;
        if (tab is null)
        {
            return;
        }

        tab.GroupId = groupId;
        tab.GroupColor = color;
    }

    public void ToggleBookmarkForActiveTab()
    {
        if (ActiveTab is null || ActiveTab.Url.Equals(BrowserSettings.NewTabPage, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var existing = _services.BookmarkService.FindByUrl(ActiveTab.Url);
        if (existing is not null)
        {
            _services.BookmarkService.RemoveBookmark(existing.Id);
            StatusText = L.Get("BookmarkRemoved");
        }
        else
        {
            _services.BookmarkService.AddBookmark(ActiveTab.Title, ActiveTab.Url);
            StatusText = L.Get("BookmarkAdded");
        }

        RefreshBookmarkBar();
    }

    public void RefreshBookmarkBar()
    {
        BookmarkBarItems.Clear();
        foreach (var bookmark in _services.BookmarkService.Entries.OrderBy(entry => entry.SortOrder).Take(12))
        {
            BookmarkBarItems.Add(bookmark);
        }
    }

    public void RecordHistory(BrowserTabViewModel tab) =>
        _services.HistoryService.AddVisit(tab.Title, tab.Url);

    public void SelectRelativeTab(int direction)
    {
        if (Tabs.Count == 0 || ActiveTab is null)
        {
            return;
        }

        var index = Tabs.IndexOf(ActiveTab);
        if (index < 0)
        {
            return;
        }

        var nextIndex = (index + direction + Tabs.Count) % Tabs.Count;
        ActiveTab = Tabs[nextIndex];
    }

    public void SelectTabByNumber(int number)
    {
        if (Tabs.Count == 0)
        {
            return;
        }

        if (number >= 9)
        {
            ActiveTab = Tabs[^1];
            return;
        }

        var index = number - 1;
        if (index >= 0 && index < Tabs.Count)
        {
            ActiveTab = Tabs[index];
        }
    }

    [RelayCommand(CanExecute = nameof(CanNavigateActiveTab))]
    private void Navigate() => ActiveTab?.RequestNavigation();

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void GoBack() => ActiveTab?.RequestGoBack();

    [RelayCommand(CanExecute = nameof(CanGoForward))]
    private void GoForward() => ActiveTab?.RequestGoForward();

    [RelayCommand(CanExecute = nameof(CanNavigateActiveTab))]
    private void Reload() => ActiveTab?.RequestReload();

    [RelayCommand(CanExecute = nameof(CanNavigateActiveTab))]
    private void GoHome()
    {
        if (Application.Current is App app)
        {
            ActiveTab?.RequestNavigation(app.BrowserPreferences.HomePage);
        }
    }

    public void ToggleFindBar()
    {
        IsFindBarOpen = !IsFindBarOpen;
        if (!IsFindBarOpen)
        {
            ActiveTab?.RequestFindStop();
        }
    }

    public void ApplyFindQuery(bool forward = true, bool matchCase = false)
    {
        ActiveTab?.RequestFind(FindQuery, forward, matchCase);
    }

    public void ZoomActiveTab(double delta)
    {
        if (ActiveTab is null)
        {
            return;
        }

        ActiveTab.ZoomFactor = Math.Clamp(ActiveTab.ZoomFactor + delta, 0.25, 4.0);
        ActiveTab.RequestZoom(ActiveTab.ZoomFactor);
    }

    public void ResetZoomActiveTab()
    {
        if (ActiveTab is null)
        {
            return;
        }

        ActiveTab.ZoomFactor = 1.0;
        ActiveTab.RequestZoom(1.0);
    }

    partial void OnActiveTabChanged(BrowserTabViewModel? value)
    {
        NotifyNavigationCommands();
        OnPropertyChanged(nameof(ActiveAddressBarText));
    }

    public void NotifyNavigationCommands()
    {
        NavigateCommand.NotifyCanExecuteChanged();
        GoBackCommand.NotifyCanExecuteChanged();
        GoForwardCommand.NotifyCanExecuteChanged();
        ReloadCommand.NotifyCanExecuteChanged();
        GoHomeCommand.NotifyCanExecuteChanged();
    }

    private bool CanNavigateActiveTab() => ActiveTab is not null;

    private bool CanGoBack() => ActiveTab?.CanGoBack == true;

    private bool CanGoForward() => ActiveTab?.CanGoForward == true;
}
