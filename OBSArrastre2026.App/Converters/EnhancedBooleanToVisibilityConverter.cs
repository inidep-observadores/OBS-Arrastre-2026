using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OBSArrastre2026.App.Converters;

public class EnhancedBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool boolValue = value is bool b && b;
        bool invert = parameter?.ToString()?.Equals("invert", StringComparison.OrdinalIgnoreCase) ?? false;

        if (invert)
        {
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }

        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
