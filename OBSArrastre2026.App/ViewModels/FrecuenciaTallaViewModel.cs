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
    public int NroMachos { get => _nroMachos; set => SetProperty(ref _nroMachos, value); }
    public int NroHembras { get => _nroHembras; set => SetProperty(ref _nroHembras, value); }
    public int NroIndeterminados { get => _nroIndeterminados; set => SetProperty(ref _nroIndeterminados, value); }

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
