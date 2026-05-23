using CommunityToolkit.Mvvm.ComponentModel;
using NovaBrowser.Services;
using Windows.UI;

namespace NovaBrowser.ViewModels;

public sealed partial class ThemeColorItemViewModel : ObservableObject
{
    private readonly Action<string> _onHexChanged;

    public string Key { get; }

    public string DisplayName { get; }

    public string Category { get; }

    [ObservableProperty]
    private string _hexValue;

    [ObservableProperty]
    private Color _color;

    public ThemeColorItemViewModel(
        string key,
        string displayName,
        string category,
        string initialHex,
        Action<string> onHexChanged)
    {
        Key = key;
        DisplayName = displayName;
        Category = category;
        _hexValue = NormalizeHex(initialHex);
        _color = ThemeColorHelper.ParseColor(_hexValue);
        _onHexChanged = onHexChanged;
    }

    partial void OnHexValueChanged(string value)
    {
        if (!ThemeColorHelper.IsValidHex(value))
        {
            return;
        }

        var normalized = NormalizeHex(value);
        if (!string.Equals(normalized, value, StringComparison.OrdinalIgnoreCase))
        {
            HexValue = normalized;
            return;
        }

        Color = ThemeColorHelper.ParseColor(normalized);
        _onHexChanged(normalized);
    }

    public void SetHex(string hex, bool notify = true)
    {
        var normalized = NormalizeHex(hex);
        Color = ThemeColorHelper.ParseColor(normalized);

        if (notify)
        {
            HexValue = normalized;
        }
        else
        {
            _hexValue = normalized;
            OnPropertyChanged(nameof(HexValue));
        }
    }

    private static string NormalizeHex(string hex)
    {
        var value = hex.Trim();
        if (!value.StartsWith('#'))
        {
            value = "#" + value;
        }

        return value.ToUpperInvariant();
    }
}
