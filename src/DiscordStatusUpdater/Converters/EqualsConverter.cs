// File: EqualsConverter.cs
// Description: Compares a bound string to the ConverterParameter; used for page navigation and page visibility.
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-10-09
// Last edit date: 2026-10-09
// Version: 1.1.0

using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DiscordStatusUpdater.Converters;

/// <summary>
/// Forward: true when the bound value equals the parameter (drives RadioButton.IsChecked), or the matching
/// Visibility when bound to a Visibility property (drives page switching).
/// Back: when a RadioButton becomes checked it writes the parameter back to the source.
/// </summary>
public class EqualsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var equal = string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.Ordinal);
        if (targetType == typeof(Visibility))
            return equal ? Visibility.Visible : Visibility.Collapsed;
        return equal;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? parameter : Binding.DoNothing;
}
