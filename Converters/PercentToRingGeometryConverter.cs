using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace qiyana.Converters;

public class PercentToRingGeometryConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int percent)
            return new StreamGeometry();

        var clamped = Math.Clamp(percent, 0, 100);
        if (clamped == 0)
            return new StreamGeometry();

        var totalLength = 320.0;
        var visible = totalLength * clamped / 100.0;

        var geometry = new StreamGeometry();

        using (var ctx = geometry.Open())
        {
            var start = new Avalonia.Point(40, 0);
            ctx.BeginFigure(start, false);

            if (visible <= 40)
            {
                ctx.LineTo(new Avalonia.Point(40 + visible, 0));
            }
            else if (visible <= 120)
            {
                ctx.LineTo(new Avalonia.Point(80, 0));
                ctx.LineTo(new Avalonia.Point(80, visible - 40));
            }
            else if (visible <= 200)
            {
                ctx.LineTo(new Avalonia.Point(80, 0));
                ctx.LineTo(new Avalonia.Point(80, 80));
                ctx.LineTo(new Avalonia.Point(200 - visible, 80));
            }
            else if (visible <= 280)
            {
                ctx.LineTo(new Avalonia.Point(80, 0));
                ctx.LineTo(new Avalonia.Point(80, 80));
                ctx.LineTo(new Avalonia.Point(0, 80));
                ctx.LineTo(new Avalonia.Point(0, 280 - visible));
            }
            else
            {
                ctx.LineTo(new Avalonia.Point(80, 0));
                ctx.LineTo(new Avalonia.Point(80, 80));
                ctx.LineTo(new Avalonia.Point(0, 80));
                ctx.LineTo(new Avalonia.Point(0, 0));
                ctx.LineTo(new Avalonia.Point(visible - 280, 0));
            }

            ctx.EndFigure(false);
        }

        return geometry;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
