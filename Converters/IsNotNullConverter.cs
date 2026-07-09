using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace qiyana.Converters;

public class IsNotNullConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var result = value is not null;
        if (parameter is "invert")
            result = !result;
        return result;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
