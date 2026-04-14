using System;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.ViewModels;

public sealed class MareaListItemViewModel(Marea marea)
{
    public Marea Marea { get; } = marea;

    public string CodigoDisplay => $"{Marea.NumeroInidep}/{Marea.AnioInidep % 100:D2}";
    
    public string BuqueNombre => Marea.Buque?.Nombre ?? "Sin buque";
    
    public string FechaInicioDisplay => Marea.FechaInicio.ToString("dd/MM/yyyy");
    
    public string FechaFinDisplay => Marea.FechaFin?.ToString("dd/MM/yyyy") ?? "-";
    
    public string Comentarios => Marea.Comentarios ?? string.Empty;
    
    public string Estado => Marea.FechaFin == null ? "En curso" : "Cerrada";
    
    public string ID => Marea.ID;

    // Mapeo opcional para mantener compatibilidad con la estructura genérica si fuera necesario
    // Pero usaremos propiedades específicas en el nuevo DataTemplate
}
