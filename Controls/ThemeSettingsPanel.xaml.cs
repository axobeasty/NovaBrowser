using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using NovaBrowser.Helpers;
using NovaBrowser.Models;
using NovaBrowser.Services;
using NovaBrowser.ViewModels;
using Windows.UI;

namespace NovaBrowser.Controls;

public sealed partial class ThemeSettingsPanel : UserControl
{
    private ThemeSettingsViewModel? _viewModel;
    private Flyout? _colorFlyout;
    private ThemeColorItemViewModel? _activeColorItem;
    private bool _embeddedMode;

    public event EventHandler? CloseRequested;

    public bool EmbeddedMode
    {
        get => _embeddedMode;
        set
        {
            _embeddedMode = value;
            UpdateEmbeddedChrome();
        }
    }

    public ThemeSettingsPanel()
    {
        InitializeComponent();
        Loaded += OnPanelLoaded;
        Unloaded += OnPanelUnloaded;
    }

    private void OnPanelLoaded(object sender, RoutedEventArgs e)
    {
        if (Application.Current is App app)
        {
            app.ThemeService.ThemeChanged += OnThemeChanged;
            app.Localization.LanguageChanged += OnLanguageChanged;
        }

        ApplyPanelTheme();
        ApplyLocalizedStrings();
    }

    private void OnPanelUnloaded(object sender, RoutedEventArgs e)
    {
        if (Application.Current is App app)
        {
            app.ThemeService.ThemeChanged -= OnThemeChanged;
            app.Localization.LanguageChanged -= OnLanguageChanged;
        }
    }

