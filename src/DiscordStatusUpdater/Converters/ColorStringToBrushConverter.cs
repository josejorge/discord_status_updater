using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DiscordStatusUpdater.Converters;

[ValueConversion(typeof(string), typeof(SolidColorBrush))]
public class ColorStringToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(value?.ToString() ?? "#80848E");
            return new SolidColorBrush(color);
        }
        catch
        {
            return new SolidColorBrush(Colors.Gray);
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
