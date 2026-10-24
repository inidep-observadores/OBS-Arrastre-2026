using System.IO;
using System.Text.Json;
using FluentAssertions;
using ControlMareas.App.Services;
using Xunit;

namespace ControlMareas.Tests.Services;

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

    [Fact]
    public async Task ReadCapturasPalangreAsync_ShouldExtractDataCorrectly_FromCP7726()
    {
        // Arrange
        var baseDir = AppContext.BaseDirectory;
        var directory = new DirectoryInfo(baseDir);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "docs", "Demo")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("No se encontró la carpeta docs/Demo");
        var cpPath = Path.Combine(directory!.FullName, "docs", "Demo", "CP7726.DBF");
        File.Exists(cpPath).Should().BeTrue();

        var service = new DbfExtractorService();

        // Act
        var capturas = await service.ReadCapturasPalangreAsync(cpPath);

        // Assert
        capturas.Should().NotBeNull();
        capturas.Count.Should().Be(16);

        var first = capturas[0];
        first.Lance.Should().Be(1);
        first.Marea.Should().Be(77);
        first.Barco.Should().Be("ARGENOVA XIV");
        first.Identific.Should().Be("LW8316");
        first.Fecha.Should().Be(new DateTime(2026, 5, 27));
        first.FechaVi.Should().Be(new DateTime(2026, 5, 28));
        first.FechaFin.Should().Be(new DateTime(2026, 5, 28));
        first.HoraInic.Should().Be(7.49);
        first.HoraFinF.Should().Be(9.12);
        first.HoraIniV.Should().Be(9.00);
        first.HoraFinal.Should().Be(21.00);
        first.AnzNTotal.Should().Be(6000);
        first.Carnada.Should().Be("CALAMAR");

        // Profundidad virado promedio (1150 + 1280) / 2 = 1215
        first.ProfIniV.Should().Be(1150);
        first.ProfFinV.Should().Be(1280);
        first.ProfInic.Should().Be(1215);
        first.ProfFinal.Should().Be(1215);

        // Especies
        first.Especies.Should().ContainKey("7218280201");
        first.Especies["7218280201"].Should().Be(500.00);
        first.Especies.Should().ContainKey("7210090401");
        first.Especies["7210090401"].Should().Be(10.00);
        first.CaptTotal.Should().Be(510.00);
    }
}

