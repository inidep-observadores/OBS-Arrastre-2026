using FluentAssertions;
using NSubstitute;
using OBSArrastre2026.App.Features.Mareas;
using OBSArrastre2026.App.Services;
using OBSArrastre2026.App.ViewModels;

namespace OBSArrastre2026.Tests.Validators;

public sealed class MareaValidatorTests
{
    private readonly MareaValidator _validator = new();

    private MareaEditViewModel CrearVm()
    {
        var buqueService = Substitute.For<IBuqueService>();
        buqueService.GetBuquesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<BuqueListItemViewModel>>(Array.Empty<BuqueListItemViewModel>()));

        var mareaService = Substitute.For<IMareaService>();
        var importService = Substitute.For<IMareaImportService>();
        var jsonService = Substitute.For<IJsonImportService>();
        var activeMareaManager = Substitute.For<IActiveMareaManager>();

        return new MareaEditViewModel(
            () => { },
            _validator,
            mareaService,
            buqueService,
            importService,
            jsonService,
            activeMareaManager);
    }

    [Fact]
    public void AnioInidep_MenorA2000_FallaValidacion()
    {
        var vm = CrearVm();
        vm.AnioInidep = 1999;

        var result = _validator.Validate(vm);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(vm.AnioInidep));
    }

    [Fact]
    public void AnioInidep_MayorA2100_FallaValidacion()
    {
        var vm = CrearVm();
        vm.AnioInidep = 2101;

        var result = _validator.Validate(vm);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(vm.AnioInidep));
    }

    [Theory]
    [InlineData(2000)]
    [InlineData(2025)]
    [InlineData(2100)]
    public void AnioInidep_DentroDeRango_Valido(int anio)
    {
        var vm = CrearVm();
        vm.AnioInidep = anio;
        vm.NumeroInidep = 1;
        vm.FechaInicio = DateTime.Today;
        vm.SelectedBuque = new BuqueListItemViewModel("id", "Buque Test", 1, 1, null, null);

        var result = _validator.Validate(vm);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(vm.AnioInidep));
    }

    [Fact]
    public void NumeroInidep_CeroOMenos_FallaValidacion()
    {
        var vm = CrearVm();
        vm.NumeroInidep = 0;

        var result = _validator.Validate(vm);

        result.Errors.Should().Contain(e => e.PropertyName == nameof(vm.NumeroInidep));
    }

    [Fact]
    public void NumeroInidep_Positivo_Valido()
    {
        var vm = CrearVm();
        vm.AnioInidep = 2025;
        vm.NumeroInidep = 5;
        vm.FechaInicio = DateTime.Today;
        vm.SelectedBuque = new BuqueListItemViewModel("id", "Buque Test", 1, 1, null, null);

        var result = _validator.Validate(vm);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(vm.NumeroInidep));
    }

    [Fact]
    public void FechaFin_AnteriorAFechaInicio_FallaValidacion()
    {
        var vm = CrearVm();
        vm.AnioInidep = 2025;
        vm.NumeroInidep = 1;
        vm.FechaInicio = new DateTime(2025, 6, 15);
        vm.FechaFin = new DateTime(2025, 6, 10);
        vm.SelectedBuque = new BuqueListItemViewModel("id", "Buque Test", 1, 1, null, null);

        var result = _validator.Validate(vm);

        result.Errors.Should().Contain(e => e.PropertyName == nameof(vm.FechaFin));
    }

    [Fact]
    public void FechaFin_IgualAFechaInicio_Valida()
    {
        var vm = CrearVm();
        vm.AnioInidep = 2025;
        vm.NumeroInidep = 1;
        vm.FechaInicio = new DateTime(2025, 6, 15);
        vm.FechaFin = new DateTime(2025, 6, 15);
        vm.SelectedBuque = new BuqueListItemViewModel("id", "Buque Test", 1, 1, null, null);

        var result = _validator.Validate(vm);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(vm.FechaFin));
    }

    [Fact]
    public void FechaFin_Nula_NoGeneraError()
    {
        var vm = CrearVm();
        vm.AnioInidep = 2025;
        vm.NumeroInidep = 1;
        vm.FechaInicio = DateTime.Today;
        vm.FechaFin = null;
        vm.SelectedBuque = new BuqueListItemViewModel("id", "Buque Test", 1, 1, null, null);

        var result = _validator.Validate(vm);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(vm.FechaFin));
    }

    [Fact]
    public void SelectedBuque_Nulo_FallaValidacion()
    {
        var vm = CrearVm();
        vm.AnioInidep = 2025;
        vm.NumeroInidep = 1;
        vm.FechaInicio = DateTime.Today;
        vm.SelectedBuque = null;

        var result = _validator.Validate(vm);

        result.Errors.Should().Contain(e => e.PropertyName == nameof(vm.SelectedBuque));
    }

    [Fact]
    public void VmCompleto_Valido()
    {
        var vm = CrearVm();
        vm.AnioInidep = 2025;
        vm.NumeroInidep = 3;
        vm.FechaInicio = new DateTime(2025, 3, 1);
        vm.FechaFin = new DateTime(2025, 3, 15);
        vm.SelectedBuque = new BuqueListItemViewModel("id", "Buque Test", 100, 10, null, null);

        var result = _validator.Validate(vm);

        result.IsValid.Should().BeTrue();
    }
}
