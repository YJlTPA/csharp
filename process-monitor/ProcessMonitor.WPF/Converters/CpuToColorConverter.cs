using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ProcessMonitor.WPF.Converters;

[ValueConversion(typeof(double), typeof(SolidColorBrush))]
public class CpuToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double cpu)
        {
            if (cpu >= 50.0)
                return new SolidColorBrush(Color.FromRgb(0xF3, 0x8B, 0xA8)); // red
            if (cpu >= 10.0)
                return new SolidColorBrush(Color.FromRgb(0xF9, 0xE2, 0xAF)); // yellow
            return new SolidColorBrush(Color.FromRgb(0xA6, 0xE3, 0xA1));     // green
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
