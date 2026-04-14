namespace OBSArrastre2026.App.Data.Entities;

public sealed class AuditoriaMareaRegistro
{
    public string ID { get; set; } = Guid.NewGuid().ToString();
    public string LoteID { get; set; } = null!;
    public AuditoriaMareaLote Lote { get; set; } = null!;
    public string Nivel { get; set; } = null!; // ERROR, ADVERTENCIA, INFO
    public string? Entidad { get; set; } // Nombre de la tabla afectada
    public string? EntidadID { get; set; } // ID del registro afectado
    public string Mensaje { get; set; } = null!;
    public string? Metadatos { get; set; } // JSON
}
