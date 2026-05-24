using NovaBrowser.AppWindows;

namespace NovaBrowser.Services;

public sealed class SettingsWindowService
{
    private SettingsWindow? _window;

    public event EventHandler? SettingsApplied;

    public void Open()
    {
        if (_window is not null)
        {
            _window.Activate();
            return;
        }

        _window = new SettingsWindow();
        _window.SettingsApplied += (_, _) => SettingsApplied?.Invoke(this, EventArgs.Empty);
        _window.Closed += (_, _) => _window = null;
        _window.Activate();
    }
}
