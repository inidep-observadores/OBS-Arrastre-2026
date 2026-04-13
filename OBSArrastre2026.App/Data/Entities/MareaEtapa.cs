namespace OBSArrastre2026.App.Data.Entities;

public sealed class MareaEtapa
{
    public string ID { get; set; } = Guid.NewGuid().ToString();
    public DateTime FechaZarpada { get; set; } = DateTime.Today;
    public DateTime? FechaArribo { get; set; }
    public string? MareaID { get; set; }
    public string? EspecieObjetivoID { get; set; }
    public string? NombreCapitan { get; set; }
    public string? NombreOficialCubierta { get; set; }
    public string? NombreOficialPesca { get; set; }
    public int? AnioMareaBuque { get; set; }
    public int? NumeroMareaBuque { get; set; }
}
