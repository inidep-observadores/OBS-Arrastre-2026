using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.Services;

public class MareaMetadata
{
    [JsonPropertyName("import_folder")]
    public string? ImportFolder { get; set; }

    [JsonPropertyName("import_errors")]
    public List<string> ImportErrors { get; set; } = new();

    [JsonPropertyName("encoding_codepage")]
    public int? EncodingCodePage { get; set; }

    [JsonPropertyName("buque_codigo")]
    public int? BuqueCodigo { get; set; }

    [JsonPropertyName("observador_nombre")]
    public string? ObservadorNombre { get; set; }

    [JsonPropertyName("observador_apellido")]
    public string? ObservadorApellido { get; set; }

    [JsonPropertyName("observador_codigo")]
    public int? ObservadorCodigo { get; set; }
}

public static class MareaMetadataHelper
{
    private static readonly JsonSerializerOptions _options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public static MareaMetadata GetMetadata(Marea marea) => GetMetadata(marea.Metadata);

    public static MareaMetadata GetMetadata(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new MareaMetadata();
        try
        {
            return JsonSerializer.Deserialize<MareaMetadata>(json, _options) ?? new MareaMetadata();
        }
        catch
        {
            return new MareaMetadata();
        }
    }

    public static string SetMetadata(MareaMetadata meta)
    {
        return JsonSerializer.Serialize(meta, _options);
    }

    public static string SetImportFolder(string? existingJson, string folder)
    {
        var meta = GetMetadata(existingJson);
        meta.ImportFolder = folder;
        return SetMetadata(meta);
    }
    
    public static string? GetImportFolder(string? json)
    {
        return GetMetadata(json).ImportFolder;
    }

    public static string UpdateFromLegacyFields(string? existingJson, int? buqueCodigo, string? obsNombre, string? obsApellido, int? obsCodigo)
    {
        var meta = GetMetadata(existingJson);
        meta.BuqueCodigo = buqueCodigo;
        meta.ObservadorNombre = obsNombre;
        meta.ObservadorApellido = obsApellido;
        meta.ObservadorCodigo = obsCodigo;
        return SetMetadata(meta);
    }
}
