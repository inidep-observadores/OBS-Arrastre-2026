using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ControlMareas.App.Converters;

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool invert = parameter?.ToString()?.Equals("invert", StringComparison.OrdinalIgnoreCase) ?? false;
        bool isNull = value == null;

        if (invert)
        {
            return isNull ? Visibility.Collapsed : Visibility.Visible;
        }

        return isNull ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
