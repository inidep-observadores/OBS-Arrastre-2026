using System;
using System.Collections.Generic;

namespace OBSArrastre2026.App.Models.Reports;

public class MareaSummarySpeciesItem
{
    public string EspecieId { get; set; } = string.Empty;
    public string NombreCientifico { get; set; } = string.Empty;
    public string CodigoInidep { get; set; } = string.Empty;
    public double CapturaTotal { get; set; }
    public double DescarteKg { get; set; }
    public double DescartePorcentaje => CapturaTotal > 0 ? (DescarteKg * 100.0 / CapturaTotal) : 0;
    public double ProduccionTotal { get; set; }
    public int NroLances { get; set; }
    public int NroDias { get; set; }
    public double? PorcentajeJuveniles { get; set; }
    public bool EsObjetivo { get; set; }
}

public class MareaSummaryAreaItem
{
    public string Area { get; set; } = string.Empty;
    public double CapturaKg { get; set; }
    public int CantidadLances { get; set; }
    public int DiasPesca { get; set; }
}

public class MareaSummarySection
{
    public string Titulo { get; set; } = string.Empty;
    public bool EsEtapa { get; set; }
    public int? NumeroEtapa { get; set; }
    
    // Datos generales
    public int DiasNavegados { get; set; }
    public int DiasPesca { get; set; }
    public int CantidadLances { get; set; }
    public int CantidadMuestrasCaptura { get; set; }
    public int CantidadMuestrasDescarte { get; set; }
    public int CantidadSubmuestras { get; set; }

    // Especies Objetivo
    public List<MareaSummarySpeciesItem> EspeciesObjetivo { get; set; } = new();
    
    // Áreas
    public List<MareaSummaryAreaItem> Areas { get; set; } = new();
    public string AreaMasLances { get; set; } = string.Empty;
    public string AreaMayorCaptura { get; set; } = string.Empty;
}

public class MareaSummaryReport
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
    
    public MareaSummarySection ResumenGeneral { get; set; } = new();
    public List<MareaSummarySection> ResumenEtapas { get; set; } = new();
    
    public DateTime FechaGeneracion { get; set; } = DateTime.Now;
}
