namespace ControlMareas.App.Configuration;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string FolderName { get; init; } = "ControlMareas";

    public string FileName { get; init; } = "control-mareas.db";
}
