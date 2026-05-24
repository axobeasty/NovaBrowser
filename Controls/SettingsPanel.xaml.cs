using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using NovaBrowser.Helpers;
using NovaBrowser.Models;
using NovaBrowser.Services;
using NovaBrowser.ViewModels;
using Windows.Storage.Pickers;

namespace NovaBrowser.Controls;

public sealed partial class SettingsPanel : UserControl
{
    private SettingsViewModel? _viewModel;
    private bool _suppressLanguageChange;
    private bool _suppressSearchEngineChange;
    private bool _suppressSectionChange;
    private bool _suppressProfileChange;

    public event EventHandler? CloseRequested;
    public event EventHandler? CheckUpdatesRequested;
    public event EventHandler<FeatureWindowKind>? OpenFeatureRequested;

    public SettingsPanel()
    {
        InitializeComponent();
        Loaded += OnPanelLoaded;
        Unloaded += OnPanelUnloaded;
    }

    public void Initialize(SettingsViewModel viewModel)
    {
        _viewModel = viewModel;
        ThemePanel.EmbeddedMode = true;
        ThemePanel.Initialize(viewModel.Theme);

        BindAllSections();
        ApplyLocalizedStrings();

        _suppressSectionChange = true;
        SectionList.SelectedItem = GeneralNavItem;
        _suppressSectionChange = false;

        ShowSection("general");
    }

    private void OnPanelLoaded(object sender, RoutedEventArgs e)
    {
        if (Application.Current is App app)
        {
            app.ThemeService.ThemeChanged += OnThemeChanged;
            app.Localization.LanguageChanged += OnLanguageChanged;
        }

        ApplyPanelTheme();
    }

    private void OnPanelUnloaded(object sender, RoutedEventArgs e)
    {
        if (Application.Current is App app)
        {
            app.ThemeService.ThemeChanged -= OnThemeChanged;
            app.Localization.LanguageChanged -= OnLanguageChanged;
        }
    }

