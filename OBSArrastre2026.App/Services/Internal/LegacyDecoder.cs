using OBSArrastre2026.App.Models.Import;

namespace OBSArrastre2026.App.Services.Internal;

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

    /// <summary>
    /// Desempaqueta una cadena de conteo biológico (Talla + M/H/I/T).
    /// Soporta bloques de 3 dígitos (basado en inspección de archivos Raw).
    /// Ejemplo: 86000000000024 -> Talla 86, M 0, H 0, I 0, T 24 (Recalculado: 24)
    /// </summary>
    public static DecodedTally DecodeTally(object? rawValue)
    {
        if (rawValue == null) return new DecodedTally(0, 0, 0, 0, 0);

        string s = rawValue.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(s) || s == "0") return new DecodedTally(0, 0, 0, 0, 0);

        try
        {
            // Si es un número simple o cadena corta, asumimos que es un conteo directo (no empaquetado)
            // En este caso, no conocemos la talla desde el valor, pero el orquestador sí (por el índice i)
            // Sin embargo, para mantener compatibilidad con el loop de DbfExtractorService, 
            // si la cadena es corta, devolvemos el valor como Total.
            if (s.Length < 10)
            {
                if (int.TryParse(s, out int simpleCount))
                {
                    // IMPORTANTE: En este modo "simple", la talla NO viene en el string.
                    // El DbfExtractorService deberá asignar la talla basándose en PRIM_TALLA + i*INTERVALO.
                    return new DecodedTally(0, 0, 0, 0, simpleCount);
                }
            }

            // El formato observado en Raw es [Talla(2 o 3)][M(3)][H(3)][I(3)][T(3)]
            int totalLen = s.Length;
            int blockSize = 3; 
            int tallyPartLen = blockSize * 4; 
            int sizePartLen = totalLen - tallyPartLen;

            if (sizePartLen <= 0) 
            {
                // Fallback final: si no es empaquetado pero parsea a int, es un conteo simple
                if (int.TryParse(s, out int c)) return new DecodedTally(0, 0, 0, 0, c);
                return new DecodedTally(0, 0, 0, 0, 0);
            }

            int size = int.Parse(s.Substring(0, sizePartLen));
            int m = int.Parse(s.Substring(sizePartLen, blockSize));
            int h = int.Parse(s.Substring(sizePartLen + blockSize, blockSize));
            int i = int.Parse(s.Substring(sizePartLen + (blockSize * 2), blockSize));
            int t = int.Parse(s.Substring(sizePartLen + (blockSize * 3), blockSize));

            int calculatedTotal = t; // Preservamos el valor original del DBF para NroTotal
            return new DecodedTally(size, m, h, i, calculatedTotal);
        }
        catch
        {
            return new DecodedTally(0, 0, 0, 0, 0);
        }
    }

    /// <summary>
    /// Empaqueta un conteo biológico en una cadena de 14 dígitos [Talla(2)][M(3)][H(3)][I(3)][T(3)].
    /// </summary>
    public static string EncodeTally(int size, int m, int h, int i, int t)
    {
        // El formato es [Talla(2)][M(3)][H(3)][I(3)][T(3)] = 14 dígitos
        // Aseguramos que los valores no excedan los límites de los bloques
        string sSize = Math.Min(size, 99).ToString("D2");
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
}
