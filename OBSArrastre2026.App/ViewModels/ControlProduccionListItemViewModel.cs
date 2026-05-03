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
    public double CapturaTotal { get; set; }
    public bool IsSummaryView { get; set; }

    public string FechaDisplay => Fecha.ToString("dd/MM/yyyy");
    
    public string ProduccionTotalDisplay => ProduccionTotal.ToString("N1");
    public string CapturaReconstruidaDisplay => CapturaReconstruida.ToString("N1");
    public string CapturaTotalDisplay => CapturaTotal.ToString("N1");

    public double DiferenciaKg => CapturaTotal - CapturaReconstruida;
    public double DiferenciaPorcentaje => CapturaTotal > 0 
        ? (DiferenciaKg * 100.0 / CapturaTotal) 
        : (CapturaReconstruida > 0 ? -100.0 : 0);

    public string DiferenciaKgDisplay => DiferenciaKg.ToString("N1");
    public string DiferenciaPorcentajeDisplay => DiferenciaPorcentaje.ToString("N1") + "%";

    public bool HasDiferenciaSignificativa => Math.Abs(DiferenciaKg) > (CapturaTotal * 0.01);
}
