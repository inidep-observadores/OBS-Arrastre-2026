using FluentAssertions;
using OBSArrastre2026.App.Data.Entities;
using OBSArrastre2026.App.ViewModels;

namespace OBSArrastre2026.Tests.ViewModels;

public sealed class FrecuenciaTallaViewModelTests
{
    // ── UpdateTotal ──────────────────────────────────────────────────────────

    [Fact]
    public void NroTotal_ActualizaAlCambiarMachos()
    {
        var vm = new FrecuenciaTallaViewModel { NroMachos = 5, NroHembras = 3, NroIndeterminados = 2 };

        vm.NroMachos = 10;

        vm.NroTotal.Should().Be(15);
    }

    [Fact]
    public void NroTotal_ActualizaAlCambiarHembras()
    {
        var vm = new FrecuenciaTallaViewModel { NroMachos = 5, NroHembras = 3, NroIndeterminados = 2 };

        vm.NroHembras = 7;

        vm.NroTotal.Should().Be(14);
    }

    [Fact]
    public void NroTotal_ActualizaAlCambiarIndeterminados()
    {
        var vm = new FrecuenciaTallaViewModel { NroMachos = 4, NroHembras = 4, NroIndeterminados = 2 };

        vm.NroIndeterminados = 0;

        vm.NroTotal.Should().Be(8);
    }

    [Fact]
    public void NroTotal_TodosCero_NoActualizaTotal()
    {
        // Cuando la suma es 0, UpdateTotal no asigna el total (condición suma > 0)
        var vm = new FrecuenciaTallaViewModel { NroMachos = 5 };

        vm.NroMachos = 0;

        // La condición interna es "if (suma > 0) NroTotal = suma"
        // Por eso cuando todo llega a 0, el total se mantiene en su último valor asignado
        vm.NroTotal.Should().Be(5); // El último valor antes de ser 0
    }

    [Fact]
    public void NroTotal_SumaCorrecta_DeTresCampos()
    {
        var vm = new FrecuenciaTallaViewModel
        {
            NroMachos = 10,
            NroHembras = 15,
            NroIndeterminados = 5
        };

        vm.NroTotal.Should().Be(30);
    }

    [Fact]
    public void Total_EsAliasDeNroTotal()
    {
        var vm = new FrecuenciaTallaViewModel { NroMachos = 8, NroHembras = 4, NroIndeterminados = 0 };

        vm.Total.Should().Be(vm.NroTotal);
    }

    // ── ToEntity ─────────────────────────────────────────────────────────────

    [Fact]
    public void ToEntity_MapeoCompleto_EsCorrecto()
    {
        var vm = new FrecuenciaTallaViewModel
        {
            Talla = 35.5,
            NroMachos = 10,
            NroHembras = 12,
            NroIndeterminados = 3,
            NroLangostinosMachoMaduros = 4,
            NroLangostinosHembraMaduras = 5,
            NroLangostinosHembraImpregnadas = 2
        };

        var entity = vm.ToEntity();

        entity.ID.Should().Be(vm.Id);
        entity.Talla.Should().Be(35.5);
        entity.NroMachos.Should().Be(10);
        entity.NroHembras.Should().Be(12);
        entity.NroIndeterminados.Should().Be(3);
        entity.NroTotal.Should().Be(vm.NroTotal);
        entity.NroLangostinosMachoMaduros.Should().Be(4);
        entity.NroLangostinosHembraMaduras.Should().Be(5);
        entity.NroLangostinosHembraImpregnadas.Should().Be(2);
    }

    [Fact]
    public void ConstruidoDesdeEntidad_PreservaTodosLosCampos()
    {
        var entity = new FrecuenciaTalla
        {
            ID = Guid.NewGuid().ToString(),
            Talla = 22.0,
            NroMachos = 6,
            NroHembras = 8,
            NroIndeterminados = 1,
            NroTotal = 15,
            NroLangostinosMachoMaduros = 3,
            NroLangostinosHembraMaduras = 2,
            NroLangostinosHembraImpregnadas = 1
        };

        var vm = new FrecuenciaTallaViewModel(entity);

        vm.Id.Should().Be(entity.ID);
        vm.Talla.Should().Be(22.0);
        vm.NroMachos.Should().Be(6);
        vm.NroHembras.Should().Be(8);
        vm.NroIndeterminados.Should().Be(1);
        vm.NroTotal.Should().Be(15);
    }
}
