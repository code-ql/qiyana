using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace qiyana.Converters;

public class WinToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool win)
            return win ? "胜利" : "失败";
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class WinToForegroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool win)
            return win ? new SolidColorBrush(Color.Parse("#4a9eff")) : new SolidColorBrush(Color.Parse("#ff4a4a"));
        return new SolidColorBrush(Colors.Gray);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class BoolToHighlightConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool highlight && highlight)
            return new SolidColorBrush(Color.Parse("#2a2a5c"));
        return new SolidColorBrush(Color.Parse("#1e1e2e"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
