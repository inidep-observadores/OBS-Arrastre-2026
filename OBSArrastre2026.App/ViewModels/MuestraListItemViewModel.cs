using System;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.ViewModels;

public sealed class MuestraListItemViewModel(Muestra muestra)
{
    public Muestra Muestra { get; } = muestra;

    public string LanceNro => Muestra.Lance?.NroLance.ToString() ?? "-";

    public string EtapaDisplay => Muestra.Lance?.MareaEtapa != null 
        ? $"Etapa {Muestra.Lance.MareaEtapa.NumeroEtapa}" 
        : "-";
    
    public string LanceFecha 
    {
        get
        {
            if (Muestra.Lance != null && DateTime.TryParse(Muestra.Lance.Fecha, out var date))
            {
                return date.ToString("dd/MM/yyyy");
            }
            return Muestra.Lance?.Fecha ?? "-";
        }
    }

    public string LanceHora => Muestra.Lance?.HoraFinal ?? "-";

    public string EspecieDisplay => Muestra.Especie?.FullDisplayName ?? "Sin Especie";

    public string PesoDisplay => Muestra.PesoMuestra_PesoGramos.HasValue 
        ? $"{(Muestra.PesoMuestra_PesoGramos.Value / 1000.0):N2} kg" 
        : "-";

    public bool Automatica => Muestra.Automatica;

    public string TipoMuestraDisplay => Muestra.TipoMuestra switch
    {
        1 => Automatica ? "Estándar (Auto)" : "Estándar",
        2 => Automatica ? "Descarte (Auto)" : "Descarte",
        _ => Automatica ? $"Tipo {Muestra.TipoMuestra} (Auto)" : $"Tipo {Muestra.TipoMuestra}"
    };

    public string ID => Muestra.ID;
}
