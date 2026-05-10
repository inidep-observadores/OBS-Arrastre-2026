namespace OBSArrastre2026.App.Data.Entities;

public sealed class ItemContenidoGastrico
{
    public string ID { get; set; } = Guid.NewGuid().ToString();

    public string? ItemSubmuestraID { get; set; }
    public ItemSubmuestra? ItemSubmuestra { get; set; }

    public int Grupo { get; set; }
    public double Porcentaje { get; set; }
    public int CantPiezas { get; set; }
    public string? Comentarios { get; set; }
    public string? Metadata { get; set; }
}
