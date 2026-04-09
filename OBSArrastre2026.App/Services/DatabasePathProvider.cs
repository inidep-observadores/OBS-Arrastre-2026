using System.IO;
using OBSArrastre2026.App.Configuration;

namespace OBSArrastre2026.App.Services;

public interface IDatabasePathProvider
{
    string GetDatabasePath();

    string GetConnectionString();
}

public sealed class DatabasePathProvider(DatabaseOptions options) : IDatabasePathProvider
{
    public string GetDatabasePath()
    {
        var baseFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            options.FolderName);

        Directory.CreateDirectory(baseFolder);

        return Path.Combine(baseFolder, options.FileName);
    }

    public string GetConnectionString() => $"Data Source={GetDatabasePath()}";
}
