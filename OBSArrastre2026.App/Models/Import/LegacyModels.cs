namespace OBSArrastre2026.App.Models.Import;

/// <summary>
/// Modelo para representar los datos biológicos desempaquetados de una talla.
/// </summary>
public record DecodedTally(
    int Size,
    int Males,
    int Females,
    int Indeterminate,
    int Total);


/// <summary>
/// Representa un registro crudo de CAPTURA (C*.DBF) para validación.
/// </summary>
public class LegacyCaptura
{
    public string Barco { get; set; } = string.Empty;
    public double Marea { get; set; }
    public double Lance { get; set; }
    public DateTime Fecha { get; set; }
    public double HoraInic { get; set; }
    public double HoraFinal { get; set; }
    public double LatInic { get; set; }
    public double LongInic { get; set; }
    public double LatFinal { get; set; }
    public double LongFinal { get; set; }
    public double ProfInic { get; set; }
    public double ProfFinal { get; set; }
    public double CaptTotal { get; set; }
    public double Descarte { get; set; }
    
    // Especies (se manejan dinámicamente o por convención de nombres)
    public Dictionary<long, double> Especies { get; } = new(); // CodEspecie -> KG
    public Dictionary<long, double> DescartesPorEspecie { get; } = new(); // CodEspecie -> KG
}

/// <summary>
/// Representa un registro crudo de MUESTRAS (M*.DBF).
/// </summary>
public class LegacyMuestra
{
    public string Barco { get; set; } = string.Empty;
    public double Marea { get; set; }
    public double Lance { get; set; }
    public DateTime Fecha { get; set; }
    public string Especie { get; set; } = string.Empty;
    public long CodEspec { get; set; }
    public double Area { get; set; }
    public int PrimTalla { get; set; }
    public int UltTalla { get; set; }
    public int Intervalo { get; set; }
    public double PesoMues { get; set; }
    public double FactPond { get; set; }
    
    // Tallas empaquetadas o decodificadas
    public List<DecodedTally> Tallies { get; } = new();
}

/// <summary>
/// Representa un registro de SUBMUES (S*.DBF).
/// </summary>
public class LegacySubmuestra
{
    public string Barco { get; set; } = string.Empty;
    public double Marea { get; set; }
    public double Lance { get; set; }
    public DateTime Fecha { get; set; }
    public string Especie { get; set; } = string.Empty;
    public int NEjemplar { get; set; }
    public int LargoTot { get; set; }
    public int LargoSta { get; set; }
    public double PesoTot { get; set; }
    public int Sexo { get; set; }
    public int Estadio { get; set; }
}

/// <summary>
/// Representa un registro de LG (L*.DBF) - Parámetros alométricos.
/// </summary>
public class LegacyLg
{
    public string Barco { get; set; } = string.Empty;
    public double Marea { get; set; }
    public double Lance { get; set; }
    public DateTime Fecha { get; set; }
    public long CodEspecIE { get; set; }
    public double ParamA { get; set; }
    public double ParamB { get; set; }
    public Dictionary<int, double> Frecuencias { get; } = new(); // Índice -> Frecuencia
}

/// <summary>
/// Representa un registro de seguimiento satelital (T*.DBF).
/// </summary>
public class LegacyTracking
{
    public string Buque { get; set; } = string.Empty;
    public string Matricula { get; set; } = string.Empty;
    public string FechaStr { get; set; } = string.Empty; // "YYYY-MM-DD HH:MM:SS"
    public double Latitud { get; set; }
    public double Longitud { get; set; }
    public double Velocidad { get; set; }
    public double Rumbo { get; set; }

    private DateTime? _cachedDateTime;
    public DateTime GetUtcDateTime()
    {
        if (_cachedDateTime.HasValue) return _cachedDateTime.Value;
        if (DateTime.TryParse(FechaStr, out var dt))
        {
            _cachedDateTime = dt;
            return dt;
        }
        return DateTime.MinValue;
    }
}

/// <summary>
/// Representa un registro de producción (P*.DBF).
/// </summary>
public class LegacyProduccion
{
    public string Barco { get; set; } = string.Empty;
    public double Marea { get; set; }
    public DateTime Fecha { get; set; }
    public string Especie { get; set; } = string.Empty;
    public string Producto { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public int Operarios { get; set; }
    public double Factor { get; set; }
    public double Kilos { get; set; }
}
