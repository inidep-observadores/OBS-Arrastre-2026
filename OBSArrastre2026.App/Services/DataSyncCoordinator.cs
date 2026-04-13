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

        var baseDir = AppDomain.CurrentDomain.BaseDirectory;

        // Rutas locales de la aplicación
        _rawPath = Path.Combine(baseDir, "Data", "Import", "Raw");
        _stagingPath = Path.Combine(baseDir, "Data", "Import", "Staging");

        // Asegurar directorios locales
        Directory.CreateDirectory(_rawPath);
        Directory.CreateDirectory(_stagingPath);
    }

    public async Task SyncAllAsync()
    {
        // Optimizamos: Pasamos el chequeo de tabla vacía para decidir si importar
        await SyncItemAsync(
            "buques.DBF", 
            "buques.json", 
            _jsonImporter.IsBuquesEmptyAsync,
            _dbfExtractor.ExtractBuquesAsync, 
            _jsonImporter.ImportBuquesAsync);

        await SyncItemAsync(
            "especie1.DBF", 
            "especies.json", 
            _jsonImporter.IsEspeciesEmptyAsync,
            _dbfExtractor.ExtractEspeciesAsync, 
            _jsonImporter.ImportEspeciesAsync);
    }

    private async Task SyncItemAsync(
        string dbfFilename, 
        string jsonFilename, 
        Func<Task<bool>> isTableEmpty,
        Func<string, string, Task> extractor, 
        Func<string, Task> importer)
    {
        var localDbfFile = Path.Combine(_rawPath, dbfFilename);
        var jsonFile = Path.Combine(_stagingPath, jsonFilename);

        // Fallback: Si no está en Data/Import/Raw, buscar en la carpeta source_data de la raíz (solo para desarrollo)
        string dbfFile = localDbfFile;
        if (!File.Exists(dbfFile))
        {
            var projectRoot = FindProjectRoot(AppDomain.CurrentDomain.BaseDirectory);
            if (projectRoot != null)
            {
                var sourceDataPath = Path.Combine(projectRoot, "source_data", dbfFilename);
                if (File.Exists(sourceDataPath))
                {
                    dbfFile = sourceDataPath;
                }
            }
        }

        if (!File.Exists(dbfFile)) return;

        // 1. Decidir si regenerar el JSON intermedio (solo si el DBF es más nuevo)
        bool dbfChanged = !File.Exists(jsonFile) || File.GetLastWriteTime(dbfFile) > File.GetLastWriteTime(jsonFile);

        if (dbfChanged)
        {
            await extractor(dbfFile, jsonFile);
        }

        // 2. Decidir si realizar la importación a la base de datos
        // Importamos solo si:
        // - El DBF cambió (tenemos nuevos datos en el JSON recién extraído)
        // - O la tabla en la base de datos está vacía (carga inicial)
        if (dbfChanged || await isTableEmpty())
        {
            await importer(jsonFile);
        }
    }

    private string? FindProjectRoot(string startPath)
    {
        var directory = new DirectoryInfo(startPath);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "OBSArrastre2026.sln")))
        {
            directory = directory.Parent;
        }
        return directory?.FullName;
    }
}
