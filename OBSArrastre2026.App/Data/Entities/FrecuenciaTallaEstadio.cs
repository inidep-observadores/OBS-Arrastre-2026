namespace OBSArrastre2026.App.Data.Entities;

public sealed class FrecuenciaTallaEstadio
{
    public string ID { get; set; } = Guid.NewGuid().ToString();

    public string? MuestraID { get; set; }
    public Muestra? Muestra { get; set; }

    public double Talla { get; set; }
    public string? EstadiosHembras { get; set; }
    public string? EstadiosMachos { get; set; }
    public int NroIndeterminados { get; set; }
}
