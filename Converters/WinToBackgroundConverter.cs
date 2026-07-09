using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace qiyana.Converters;

public class WinToBackgroundConverter : IValueConverter
{
    private static bool IsWin(object? value) =>
        value is string s ? s == "Win" : value is bool b && b;

    private static string Mix(string color, string bg, double factor)
    {
        var cr = System.Convert.ToByte(color.Substring(1, 2), 16);
        var cg = System.Convert.ToByte(color.Substring(3, 2), 16);
        var cb = System.Convert.ToByte(color.Substring(5, 2), 16);
        var br = System.Convert.ToByte(bg.Substring(1, 2), 16);
        var bg2 = System.Convert.ToByte(bg.Substring(3, 2), 16);
        var bb = System.Convert.ToByte(bg.Substring(5, 2), 16);
        var r = (byte)(cr + (br - cr) * factor);
        var g = (byte)(cg + (bg2 - cg) * factor);
        var b = (byte)(cb + (bb - cb) * factor);
        return $"#{r:x2}{g:x2}{b:x2}";
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isHighlight = parameter is string p && p == "Highlight";
        var win = IsWin(value);
        var full = win ? "#1e3a5f" : "#5f1e1e";
        var bg = "#1e1e2e";
        if (isHighlight)
        {
            var r = System.Convert.ToByte(full.Substring(1, 2), 16);
            var g = System.Convert.ToByte(full.Substring(3, 2), 16);
            var b = System.Convert.ToByte(full.Substring(5, 2), 16);
            r = (byte)Math.Min(r * 2, 255);
            g = (byte)Math.Min(g * 2, 255);
            b = (byte)Math.Min(b * 2, 255);
            return new SolidColorBrush(Color.Parse($"#{r:x2}{g:x2}{b:x2}"));
        }
        return new SolidColorBrush(Color.Parse(Mix(full, bg, 0.7)));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
