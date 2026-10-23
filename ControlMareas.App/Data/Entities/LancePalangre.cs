namespace ControlMareas.App.Data.Entities;

public sealed class LancePalangre
{
    public string LanceId { get; set; } = string.Empty;
    public Lance Lance { get; set; } = null!;

    public string? Identificacion { get; set; }

    // Filado (Calado)
    public string? HoraInicioFilado { get; set; }
    public string? HoraFinFilado { get; set; }
    public double? LatitudInicioFiladoDecimal { get; set; }
    public double? LongitudInicioFiladoDecimal { get; set; }
    public double? LatitudFinFiladoDecimal { get; set; }
    public double? LongitudFinFiladoDecimal { get; set; }
    public int? ProfundidadInicioFiladoM { get; set; }
    public int? ProfundidadFinFiladoM { get; set; }

    // Virado
    public string? FechaVirado { get; set; }
    public string? HoraInicioVirado { get; set; }
    public string? HoraFinVirado { get; set; }
    public double? LatitudInicioViradoDecimal { get; set; }
    public double? LongitudInicioViradoDecimal { get; set; }
    public double? LatitudFinViradoDecimal { get; set; }
    public double? LongitudFinViradoDecimal { get; set; }
    public int? ProfundidadInicioViradoM { get; set; }
    public int? ProfundidadFinViradoM { get; set; }
    public int? RumboViradoGrados { get; set; }

    // Arte de pesca y aparejo
    public string? TipoPalangre { get; set; }
    public string? MaterialLineaMadre { get; set; }
    public int? LargoLineaMadreM { get; set; }
    public int? TramosLineaMadre { get; set; }
    public int? LargoBrazoladaCm { get; set; }
    public int? SeparacionBrazoladasM { get; set; }
    public int? NumeroBrazoladasPorTramo { get; set; }
    public string? TipoAnzuelo { get; set; }
    public int? LargoAnzueloMm { get; set; }
    public int? AnchoAnzueloMm { get; set; }
    public int? AnzuelosTotal { get; set; }
    public int? AnzuelosConCarnada { get; set; }
    public int? AnzuelosSinCarnada { get; set; }
    public string? Carnada { get; set; }
    public string? Observaciones { get; set; }
}
