using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using OBSArrastre2026.App.Data;
using OBSArrastre2026.App.Data.Entities;
using OBSArrastre2026.App.Services;
using OBSArrastre2026.Tests.Fixtures;

namespace OBSArrastre2026.Tests.Services;

public sealed class ProduccionServiceTests : IDisposable
{
    private readonly DatabaseFixture _fixture;
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly ProduccionService _service;

    public ProduccionServiceTests()
    {
        _fixture = new DatabaseFixture();
        _factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        _factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(_fixture.CreateContext()));
        _service = new ProduccionService(_factory);
    }

    private async Task SeedPrerequisitosAsync(string etapaId = "e1", string productoId = "p1")
    {
        using var ctx = _fixture.CreateContext();
        if (!await ctx.MareaEtapas.AnyAsync(e => e.ID == etapaId))
            ctx.MareaEtapas.Add(new MareaEtapa { ID = etapaId, FechaZarpada = DateTime.Today });
        if (!await ctx.Productos.AnyAsync(p => p.Id == productoId))
            ctx.Productos.Add(new Producto { Id = productoId, Codigo = "MRL", Descripcion = "Merluza" });
        await ctx.SaveChangesAsync();
    }

    // ── SaveRegistroProduccionAsync / GetRegistroProduccionAsync ──────────────

    [Fact]
    public async Task SaveRegistro_Nuevo_SeGuardaCorrectamente()
    {
        await SeedPrerequisitosAsync();

        var registro = new RegistroProduccion
        {
            Id = "r1",
            MareaEtapaId = "e1",
            IdProducto = "p1",
            Fecha = "2025-04-10",
            Kg = 800
        };

        await _service.SaveRegistroProduccionAsync(registro);

        var result = await _service.GetRegistroProduccionAsync("r1");
        result.Should().NotBeNull();
        result!.Kg.Should().Be(800);
    }

    [Fact]
    public async Task SaveRegistro_Existente_ActualizaLosCampos()
    {
        await SeedPrerequisitosAsync();

        var registro = new RegistroProduccion
        {
            Id = "r1",
            MareaEtapaId = "e1",
            IdProducto = "p1",
            Fecha = "2025-04-10",
            Kg = 500
        };
        await _service.SaveRegistroProduccionAsync(registro);

        registro.Kg = 1200;
        await _service.SaveRegistroProduccionAsync(registro);

        var result = await _service.GetRegistroProduccionAsync("r1");
        result!.Kg.Should().Be(1200);
    }

    [Fact]
    public async Task GetRegistroProduccion_Inexistente_DevuelveNull()
    {
        var result = await _service.GetRegistroProduccionAsync("no-existe");

        result.Should().BeNull();
    }

    // ── GetRegistrosProduccionAsync (por etapa) ───────────────────────────────

    [Fact]
    public async Task GetRegistrosProduccion_PorEtapa_DevuelveSoloLosDeEsaEtapa()
    {
        await SeedPrerequisitosAsync("e1");
        await SeedPrerequisitosAsync("e2", "p2");

        using (var ctx = _fixture.CreateContext())
        {
            ctx.RegistrosProduccion.AddRange(
                new RegistroProduccion { Id = "r1", MareaEtapaId = "e1", IdProducto = "p1", Fecha = "2025-04-01", Kg = 100 },
                new RegistroProduccion { Id = "r2", MareaEtapaId = "e2", IdProducto = "p2", Fecha = "2025-04-02", Kg = 200 });
            await ctx.SaveChangesAsync();
        }

        var result = await _service.GetRegistrosProduccionAsync("e1");

        result.Should().HaveCount(1);
        result[0].Id.Should().Be("r1");
    }

    [Fact]
    public async Task GetRegistrosProduccion_OrdenadoPorFecha()
    {
        await SeedPrerequisitosAsync();

        using (var ctx = _fixture.CreateContext())
        {
            ctx.RegistrosProduccion.AddRange(
                new RegistroProduccion { Id = "r1", MareaEtapaId = "e1", IdProducto = "p1", Fecha = "2025-04-10", Kg = 100 },
                new RegistroProduccion { Id = "r2", MareaEtapaId = "e1", IdProducto = "p1", Fecha = "2025-04-01", Kg = 200 });
            await ctx.SaveChangesAsync();
        }

        var result = await _service.GetRegistrosProduccionAsync("e1");

        result[0].Fecha.Should().Be("2025-04-01");
        result[1].Fecha.Should().Be("2025-04-10");
    }

    // ── DeleteRegistroProduccionAsync ─────────────────────────────────────────

    [Fact]
    public async Task DeleteRegistro_Existente_SeElimina()
    {
        await SeedPrerequisitosAsync();

        var registro = new RegistroProduccion
        {
            Id = "r1",
            MareaEtapaId = "e1",
            IdProducto = "p1",
            Fecha = "2025-04-10",
            Kg = 400
        };
        await _service.SaveRegistroProduccionAsync(registro);

        await _service.DeleteRegistroProduccionAsync("r1");

        var result = await _service.GetRegistroProduccionAsync("r1");
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteRegistro_Inexistente_NoLanzaExcepcion()
    {
        var act = async () => await _service.DeleteRegistroProduccionAsync("no-existe");

        await act.Should().NotThrowAsync();
    }

    // ── Factor y Especie se preservan en el update ────────────────────────────

    [Fact]
    public async Task SaveRegistro_ConFactor_SeGuardaYRecupera()
    {
        await SeedPrerequisitosAsync();

        var registro = new RegistroProduccion
        {
            Id = "r1",
            MareaEtapaId = "e1",
            IdProducto = "p1",
            Fecha = "2025-04-15",
            Kg = 600,
            Factor = 1.15
        };

        await _service.SaveRegistroProduccionAsync(registro);

        var result = await _service.GetRegistroProduccionAsync("r1");
        result!.Factor.Should().Be(1.15);
    }

    public void Dispose() => _fixture.Dispose();
}
