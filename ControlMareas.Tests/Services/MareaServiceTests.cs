using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ControlMareas.App.Data;
using ControlMareas.App.Data.Entities;
using ControlMareas.App.Services;
using ControlMareas.Tests.Fixtures;

namespace ControlMareas.Tests.Services;

public sealed class MareaServiceTests : IDisposable
{
    private readonly DatabaseFixture _fixture;
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly MareaService _service;

    public MareaServiceTests()
    {
        _fixture = new DatabaseFixture();
        _factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        _factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(_fixture.CreateContext()));
        _service = new MareaService(_factory);
    }

    // ── SaveMareaAsync / GetMareaAsync ────────────────────────────────────────

    [Fact]
    public async Task SaveMareaAsync_Nueva_SeGuardaCorrectamente()
    {
        var marea = new Marea
        {
            ID = "m1",
            AnioInidep = 2025,
            NumeroInidep = 3,
            FechaInicio = new DateTime(2025, 3, 1)
        };

        await _service.SaveMareaAsync(marea);

        var result = await _service.GetMareaAsync("m1");
        result.Should().NotBeNull();
        result!.NumeroInidep.Should().Be(3);
        result.AnioInidep.Should().Be(2025);
    }

    [Fact]
    public async Task SaveMareaAsync_Existente_Actualiza()
    {
        var marea = new Marea { ID = "m1", AnioInidep = 2025, NumeroInidep = 1, FechaInicio = DateTime.Today };
        await _service.SaveMareaAsync(marea);

        marea.Comentarios = "Actualizado";
        await _service.SaveMareaAsync(marea);

        var result = await _service.GetMareaAsync("m1");
        result!.Comentarios.Should().Be("Actualizado");
    }

    [Fact]
    public async Task GetMareaAsync_Inexistente_DevuelveNull()
    {
        var result = await _service.GetMareaAsync("no-existe");

        result.Should().BeNull();
    }

    // ── FindMareaAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task FindMareaAsync_ExistenteConNumeroYAnio_LaEncuentra()
    {
        var marea = new Marea { ID = "m1", AnioInidep = 2025, NumeroInidep = 7, FechaInicio = DateTime.Today };
        await _service.SaveMareaAsync(marea);

        var result = await _service.FindMareaAsync(7, 2025);

        result.Should().NotBeNull();
        result!.ID.Should().Be("m1");
    }

    [Fact]
    public async Task FindMareaAsync_NumeroInexistente_DevuelveNull()
    {
        var result = await _service.FindMareaAsync(99, 2025);

        result.Should().BeNull();
    }

    // ── DeleteMareaAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteMareaAsync_Existente_SeElimina()
    {
        var marea = new Marea { ID = "m1", AnioInidep = 2025, NumeroInidep = 1, FechaInicio = DateTime.Today };
        await _service.SaveMareaAsync(marea);

        await _service.DeleteMareaAsync("m1");

        var result = await _service.GetMareaAsync("m1");
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteMareaAsync_Inexistente_NoLanzaExcepcion()
    {
        var act = async () => await _service.DeleteMareaAsync("no-existe");

        await act.Should().NotThrowAsync();
    }

    // ── GetAniosExistentesAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetAniosExistentesAsync_DevolverDistintos_OrdenadosDesc()
    {
        using (var ctx = _fixture.CreateContext())
        {
            ctx.Mareas.AddRange(
                new Marea { ID = "m1", AnioInidep = 2023, NumeroInidep = 1, FechaInicio = DateTime.Today },
                new Marea { ID = "m2", AnioInidep = 2025, NumeroInidep = 1, FechaInicio = DateTime.Today },
                new Marea { ID = "m3", AnioInidep = 2023, NumeroInidep = 2, FechaInicio = DateTime.Today });
            await ctx.SaveChangesAsync();
        }

        var result = await _service.GetAniosExistentesAsync();

        result.Should().ContainInOrder(2025, 2023);
        result.Should().HaveCount(2);
    }

    // ── HasExistingDataAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task HasExistingDataAsync_SinEtapas_RetornaFalse()
    {
        var marea = new Marea { ID = "m1", AnioInidep = 2025, NumeroInidep = 1, FechaInicio = DateTime.Today };
        await _service.SaveMareaAsync(marea);

        var result = await _service.HasExistingDataAsync("m1");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasExistingDataAsync_ConLances_RetornaTrue()
    {
        using (var ctx = _fixture.CreateContext())
        {
            var marea = new Marea { ID = "m1", AnioInidep = 2025, NumeroInidep = 1, FechaInicio = DateTime.Today };
            var etapa = new MareaEtapa { ID = "e1", MareaID = "m1", FechaZarpada = DateTime.Today };
            var lance = new Lance { Id = "l1", MareaEtapaId = "e1", NroLance = 1, Fecha = "2025-01-01" };

            ctx.Mareas.Add(marea);
            ctx.MareaEtapas.Add(etapa);
            ctx.Lances.Add(lance);
            await ctx.SaveChangesAsync();
        }

        var result = await _service.HasExistingDataAsync("m1");

        result.Should().BeTrue();
    }

    // ── GetMareasAsync (filtros) ──────────────────────────────────────────────

    [Fact]
    public async Task GetMareasAsync_FiltradoPorAnio_DevuelveSoloEseAnio()
    {
        using (var ctx = _fixture.CreateContext())
        {
            ctx.Mareas.AddRange(
                new Marea { ID = "m1", AnioInidep = 2024, NumeroInidep = 1, FechaInicio = DateTime.Today },
                new Marea { ID = "m2", AnioInidep = 2025, NumeroInidep = 1, FechaInicio = DateTime.Today });
            await ctx.SaveChangesAsync();
        }

        var result = await _service.GetMareasAsync(anio: 2024);

        result.Should().HaveCount(1);
        result[0].AnioInidep.Should().Be(2024);
    }

    [Fact]
    public async Task GetMareasAsync_BusquedaTextual_EncuentraPorComentarios()
    {
        using (var ctx = _fixture.CreateContext())
        {
            ctx.Mareas.AddRange(
                new Marea { ID = "m1", AnioInidep = 2025, NumeroInidep = 1, FechaInicio = DateTime.Today, Comentarios = "prueba especial" },
                new Marea { ID = "m2", AnioInidep = 2025, NumeroInidep = 2, FechaInicio = DateTime.Today, Comentarios = "sin coincidencia" });
            await ctx.SaveChangesAsync();
        }

        var result = await _service.GetMareasAsync(busquedaTextual: "especial");

        result.Should().HaveCount(1);
        result[0].ID.Should().Be("m1");
    }

    public void Dispose() => _fixture.Dispose();
}
