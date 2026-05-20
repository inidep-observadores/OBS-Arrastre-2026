using System.IO;
using System.Collections.Generic;

namespace ControlMareas.App.Services;

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

        // Las carpetas de TRABAJO (donde escribimos) deben estar en LocalAppData para evitar errores de permisos en Program Files
        var userAppData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ControlDeMareas", // Coincide con el nombre de la carpeta de la base de datos
            "Import");

        _rawPath = Path.Combine(userAppData, "Raw");
        _stagingPath = Path.Combine(userAppData, "Staging");

        // Asegurar directorios de trabajo
        Directory.CreateDirectory(_rawPath);
        Directory.CreateDirectory(_stagingPath);
    }

    public async Task SyncAllAsync()
    {
        Console.WriteLine("Iniciando sincronización de datos maestros...");
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

        await SyncItemAsync(
            "ESPECIEvie.DBF", 
            "especies_viejas.json", 
            _jsonImporter.IsEspeciesViejasEmptyAsync,
            _dbfExtractor.ExtractEspeciesAsync, 
            _jsonImporter.ImportEspeciesViejasAsync);
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

        var searchPaths = new List<string>
        {
            Path.Combine(_rawPath, dbfFilename),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "source_data", dbfFilename),
            Path.Combine(FindProjectRoot(AppDomain.CurrentDomain.BaseDirectory) ?? "", "source_data", dbfFilename),
            Path.Combine(FindProjectRoot(AppDomain.CurrentDomain.BaseDirectory) ?? "", "ControlMareas.App", "Data", "Import", "Raw", dbfFilename),
            Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.FullName ?? "", "source_data", dbfFilename),
            Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.Parent?.FullName ?? "", "source_data", dbfFilename)
        };

        string? dbfFile = null;
        DateTime latestDate = DateTime.MinValue;

        foreach (var path in searchPaths)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                var currentInfo = new FileInfo(path);
                if (currentInfo.LastWriteTime > latestDate)
                {
                    dbfFile = path;
                    latestDate = currentInfo.LastWriteTime;
                }
            }
        }

        if (dbfFile == null)
        {
            Console.WriteLine($"Sincronización ERROR: No se encontró el archivo origen para {dbfFilename}.");
            System.Diagnostics.Debug.WriteLine($"Sincronización error: No se encontró el archivo origen para {dbfFilename}. Buscado en {dbfFile}");
            return;
        }

        Console.WriteLine($"Sincronizando {dbfFilename} desde {dbfFile}...");

        // 1. Decidir si regenerar el JSON intermedio (solo si el DBF es más nuevo)
        bool dbfChanged = !File.Exists(jsonFile) || File.GetLastWriteTime(dbfFile) > File.GetLastWriteTime(jsonFile);

        try 
        {
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
                Console.WriteLine($"Importando {jsonFile} a la base de datos...");
                await importer(jsonFile);
            }
            else
            {
                Console.WriteLine($"{dbfFilename} ya está actualizado.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Sincronización ERROR en {dbfFilename}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Sincronización error: {ex}");
        }
    }

    private string? FindProjectRoot(string startPath)
    {
        var directory = new DirectoryInfo(startPath);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "ControlMareas.sln")))
        {
            directory = directory.Parent;
        }
        return directory?.FullName;
    }
}
