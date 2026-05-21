namespace ControlMareas.App.Models.Import;

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
    public DateTime? FechaFin { get; set; } // Fecha de finalización del lance
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
    
    // Campos adicionales solicitados
    public double? Tiempo { get; set; }
    public double? Mar { get; set; }
    public double? DirViento { get; set; }
    public double? VelViento { get; set; }
    public double? TmpASeco { get; set; }
    public double? TmpMarF { get; set; }
    public double? PresionB { get; set; }
    public double? VelArras { get; set; }
    public double? Rumbo { get; set; }
    public double? MallCopo { get; set; }
    public double? MallAlas { get; set; }
    public double? CabFilad { get; set; }
    public double? AberVert { get; set; }
    public double? DistAlas { get; set; }
    public double? DistEPor { get; set; }
    public string? Observac { get; set; }

    // Campos de Integridad 1:1
    public double? Mus { get; set; }
    public double? EstacGral { get; set; }
    public double? Estrato { get; set; }
    public double? EdadLuna { get; set; }
    public double? Luz { get; set; }
    public double? TmpAHum { get; set; }
    public double? TmpMarS { get; set; }
    public double? Tarte { get; set; }
    public double? Narte { get; set; }
    public double? AreaBarr { get; set; }
    public double? MallSobre { get; set; }
    
    // Especies (se manejan dinámicamente o por convención de nombres)
    public Dictionary<string, double> Especies { get; } = new(); // ID Especie -> KG
    public Dictionary<string, double> DescartesPorEspecie { get; } = new(); // ID Especie -> KG
    public List<string> EspeciesOrder { get; } = new(); // Para preservar el orden de las columnas ESPECIE_1..25
}

/// <summary>
/// Representa un registro crudo de MUESTRAS (M*.DBF).
/// </summary>
public class LegacyMuestra
{
    public int NumeroOrden { get; set; }
    public string Barco { get; set; } = string.Empty;
    public double Marea { get; set; }
    public double Lance { get; set; }
    public DateTime Fecha { get; set; }
    public string Especie { get; set; } = string.Empty;
    public string CodEspec { get; set; } = string.Empty;
    public double? Fuente { get; set; }
    public double? Tarte { get; set; }
    public double? Area { get; set; }
    public int? PrimTalla { get; set; }
    public int? UltTalla { get; set; }
    public double? Intervalo { get; set; }
    public double PesoMues { get; set; }
    public double? FactPond { get; set; }
    public int TipoMuestra { get; set; } = 1; // 1 = Estandar, 2 = Descarte
    
    // Tallas empaquetadas o decodificadas
    public List<DecodedTally> Tallies { get; } = new();
}

/// <summary>
/// Representa un registro de SUBMUES (S*.DBF).
/// </summary>
public class LegacySubmuestra
{
    public int NumeroOrden { get; set; }
    public string Barco { get; set; } = string.Empty;
    public double Marea { get; set; }
    public double Lance { get; set; }
    public DateTime Fecha { get; set; }
    public double? Tarte { get; set; }
    public double? Fuente { get; set; }
    public double? Area { get; set; }
    public string Especie { get; set; } = string.Empty;
    public int NEjemplar { get; set; }
    public int LargoTot { get; set; }
    public int LargoSta { get; set; }
    public double PesoTot { get; set; }
    public double PesoVac { get; set; }
    public int Sexo { get; set; }
    public int Estadio { get; set; }
    public double PesoGon { get; set; }
    public double PesoHig { get; set; }
    public int Replecion { get; set; }
    public string Comentario { get; set; } = string.Empty;
    public double Edad { get; set; }
    public double RTotal { get; set; }
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
    public string CodEspecIE { get; set; } = string.Empty;
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
    public DateTime GetDateTime()
    {
        if (_cachedDateTime.HasValue) return _cachedDateTime.Value;
        if (DateTime.TryParse(FechaStr, out var dt))
        {
            // Nota: Se asume que la fecha ya viene en hora local en el archivo de origen.
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
    public int NumeroOrden { get; set; }
    public string Barco { get; set; } = string.Empty;
    public double Marea { get; set; }
    public DateTime Fecha { get; set; }
    public string Especie { get; set; } = string.Empty;
    public string Producto { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public int? Operarios { get; set; }
    public double Factor { get; set; }
    public double Kilos { get; set; }
}
