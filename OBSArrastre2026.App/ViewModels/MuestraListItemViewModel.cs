using System;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.ViewModels;

public sealed class MuestraListItemViewModel(Muestra muestra)
{
    public Muestra Muestra { get; } = muestra;

    public string Id => Muestra.ID;

    public string LanceDisplay => Muestra.Lance != null 
        ? $"Lance {Muestra.Lance.NroLance}" 
        : (Muestra.LanceID != null ? $"ID: {Muestra.LanceID}" : "-");

    public string EspecieDisplay => Muestra.Especie?.NombreVulgar ?? "Sin Especie";

    public string ClaseDisplay => GetClaseDisplay();

    public string PesoDisplay => Muestra.PesoMuestra_PesoGramos.HasValue 
        ? $"{(Muestra.PesoMuestra_PesoGramos.Value / 1000.0):N2} kg" 
        : "-";

    public string EstadoDisplay => Muestra.FrecuenciasTallas.Count > 0 ? "Con Datos" : "Pendiente";

    private string GetClaseDisplay()
    {
        return Muestra.Origen switch
        {
            1 => "Biologica",
            2 => "Comercial",
            3 => "Control",
            _ => "Otros"
        };
    }
}
