namespace OBSArrastre2026.App.Data.Entities;

public sealed class Muestra
{
    public string ID { get; set; } = Guid.NewGuid().ToString();

    public string? LanceID { get; set; }
    public Lance? Lance { get; set; }

    public string? EspecieID { get; set; }
    public Especie? Especie { get; set; }

    public int NumeroOrden { get; set; }
    public string? EspecieOriginal { get; set; }
    public string? Comentarios { get; set; }

    public int EjemplaresPorKg { get; set; }
    public double? Intervalo { get; set; }
    public int UnidadMedidaTalla { get; set; }
    public int ModoMedicionTalla { get; set; }
    public int Origen { get; set; }
    public int DiscriminaSexo { get; set; }
    public int HayIndeterminados { get; set; }
    public double? PesoMuestra_PesoGramos { get; set; }
    public int TipoMuestra { get; set; } = 1; // 1=Estandar, 2=Descarte
    public string? Metadata { get; set; }
    public bool Automatica { get; set; } = false;

    // Campos de Integridad 1:1
    public double? Fuente { get; set; }
    public double? Tarte { get; set; }
    public double? Area { get; set; }
    public double? FactPond { get; set; }
    public int? PrimTalla { get; set; }
    public int? UltTalla { get; set; }

    // Navigation properties
    public ICollection<FrecuenciaTalla> FrecuenciasTallas { get; set; } = new List<FrecuenciaTalla>();
    public ICollection<FrecuenciaTallaEstadio> FrecuenciasTallasEstadio { get; set; } = new List<FrecuenciaTallaEstadio>();
    public ICollection<ItemSubmuestra> ItemsSubmuestras { get; set; } = new List<ItemSubmuestra>();
}
