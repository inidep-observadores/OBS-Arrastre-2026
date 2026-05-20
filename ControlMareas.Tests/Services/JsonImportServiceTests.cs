using System.IO;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ControlMareas.App.Data;
using ControlMareas.App.Data.Entities;
using ControlMareas.App.Services;
using ControlMareas.Tests.Fixtures;

namespace ControlMareas.Tests.Services;

// Eliminamos IClassFixture para asegurar que cada prueba tenga una base de datos 
// SQLite In-Memory totalmente aislada y limpia.
public class JsonImportServiceTests : IDisposable
{
    private readonly DatabaseFixture _fixture;
    private readonly IDbContextFactory<AppDbContext> _factory;

    public JsonImportServiceTests()
    {
        _fixture = new DatabaseFixture();
        _factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        
        // Cada llamada devolverá un contexto conectado a la base de datos aislada de esta prueba
        _factory.CreateDbContextAsync().Returns(_ => Task.FromResult(_fixture.CreateContext()));
    }

    [Fact]
    public async Task ImportEspeciesAsync_ShouldInsertNewEspecies()
    {
        // Arrange
        var service = new JsonImportService(_factory);
        var jsonPath = Path.GetTempFileName();
        var data = new[]
        {
            new { CodigoInidep = "101", NombreVulgar = "Pez 1" },
            new { CodigoInidep = "102", NombreVulgar = "Pez 2" }
        };
        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(data));

        // Act
        await service.ImportEspeciesAsync(jsonPath);

        // Assert
        using var context = await _factory.CreateDbContextAsync();
        var especies = await context.Especies.ToListAsync();
        especies.Should().HaveCount(2);
        especies.Should().Contain(e => e.CodigoInidep == "101" && e.NombreVulgar == "Pez 1");
    }

    [Fact]
    public async Task ImportEspeciesAsync_ShouldOverwriteExistingWithSameCode()
    {
        // Arrange
        var service = new JsonImportService(_factory);
        var jsonPath = Path.GetTempFileName();
        
        // 1. Insertamos un registro inicial
        using (var context = await _factory.CreateDbContextAsync())
        {
            context.Especies.Add(new Especie { CodigoInidep = "500", NombreVulgar = "Original" });
            await context.SaveChangesAsync();
        }

        // 2. Preparamos JSON con el mismo código pero distinto nombre
        var data = new[] { new { CodigoInidep = "500", NombreVulgar = "Actualizado" } };
        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(data));

        // Act
        await service.ImportEspeciesAsync(jsonPath);

        // Assert
        using (var context = await _factory.CreateDbContextAsync())
        {
            var species = await context.Especies.FirstAsync(e => e.CodigoInidep == "500");
            species.NombreVulgar.Should().Be("Actualizado");
        }
    }

    [Fact]
    public async Task IsEspeciesEmptyAsync_ShouldReflectStateCorrectly()
    {
        // Arrange
        var service = new JsonImportService(_factory);

        // Act & Assert
        (await service.IsEspeciesEmptyAsync()).Should().BeTrue();

        using (var context = await _factory.CreateDbContextAsync())
        {
            context.Especies.Add(new Especie { CodigoInidep = "999", NombreVulgar = "Test" });
            await context.SaveChangesAsync();
        }

        (await service.IsEspeciesEmptyAsync()).Should().BeFalse();
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }
}
