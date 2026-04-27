using CommunityToolkit.Mvvm.ComponentModel;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.ViewModels;

public sealed class FrecuenciaTallaViewModel : ObservableObject
{
    private double _talla;
    private int _nroMachos;
    private int _nroHembras;
    private int _nroIndeterminados;

    public FrecuenciaTallaViewModel() { }

    public FrecuenciaTallaViewModel(FrecuenciaTalla entity)
    {
        Id = entity.ID;
        Talla = entity.Talla;
        NroMachos = entity.NroMachos;
        NroHembras = entity.NroHembras;
        NroIndeterminados = entity.NroIndeterminados;
    }

    public string Id { get; set; } = Guid.NewGuid().ToString();

    public double Talla { get => _talla; set => SetProperty(ref _talla, value); }
    public int NroMachos 
    { 
        get => _nroMachos; 
        set 
        {
            if (SetProperty(ref _nroMachos, value))
                OnPropertyChanged(nameof(Total));
        } 
    }
    public int NroHembras 
    { 
        get => _nroHembras; 
        set 
        {
            if (SetProperty(ref _nroHembras, value))
                OnPropertyChanged(nameof(Total));
        } 
    }
    public int NroIndeterminados 
    { 
        get => _nroIndeterminados; 
        set 
        {
            if (SetProperty(ref _nroIndeterminados, value))
                OnPropertyChanged(nameof(Total));
        } 
    }

    public int Total => NroMachos + NroHembras + NroIndeterminados;

    public FrecuenciaTalla ToEntity()
    {
        return new FrecuenciaTalla
        {
            ID = Id,
            Talla = Talla,
            NroMachos = NroMachos,
            NroHembras = NroHembras,
            NroIndeterminados = NroIndeterminados
        };
    }
}
