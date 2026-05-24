using Microsoft.UI.Xaml;
using NovaBrowser.Services;
using NovaBrowser.ViewModels;

namespace NovaBrowser.AppWindows;

public sealed partial class SettingsWindow : Window
{
    public event EventHandler? SettingsApplied;

    public SettingsWindow()
    {
        InitializeComponent();
        Title = Helpers.L.Get("SettingsTitle");
        AppWindow.Resize(new Windows.Graphics.SizeInt32(980, 720));
        AppWindow.SetIcon("Assets/AppIcon.ico");

        if (Application.Current is not App app)
        {
            return;
        }

        var viewModel = new SettingsViewModel(app.SettingsService, app.ThemeService, app.Localization, app.Services);
        SettingsPanel.Initialize(viewModel);
        SettingsPanel.CloseRequested += OnCloseRequested;
        SettingsPanel.CheckUpdatesRequested += OnCheckUpdatesRequested;
        SettingsPanel.OpenFeatureRequested += OnOpenFeatureRequested;

        app.Localization.LanguageChanged += OnLanguageChanged;
        Closed += (_, _) => app.Localization.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        Title = Helpers.L.Get("SettingsTitle");
    }

    private async void OnCheckUpdatesRequested(object? sender, EventArgs e)
    {
        if (Application.Current is App app)
        {
            await app.UpdateCoordinator.CheckManuallyAsync();
        }
    }

    private void OnOpenFeatureRequested(object? sender, Models.FeatureWindowKind kind)
    {
        if (Application.Current is App app)
        {
            app.FeatureWindows.Open(kind);
        }
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        SettingsApplied?.Invoke(this, EventArgs.Empty);
        Close();
    }
}
