using FluentAssertions;
using OBSArrastre2026.App.Data.Entities;
using OBSArrastre2026.App.Models;
using OBSArrastre2026.App.ViewModels;

namespace OBSArrastre2026.Tests.ViewModels;

public sealed class CatchItemViewModelTests
{
    private static Especie CrearEspecie(string id, string vulgar, string cientifico, bool frecuente = false) =>
        new() { ID = id, NombreVulgar = vulgar, NombreCientifico = cientifico, Frecuente = frecuente };

    private static List<Especie> EspeciesBase() =>
    [
        CrearEspecie("1", "Merluza común", "Merluccius hubbsi", frecuente: true),
        CrearEspecie("2", "Langostino", "Pleoticus muelleri", frecuente: true),
        CrearEspecie("3", "Calamar illex", "Illex argentinus"),
        CrearEspecie("4", "Abadejo", "Genypterus blacodes")
    ];

    // ── FilteredEspecies ─────────────────────────────────────────────────────

    [Fact]
    public void FilteredEspecies_SinTexto_DevuelveTop20PorFrecuencia()
    {
        var vm = new CatchItemViewModel(new ItemCaptura(), EspeciesBase());

        var filtered = vm.FilteredEspecies.ToList();

        filtered.Should().HaveCount(4);
        filtered.First().Frecuente.Should().BeTrue();
    }

    [Fact]
    public void FilteredEspecies_ConTexto_FiltroInsensibleAMayusculas()
    {
        var vm = new CatchItemViewModel(new ItemCaptura(), EspeciesBase());

        vm.SearchText = "merluza";

        vm.FilteredEspecies.Should().Contain(e => e.NombreVulgar == "Merluza común");
        vm.FilteredEspecies.Should().NotContain(e => e.NombreVulgar == "Langostino");
    }

    [Fact]
    public void FilteredEspecies_BusquedaPorNombreCientifico_Encuentra()
    {
        var vm = new CatchItemViewModel(new ItemCaptura(), EspeciesBase());

        vm.SearchText = "Illex";

        vm.FilteredEspecies.Should().Contain(e => e.NombreCientifico == "Illex argentinus");
    }

    [Fact]
    public void SearchText_Vacio_LimpiaSelecion()
    {
        var especie = CrearEspecie("1", "Merluza común", "Merluccius hubbsi");
        var item = new ItemCaptura { Especie = especie, EspecieID = especie.ID };
        var vm = new CatchItemViewModel(item, EspeciesBase());

        vm.SearchText = string.Empty;

        vm.SelectedEspecie.Should().BeNull();
        vm.IsExpanded.Should().BeFalse();
    }

    [Fact]
    public void SelectedEspecie_AlSeleccionar_ActualizaSearchTextConNombreVulgar()
    {
        var vm = new CatchItemViewModel(new ItemCaptura(), EspeciesBase());
        var especie = EspeciesBase().First();

        vm.SelectedEspecie = especie;

        vm.SearchText.Should().Be(especie.NombreVulgar);
        vm.IsExpanded.Should().BeFalse();
    }

    // ── SummaryText ──────────────────────────────────────────────────────────

    [Fact]
    public void SummaryText_SinEspecie_MuestraPlaceholder()
    {
        var vm = new CatchItemViewModel(new ItemCaptura(), EspeciesBase());

        vm.SummaryText.Should().Be("Nueva especie...");
    }

    [Fact]
    public void SummaryText_ConEspecie_Incluye_NombreYCaptura()
    {
        var especie = CrearEspecie("1", "Merluza común", "Merluccius hubbsi");
        var item = new ItemCaptura
        {
            Especie = especie,
            EspecieID = especie.ID,
            TipoDatoCaptura = TipoDatoCaptura.Kilogramos,
            DatoCaptura = 150
        };
        var vm = new CatchItemViewModel(item, EspeciesBase());

        vm.SummaryText.Should().Contain("Merluza común");
        vm.SummaryText.Should().Contain("150");
    }

    // ── RequestDeletion ──────────────────────────────────────────────────────

    [Fact]
    public void RemoveCommand_InvocaRequestDeletion()
    {
        var vm = new CatchItemViewModel(new ItemCaptura(), EspeciesBase());
        CatchItemViewModel? deletado = null;
        vm.RequestDeletion = x => deletado = x;

        vm.RemoveCommand.Execute(null);

        deletado.Should().BeSameAs(vm);
    }

    // ── ToEntity ─────────────────────────────────────────────────────────────

    [Fact]
    public void ToEntity_DevuelveEntidadOriginal()
    {
        var entity = new ItemCaptura { DatoCaptura = 300 };
        var vm = new CatchItemViewModel(entity, EspeciesBase());

        vm.ToEntity().Should().BeSameAs(entity);
    }
}