    private void OnThemeChanged(object? sender, Models.BrowserTheme e) =>
        ApplyPanelTheme();

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        ApplyLocalizedStrings();
        _viewModel?.RefreshLocalization();
        RebuildColorGroups();
    }

    private void ApplyLocalizedStrings()
    {
        HeaderTitleText.Text = L.Get("ThemePanelTitle");
        HeaderSubtitleText.Text = L.Get("ThemePanelSubtitle");
        ModeLabelText.Text = L.Get("ThemeModeLabel");
        SavedThemesLabelText.Text = L.Get("SavedThemesLabel");
        ThemeNameLabelText.Text = L.Get("ThemeNameLabel");
        ThemeNameBox.PlaceholderText = L.Get("DefaultThemeName");
        LoadLightButton.Content = L.Get("ThemeLightButton");
        LoadDarkButton.Content = L.Get("ThemeDarkButton");
        SaveCustomButton.Content = L.Get("ThemeSaveCustom");
        DeleteThemeButton.Content = L.Get("ThemeDelete");
        ColorConstructorTitleText.Text = L.Get("ColorConstructorTitle");
        ColorConstructorHintText.Text = L.Get("ColorConstructorHint");
        CancelButton.Content = L.Get("Cancel");
        ApplyButton.Content = L.Get("Apply");
        ToolTipService.SetToolTip(CloseButton, L.Get("Close"));

        RefreshModeComboBox();
    }

    private void UpdateEmbeddedChrome()
    {
        var chromeVisibility = EmbeddedMode ? Visibility.Collapsed : Visibility.Visible;
        HeaderGrid.Visibility = chromeVisibility;
        FooterGrid.Visibility = chromeVisibility;
    }

    private void ApplyPanelTheme()
    {
        PanelRoot.Background = GetBrush("NovaContentBackgroundBrush");
        HeaderGrid.Background = GetBrush("NovaToolbarBackgroundBrush");
        HeaderGrid.BorderBrush = GetBrush("NovaTabStripDividerBrush");
        FooterGrid.Background = GetBrush("NovaToolbarBackgroundBrush");
        FooterGrid.BorderBrush = GetBrush("NovaTabStripDividerBrush");
        HeaderTitleText.Foreground = GetBrush("NovaTextPrimaryBrush");
        HeaderSubtitleText.Foreground = GetBrush("NovaTextSecondaryBrush");
    }

    private static Brush GetBrush(string key) =>
        (Brush)Application.Current.Resources[key];

    public void Initialize(ThemeSettingsViewModel viewModel)
    {
        _viewModel = viewModel;

        if (!EmbeddedMode)
        {
            _viewModel.CloseRequested += (_, _) => CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        CustomThemeComboBox.ItemsSource = viewModel.CustomThemes;
        if (!string.IsNullOrWhiteSpace(viewModel.SelectedCustomThemeId))
        {
            CustomThemeComboBox.SelectedItem = viewModel.CustomThemes.FirstOrDefault(t => t.Id == viewModel.SelectedCustomThemeId);
        }

        ThemeNameBox.Text = viewModel.CustomThemeName;
        ApplyLocalizedStrings();
        UpdateEmbeddedChrome();

        ModeComboBox.SelectedItem = viewModel.GetModeOptions().First(m => m.Mode == viewModel.SelectedMode);

        RebuildColorGroups();
        UpdateCustomPanels();
    }

    private void RefreshModeComboBox()
    {
        if (_viewModel is null)
        {
            return;
        }

        var selectedMode = _viewModel.SelectedMode;
        ModeComboBox.ItemsSource = _viewModel.GetModeOptions();
        ModeComboBox.SelectedItem = _viewModel.GetModeOptions().First(m => m.Mode == selectedMode);
    }

    private void RebuildColorGroups()
    {
        if (_viewModel is null)
        {
            return;
        }

        ColorGroupsHost.Children.Clear();

        foreach (var group in _viewModel.ColorProperties.GroupBy(p => p.Category))
        {
            var section = new StackPanel { Spacing = 8 };
            section.Children.Add(new TextBlock
            {
                Text = group.Key,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = (Brush)Application.Current.Resources["NovaTextPrimaryBrush"],
            });

            foreach (var item in group)
            {
                section.Children.Add(BuildColorRow(item));
            }

            ColorGroupsHost.Children.Add(section);
        }
    }

    private UIElement BuildColorRow(ThemeColorItemViewModel item)
    {
        var grid = new Grid { ColumnSpacing = 12, Padding = new Thickness(0, 4, 0, 4) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var label = new TextBlock
        {
            Text = item.DisplayName,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (Brush)Application.Current.Resources["NovaTextPrimaryBrush"],
        };

        var swatch = new Border
        {
            Width = 32,
            Height = 32,
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            BorderBrush = (Brush)Application.Current.Resources["NovaTabBorderBrush"],
        };

        var hexBox = new TextBox
        {
            Text = item.HexValue,
            FontFamily = new FontFamily("Consolas"),
            PlaceholderText = "#RRGGBB",
            VerticalAlignment = VerticalAlignment.Center,
        };

        var pickerButton = new Button
        {
            Style = (Style)Application.Current.Resources["SubtleButtonStyle"],
            Padding = new Thickness(8),
            VerticalAlignment = VerticalAlignment.Center,
            Content = new FontIcon { Glyph = "\uE790", FontSize = 14 },
        };

        void SyncSwatch() => swatch.Background = new SolidColorBrush(item.Color);

        item.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ThemeColorItemViewModel.HexValue))
            {
                hexBox.Text = item.HexValue;
            }

            if (args.PropertyName is nameof(ThemeColorItemViewModel.HexValue) or nameof(ThemeColorItemViewModel.Color))
            {
                SyncSwatch();
            }

            if (args.PropertyName == nameof(ThemeColorItemViewModel.DisplayName))
            {
                label.Text = item.DisplayName;
            }
        };

        hexBox.TextChanged += (_, _) =>
        {
            if (!string.Equals(hexBox.Text, item.HexValue, StringComparison.OrdinalIgnoreCase))
            {
                item.HexValue = hexBox.Text;
            }
        };

        pickerButton.Click += (_, _) => ShowColorPicker(item, swatch);

        SyncSwatch();

        Grid.SetColumn(label, 0);
        Grid.SetColumn(swatch, 1);
        Grid.SetColumn(hexBox, 2);
        Grid.SetColumn(pickerButton, 3);

        grid.Children.Add(label);
        grid.Children.Add(swatch);
        grid.Children.Add(hexBox);
        grid.Children.Add(pickerButton);

        return grid;
    }

    private void ShowColorPicker(ThemeColorItemViewModel item, Border swatch)
    {
        _activeColorItem = item;

        var picker = new ColorPicker
        {
            Color = item.Color,
            IsColorChannelTextInputVisible = true,
            IsColorPreviewVisible = true,
            IsColorSliderVisible = true,
            IsHexInputVisible = true,
        };

        picker.ColorChanged += (_, args) =>
        {
            if (_activeColorItem is null)
            {
                return;
            }

            _activeColorItem.SetHex(ThemeColorHelper.ToHex(args.NewColor, includeAlpha: args.NewColor.A != 255));
            swatch.Background = new SolidColorBrush(args.NewColor);
        };

        _colorFlyout = new Flyout
        {
            Content = picker,
            Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Bottom,
        };

        _colorFlyout.ShowAt(swatch);
    }

    private void UpdateCustomPanels()
    {
        if (_viewModel is null)
        {
            return;
        }

        CustomThemePickerPanel.Visibility = _viewModel.IsCustomMode ? Visibility.Visible : Visibility.Collapsed;
        DeleteThemeButton.Visibility = _viewModel.CanDeleteCustomTheme ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnModeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel is null || ModeComboBox.SelectedItem is not ThemeModeOption option)
        {
            return;
        }

        _viewModel.SelectedMode = option.Mode;
        ThemeNameBox.Text = _viewModel.CustomThemeName;
        CustomThemeComboBox.ItemsSource = _viewModel.CustomThemes;
        UpdateCustomPanels();
    }

    private void OnCustomThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel is null || CustomThemeComboBox.SelectedItem is not BrowserTheme theme)
        {
            return;
        }

        _viewModel.SelectedCustomThemeId = theme.Id;
        ThemeNameBox.Text = _viewModel.CustomThemeName;
        RebuildColorGroups();
        UpdateCustomPanels();
    }

    private void OnThemeNameChanged(object sender, TextChangedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.CustomThemeName = ThemeNameBox.Text;
            _viewModel.WorkingTheme.Name = ThemeNameBox.Text;
        }
    }

    private void OnLoadLightClick(object sender, RoutedEventArgs e)
    {
        _viewModel?.LoadLightPresetCommand.Execute(null);
        if (_viewModel is not null)
        {
            ModeComboBox.SelectedItem = _viewModel.GetModeOptions().First(m => m.Mode == ThemeSelectionType.Light);
            RebuildColorGroups();
            UpdateCustomPanels();
        }
    }

    private void OnLoadDarkClick(object sender, RoutedEventArgs e)
    {
        _viewModel?.LoadDarkPresetCommand.Execute(null);
        if (_viewModel is not null)
        {
            ModeComboBox.SelectedItem = _viewModel.GetModeOptions().First(m => m.Mode == ThemeSelectionType.Dark);
            RebuildColorGroups();
            UpdateCustomPanels();
        }
    }

    private void OnSaveCustomClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.CustomThemeName = ThemeNameBox.Text;
        _viewModel.SaveAsCustomThemeCommand.Execute(null);
        CustomThemeComboBox.ItemsSource = _viewModel.CustomThemes;
        CustomThemeComboBox.SelectedItem = _viewModel.CustomThemes.FirstOrDefault(t => t.Id == _viewModel.SelectedCustomThemeId);
        ModeComboBox.SelectedItem = _viewModel.GetModeOptions().First(m => m.Mode == ThemeSelectionType.Custom);
        RebuildColorGroups();
        UpdateCustomPanels();
    }

    private void OnDeleteCustomClick(object sender, RoutedEventArgs e)
    {
        _viewModel?.DeleteSelectedCustomThemeCommand.Execute(null);
        if (_viewModel is null)
        {
            return;
        }

        CustomThemeComboBox.ItemsSource = _viewModel.CustomThemes;
        ModeComboBox.SelectedItem = _viewModel.GetModeOptions().First(m => m.Mode == _viewModel.SelectedMode);
        ThemeNameBox.Text = _viewModel.CustomThemeName;
        RebuildColorGroups();
        UpdateCustomPanels();
    }

    private void OnApplyClick(object sender, RoutedEventArgs e) =>
        _viewModel?.ApplyThemeCommand.Execute(null);

    private void OnCancelClick(object sender, RoutedEventArgs e) =>
        _viewModel?.CancelCommand.Execute(null);

    private void OnCloseClick(object sender, RoutedEventArgs e) =>
        _viewModel?.CancelCommand.Execute(null);
}
