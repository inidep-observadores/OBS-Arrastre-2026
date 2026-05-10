using System.IO;
using System.Text.Json;
using FluentAssertions;
using OBSArrastre2026.App.Services;
using Xunit;

namespace OBSArrastre2026.Tests.Services;

public class DbfExtractorServiceTests
{
    private readonly string _dbfPath;
    private readonly string _outputPath;

    public DbfExtractorServiceTests()
    {
        // Ruta al archivo real para la prueba de integración de codificación
        var baseDir = AppContext.BaseDirectory;
        // Buscamos hacia arriba hasta encontrar la carpeta source_data
        var directory = new DirectoryInfo(baseDir);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "source_data")))
        {
            directory = directory.Parent;
        }
        
        if (directory == null) throw new DirectoryNotFoundException("No se encontró la carpeta source_data");
        
        _dbfPath = Path.Combine(directory.FullName, "source_data", "especie1.DBF");
        _outputPath = Path.Combine(baseDir, "especies_test.json");
    }

    [Fact]
    public async Task ExtractEspeciesAsync_ShouldDetectCorrectEncoding_UsingHeuristic()
    {
        // Arrange
        var service = new DbfExtractorService();

        // Act
        await service.ExtractEspeciesAsync(_dbfPath, _outputPath);

        // Assert
        File.Exists(_outputPath).Should().BeTrue();
        var json = await File.ReadAllTextAsync(_outputPath);
        var data = JsonDocument.Parse(json);
        
        // Buscamos la Merluza común (7210040101)
        bool foundCorrectly = false;
        foreach (var item in data.RootElement.EnumerateArray())
        {
            var codeProp = item.GetProperty("CodigoInidep");
            var code = codeProp.ValueKind == JsonValueKind.Number 
                ? codeProp.GetRawText() 
                : codeProp.GetString();

            if (code == "7210040101")
            {
                var name = item.GetProperty("NombreVulgar").GetString();
                name.Should().Contain("común");
                foundCorrectly = true;
                break;
            }
        }

        
        foundCorrectly.Should().BeTrue("No se encontró el registro de Merluza común o el nombre es incorrecto");
    }
}
