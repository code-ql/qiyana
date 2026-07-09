using System;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using qiyana.Services;

namespace qiyana.Converters;

public class LcuAssetConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrEmpty(path))
            return null;

        if (path.StartsWith("avares://"))
        {
            try
            {
                return new Bitmap(AssetLoader.Open(new Uri(path)));
            }
            catch
            {
                return null;
            }
        }

        var bytes = LcuImageCache.Get(path);
        if (bytes is not null)
            return new Bitmap(new MemoryStream(bytes));

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
