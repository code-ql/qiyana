using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace qiyana.Converters;

public sealed class PercentToWidthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double percent) return 0d;
        var maxWidth = parameter is string s && double.TryParse(s, out var parsed) ? parsed : 120d;
        return Math.Clamp(percent, 0, 100) / 100 * maxWidth;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
