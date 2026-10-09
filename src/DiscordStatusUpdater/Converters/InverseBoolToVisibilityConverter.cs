// File: InverseBoolToVisibilityConverter.cs
// Description: Shows an element when the bound bool is false (e.g. an error hint or an empty-state message).
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-10-09
// Last edit date: 2026-10-09
// Version: 1.0.0

using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DiscordStatusUpdater.Converters;

[ValueConversion(typeof(bool), typeof(Visibility))]
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
