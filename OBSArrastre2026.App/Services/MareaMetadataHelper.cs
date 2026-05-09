using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace OBSArrastre2026.App.Services;

public class MareaMetadata
{
    [JsonPropertyName("import_folder")]
    public string? ImportFolder { get; set; }

    [JsonPropertyName("import_errors")]
    public List<string> ImportErrors { get; set; } = new();
}

public static class MareaMetadataHelper
{
    private static readonly JsonSerializerOptions _options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

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

    public static string SetImportFolder(string? existingJson, string folder)
    {
        var meta = GetMetadata(existingJson);
        meta.ImportFolder = folder;
        return JsonSerializer.Serialize(meta, _options);
    }
    
    public static string? GetImportFolder(string? json)
    {
        return GetMetadata(json).ImportFolder;
    }
}
