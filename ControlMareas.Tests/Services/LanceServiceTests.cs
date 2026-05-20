using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ControlMareas.App.Data;
using ControlMareas.App.Data.Entities;
using ControlMareas.App.Services;
using ControlMareas.Tests.Fixtures;

namespace ControlMareas.Tests.Services;

public sealed class LanceServiceTests : IDisposable
{
    private readonly DatabaseFixture _fixture;
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly LanceService _service;

    public LanceServiceTests()
    {
        _fixture = new DatabaseFixture();
        _factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        _factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(_fixture.CreateContext()));
        _service = new LanceService(_factory);
    }

    private async Task SeedLanceAsync(string lanceId, string etapaId, int nroLance, string fecha)
    {
        using var ctx = _fixture.CreateContext();
        if (!await ctx.MareaEtapas.AnyAsync(e => e.ID == etapaId))
        {
            ctx.MareaEtapas.Add(new MareaEtapa { ID = etapaId, FechaZarpada = DateTime.Today });
        }
        ctx.Lances.Add(new Lance
        {
            Id = lanceId,
            MareaEtapaId = etapaId,
            NroLance = nroLance,
            Fecha = fecha
        });
        await ctx.SaveChangesAsync();
    }

    // ── GetLancesAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetLancesAsync_PorEtapa_DevuelveSoloLosDeEsaEtapa()
    {
        await SeedLanceAsync("l1", "e1", 1, "2025-03-01");
        await SeedLanceAsync("l2", "e2", 2, "2025-03-02");

        var result = await _service.GetLancesAsync(mareaEtapaId: "e1");

        result.Should().HaveCount(1);
        result[0].Id.Should().Be("l1");
    }

    [Fact]
    public async Task GetLancesAsync_SinFiltros_DevuelveOrdenadoPorNroLance()
    {
        await SeedLanceAsync("lB", "e1", 3, "2025-03-03");
        await SeedLanceAsync("lA", "e1", 1, "2025-03-01");
        await SeedLanceAsync("lC", "e1", 2, "2025-03-02");

        var result = await _service.GetLancesAsync(mareaEtapaId: "e1");

        result.Select(l => l.NroLance).Should().ContainInOrder(1, 2, 3);
    }

    [Fact]
    public async Task GetLancesAsync_FiltroFechaDesde_ExcluyeAnteriores()
    {
        await SeedLanceAsync("l1", "e1", 1, "2025-01-01");
        await SeedLanceAsync("l2", "e1", 2, "2025-06-01");

        var result = await _service.GetLancesAsync(fechaDesde: new DateTime(2025, 3, 1));

        result.Should().HaveCount(1);
        result[0].Id.Should().Be("l2");
    }

    [Fact]
    public async Task GetLancesAsync_FiltroFechaHasta_ExcluidePosteriores()
    {
        await SeedLanceAsync("l1", "e1", 1, "2025-01-01");
        await SeedLanceAsync("l2", "e1", 2, "2025-12-01");

        var result = await _service.GetLancesAsync(fechaHasta: new DateTime(2025, 6, 30));

        result.Should().HaveCount(1);
        result[0].Id.Should().Be("l1");
    }

    [Fact]
    public async Task GetLancesAsync_FiltroPorNroLance_Exacto()
    {
        await SeedLanceAsync("l1", "e1", 5, "2025-03-01");
        await SeedLanceAsync("l2", "e1", 10, "2025-03-02");

        var result = await _service.GetLancesAsync(nroLance: 5);

        result.Should().HaveCount(1);
        result[0].NroLance.Should().Be(5);
    }

    // ── GetLanceAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetLanceAsync_Existente_DevuelveElLance()
    {
        await SeedLanceAsync("l1", "e1", 1, "2025-03-01");

        var result = await _service.GetLanceAsync("l1");

        result.Should().NotBeNull();
        result!.NroLance.Should().Be(1);
    }

    [Fact]
    public async Task GetLanceAsync_Inexistente_DevuelveNull()
    {
        var result = await _service.GetLanceAsync("no-existe");

        result.Should().BeNull();
    }

    // ── SaveLanceAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveLanceAsync_Nuevo_SeGuardaCorrectamente()
    {
        using (var ctx = _fixture.CreateContext())
        {
            ctx.MareaEtapas.Add(new MareaEtapa { ID = "e1", FechaZarpada = DateTime.Today });
            await ctx.SaveChangesAsync();
        }

        var lance = new Lance { Id = "l1", MareaEtapaId = "e1", NroLance = 7, Fecha = "2025-05-10" };
        await _service.SaveLanceAsync(lance);

        var result = await _service.GetLanceAsync("l1");
        result.Should().NotBeNull();
        result!.NroLance.Should().Be(7);
    }

    // ── DeleteLanceAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteLanceAsync_Existente_SeElimina()
    {
        await SeedLanceAsync("l1", "e1", 1, "2025-03-01");

        await _service.DeleteLanceAsync("l1");

        var result = await _service.GetLanceAsync("l1");
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteLanceAsync_Inexistente_NoLanzaExcepcion()
    {
        var act = async () => await _service.DeleteLanceAsync("no-existe");

        await act.Should().NotThrowAsync();
    }

    // ── GetEspeciesAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetEspeciesAsync_DevuelveLasEspeciesRegistradas()
    {
        using (var ctx = _fixture.CreateContext())
        {
            ctx.Especies.AddRange(
                new Especie { ID = "e1", CodigoInidep = "001", NombreVulgar = "Merluza" },
                new Especie { ID = "e2", CodigoInidep = "002", NombreVulgar = "Langostino" });
            await ctx.SaveChangesAsync();
        }

        var result = await _service.GetEspeciesAsync();

        result.Should().HaveCount(2);
    }

    public void Dispose() => _fixture.Dispose();
}
