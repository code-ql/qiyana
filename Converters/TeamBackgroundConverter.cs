using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace qiyana.Converters;

public class TeamBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int teamId) return new SolidColorBrush(Color.Parse("#1e1e2e"));
        return teamId == 100
            ? new SolidColorBrush(Color.Parse("#0d1b33"))
            : new SolidColorBrush(Color.Parse("#330d0d"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
