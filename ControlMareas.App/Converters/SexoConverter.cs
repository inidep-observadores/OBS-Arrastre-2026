using System;
using System.Globalization;
using System.Windows.Data;

namespace ControlMareas.App.Converters;

public class SexoConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int intValue)
        {
            return intValue switch
            {
                1 => "Macho",
                2 => "Hembra",
                3 => "Indet.",
                _ => "-"
            };
        }
        return "-";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
