namespace OBSArrastre2026.App.Data.Entities;

public sealed class ItemSubmuestra
{
    public string ID { get; set; } = Guid.NewGuid().ToString();

    public string? MuestraID { get; set; }
    public Muestra? Muestra { get; set; }

    public int NroEjemplar { get; set; }
    public int? Sexo { get; set; }
    public int? Estadio { get; set; }
    public int? ReplecionGastrica { get; set; }
    public double? Edad { get; set; }
    
    public int? LargoTotalMm { get; set; }
    public int? LargoEstandarMm { get; set; }
    public double? PesoTotalGramos { get; set; }

    public string? Comentarios { get; set; }
    public string? Metadata { get; set; }

    // Campos de Integridad 1:1
    public int NumeroOrden { get; set; }
    public string? EspecieOriginal { get; set; }
    public double? Tarte { get; set; }
    public double? Fuente { get; set; }
    public double? Area { get; set; }
    public double? PesoVac { get; set; }
    public double? PesoGon { get; set; }
    public double? PesoHig { get; set; }
    public double? RTotal { get; set; }

    // Navigation properties
    public ICollection<ItemContenidoGastrico> ContenidosGastricos { get; set; } = new List<ItemContenidoGastrico>();
}
