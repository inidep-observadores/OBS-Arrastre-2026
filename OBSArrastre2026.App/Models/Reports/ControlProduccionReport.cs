using System;
using System.Collections.Generic;

namespace OBSArrastre2026.App.Models.Reports;

public class ControlProduccionReportItem
{
    public string Especie { get; set; } = string.Empty;
    public double ProduccionTotal { get; set; }
    public double CapturaReconstruida { get; set; }
    public double CapturaBruta { get; set; }
    public double DescarteKg { get; set; }
    public double CapturaRetenida { get; set; }
    public double DiferenciaKg { get; set; }
    public string DiferenciaPorcentaje { get; set; } = string.Empty;
    public bool HasDiferenciaSignificativa { get; set; }
}

public class ControlProduccionReport
{
    public string Barco { get; set; } = string.Empty;
    public string Marea { get; set; } = string.Empty;
    public int Anio { get; set; }
    public DateTime? FechaInicioMarea { get; set; }
    public DateTime? FechaFinMarea { get; set; }
    public List<ControlProduccionReportItem> Items { get; set; } = new();
    public DateTime FechaGeneracion { get; set; } = DateTime.Now;
}
