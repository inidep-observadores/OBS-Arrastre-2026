namespace OBSArrastre2026.App.Data.Entities;

public sealed class Buque
{
    public Guid Id { get; set; }

    public required string Nombre { get; set; }

    public int Matricula { get; set; }

    public int IdRadial { get; set; }

    public int? IMO { get; set; }

    public int? MMSI { get; set; }
}
