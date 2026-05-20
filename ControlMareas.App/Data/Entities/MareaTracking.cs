using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlMareas.App.Data.Entities;

/// <summary>
/// Representa un punto de seguimiento satelital (VMS) subordinado a una marea.
/// </summary>
public sealed class MareaTracking
{
    [Key]
    public string ID { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string MareaID { get; set; } = string.Empty;

    [ForeignKey(nameof(MareaID))]
    public Marea? Marea { get; set; }

    /// <summary>
    /// Fecha y hora del punto de seguimiento (se almacena tal cual viene del origen, asumiendo hora local).
    /// </summary>
    public DateTime FechaHora { get; set; }

    public double Latitud { get; set; }
    public double Longitud { get; set; }
    public double Rumbo { get; set; }
    public double Velocidad { get; set; }

    /// <summary>
    /// Matrícula del buque informada por el sistema de seguimiento.
    /// </summary>
    [MaxLength(20)]
    public string? Matricula { get; set; }
}
