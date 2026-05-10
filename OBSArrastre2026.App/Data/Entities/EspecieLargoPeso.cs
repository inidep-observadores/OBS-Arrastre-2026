using System;

namespace OBSArrastre2026.App.Data.Entities;

/// <summary>
/// Representa los parámetros biométricos para la relación Largo-Peso (P = a * L^b)
/// </summary>
public sealed class EspecieLargoPeso
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EspecieId { get; set; } = string.Empty;
    public Especie Especie { get; set; } = null!;

    /// <summary>
    /// Sexo (0: Ambos/Unificado, 1: Macho, 2: Hembra, 3: Indeterminado)
    /// </summary>
    public int Sexo { get; set; }

    public double ParamA { get; set; }
    public double ParamB { get; set; }

    /// <summary>
    /// Tipo de medida de longitud (ej: LT, LPA, LM)
    /// </summary>
    public string TipoMedida { get; set; } = "LT";

    public string? Observaciones { get; set; }
}
