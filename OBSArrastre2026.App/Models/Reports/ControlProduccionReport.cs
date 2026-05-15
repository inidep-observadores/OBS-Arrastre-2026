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

public class ControlProduccionAreaSummary
{
    public string Especie { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public double CapturaKg { get; set; }
    public double DescarteKg { get; set; }
    public double TotalHoras { get; set; }
    public int DiasPesca { get; set; }
    public int CantidadLances { get; set; }
}

public class ControlProduccionDetalleItem
{
    public string Especie { get; set; } = string.Empty;
    public string Producto { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public double Kilos { get; set; }
}

public class ControlProduccionEtapaReport
{
    public int NumeroEtapa { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public List<double> Lats { get; set; } = new();
    public List<double> Lons { get; set; } = new();
    public List<ControlProduccionReportItem> Items { get; set; } = new();
    public List<ControlProduccionAreaSummary> AreaSummaries { get; set; } = new();
    public List<ControlProduccionDetalleItem> ProduccionDetalle { get; set; } = new();
}

public class ControlProduccionReport
{
    public string Barco { get; set; } = string.Empty;
    public string Marea { get; set; } = string.Empty;
    public int Anio { get; set; }
    public int? BuqueCodigo { get; set; }
    public string? ObservadorNombre { get; set; }
    public string? ObservadorApellido { get; set; }
    public int? ObservadorCodigo { get; set; }
    public DateTime? FechaInicioMarea { get; set; }
    public DateTime? FechaFinMarea { get; set; }
    public List<ControlProduccionEtapaReport> Etapas { get; set; } = new();
    public DateTime FechaGeneracion { get; set; } = DateTime.Now;
}
public class ControlProduccionDetalleEspecieItem
{
    public DateTime Fecha { get; set; }
    public double ProduccionTotal { get; set; }
    public double CapturaReconstruida { get; set; }
    public double CapturaBruta { get; set; }
    public double DescarteKg { get; set; }
    public double CapturaRetenida { get; set; }
    public double DiferenciaKg { get; set; }
    public double SaldoAcumuladoKg { get; set; }
    public string DiferenciaPorcentaje { get; set; } = string.Empty;
}

public class ControlProduccionDetalleEspecieReport
{
    public string Barco { get; set; } = string.Empty;
    public string Marea { get; set; } = string.Empty;
    public int Anio { get; set; }
    public int? BuqueCodigo { get; set; }
    public string? ObservadorNombre { get; set; }
    public string? ObservadorApellido { get; set; }
    public int? ObservadorCodigo { get; set; }
    public DateTime? FechaInicioMarea { get; set; }
    public DateTime? FechaFinMarea { get; set; }

    public string Especie { get; set; } = string.Empty;
    public List<ControlProduccionDetalleEspecieItem> Items { get; set; } = new();

    public double TotalCaptura { get; set; }
    public double TotalDescarte { get; set; }
    public double TotalRetenida { get; set; }
    public DateTime FechaGeneracion { get; set; } = DateTime.Now;
}
