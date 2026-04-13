namespace OBSArrastre2026.App.Data.Entities;

public sealed class ItemCaptura
{
    public string ID { get; set; } = Guid.NewGuid().ToString();

    public string? LanceID { get; set; }
    public Lance? Lance { get; set; }

    public string? EspecieID { get; set; }
    public Especie? Especie { get; set; }

    public int NumeroOrden { get; set; }
    public int TipoDatoCaptura { get; set; }
    public double DatoCaptura { get; set; }
    public int TipoDatoDescarte { get; set; }
    public double DatoDescarte { get; set; }
}
