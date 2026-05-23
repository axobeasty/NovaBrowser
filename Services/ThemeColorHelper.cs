using Microsoft.UI;
using Windows.UI;

namespace NovaBrowser.Services;

public static class ThemeColorHelper
{
    public static Color ParseColor(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return Colors.Transparent;
        }

        hex = hex.Trim().TrimStart('#');

        if (hex.Length is 6)
        {
            hex = "FF" + hex;
        }

        if (hex.Length != 8)
        {
            return Colors.Transparent;
        }

        return Color.FromArgb(
            Convert.ToByte(hex[..2], 16),
            Convert.ToByte(hex[2..4], 16),
            Convert.ToByte(hex[4..6], 16),
            Convert.ToByte(hex[6..8], 16));
    }

    public static string ToHex(Color color, bool includeAlpha = false) =>
        includeAlpha
            ? $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}"
            : $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    public static bool IsLightColor(Color color)
    {
        var luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255.0;
        return luminance > 0.58;
    }

    public static bool IsValidHex(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return false;
        }

        var value = hex.Trim().TrimStart('#');
        return value.Length is 6 or 8 && value.All(static c => Uri.IsHexDigit(c));
    }
}
