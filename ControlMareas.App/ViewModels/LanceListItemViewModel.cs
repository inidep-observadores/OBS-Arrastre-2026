using System;
using ControlMareas.App.Data.Entities;

namespace ControlMareas.App.ViewModels;

public sealed class LanceListItemViewModel(Lance lance)
{
    public Lance Lance { get; } = lance;

    public int NroLance => Lance.NroLance;
    
    public string FechaDisplay
    {
        get
        {
            if (DateTime.TryParse(Lance.Fecha, out var date))
            {
                return date.ToString("dd/MM/yyyy");
            }
            return Lance.Fecha;
        }
    }
    
    public string HoraInicio => Lance.HoraInicio ?? "-";
    
    public string HoraFin => Lance.HoraFinal ?? "-";
    
    public string LatitudDisplay => FormatCoordinate(Lance.LatitudInicioDecimal, true);
    
    public string LongitudDisplay => FormatCoordinate(Lance.LongitudInicioDecimal, false);
    
    public string CapturaTotal => Lance.CapturaTotalKg?.ToString("N0") ?? "0";

    public string ID => Lance.Id;

    private string FormatCoordinate(double? value, bool isLatitude)
    {
        if (!value.HasValue) return "-";
        
        double absolute = Math.Abs(value.Value);
        int degrees = (int)absolute;
        double minutes = (absolute - degrees) * 60;
        
        string quadrant = isLatitude 
            ? (value.Value >= 0 ? "N" : "S") 
            : (value.Value >= 0 ? "E" : "O");
            
        // Formato GGº MM,M' C (C= cuadrante N,S,E,O)
        return $"{degrees}º {minutes:00.0}' {quadrant}".Replace('.', ',');
    }
}
