namespace OBSArrastre2026.App.Data.Entities;

public sealed class RegistroProduccion
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string MareaEtapaId { get; set; } = string.Empty;
    public MareaEtapa MareaEtapa { get; set; } = null!;

    public string Fecha { get; set; } = string.Empty; // SQLite uses TEXT for dates
    
    public string IdProducto { get; set; } = string.Empty;
    public Producto Producto { get; set; } = null!;

    public string? Categoria { get; set; }
    
    public string? EspecieId { get; set; }
    public string? EspecieOriginal { get; set; }
    public Especie? Especie { get; set; }

    public double? Factor { get; set; }
    public int? Operarios { get; set; }
    public double? Kg { get; set; }
    public int NumeroOrden { get; set; }
    public string? Comentarios { get; set; }
    public string? Metadata { get; set; }
}
