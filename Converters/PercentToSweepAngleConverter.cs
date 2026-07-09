using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace qiyana.Converters;

public class PercentToSweepAngleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int percent)
        {
            var clamped = Math.Clamp(percent, 0, 100);
            return clamped / 100.0 * 360.0;
        }

        return 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
