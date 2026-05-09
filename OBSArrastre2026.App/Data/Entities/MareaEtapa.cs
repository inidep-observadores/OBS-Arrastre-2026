namespace OBSArrastre2026.App.Data.Entities;

public sealed class MareaEtapa
{
    public string ID { get; set; } = Guid.NewGuid().ToString();
    public DateTime FechaZarpada { get; set; } = DateTime.Today;
    public DateTime? FechaArribo { get; set; }
    public string? MareaID { get; set; }
    public Marea? Marea { get; set; }
    public string? EspecieObjetivoID { get; set; }
    public Especie? EspecieObjetivo { get; set; }
    public string? NombreCapitan { get; set; }
    public string? NombreOficialCubierta { get; set; }
    public string? NombreOficialPesca { get; set; }
    public int? AnioMareaBuque { get; set; }
    public int? NumeroMareaBuque { get; set; }
    public string? Metadata { get; set; }

    // Navigation properties
    public ICollection<RegistroProduccion> RegistrosProduccion { get; set; } = new List<RegistroProduccion>();
    public ICollection<Lance> Lances { get; set; } = new List<Lance>();
}
