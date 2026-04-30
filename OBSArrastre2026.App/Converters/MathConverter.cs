using System;
using System.Globalization;
using System.Windows.Data;

namespace OBSArrastre2026.App.Converters;

public class MathConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return value ?? 0;

        double v = System.Convert.ToDouble(value);
        string p = parameter.ToString()!;

        if (p.StartsWith("x+"))
        {
            double offset = double.Parse(p.Substring(2));
            return v + offset;
        }
        else if (p.StartsWith("x-"))
        {
            double offset = double.Parse(p.Substring(2));
            return v - offset;
        }

        return v;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
