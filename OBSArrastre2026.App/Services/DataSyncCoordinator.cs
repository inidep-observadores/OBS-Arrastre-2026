using System.IO;

namespace OBSArrastre2026.App.Services;

public sealed class DataSyncCoordinator : IDataSyncCoordinator
{
    private readonly IDbfExtractorService _dbfExtractor;
    private readonly IJsonImportService _jsonImporter;
    private readonly string _rawPath;
    private readonly string _stagingPath;

    public DataSyncCoordinator(IDbfExtractorService dbfExtractor, IJsonImportService jsonImporter)
    {
        _dbfExtractor = dbfExtractor;
        _jsonImporter = jsonImporter;

        // Rutas relativas al directorio de ejecución o raíz del proyecto
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        
        // En desarrollo, buscamos subir niveles hasta la raíz si es necesario, 
        // pero para despliegue asumiremos una carpeta 'Data' cercana al ejecutable.
        _rawPath = Path.Combine(baseDir, "Data", "Import", "Raw");
        _stagingPath = Path.Combine(baseDir, "Data", "Import", "Staging");

        // Asegurar directorios
        Directory.CreateDirectory(_rawPath);
        Directory.CreateDirectory(_stagingPath);
    }

    public async Task SyncAllAsync()
    {
        await SyncItemAsync("buques.DBF", "buques.json", _dbfExtractor.ExtractBuquesAsync, _jsonImporter.ImportBuquesAsync);
        await SyncItemAsync("especie1.DBF", "especies.json", _dbfExtractor.ExtractEspeciesAsync, _jsonImporter.ImportEspeciesAsync);
    }

    private async Task SyncItemAsync(
        string dbfFilename, 
        string jsonFilename, 
        Func<string, string, Task> extractor, 
        Func<string, Task> importer)
    {
        var dbfFile = Path.Combine(_rawPath, dbfFilename);
        var jsonFile = Path.Combine(_stagingPath, jsonFilename);

        if (!File.Exists(dbfFile)) return;

        // Decidir si regenerar JSON
        bool shouldExtract = !File.Exists(jsonFile) || File.GetLastWriteTime(dbfFile) > File.GetLastWriteTime(jsonFile);

        if (shouldExtract)
        {
            await extractor(dbfFile, jsonFile);
        }

        // Importar a la base de datos
        await importer(jsonFile);
    }
}
