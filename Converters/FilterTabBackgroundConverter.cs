using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace qiyana.Converters;

public class FilterTabBackgroundConverter : IValueConverter
{
    private static readonly SolidColorBrush SelectedBrush = new(Color.Parse("#3a3a5c"));
    private static readonly SolidColorBrush TransparentBrush = new(Colors.Transparent);

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var selected = value?.ToString();
        var tab = parameter?.ToString();
        return string.Equals(selected, tab, StringComparison.OrdinalIgnoreCase)
            ? SelectedBrush
            : TransparentBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
