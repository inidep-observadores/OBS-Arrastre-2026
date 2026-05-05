using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OBSArrastre2026.App.ViewModels;

public sealed class ControlProduccionListItemViewModel : ObservableObject
{
    public DateTime Fecha { get; set; }
    public string Especie { get; set; } = string.Empty;
    public double ProduccionTotal { get; set; }
    public double CapturaReconstruida { get; set; }
    public string? EspecieId { get; set; }
    public double CapturaBruta { get; set; }
    public double DescarteKg { get; set; }
    public double CapturaRetenida { get; set; }
    public int NumeroEtapa { get; set; }
    public string EtapaDisplay { get; set; } = string.Empty;
    public bool IsSummaryView { get; set; }

    public string FechaDisplay => Fecha.ToString("dd/MM/yyyy");
    
    public string ProduccionTotalDisplay => ProduccionTotal.ToString("N1");
    public string CapturaReconstruidaDisplay => CapturaReconstruida.ToString("N1");
    public string CapturaBrutaDisplay => CapturaBruta.ToString("N1");
    public string DescarteKgDisplay => DescarteKg.ToString("N1");
    public string CapturaRetenidaDisplay => CapturaRetenida.ToString("N1");

    public double DiferenciaKg => CapturaRetenida - CapturaReconstruida;
    public double DiferenciaPorcentaje => CapturaRetenida > 0 
        ? (DiferenciaKg * 100.0 / CapturaRetenida) 
        : (CapturaReconstruida > 0 ? -100.0 : 0);

    public string DiferenciaKgDisplay => DiferenciaKg.ToString("N1");
    public string DiferenciaPorcentajeDisplay => DiferenciaPorcentaje.ToString("N2") + "%";

    public bool HasDiferenciaSignificativa => Math.Abs(DiferenciaKg) > (CapturaRetenida * 0.01);
}
