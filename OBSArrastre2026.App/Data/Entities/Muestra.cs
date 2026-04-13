namespace OBSArrastre2026.App.Data.Entities;

public sealed class Muestra
{
    public string ID { get; set; } = Guid.NewGuid().ToString();

    public string? LanceID { get; set; }
    public Lance? Lance { get; set; }

    public string? EspecieID { get; set; }
    public Especie? Especie { get; set; }

    public string? Comentarios { get; set; }

    public int EjemplaresPorKg { get; set; }
    public double Intervalo { get; set; }
    public int UnidadMedidaTalla { get; set; }
    public int ModoMedicionTalla { get; set; }
    public int Origen { get; set; }
    public int DiscriminaSexo { get; set; }
    public int HayIndeterminados { get; set; }
    public double? PesoMuestra_PesoGramos { get; set; }

    // Navigation properties
    public ICollection<FrecuenciaTalla> FrecuenciasTallas { get; set; } = new List<FrecuenciaTalla>();
    public ICollection<FrecuenciaTallaEstadio> FrecuenciasTallasEstadio { get; set; } = new List<FrecuenciaTallaEstadio>();
    public ICollection<ItemSubmuestra> ItemsSubmuestras { get; set; } = new List<ItemSubmuestra>();
}
