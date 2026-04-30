using System;
using System.Globalization;
using System.Windows.Data;

namespace OBSArrastre2026.App.Converters;

public class CoordinateConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double val) return "-";
        
        bool isLat = parameter?.ToString()?.Equals("lat", StringComparison.OrdinalIgnoreCase) ?? true;
        
        double abs = Math.Abs(val);
        int deg = (int)abs;
        double min = (abs - deg) * 60;
        
        string q = isLat ? (val >= 0 ? "N" : "S") : (val >= 0 ? "E" : "O");
        return $"{deg}º {min:00.0}' {q}".Replace('.', ',');
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
