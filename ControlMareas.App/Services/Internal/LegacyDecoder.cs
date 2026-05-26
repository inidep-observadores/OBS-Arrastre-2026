using ControlMareas.App.Models.Import;

namespace ControlMareas.App.Services.Internal;

/// <summary>
/// Utilidad para decodificar formatos legacy (coordenadas, horas, tallas empaquetadas).
/// </summary>
public static class LegacyDecoder
{
    /// <summary>
    /// Decodifica coordenadas en formato DD.mmd (Grados.MinutosDécimas).
    /// Asume cuadrante SW (negativo para Latitud y Longitud).
    /// </summary>
    public static double DecodeCoordinate(double? value)
    {
        if (value == null || value == 0) return 0;

        // Formato legacy: DD.mmd (Grados.MinutosDécima)
        // Ejemplo: 40.441 -> 40 grados, 44 minutos, 1 décima de minuto.
        double val = Math.Abs(value.Value);
        
        int degrees = (int)Math.Truncate(val);
        
        // Extraer la parte decimal (los .441) y convertir a minutos reales
        // Usamos Round para evitar 0.440999999998 de la aritmética double
        double minutesPart = Math.Round(val - degrees, 3) * 100; // 0.441 -> 44.1
        
        double decimalDegrees = degrees + (minutesPart / 60.0);

        return decimalDegrees * -1; // Siempre Hemisferio Sur / Oeste (SW)
    }

    /// <summary>
    /// Codifica coordenadas decimales al formato DD.mmd (Grados.MinutosDécimas).
    /// </summary>
    public static double EncodeCoordinate(double decimalDegrees)
    {
        double val = Math.Abs(decimalDegrees);
        int degrees = (int)Math.Truncate(val);
        double minutesPart = (val - degrees) * 60.0;
        
        // Formato DD.mmd -> Grados + (Minutos / 100)
        // Ejemplo: 40.735 -> 40 grados, 44.1 minutos -> 40.441
        return degrees + (Math.Round(minutesPart, 1) / 100.0);
    }

    /// <summary>
    /// Decodifica horas en formato HH.mm (Horas.Minutos).
    /// </summary>
    public static TimeSpan DecodeTime(double? value)
    {
        if (value == null) return TimeSpan.Zero;

        double absoluteValue = Math.Abs(value.Value);
        int hours = (int)Math.Floor(absoluteValue);
        int minutes = (int)Math.Round((absoluteValue - hours) * 100);

        // Validación básica de límites
        if (hours >= 24) hours = 23;
        if (minutes >= 60) minutes = 59;

        return new TimeSpan(hours, minutes, 0);
    }

    /// <summary>
    /// Codifica un TimeSpan al formato HH.mm (Horas.Minutos).
    /// </summary>
    public static double EncodeTime(TimeSpan time)
    {
        return time.Hours + (time.Minutes / 100.0);
    }

    public static DecodedTally DecodeTally(object? rawValue)
    {
        if (rawValue == null) return new DecodedTally(0, 0, 0, 0, 0);

        string s = rawValue.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(s) || s == "0") return new DecodedTally(0, 0, 0, 0, 0);

