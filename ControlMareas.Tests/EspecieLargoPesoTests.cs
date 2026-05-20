using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ControlMareas.App.Data;
using ControlMareas.App.Data.Entities;
using ControlMareas.App.Services;
using ControlMareas.Tests.Fixtures;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ControlMareas.Tests;

public class EspecieLargoPesoTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public EspecieLargoPesoTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SeedLargoPeso_ShouldContainPintarrojaWithCorrectParameters()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        
        // Sembramos la especie Pintarroja primero
        var pintarroja = new Especie
        {
            ID = Guid.NewGuid().ToString(),
            CodigoInidep = "7106010101",
            NombreCientifico = "Schroederichthys bivius",
            NombreVulgar = "Pintarroja",
            Frecuente = 1
        };
        context.Especies.Add(pintarroja);
        await context.SaveChangesAsync();

        var dbContextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        dbContextFactory.CreateDbContextAsync().Returns(_ => Task.FromResult(_fixture.CreateContext()));
        
        var initializer = new DatabaseInitializer(dbContextFactory);

        // Act
        await initializer.SeedCatalogsAsync();

        // Assert
        using var assertContext = _fixture.CreateContext();
        var seeds = await assertContext.EspeciesLargoPeso
            .Include(lp => lp.Especie)
            .Where(lp => lp.Especie.CodigoInidep == "7106010101")
            .ToListAsync();

        seeds.Should().HaveCount(2);

        var macho = seeds.FirstOrDefault(s => s.Sexo == 1);
        macho.Should().NotBeNull();
        macho!.ParamA.Should().Be(0.00180);
        macho.ParamB.Should().Be(3.1200);
        macho.TipoMedida.Should().Be("LT");

        var hembra = seeds.FirstOrDefault(s => s.Sexo == 2);
        hembra.Should().NotBeNull();
        hembra!.ParamA.Should().Be(0.00130);
        hembra.ParamB.Should().Be(3.2100);
        hembra.TipoMedida.Should().Be("LT");

        // Verificamos coherencia del peso (en gramos)
        // Peso = a * (Largo ^ b)
        double largoCm = 50.0;
        
        double pesoMacho = macho.ParamA * Math.Pow(largoCm, macho.ParamB);
        double pesoHembra = hembra.ParamA * Math.Pow(largoCm, hembra.ParamB);

        // Para Pintarroja macho de 50 cm el peso debería ser ~359.8 g
        pesoMacho.Should().BeApproximately(359.80, 0.1);
        // Para Pintarroja hembra de 50 cm el peso debería ser ~369.5 g
        pesoHembra.Should().BeApproximately(369.52, 0.1);

        // Los pesos calculados para un tiburón de 50cm deben ser coherentes (entre 200g y 600g)
        pesoMacho.Should().BeInRange(200.0, 600.0);
        pesoHembra.Should().BeInRange(200.0, 600.0);
    }
}
