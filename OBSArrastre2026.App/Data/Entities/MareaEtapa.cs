using System.ComponentModel.DataAnnotations.Schema;
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

    /// <summary>
    /// Calcula el número de etapa basado en el orden cronológico de las etapas de la misma marea.
    /// </summary>
    [NotMapped]
    public int NumeroEtapa
    {
        get
        {
            if (Marea == null || Marea.Etapas == null || !Marea.Etapas.Any())
                return 1;

            var ordered = Marea.Etapas
                .OrderBy(e => e.FechaZarpada)
                .ThenBy(e => e.ID) // Desempate determinista
                .ToList();

            var index = ordered.FindIndex(e => e.ID == ID);
            return index >= 0 ? index + 1 : 1;
        }
    }

    /// <summary>
    /// Calcula el número de etapa para una etapa específica dentro de un conjunto dado de etapas.
    /// </summary>
    public static int CalcularNumeroEtapa(MareaEtapa etapa, IEnumerable<MareaEtapa> etapasDeLaMarea)
    {
        if (etapa == null || etapasDeLaMarea == null || !etapasDeLaMarea.Any())
            return 1;

        var ordered = etapasDeLaMarea
            .OrderBy(e => e.FechaZarpada)
            .ThenBy(e => e.ID)
            .ToList();

        var index = ordered.FindIndex(e => e.ID == etapa.ID);
        return index >= 0 ? index + 1 : 1;
    }
}
