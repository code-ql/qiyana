using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace qiyana.Converters;

public class DoubleToGridLengthConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d && d > 0)
            return new GridLength(d, GridUnitType.Star);
        return new GridLength(0);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
