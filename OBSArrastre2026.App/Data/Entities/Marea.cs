namespace OBSArrastre2026.App.Data.Entities;

public sealed class Marea
{
    public string ID { get; set; } = Guid.NewGuid().ToString();
    public int AnioInidep { get; set; }
    public int NumeroInidep { get; set; }
    public string? Codigo { get; set; }
    public string? Comentarios { get; set; }
    public DateTime FechaInicio { get; set; } = DateTime.Today;
    public DateTime? FechaFin { get; set; }
    public string? BuqueID { get; set; }
    public Buque? Buque { get; set; }

    public ICollection<MareaEtapa> Etapas { get; set; } = new List<MareaEtapa>();
}
