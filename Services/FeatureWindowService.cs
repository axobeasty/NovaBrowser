using NovaBrowser.Models;
using NovaBrowser.AppWindows;

namespace NovaBrowser.Services;

public sealed class FeatureWindowService
{
    private readonly Dictionary<FeatureWindowKind, FeatureWindow> _windows = new();

    public event Action<string>? NavigateRequested;

    public void Open(FeatureWindowKind kind)
    {
        if (_windows.TryGetValue(kind, out var existing))
        {
            existing.Activate();
            return;
        }

        var window = new FeatureWindow(kind);
        window.NavigateRequested += (_, url) => NavigateRequested?.Invoke(url);
        window.Closed += (_, _) => _windows.Remove(kind);
        _windows[kind] = window;
        window.Activate();
    }

    public void CloseAll()
    {
        foreach (var window in _windows.Values.ToList())
        {
            window.Close();
        }

        _windows.Clear();
    }
}
