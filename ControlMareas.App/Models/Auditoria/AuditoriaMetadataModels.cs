using System.Text.Json.Serialization;

namespace ControlMareas.App.Models.Auditoria;

/// <summary>
/// Metadatos comunes para todos los registros de auditoría.
/// </summary>
public record LoteMetadata
{
    [JsonPropertyName("versionValidador")]
    public string? VersionValidador { get; init; }

    [JsonPropertyName("duracionMs")]
    public long? DuracionMs { get; init; }

    [JsonPropertyName("parametros")]
    public Dictionary<string, object>? Parametros { get; init; }
}

/// <summary>
/// Metadatos detallados para un hallazgo de auditoría.
/// </summary>
public record RegistroMetadata
{
    [JsonPropertyName("campo")]
    public string? Campo { get; init; }

    [JsonPropertyName("valorEncontrado")]
    public object? ValorEncontrado { get; init; }

    [JsonPropertyName("valorEsperado")]
    public object? ValorEsperado { get; init; }

    [JsonPropertyName("regla")]
    public string? Regla { get; init; }

    [JsonPropertyName("codigoError")]
    public string? CodigoError { get; init; }

    [JsonPropertyName("contexto")]
    public Dictionary<string, object>? Contexto { get; init; }
}
