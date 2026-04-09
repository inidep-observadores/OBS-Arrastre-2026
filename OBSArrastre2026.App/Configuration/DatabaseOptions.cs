namespace OBSArrastre2026.App.Configuration;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string FolderName { get; init; } = "OBSArrastre2026";

    public string FileName { get; init; } = "obs-arrastre-2026.db";
}
