using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ControlMareas.App.Data;
using ControlMareas.App.Data.Entities;
using ControlMareas.App.Services;
using ControlMareas.Tests.Fixtures;

namespace ControlMareas.Tests.Services;

public sealed class BuqueServiceTests : IDisposable
{
    private readonly DatabaseFixture _fixture;
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly BuqueService _service;

    public BuqueServiceTests()
    {
        _fixture = new DatabaseFixture();
        _factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        _factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(_fixture.CreateContext()));
        _service = new BuqueService(_factory);
    }

    [Fact]
    public async Task GetBuquesAsync_SinFiltro_DevuelveTodos()
    {
        using (var ctx = _fixture.CreateContext())
        {
            ctx.Buques.AddRange(
                new Buque { Id = "b1", Nombre = "Atlantis", Matricula = 100, IdRadial = 1 },
                new Buque { Id = "b2", Nombre = "Zeus", Matricula = 200, IdRadial = 2 });
            await ctx.SaveChangesAsync();
        }

        var result = await _service.GetBuquesAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetBuquesAsync_OrdenadoPorNombre()
    {
        using (var ctx = _fixture.CreateContext())
        {
            ctx.Buques.AddRange(
                new Buque { Id = "b1", Nombre = "Zeus", Matricula = 100, IdRadial = 1 },
                new Buque { Id = "b2", Nombre = "Atlantis", Matricula = 200, IdRadial = 2 });
            await ctx.SaveChangesAsync();
        }

        var result = await _service.GetBuquesAsync();

        result[0].Nombre.Should().Be("Atlantis");
        result[1].Nombre.Should().Be("Zeus");
    }

    [Fact]
    public async Task GetBuquesAsync_OnlyWithMareas_FiltraSoloBuquesConMarea()
    {
        using (var ctx = _fixture.CreateContext())
        {
            ctx.Buques.AddRange(
                new Buque { Id = "b1", Nombre = "ConMarea", Matricula = 100, IdRadial = 1 },
                new Buque { Id = "b2", Nombre = "SinMarea", Matricula = 200, IdRadial = 2 });

            ctx.Mareas.Add(new Marea
            {
                ID = "m1",
                AnioInidep = 2025,
                NumeroInidep = 1,
                BuqueID = "b1",
                FechaInicio = DateTime.Today
            });

            await ctx.SaveChangesAsync();
        }

        var result = await _service.GetBuquesAsync(onlyWithMareas: true);

        result.Should().HaveCount(1);
        result[0].Nombre.Should().Be("ConMarea");
    }

    [Fact]
    public async Task GetBuquesAsync_SinBuques_DevuelveListaVacia()
    {
        var result = await _service.GetBuquesAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBuquesAsync_MapeoCompleto_EsCorrecto()
    {
        using (var ctx = _fixture.CreateContext())
        {
            ctx.Buques.Add(new Buque
            {
                Id = "b1",
                Nombre = "Antártida I",
                Matricula = 1234,
                IdRadial = 42,
                IMO = 9876543,
                MMSI = 701234567
            });
            await ctx.SaveChangesAsync();
        }

        var result = await _service.GetBuquesAsync();

        var vm = result[0];
        vm.ID.Should().Be("b1");
        vm.Nombre.Should().Be("Antártida I");
        vm.Matricula.Should().Be(1234);
        vm.IdRadial.Should().Be(42);
        vm.IMO.Should().Be(9876543);
        vm.MMSI.Should().Be(701234567);
    }

    public void Dispose() => _fixture.Dispose();
}