        try
        {
            s = s.Trim();
            
            // Remover cualquier parte decimal residual de conversiones double/decimal en DBF (ej. "84000003004007.0")
            int dotIndex = s.IndexOf('.');
            if (dotIndex >= 0)
            {
                s = s.Substring(0, dotIndex);
            }

            if (string.IsNullOrEmpty(s) || s == "0") return new DecodedTally(0, 0, 0, 0, 0);

            // De acuerdo a las directrices de FoxPro e INIDEP, interpretamos el valor como un string de 15 dígitos
            // relleno con ceros a la izquierda. Luego se divide en 5 grupos de 3 dígitos de izquierda a derecha.
            string padded = s.PadLeft(15, '0');

            // Extraer bloques de 3 dígitos
            int size = int.Parse(padded.Substring(0, 3));
            int m = int.Parse(padded.Substring(3, 3));
            int h = int.Parse(padded.Substring(6, 3));
            int i = int.Parse(padded.Substring(9, 3));
            int t = int.Parse(padded.Substring(12, 3));

            int calculatedTotal = (m + h + i) > 0 ? (m + h + i) : t;
            return new DecodedTally(size, m, h, i, calculatedTotal);
        }
        catch
        {
            // Fallback ultra-defensivo en caso de error de parseo inesperado
            try
            {
                if (double.TryParse(s, out double d))
                {
                    long valLong = (long)Math.Round(d);
                    if (valLong > 0)
                    {
                        return new DecodedTally(0, 0, 0, 0, (int)valLong);
                    }
                }
            }
            catch {}
            return new DecodedTally(0, 0, 0, 0, 0);
        }
    }

    /// <summary>
    /// Empaqueta un conteo biológico en una cadena de 14 dígitos [Talla(2)][M(3)][H(3)][I(3)][T(3)].
    /// </summary>
    public static string EncodeTally(int size, int m, int h, int i, int t)
    {
        // El formato es [Talla(2 o 3)][M(3)][H(3)][I(3)][T(3)]
        // Talla puede ser de 2 o 3 dígitos (ej. 100 -> 15 dígitos totales)
        string sSize = size.ToString();
        string sM = Math.Min(m, 999).ToString("D3");
        string sH = Math.Min(h, 999).ToString("D3");
        string sI = Math.Min(i, 999).ToString("D3");
        string sT = Math.Min(t, 999).ToString("D3");

        return $"{sSize}{sM}{sH}{sI}{sT}";
    }

    /// <summary>
    /// Fusiona una muestra base (M*) con su extensión (X*).
    /// </summary>
    public static void MergeExtendedMuestras(LegacyMuestra baseMuestra, LegacyMuestra extension)
    {
        if (baseMuestra == null || extension == null) return;
        
        // Solo fusionamos si son del mismo barco, marea, lance y especie (Validado por el orquestador)
        foreach (var tally in extension.Tallies)
        {
            // Solo agregamos si no existe ya esa talla en la base (por seguridad)
            if (!baseMuestra.Tallies.Any(t => t.Size == tally.Size))
            {
                baseMuestra.Tallies.Add(tally);
            }
        }

        // Actualizamos la última talla de la muestra consolidada
        if (baseMuestra.Tallies.Any())
        {
            baseMuestra.UltTalla = baseMuestra.Tallies.Max(t => t.Size);
        }
    }

    /// <summary>
    /// Decodifica el formato de 9 dígitos (MMMHHHIII) usado en los archivos L* para langostinos.
    /// MMM: Machos Maduros, HHH: Hembras Maduras, III: Hembras Impregnadas.
    /// </summary>
    public static (int MatureMales, int MatureFemales, int ImpregnatedFemales) DecodeMatureTally(double value)
    {
        if (value <= 0) return (0, 0, 0);
        
        // El valor es un número de hasta 9 dígitos: MMMHHHIII
        // Ejemplo: 25012003 -> 025 012 003
        string s = ((long)value).ToString().PadLeft(9, '0');
        
        // Si el número tiene más de 9 dígitos, tomamos los últimos 9 por seguridad 
        // (aunque el formato estándar debería ser 9).
        if (s.Length > 9) s = s.Substring(s.Length - 9);

        try
        {
            int mm = int.Parse(s.Substring(0, 3));
            int hm = int.Parse(s.Substring(3, 3));
            int hi = int.Parse(s.Substring(6, 3));
            
            return (mm, hm, hi);
        }
        catch
        {
            return (0, 0, 0);
        }
    }

    /// <summary>
    /// Codifica el formato de 9 dígitos (MMMHHHIII) usado en los archivos L*.
    /// </summary>
    public static double EncodeMatureTally(int mm, int hm, int hi)
    {
        // MMMHHHIII
        string s = $"{Math.Min(mm, 999):D3}{Math.Min(hm, 999):D3}{Math.Min(hi, 999):D3}";
        return double.Parse(s);
    }

    /// <summary>
    /// Retorna la clave de área estadística a partir de coordenadas decimales,
    /// usando la misma lógica que GetCuadricula en los servicios de reporte.
    /// Si alguna coordenada es nula, retorna "S/D".
    /// Formato de clave: (int(|lat|) * 100) + int(|lon|), p.ej. "4160".
    /// </summary>
    public static string GetAreaKey(double? lat, double? lon)
    {
        if (!lat.HasValue || !lon.HasValue) return "S/D";
        double latAbs = Math.Abs(lat.Value);
        double lonAbs = Math.Abs(lon.Value);
        int cuad = ((int)Math.Truncate(latAbs) * 100) + (int)Math.Truncate(lonAbs);
        return cuad.ToString();
    }

    /// <summary>
    /// Calcula el área estadística (cuadrícula + cuadrante decimal) a partir de coordenadas.
    /// Formato: (LatAbs * 100 + LonAbs) + .Cuadrante
    /// Cuadrantes: NO=1, NE=2, SO=3, SE=4
    /// </summary>
    public static double CalculateGridArea(double lat, double lon)
    {
        double latAbs = Math.Abs(lat);
        double lonAbs = Math.Abs(lon);

        // Parte entera: Lat * 100 + Lon
        int baseCuad = ((int)Math.Truncate(latAbs) * 100) + (int)Math.Truncate(lonAbs);

        // Cuadrante decimal (30' x 30')
        double latDec = latAbs - Math.Truncate(latAbs);
        double lonDec = lonAbs - Math.Truncate(lonAbs);

        int cuadrante = 0;
        if (latDec < 0.5) // Norte
        {
            cuadrante = (lonDec >= 0.5) ? 1 : 2; // Oeste (1) o Este (2)
        }
        else // Sur
        {
            cuadrante = (lonDec >= 0.5) ? 3 : 4; // Oeste (3) o Este (4)
        }

        return baseCuad + (cuadrante / 10.0);
    }
}
