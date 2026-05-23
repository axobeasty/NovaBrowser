using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using NovaBrowser.Helpers;
using NovaBrowser.Models;
using NovaBrowser.Services;
using NovaBrowser.ViewModels;

namespace NovaBrowser.Controls;

public sealed partial class SettingsPanel : UserControl
{
    private SettingsViewModel? _viewModel;
    private bool _suppressLanguageChange;
    private bool _suppressSearchEngineChange;
    private bool _suppressSectionChange;

    public event EventHandler? CloseRequested;
    public event EventHandler? CheckUpdatesRequested;

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

        BindGeneralSection();
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

    private void OnLanguageChanged(object? sender, EventArgs e) => ApplyLocalizedStrings();

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

        _suppressLanguageChange = false;
        _suppressSearchEngineChange = false;
    }

    private void ApplyLocalizedStrings()
    {
        HeaderTitleText.Text = L.Get("SettingsTitle");
        GeneralNavItem.Content = CreateNavItem(L.Get("SettingsSectionGeneral"));
        AppearanceNavItem.Content = CreateNavItem(L.Get("SettingsSectionAppearance"));
        AboutNavItem.Content = CreateNavItem(L.Get("SettingsSectionAbout"));

        LanguageLabelText.Text = L.Get("LanguageLabel");
        HomePageLabelText.Text = L.Get("SettingsHomePageLabel");
        HomePageBox.PlaceholderText = L.Get("SettingsHomePagePlaceholder");
        SearchEngineLabelText.Text = L.Get("SettingsSearchEngineLabel");
        CustomSearchEngineLabelText.Text = L.Get("SettingsCustomSearchEngineLabel");
        CustomSearchEngineBox.PlaceholderText = L.Get("SettingsCustomSearchEnginePlaceholder");
        CustomSearchEngineHintText.Text = L.Get("SettingsCustomSearchEngineHint");

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
    }

    private static Brush GetBrush(string key) =>
        (Brush)Application.Current.Resources[key];

    private void ShowSection(string sectionTag)
    {
        GeneralSection.Visibility = sectionTag == "general" ? Visibility.Visible : Visibility.Collapsed;
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

    private void OnCheckUpdatesClick(object sender, RoutedEventArgs e) =>
        CheckUpdatesRequested?.Invoke(this, EventArgs.Empty);

    private void OnApplyClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.SaveGeneralSettings();
        _viewModel.Theme.CommitThemeChanges();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        _viewModel?.Theme.RevertThemeChanges();
        _viewModel?.LoadFromSettings();
        BindGeneralSection();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) =>
        OnCancelClick(sender, e);
}
