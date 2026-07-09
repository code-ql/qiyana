using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace qiyana.Converters;

public class EpochToRelativeTimeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is long ms)
        {
            var dt = DateTimeOffset.FromUnixTimeMilliseconds(ms);
            return dt.LocalDateTime.ToString("yyyy-MM-dd");
        }
        return "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
