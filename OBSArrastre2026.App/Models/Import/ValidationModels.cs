namespace OBSArrastre2026.App.Models.Import;

public enum ValidationLevel
{
    Info,
    Warning,
    AutoFixed,
    Error,
    Fatal
}

public record ValidationIssue(
    ValidationLevel Level,
    string Category, // Ej: "Geografía", "Biometría", "Especie"
    string Message,
    string? Context = null, // Ej: "Lance 14", "Dato: Talla 85"
    string? OriginalValue = null,
    string? CorrectedValue = null);

public record EtapaValidationInfo(int Numero, DateTime? FechaInicio, DateTime? FechaFin);

public class MareaValidationReport
{
    public string? MareaMetadata { get; set; }
    public string? ImportPath { get; set; }
    public bool IsTrackingOnly { get; set; }
    public string Barco { get; set; } = string.Empty;
    public string Marea { get; set; } = string.Empty;
    public int Año { get; set; }
    public int? BuqueCodigo { get; set; }
    public string? ObservadorNombre { get; set; }
    public string? ObservadorApellido { get; set; }
    public int? ObservadorCodigo { get; set; }
    public TipoDatoDescarte UnidadDescarte { get; set; } = TipoDatoDescarte.Kilogramos;
    
    public DateTime? FechaInicioMarea { get; set; }
    public DateTime? FechaFinMarea { get; set; }
    public List<EtapaValidationInfo> Etapas { get; set; } = new();
    
    public List<ValidationIssue> Issues { get; } = new();
    
    // Datos extraídos para posterior commit
    public List<LegacyCaptura> Capturas { get; set; } = new();
    public List<LegacyMuestra> Muestras { get; set; } = new();
    public List<LegacySubmuestra> Submuestras { get; set; } = new();
    public List<LegacyLg> Lgs { get; set; } = new();
    public List<LegacyTracking> Tracking { get; set; } = new();
    public List<LegacyProduccion> Produccion { get; set; } = new();
    
    public List<string> ArchivosProcesados { get; set; } = new();
    
    public int TotalLances { get; set; }
    public int TotalErrors => Issues.Count(i => i.Level == ValidationLevel.Error || i.Level == ValidationLevel.Fatal);
    public int TotalWarnings => Issues.Count(i => i.Level == ValidationLevel.Warning);
    public int TotalAutoFixes => Issues.Count(i => i.Level == ValidationLevel.AutoFixed);
    
    public bool HasFatalErrors => Issues.Any(i => i.Level == ValidationLevel.Fatal);

    public void AddIssue(ValidationLevel level, string category, string message, string? context = null, string? original = null, string? corrected = null)
    {
        Issues.Add(new ValidationIssue(level, category, message, context, original, corrected));
    }
}
