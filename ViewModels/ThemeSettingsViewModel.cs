using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using NovaBrowser.Models;
using NovaBrowser.Services;
using System.Collections.ObjectModel;

namespace NovaBrowser.ViewModels;

public sealed partial class ThemeSettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly ThemeService _themeService;
    private bool _suppressSelectionReload;

    public event EventHandler? CloseRequested;

    public ObservableCollection<ThemeColorItemViewModel> ColorProperties { get; } = [];

    public ObservableCollection<BrowserTheme> CustomThemes { get; } = [];

    public IReadOnlyList<ThemeModeOption> ModeOptions { get; } =
    [
        new(ThemeSelectionType.System, "Системная"),
        new(ThemeSelectionType.Light, "Светлая"),
        new(ThemeSelectionType.Dark, "Тёмная"),
        new(ThemeSelectionType.Custom, "Своя тема"),
    ];

    [ObservableProperty]
    private ThemeSelectionType _selectedMode;

    [ObservableProperty]
    private BrowserTheme _workingTheme;

    [ObservableProperty]
    private string _customThemeName = "Моя тема";

    [ObservableProperty]
    private string? _selectedCustomThemeId;

    [ObservableProperty]
    private bool _isCustomMode;

    [ObservableProperty]
    private bool _canDeleteCustomTheme;

    public ThemeSettingsViewModel(SettingsService settingsService, ThemeService themeService)
    {
        _settingsService = settingsService;
        _themeService = themeService;
        _workingTheme = themeService.CurrentTheme.Clone();
        _selectedMode = settingsService.Current.ThemeSelection;
        _selectedCustomThemeId = settingsService.Current.ActiveCustomThemeId;

        ReloadCustomThemes();
        BuildColorProperties();
        UpdateCustomModeFlags();
    }

    [RelayCommand]
    private void PreviewTheme()
    {
        _themeService.ApplyTheme(WorkingTheme.Clone(), persist: false);
    }

    [RelayCommand]
    private void ApplyTheme()
    {
        if (SelectedMode == ThemeSelectionType.Custom)
        {
            var custom = WorkingTheme.Clone();
            custom.IsBuiltIn = false;

            if (string.IsNullOrWhiteSpace(custom.Name))
            {
                custom.Name = CustomThemeName;
            }

            if (string.IsNullOrWhiteSpace(custom.Id) || custom.IsBuiltIn)
            {
                custom.Id = Guid.NewGuid().ToString("N");
            }

            _themeService.SaveCustomTheme(custom);
        }
        else
        {
            _themeService.SetSelection(SelectedMode, WorkingTheme.Clone(), persist: true);
        }

        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void SaveAsCustomTheme()
    {
        var theme = WorkingTheme.Clone();
        theme.Id = Guid.NewGuid().ToString("N");
        theme.Name = string.IsNullOrWhiteSpace(CustomThemeName) ? "Моя тема" : CustomThemeName.Trim();
        theme.IsBuiltIn = false;

        _themeService.SaveCustomTheme(theme);

        _suppressSelectionReload = true;
        SelectedMode = ThemeSelectionType.Custom;
        SelectedCustomThemeId = theme.Id;
        _suppressSelectionReload = false;

        ReloadCustomThemes();
        WorkingTheme = theme.Clone();
        SyncColorPropertiesFromTheme();
        UpdateCustomModeFlags();
    }

    [RelayCommand]
    private void DeleteSelectedCustomTheme()
    {
        if (string.IsNullOrWhiteSpace(SelectedCustomThemeId))
        {
            return;
        }

        _themeService.DeleteCustomTheme(SelectedCustomThemeId);
        ReloadCustomThemes();

        _suppressSelectionReload = true;
        SelectedMode = ThemeSelectionType.Dark;
        SelectedCustomThemeId = null;
        _suppressSelectionReload = false;

        LoadThemeForSelection();
        UpdateCustomModeFlags();
    }

    [RelayCommand]
    private void LoadLightPreset()
    {
        SelectedMode = ThemeSelectionType.Light;
    }

    [RelayCommand]
    private void LoadDarkPreset()
    {
        SelectedMode = ThemeSelectionType.Dark;
    }

    [RelayCommand]
    private void Cancel()
    {
        _themeService.ApplySavedSelection();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    partial void OnSelectedModeChanged(ThemeSelectionType value)
    {
        if (_suppressSelectionReload)
        {
            UpdateCustomModeFlags();
            return;
        }

        LoadThemeForSelection();
        UpdateCustomModeFlags();
    }

    partial void OnSelectedCustomThemeIdChanged(string? value)
    {
        if (_suppressSelectionReload || SelectedMode != ThemeSelectionType.Custom)
        {
            return;
        }

        var custom = CustomThemes.FirstOrDefault(t => t.Id == value);
        if (custom is not null)
        {
            WorkingTheme = custom.Clone();
            CustomThemeName = custom.Name;
            SyncColorPropertiesFromTheme();
            PreviewTheme();
        }

        UpdateCustomModeFlags();
    }

    private void LoadThemeForSelection()
    {
        WorkingTheme = SelectedMode switch
        {
            ThemeSelectionType.Light => ThemeCatalog.Light.Clone(),
            ThemeSelectionType.Dark => ThemeCatalog.Dark.Clone(),
            ThemeSelectionType.Custom => CustomThemes.FirstOrDefault(t => t.Id == SelectedCustomThemeId)?.Clone()
                ?? _themeService.ResolveCustomTheme(_settingsService.Current).Clone(),
            _ => _themeService.ResolveTheme(_settingsService.Current, GetSystemTheme()).Clone(),
        };

        if (SelectedMode == ThemeSelectionType.Custom && WorkingTheme is not null)
        {
            CustomThemeName = WorkingTheme.Name;
        }

        SyncColorPropertiesFromTheme();
        PreviewTheme();
    }

    private void BuildColorProperties()
    {
        ColorProperties.Clear();

        foreach (var property in ThemeColorCatalog.All)
        {
            ColorProperties.Add(new ThemeColorItemViewModel(
                property.Key,
                property.DisplayName,
                property.Category,
                property.Getter(WorkingTheme),
                hex =>
                {
                    property.Setter(WorkingTheme, hex);
                    PreviewTheme();
                }));
        }
    }

    private void SyncColorPropertiesFromTheme()
    {
        foreach (var item in ColorProperties)
        {
            var property = ThemeColorCatalog.All.First(p => p.Key == item.Key);
            item.SetHex(property.Getter(WorkingTheme), notify: false);
        }
    }

    private void ReloadCustomThemes()
    {
        CustomThemes.Clear();
        foreach (var theme in _settingsService.Current.CustomThemes)
        {
            CustomThemes.Add(theme.Clone());
        }
    }

    private void UpdateCustomModeFlags()
    {
        IsCustomMode = SelectedMode == ThemeSelectionType.Custom;
        CanDeleteCustomTheme = IsCustomMode &&
                               !string.IsNullOrWhiteSpace(SelectedCustomThemeId) &&
                               CustomThemes.Any(t => t.Id == SelectedCustomThemeId);
    }

    private static ElementTheme GetSystemTheme()
    {
        if (App.Window?.Content is FrameworkElement root)
        {
            return root.ActualTheme;
        }

        return ElementTheme.Default;
    }
}

public sealed record ThemeModeOption(ThemeSelectionType Mode, string Title);
