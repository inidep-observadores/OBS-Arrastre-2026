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
    public string? Comentarios { get; set; }

    // Navigation properties
    public ICollection<ItemContenidoGastrico> ContenidosGastricos { get; set; } = new List<ItemContenidoGastrico>();
}
