using System.Globalization;
using System.Windows.Data;

namespace ProcessMonitor.WPF.Converters;

/// <summary>
/// Converts memory bytes to a normalized 0–200 width value for a mini bar.
/// Treats 2 GB as 100% (width=200).
/// </summary>
[ValueConversion(typeof(long), typeof(double))]
public class MemoryToWidthConverter : IValueConverter
{
    private const double MaxBytes = 2L * 1024 * 1024 * 1024; // 2 GB = 100%
    private const double MaxWidth = 200.0;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is long bytes)
            return Math.Clamp(bytes / MaxBytes * MaxWidth, 0, MaxWidth);
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
