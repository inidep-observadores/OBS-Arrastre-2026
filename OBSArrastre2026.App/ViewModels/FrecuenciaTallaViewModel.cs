using CommunityToolkit.Mvvm.ComponentModel;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.ViewModels;

public sealed class FrecuenciaTallaViewModel : ObservableObject
{
    private double _talla;
    private int _nroMachos;
    private int _nroHembras;
    private int _nroIndeterminados;
    private int _nroTotal;
    private int _nroLangostinosMachoMaduros;
    private int _nroLangostinosHembraMaduras;
    private int _nroLangostinosHembraImpregnadas;

    public FrecuenciaTallaViewModel() { }

    public FrecuenciaTallaViewModel(FrecuenciaTalla entity)
    {
        Id = entity.ID;
        Talla = entity.Talla;
        NroMachos = entity.NroMachos;
        NroHembras = entity.NroHembras;
        NroIndeterminados = entity.NroIndeterminados;
        NroLangostinosMachoMaduros = entity.NroLangostinosMachoMaduros;
        NroLangostinosHembraMaduras = entity.NroLangostinosHembraMaduras;
        NroLangostinosHembraImpregnadas = entity.NroLangostinosHembraImpregnadas;
        NroTotal = entity.NroTotal;
    }

    public string Id { get; set; } = Guid.NewGuid().ToString();

    public double Talla { get => _talla; set => SetProperty(ref _talla, value); }
    public int NroMachos 
    { 
        get => _nroMachos; 
        set 
        {
            if (SetProperty(ref _nroMachos, value))
                UpdateTotal();
        } 
    }
    public int NroHembras 
    { 
        get => _nroHembras; 
        set 
        {
            if (SetProperty(ref _nroHembras, value))
                UpdateTotal();
        } 
    }
    public int NroIndeterminados 
    { 
        get => _nroIndeterminados; 
        set 
        {
            if (SetProperty(ref _nroIndeterminados, value))
                UpdateTotal();
        } 
    }

    public int NroLangostinosMachoMaduros { get => _nroLangostinosMachoMaduros; set => SetProperty(ref _nroLangostinosMachoMaduros, value); }
    public int NroLangostinosHembraMaduras { get => _nroLangostinosHembraMaduras; set => SetProperty(ref _nroLangostinosHembraMaduras, value); }
    public int NroLangostinosHembraImpregnadas { get => _nroLangostinosHembraImpregnadas; set => SetProperty(ref _nroLangostinosHembraImpregnadas, value); }

    public int NroTotal { get => _nroTotal; set => SetProperty(ref _nroTotal, value); }
    public int Total => NroTotal;

    private void UpdateTotal()
    {
        int suma = NroMachos + NroHembras + NroIndeterminados;
        if (suma > 0)
        {
            NroTotal = suma;
        }
        OnPropertyChanged(nameof(Total));
    }


    public FrecuenciaTalla ToEntity()
    {
        return new FrecuenciaTalla
        {
            ID = Id,
            Talla = Talla,
            NroMachos = NroMachos,
            NroHembras = NroHembras,
            NroIndeterminados = NroIndeterminados,
            NroLangostinosMachoMaduros = NroLangostinosMachoMaduros,
            NroLangostinosHembraMaduras = NroLangostinosHembraMaduras,
            NroLangostinosHembraImpregnadas = NroLangostinosHembraImpregnadas,
            NroTotal = NroTotal
        };
    }
}
