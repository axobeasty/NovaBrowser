using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NovaBrowser.Models;

namespace NovaBrowser.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    public ObservableCollection<BrowserTabViewModel> Tabs { get; } = [];

    [ObservableProperty]
    private BrowserTabViewModel? _activeTab;

    [ObservableProperty]
    private string _statusText = "Готов";

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

    public MainPageViewModel()
    {
        NewTab();
    }

    [RelayCommand]
    private void NewTab()
    {
        OpenUrlInNewTab(BrowserSettings.NewTabPage);
    }

    public void OpenUrlInNewTab(string url)
    {
        var tab = new BrowserTabViewModel();
        Tabs.Add(tab);
        ActiveTab = tab;
        tab.RequestNavigation(url);
    }

    [RelayCommand]
    private void CloseTab(BrowserTabViewModel? tab)
    {
        if (tab is null || !Tabs.Contains(tab))
        {
            return;
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

    [RelayCommand(CanExecute = nameof(CanNavigateActiveTab))]
    private void Navigate() => ActiveTab?.RequestNavigation();

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void GoBack() => ActiveTab?.RequestGoBack();

    [RelayCommand(CanExecute = nameof(CanGoForward))]
    private void GoForward() => ActiveTab?.RequestGoForward();

    [RelayCommand(CanExecute = nameof(CanNavigateActiveTab))]
    private void Reload() => ActiveTab?.RequestReload();

    [RelayCommand(CanExecute = nameof(CanNavigateActiveTab))]
    private void GoHome() => ActiveTab?.RequestNavigation(BrowserSettings.HomePage);

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
