namespace OBSArrastre2026.App.Data.Entities;

public sealed class Buque
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public required string Nombre { get; set; }

    public int Matricula { get; set; }

    public int IdRadial { get; set; }

    public int? IMO { get; set; }

    public int? MMSI { get; set; }

    // Navigation properties
    public ICollection<Marea> Mareas { get; set; } = new List<Marea>();
}
