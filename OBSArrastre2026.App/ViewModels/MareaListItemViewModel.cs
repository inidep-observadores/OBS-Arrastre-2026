using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using OBSArrastre2026.App.Data.Entities;
using OBSArrastre2026.App.Services;

namespace OBSArrastre2026.App.ViewModels;

public sealed class MareaListItemViewModel
{
    private readonly IActiveMareaManager _activeMareaManager;

    public MareaListItemViewModel(Marea marea, IActiveMareaManager activeMareaManager)
    {
        Marea = marea;
        _activeMareaManager = activeMareaManager;
        SetActiveCommand = new AsyncRelayCommand(() => _activeMareaManager.SetActiveMareaAsync(Marea.ID));
    }

    public Marea Marea { get; }

    public string CodigoDisplay => $"{Marea.NumeroInidep}/{Marea.AnioInidep % 100:D2}";
    
    public string BuqueNombre => Marea.Buque?.Nombre ?? "Sin buque";
    
    public string FechaInicioDisplay => Marea.FechaInicio.ToString("dd/MM/yyyy");
    
    public string FechaFinDisplay => Marea.FechaFin?.ToString("dd/MM/yyyy") ?? "-";
    
    public string Comentarios => Marea.Comentarios ?? string.Empty;
    
    public string Estado => Marea.FechaFin == null ? "En curso" : "Cerrada";
    
    public string ID => Marea.ID;

    public bool IsActive => _activeMareaManager.ActiveMareaId == Marea.ID;

    public ICommand SetActiveCommand { get; }
}
