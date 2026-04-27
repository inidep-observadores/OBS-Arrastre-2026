using System;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.ViewModels;

public sealed class MuestraListItemViewModel(Muestra muestra)
{
    public Muestra Muestra { get; } = muestra;

    public string LanceNro => Muestra.Lance?.NroLance.ToString() ?? "-";
    
    public string LanceFecha => Muestra.Lance?.Fecha ?? "-";

    public string LanceHora => Muestra.Lance?.HoraFinal ?? "-";

    public string EspecieDisplay => Muestra.Especie?.NombreVulgar ?? "Sin Especie";

    public string PesoDisplay => Muestra.PesoMuestra_PesoGramos.HasValue 
        ? $"{(Muestra.PesoMuestra_PesoGramos.Value / 1000.0):N2} kg" 
        : "-";
}
