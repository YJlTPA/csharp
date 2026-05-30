using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using KbmTrainer.WPF.ViewModels;

namespace KbmTrainer.WPF.Converters;

public class CharStateToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is CharState state)
        {
            var app = Application.Current;
            string key = state switch
            {
                CharState.Correct => "CorrectColor",
                CharState.Incorrect => "IncorrectColor",
                CharState.Current => "CurrentColor",
                _ => "PendingColor"
            };

            if (app.Resources.Contains(key) && app.Resources[key] is Brush brush)
                return brush;

            // fallback colors
            return state switch
            {
                CharState.Correct => new SolidColorBrush(Color.FromRgb(0xA6, 0xE3, 0xA1)),
                CharState.Incorrect => new SolidColorBrush(Color.FromRgb(0xF3, 0x8B, 0xA8)),
                CharState.Current => new SolidColorBrush(Color.FromRgb(0xCD, 0xD6, 0xF4)),
                _ => new SolidColorBrush(Color.FromRgb(0x6C, 0x70, 0x86))
            };
        }

        return Binding.DoNothing;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
