using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace OBSArrastre2026.App.Data.Entities;

public sealed class MareaEtapa
{
    public string ID { get; set; } = Guid.NewGuid().ToString();

    public string? MareaID { get; set; }
    public Marea? Marea { get; set; }

    public DateTime FechaZarpada { get; set; } = DateTime.Today;
    public DateTime? FechaArribo { get; set; }

    public string? EspecieObjetivoID { get; set; }
    public Especie? EspecieObjetivo { get; set; }

    public string? NombreCapitan { get; set; }
    public string? NombreOficialCubierta { get; set; }
    public string? NombreOficialPesca { get; set; }

    public int? AnioMareaBuque { get; set; }
    public int? NumeroMareaBuque { get; set; }

    public string? Metadata { get; set; }

    public ICollection<Lance> Lances { get; set; } = new List<Lance>();
    public ICollection<RegistroProduccion> RegistrosProduccion { get; set; } = new List<RegistroProduccion>();

    [NotMapped]
    public int NumeroEtapa
    {
        get
        {
            if (Marea == null || Marea.Etapas == null || !Marea.Etapas.Any())
            {
                return 1;
            }

            // Ordenar por fecha y luego por ID para asegurar determinismo
            var list = Marea.Etapas.ToList();
            var ordered = list
                .Where(e => e != null)
                .OrderBy(e => e.FechaZarpada)
                .ThenBy(e => e.ID)
                .ToList();

            // Buscar por ID para evitar problemas de referencia de objeto
            for (int i = 0; i < ordered.Count; i++)
            {
                if (ordered[i].ID == this.ID)
                {
                    return i + 1;
                }
            }

            return 1;
        }
    }

    [NotMapped]
    public string DisplayName => $"Etapa {NumeroEtapa} ({FechaZarpada:dd/MM/yyyy})";
}
