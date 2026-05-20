using CommunityToolkit.Mvvm.ComponentModel;
using ControlMareas.App.Data.Entities;

namespace ControlMareas.App.ViewModels;

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
    private double _pesoCalculadoKg;
    private System.Collections.Generic.IReadOnlyList<EspecieLargoPeso>? _parametrosAlometricos;

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

    public double Talla 
    { 
        get => _talla; 
        set 
        {
            if (SetProperty(ref _talla, value))
                UpdateTotal();
        } 
    }
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

    public double PesoCalculadoKg { get => _pesoCalculadoKg; set => SetProperty(ref _pesoCalculadoKg, value); }

    public void SetParametros(System.Collections.Generic.IReadOnlyList<EspecieLargoPeso>? parametros)
    {
        _parametrosAlometricos = parametros;
        UpdateTotal();
    }

    private void UpdateTotal()
    {
        int suma = NroMachos + NroHembras + NroIndeterminados;
        if (suma > 0)
        {
            NroTotal = suma;
        }
        OnPropertyChanged(nameof(Total));
        RecalcularPeso();
    }

    private void RecalcularPeso()
    {
        if (_parametrosAlometricos == null || _parametrosAlometricos.Count == 0 || Talla <= 0)
        {
            PesoCalculadoKg = 0;
            return;
        }

        double pesoGramos = 0;
        int sumaSexos = NroMachos + NroHembras + NroIndeterminados;

        if (sumaSexos > 0)
        {
            // Hembras (Sexo = 2)
            if (NroHembras > 0)
            {
                var pM = _parametrosAlometricos.FirstOrDefault(p => p.Sexo == 2) ?? _parametrosAlometricos.FirstOrDefault(p => p.Sexo == 0);
                if (pM != null && pM.ParamA > 0)
                    pesoGramos += NroHembras * (pM.ParamA * System.Math.Pow(Talla, pM.ParamB));
            }

            // Machos (Sexo = 1)
            if (NroMachos > 0)
            {
                var pM = _parametrosAlometricos.FirstOrDefault(p => p.Sexo == 1) ?? _parametrosAlometricos.FirstOrDefault(p => p.Sexo == 0);
                if (pM != null && pM.ParamA > 0)
                    pesoGramos += NroMachos * (pM.ParamA * System.Math.Pow(Talla, pM.ParamB));
            }

            // Indeterminados (Sexo = 0)
            if (NroIndeterminados > 0)
            {
                var pM = _parametrosAlometricos.FirstOrDefault(p => p.Sexo == 0);
                if (pM != null && pM.ParamA > 0)
                    pesoGramos += NroIndeterminados * (pM.ParamA * System.Math.Pow(Talla, pM.ParamB));
            }
        }
        else if (NroTotal > 0)
        {
            // Si no hay desglose por sexos pero hay total, se toma como indeterminado
            var pM = _parametrosAlometricos.FirstOrDefault(p => p.Sexo == 0);
            if (pM != null && pM.ParamA > 0)
                pesoGramos += NroTotal * (pM.ParamA * System.Math.Pow(Talla, pM.ParamB));
        }

        PesoCalculadoKg = System.Math.Round(pesoGramos / 1000.0, 2);
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
