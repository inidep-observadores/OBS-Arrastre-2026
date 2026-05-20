using System;
using System.Collections.Generic;


namespace ControlMareas.App.Models.Reports;

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

public class MareaSummarySampledSpeciesItem
{
    public string EspecieId { get; set; } = string.Empty;
    public string NombreCientifico { get; set; } = string.Empty;
    public int MuestrasCaptura { get; set; }
    public int MuestrasDescarte { get; set; }
    public int MuestrasConSubmuestra { get; set; }
}

public class MareaSummaryAreaItem
{
    public string Area { get; set; } = string.Empty;
    public double CapturaKg { get; set; }
    public int CantidadLances { get; set; }
    public int DiasPesca { get; set; }
}

/// <summary>Datos de la especie objetivo para la narrativa de una etapa.</summary>
public class NarrativaEspecieObjetivo
{
    public string NombreVulgar { get; set; } = string.Empty;
    public string NombreCientifico { get; set; } = string.Empty;
    public double CapturaKg { get; set; }
    public double DescarteKg { get; set; }
    public double DescartePct => CapturaKg > 0 ? DescarteKg * 100.0 / CapturaKg : 0;
    /// <summary>true cuando el descarte es 100% (no se retuvo nada).</summary>
    public bool DescarteTotal => CapturaKg > 0 && DescarteKg >= CapturaKg;
}

/// <summary>Datos resumidos de una especie secundaria observada en la captura (para la narrativa).</summary>
public class NarrativaEspecieSecundaria
{
    public string NombreVulgar { get; set; } = string.Empty;
    public string NombreCientifico { get; set; } = string.Empty;
    public double CapturaKg { get; set; }
    public double DescarteKg { get; set; }
    public double DescartePct => CapturaKg > 0 ? DescarteKg * 100.0 / CapturaKg : 0;
    /// <summary>true cuando fue descartada completamente (sin retención).</summary>
    public bool DescarteTotal => CapturaKg > 0 && Math.Abs(DescartePct - 100.0) < 0.01;
}

/// <summary>Resumen de muestras de una especie para el párrafo final de la narrativa.</summary>
public class NarrativaMuestraEspecie
{
    public string NombreVulgar { get; set; } = string.Empty;
    public string NombreCientifico { get; set; } = string.Empty;
    public int TotalMuestras { get; set; }
}

/// <summary>Datos narrativos de una etapa individual.</summary>
public class NarrativaEtapa
{
    public int Numero { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public int TotalLances { get; set; }
    public int DiasPesca { get; set; }
    public double CapturaKg { get; set; }
    public double DescarteKg { get; set; }
    public double DescartePct => CapturaKg > 0 ? DescarteKg * 100.0 / CapturaKg : 0;
    /// <summary>Cuadrados estadísticos donde operó el buque.</summary>
    public List<string> Cuadrados { get; set; } = new();
    
    /// <summary>Área/Cuadrado con más lances/operaciones.</summary>
    public string? CuadradoMasLances { get; set; }
    public int CuadradoMasLancesNro { get; set; }
    
    /// <summary>Área/Cuadrado con mayor volumen de captura.</summary>
    public string? CuadradoMayorCaptura { get; set; }
    public double CuadradoMayorCapturaKg { get; set; }
    
    /// <summary>Obsoleto: use CuadradoMasLances o CuadradoMayorCaptura. Se mantiene por compatibilidad.</summary>
    public string? CuadradoDominante { get; set; }
    public int CuadradoDominanteLances { get; set; }
    public double CuadradoDominanteCapturaKg { get; set; }
    public int CuadradoDominanteDias { get; set; }

    /// <summary>Especie objetivo declarada en la etapa (puede diferir entre etapas).</summary>
    public NarrativaEspecieObjetivo? EspecieObjetivo { get; set; }
    /// <summary>Especies secundarias con captura relevante observadas en la etapa.</summary>
    public List<NarrativaEspecieSecundaria> EspeciesSecundarias { get; set; } = new();
}

public class MareaSummarySection
{
    public string Titulo { get; set; } = string.Empty;
    public bool EsEtapa { get; set; }
    public int? NumeroEtapa { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }

    
    // Datos generales
    public int DiasNavegados { get; set; }
    public int DiasPesca { get; set; }
    public int CantidadLances { get; set; }
    public int CantidadMuestrasCaptura { get; set; }
    public int CantidadMuestrasDescarte { get; set; }
    public int CantidadSubmuestras { get; set; }

    // Especies Muestreadas
    public List<MareaSummarySampledSpeciesItem> EspeciesMuestreadas { get; set; } = new();

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

    // --- Datos para la narrativa textual ---
    /// <summary>Datos narrativos detallados por etapa (en orden cronológico).</summary>
    public List<NarrativaEtapa> NarrativaEtapas { get; set; } = new();
    /// <summary>Resumen de muestras por especie (para el párrafo de cierre).</summary>
    public List<NarrativaMuestraEspecie> NarrativaMuestras { get; set; } = new();
    /// <summary>Captura total de la marea en kg.</summary>
    public double NarrativaCapturaTotal { get; set; }
    /// <summary>Descarte total de la marea en kg.</summary>
    public double NarrativaDescarteTotal { get; set; }
    public double NarrativaDescartePct => NarrativaCapturaTotal > 0 ? NarrativaDescarteTotal * 100.0 / NarrativaCapturaTotal : 0;
    /// <summary>Total de lances de la marea.</summary>
    public int NarrativaTotalLances { get; set; }
    /// <summary>Total de días de pesca de la marea.</summary>
    public int NarrativaTotalDiasPesca { get; set; }
    /// <summary>Especies objetivo únicas presentes en toda la marea (deduplicadas por nombre).</summary>
    public List<NarrativaEspecieObjetivo> NarrativaEspeciesObjetivo { get; set; } = new();

    public DateTime FechaGeneracion { get; set; } = DateTime.Now;
}
