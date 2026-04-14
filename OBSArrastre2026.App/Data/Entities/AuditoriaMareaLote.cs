namespace OBSArrastre2026.App.Data.Entities;

public sealed class AuditoriaMareaLote
{
    public string ID { get; set; } = Guid.NewGuid().ToString();
    public string MareaID { get; set; } = null!;
    public Marea Marea { get; set; } = null!;
    public DateTime Fecha { get; set; } = DateTime.Now;
    public string Tipo { get; set; } = null!; // IMPORTACION, VALIDACION
    public string Resultado { get; set; } = null!; // EXITO, ADVERTENCIAS, ERRORES
    public string? Metadatos { get; set; } // JSON

    public ICollection<AuditoriaMareaRegistro> Registros { get; set; } = new List<AuditoriaMareaRegistro>();
}