    private void OnThemeChanged(object? sender, BrowserTheme e) => ApplyPanelTheme();

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        ApplyLocalizedStrings();
        BindAllSections();
    }

    private void BindAllSections()
    {
        BindGeneralSection();
        BindPrivacySection();
        BindDataSection();
        BindProfilesSection();
    }

    private void BindGeneralSection()
    {
        if (_viewModel is null)
        {
            return;
        }

        _suppressLanguageChange = true;
        _suppressSearchEngineChange = true;

        LanguageComboBox.ItemsSource = _viewModel.GetLanguageOptions();
        LanguageComboBox.SelectedItem = _viewModel.GetLanguageOptions()
            .FirstOrDefault(option => option.Code == _viewModel.UiLanguage)
            ?? _viewModel.GetLanguageOptions().First();

        SearchEngineComboBox.ItemsSource = _viewModel.GetSearchEngineOptions();
        SearchEngineComboBox.SelectedItem = _viewModel.GetSearchEngineOptions()
            .FirstOrDefault(option => option.Id == _viewModel.SearchEngineId)
            ?? _viewModel.GetSearchEngineOptions().First();

        HomePageBox.Text = _viewModel.HomePage;
        CustomSearchEngineBox.Text = _viewModel.CustomSearchEngineUrl;
        CustomSearchEnginePanel.Visibility = _viewModel.IsCustomSearchEngineVisible
            ? Visibility.Visible
            : Visibility.Collapsed;

        SessionRestoreComboBox.ItemsSource = _viewModel.GetSessionRestoreOptions();
        SessionRestoreComboBox.DisplayMemberPath = nameof(SessionRestoreOption.Title);
        SessionRestoreComboBox.SelectedItem = _viewModel.GetSessionRestoreOptions()
            .FirstOrDefault(option => option.Mode == _viewModel.SessionRestore)
            ?? _viewModel.GetSessionRestoreOptions().First();

        BookmarkBarToggle.IsOn = _viewModel.ShowBookmarkBar;

        _suppressLanguageChange = false;
        _suppressSearchEngineChange = false;
    }

    private void BindPrivacySection()
    {
        if (_viewModel is null)
        {
            return;
        }

        AdBlockToggle.IsOn = _viewModel.AdBlockEnabled;
        TelemetryToggle.IsOn = _viewModel.TelemetryEnabled;
    }

    private void BindDataSection()
    {
        if (_viewModel is null)
        {
            return;
        }

        DownloadDirectoryBox.Text = _viewModel.DownloadDirectory;
    }

    private void BindProfilesSection()
    {
        if (_viewModel is null)
        {
            return;
        }

        _suppressProfileChange = true;
        ProfilesComboBox.ItemsSource = _viewModel.Profiles;
        ProfilesComboBox.SelectedItem = _viewModel.Profiles
            .FirstOrDefault(profile => profile.Id == _viewModel.ActiveProfileId)
            ?? _viewModel.Profiles.FirstOrDefault();
        _suppressProfileChange = false;
    }

    private void ApplyLocalizedStrings()
    {
        HeaderTitleText.Text = L.Get("SettingsTitle");
        GeneralNavItem.Content = CreateNavItem(L.Get("SettingsSectionGeneral"));
        PrivacyNavItem.Content = CreateNavItem(L.Get("SettingsSectionPrivacy"));
        DataNavItem.Content = CreateNavItem(L.Get("SettingsSectionData"));
        ProfilesNavItem.Content = CreateNavItem(L.Get("SettingsSectionProfiles"));
        ToolsNavItem.Content = CreateNavItem(L.Get("SettingsSectionTools"));
        AppearanceNavItem.Content = CreateNavItem(L.Get("SettingsSectionAppearance"));
        AboutNavItem.Content = CreateNavItem(L.Get("SettingsSectionAbout"));

        LanguageLabelText.Text = L.Get("LanguageLabel");
        HomePageLabelText.Text = L.Get("SettingsHomePageLabel");
        HomePageBox.PlaceholderText = L.Get("SettingsHomePagePlaceholder");
        SearchEngineLabelText.Text = L.Get("SettingsSearchEngineLabel");
        CustomSearchEngineLabelText.Text = L.Get("SettingsCustomSearchEngineLabel");
        CustomSearchEngineBox.PlaceholderText = L.Get("SettingsCustomSearchEnginePlaceholder");
        CustomSearchEngineHintText.Text = L.Get("SettingsCustomSearchEngineHint");
        SessionRestoreLabelText.Text = L.Get("SettingsSessionRestoreLabel");
        BookmarkBarToggle.Header = L.Get("SettingsBookmarkBar");

        AdBlockToggle.Header = L.Get("SettingsAdBlock");
        TelemetryToggle.Header = L.Get("SettingsTelemetry");
        PrivacyHintText.Text = L.Get("SettingsPrivacyHint");
        ClearHistoryButton.Content = L.Get("SettingsClearHistory");
        ClearDownloadsButton.Content = L.Get("SettingsClearDownloads");
        ClearAllDataButton.Content = L.Get("SettingsClearData");

        DownloadDirectoryLabelText.Text = L.Get("SettingsDownloadDirectory");
        ImportChromeButton.Content = L.Get("SettingsImportChrome");
        ImportEdgeButton.Content = L.Get("SettingsImportEdge");
        ExportSyncButton.Content = L.Get("SettingsExportSync");
        ImportSyncButton.Content = L.Get("SettingsImportSync");
        DefaultBrowserButton.Content = L.Get("SettingsDefaultBrowser");

        ProfilesHintText.Text = L.Get("SettingsProfilesHint");
        NewProfileNameBox.PlaceholderText = L.Get("SettingsNewProfilePlaceholder");
        CreateProfileButton.Content = L.Get("SettingsCreateProfile");

        ToolsHintText.Text = L.Get("SettingsToolsHint");
        OpenBookmarksWindowButton.Content = L.Get("WindowBookmarksTitle");
        OpenHistoryWindowButton.Content = L.Get("WindowHistoryTitle");
        OpenDownloadsWindowButton.Content = L.Get("WindowDownloadsTitle");
        OpenScriptsWindowButton.Content = L.Get("WindowScriptsTitle");
        OpenPasswordsWindowButton.Content = L.Get("WindowPasswordsTitle");

        AboutDescriptionText.Text = L.Get("SettingsAboutDescription");
        AboutVersionText.Text = L.Format("SettingsAboutVersion", AppVersionService.CurrentVersionLabel);
        CheckUpdatesButton.Content = L.Get("SettingsCheckUpdatesButton");
        CancelButton.Content = L.Get("Cancel");
        ApplyButton.Content = L.Get("Apply");
        ToolTipService.SetToolTip(CloseButton, L.Get("Close"));

        if (_viewModel is not null)
        {
            SearchEngineComboBox.ItemsSource = _viewModel.GetSearchEngineOptions();
            LanguageComboBox.ItemsSource = _viewModel.GetLanguageOptions();
            SessionRestoreComboBox.ItemsSource = _viewModel.GetSessionRestoreOptions();
        }
    }

    private static TextBlock CreateNavItem(string text) =>
        new()
        {
            Text = text,
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        };

    private void ApplyPanelTheme()
    {
        PanelRoot.Background = GetBrush("NovaContentBackgroundBrush");
        HeaderGrid.Background = GetBrush("NovaToolbarBackgroundBrush");
        HeaderGrid.BorderBrush = GetBrush("NovaTabStripDividerBrush");
        FooterGrid.Background = GetBrush("NovaToolbarBackgroundBrush");
        FooterGrid.BorderBrush = GetBrush("NovaTabStripDividerBrush");
        NavigationColumn.Background = GetBrush("NovaToolbarBackgroundBrush");
        NavigationColumn.BorderBrush = GetBrush("NovaTabStripDividerBrush");
        ContentColumn.Background = GetBrush("NovaContentBackgroundBrush");
        HeaderTitleText.Foreground = GetBrush("NovaTextPrimaryBrush");
        AboutDescriptionText.Foreground = GetBrush("NovaTextSecondaryBrush");
        AboutVersionText.Foreground = GetBrush("NovaTextPrimaryBrush");
        PrivacyHintText.Foreground = GetBrush("NovaTextSecondaryBrush");
        ProfilesHintText.Foreground = GetBrush("NovaTextSecondaryBrush");
        ToolsHintText.Foreground = GetBrush("NovaTextSecondaryBrush");
    }

    private static Brush GetBrush(string key) =>
        (Brush)Application.Current.Resources[key];

    private void ShowSection(string sectionTag)
    {
        GeneralSection.Visibility = sectionTag == "general" ? Visibility.Visible : Visibility.Collapsed;
        PrivacySection.Visibility = sectionTag == "privacy" ? Visibility.Visible : Visibility.Collapsed;
        DataSection.Visibility = sectionTag == "data" ? Visibility.Visible : Visibility.Collapsed;
        ProfilesSection.Visibility = sectionTag == "profiles" ? Visibility.Visible : Visibility.Collapsed;
        ToolsSection.Visibility = sectionTag == "tools" ? Visibility.Visible : Visibility.Collapsed;
        AppearanceSection.Visibility = sectionTag == "appearance" ? Visibility.Visible : Visibility.Collapsed;
        AboutSection.Visibility = sectionTag == "about" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnSectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSectionChange || SectionList.SelectedItem is not ListViewItem item)
        {
            return;
        }

        ShowSection(item.Tag?.ToString() ?? "general");
    }

    private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressLanguageChange || _viewModel is null || LanguageComboBox.SelectedItem is not LanguageOption option)
        {
            return;
        }

        _viewModel.UiLanguage = option.Code;
    }

    private void OnSearchEngineSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSearchEngineChange || _viewModel is null || SearchEngineComboBox.SelectedItem is not SearchEngineOption option)
        {
            return;
        }

        _viewModel.SearchEngineId = option.Id;
        CustomSearchEnginePanel.Visibility = _viewModel.IsCustomSearchEngineVisible
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void OnHomePageChanged(object sender, TextChangedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.HomePage = HomePageBox.Text;
        }
    }

    private void OnCustomSearchEngineChanged(object sender, TextChangedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.CustomSearchEngineUrl = CustomSearchEngineBox.Text;
        }
    }

    private void OnSessionRestoreChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel is null || SessionRestoreComboBox.SelectedItem is not SessionRestoreOption option)
        {
            return;
        }

        _viewModel.SessionRestore = option.Mode;
    }

    private void OnBookmarkBarToggled(object sender, RoutedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.ShowBookmarkBar = BookmarkBarToggle.IsOn;
        }
    }

    private void OnAdBlockToggled(object sender, RoutedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.AdBlockEnabled = AdBlockToggle.IsOn;
        }
    }

    private void OnTelemetryToggled(object sender, RoutedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.TelemetryEnabled = TelemetryToggle.IsOn;
        }
    }

    private void OnDownloadDirectoryChanged(object sender, TextChangedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.DownloadDirectory = DownloadDirectoryBox.Text;
        }
    }

    private void OnProfileChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressProfileChange || _viewModel is null || ProfilesComboBox.SelectedItem is not UserProfile profile)
        {
            return;
        }

        _viewModel.ActiveProfileId = profile.Id;
    }

    private void OnCreateProfileClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel is null || string.IsNullOrWhiteSpace(NewProfileNameBox.Text))
        {
            return;
        }

        _viewModel.CreateProfile(NewProfileNameBox.Text.Trim());
        NewProfileNameBox.Text = string.Empty;
        BindProfilesSection();
    }

    private void OnClearHistoryClick(object sender, RoutedEventArgs e) => _viewModel?.ClearHistoryOnly();

    private void OnClearDownloadsClick(object sender, RoutedEventArgs e) => _viewModel?.ClearDownloadsOnly();

    private void OnClearDataClick(object sender, RoutedEventArgs e) => _viewModel?.ClearBrowsingData();

    private void OnImportChromeClick(object sender, RoutedEventArgs e) => _viewModel?.ImportChromeBookmarks();

    private void OnImportEdgeClick(object sender, RoutedEventArgs e) => _viewModel?.ImportEdgeBookmarks();

    private async void OnExportSyncClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        var picker = new FileSavePicker();
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeChoices.Add(L.Get("SettingsSyncFileType"), [".json"]);
        picker.SuggestedFileName = "NovaBrowser-sync";
        WinRT.Interop.InitializeWithWindow.Initialize(picker, App.WindowHandle);
        var file = await picker.PickSaveFileAsync();
        if (file is not null)
        {
            _viewModel.ExportSync(file.Path);
        }
    }

    private async void OnImportSyncClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        var picker = new FileOpenPicker();
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add(".json");
        WinRT.Interop.InitializeWithWindow.Initialize(picker, App.WindowHandle);
        var file = await picker.PickSingleFileAsync();
        if (file is not null)
        {
            _viewModel.ImportSync(file.Path);
            BindAllSections();
        }
    }

    private void OnDefaultBrowserClick(object sender, RoutedEventArgs e) =>
        _viewModel?.OpenDefaultAppsSettings();

    private void OnOpenBookmarksWindowClick(object sender, RoutedEventArgs e) =>
        OpenFeatureRequested?.Invoke(this, FeatureWindowKind.Bookmarks);

    private void OnOpenHistoryWindowClick(object sender, RoutedEventArgs e) =>
        OpenFeatureRequested?.Invoke(this, FeatureWindowKind.History);

    private void OnOpenDownloadsWindowClick(object sender, RoutedEventArgs e) =>
        OpenFeatureRequested?.Invoke(this, FeatureWindowKind.Downloads);

    private void OnOpenScriptsWindowClick(object sender, RoutedEventArgs e) =>
        OpenFeatureRequested?.Invoke(this, FeatureWindowKind.UserScripts);

    private void OnOpenPasswordsWindowClick(object sender, RoutedEventArgs e) =>
        OpenFeatureRequested?.Invoke(this, FeatureWindowKind.Passwords);

    private void OnCheckUpdatesClick(object sender, RoutedEventArgs e) =>
        CheckUpdatesRequested?.Invoke(this, EventArgs.Empty);

    private void OnApplyClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.SaveAllSettings();
        _viewModel.Theme.CommitThemeChanges();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        _viewModel?.Theme.RevertThemeChanges();
        _viewModel?.LoadFromSettings();
        BindAllSections();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) =>
        OnCancelClick(sender, e);
}
