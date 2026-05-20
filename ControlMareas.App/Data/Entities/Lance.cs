using System.ComponentModel.DataAnnotations.Schema;
using ControlMareas.App.Models;

namespace ControlMareas.App.Data.Entities;


public sealed class Lance
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string MareaEtapaId { get; set; } = string.Empty;
    public MareaEtapa MareaEtapa { get; set; } = null!;

    public int NroLance { get; set; }
    public string Fecha { get; set; } = string.Empty;
    public string? HoraInicio { get; set; }
    public string? HoraFinal { get; set; }

    public double? LatitudInicioDecimal { get; set; }
    public double? LongitudInicioDecimal { get; set; }
    public double? LatitudFinalDecimal { get; set; }
    public double? LongitudFinalDecimal { get; set; }

    public int? ProfundidadInicioM { get; set; }
    public int? ProfundidadFinalM { get; set; }

    public int? EstadoTiempoCodigo { get; set; }
    public int? EstadoMarCodigo { get; set; }
    public int? VientoDireccionGrados { get; set; }
    public int? VientoFuerzaBeaufort { get; set; }

    public double? TemperaturaAireC { get; set; }
    public double? TemperaturaRedC { get; set; }
    public int? PresionHpa { get; set; }

    public double? CapturaTotalKg { get; set; }
    public double? DescarteTotalKg { get; set; }
    public double? VelocidadArrastreNudos { get; set; }
    public int? RumboGrados { get; set; }

    public int? MallaCopoMm { get; set; }
    public int? MallaAlasMm { get; set; }
    public int? CableFiladoM { get; set; }
    public double? AberturaVerticalM { get; set; }
    public double? DistanciaAlasM { get; set; }
    public double? DistanciaPortonesM { get; set; }

    public int SelectividadSiNo { get; set; } // 0 o 1
    public string? Comentarios { get; set; }
    public string? Metadata { get; set; }

    // Campos de Integridad 1:1 (Legacy DBF)
    public double? Mus { get; set; }
    public double? EstacionGral { get; set; }
    public double? Estrato { get; set; }
    public double? EdadLuna { get; set; }
    public double? Luz { get; set; }
    public double? TmpAHum { get; set; }
    public double? TmpMarS { get; set; }
    public double? ArteTipo { get; set; }
    public double? ArteNro { get; set; }
    public double? AreaBarrida { get; set; }
    public double? MallaSobre { get; set; }

    // Navigation properties
    public ICollection<Muestra> Muestras { get; set; } = new List<Muestra>();
    public ICollection<ItemCaptura> ItemsCaptura { get; set; } = new List<ItemCaptura>();

    [NotMapped]
    public double SumaPesoMuestras => ItemsCaptura
        .Where(i => i.TipoDatoCaptura == TipoDatoCaptura.Muestra)
        .Sum(i => i.DatoCaptura);

    [NotMapped]
    public double SumaPesoCapturadoNoMuestreado => ItemsCaptura
        .Where(i => i.TipoDatoCaptura != TipoDatoCaptura.Muestra)
        .Sum(i => i.CapturaTotalKgCalculado);
}

