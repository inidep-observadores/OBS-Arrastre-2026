namespace OBSArrastre2026.App.Data.Entities;

public sealed class Especie
{
    public string ID { get; set; } = Guid.NewGuid().ToString();
    public string? CodigoInidep { get; set; }
    public string? DocumentoInformativo { get; set; }
    public string? Especifico { get; set; }
    public string? Familia { get; set; }
    public bool Frecuente { get; set; }
    public string? Genero { get; set; }
    public string? NombreCientifico { get; set; }
    public string? NombreVulgar { get; set; }
    public string? Orden { get; set; }

    public string FullDisplayName => string.IsNullOrEmpty(NombreCientifico) 
        ? NombreVulgar ?? "" 
        : $"{NombreVulgar} ({NombreCientifico})";

    // Navigation properties
    public ICollection<MareaEtapa> MareaEtapas { get; set; } = new List<MareaEtapa>();
    public ICollection<Muestra> Muestras { get; set; } = new List<Muestra>();
    public ICollection<ItemCaptura> ItemsCaptura { get; set; } = new List<ItemCaptura>();
}
