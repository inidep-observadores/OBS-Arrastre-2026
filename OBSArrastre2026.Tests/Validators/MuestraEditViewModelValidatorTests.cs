using FluentAssertions;
using NSubstitute;
using OBSArrastre2026.App.Services;
using OBSArrastre2026.App.ViewModels;

namespace OBSArrastre2026.Tests.Validators;

public sealed class MuestraEditViewModelValidatorTests
{
    private readonly MuestraEditViewModelValidator _validator = new();

    private MuestraEditViewModel CrearVm()
    {
        var muestraService = Substitute.For<IMuestraService>();
        var lanceService = Substitute.For<ILanceService>();
        lanceService.GetEspeciesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OBSArrastre2026.App.Data.Entities.Especie>>(Array.Empty<OBSArrastre2026.App.Data.Entities.Especie>()));

        return new MuestraEditViewModel(
            () => { },
            _validator,
            muestraService,
            lanceService,
            lanceId: "lance-1");
    }

    [Fact]
    public void EspecieId_Nulo_FallaValidacion()
    {
        var vm = CrearVm();
        vm.EspecieId = null;

        var result = _validator.Validate(vm);

        result.Errors.Should().Contain(e => e.PropertyName == nameof(vm.EspecieId));
    }

    [Fact]
    public void EspecieId_VacioString_FallaValidacion()
    {
        var vm = CrearVm();
        vm.EspecieId = string.Empty;

        var result = _validator.Validate(vm);

        result.Errors.Should().Contain(e => e.PropertyName == nameof(vm.EspecieId));
    }

    [Fact]
    public void EspecieId_ConValor_Valido()
    {
        var vm = CrearVm();
        vm.EspecieId = "esp-001";

        var result = _validator.Validate(vm);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(vm.EspecieId));
    }

    [Fact]
    public void PesoMuestraKg_Cero_CuandoEstaPresente_FallaValidacion()
    {
        var vm = CrearVm();
        vm.EspecieId = "esp-001";
        vm.PesoMuestraKg = 0;

        var result = _validator.Validate(vm);

        result.Errors.Should().Contain(e => e.PropertyName == nameof(vm.PesoMuestraKg));
    }

    [Fact]
    public void PesoMuestraKg_Negativo_FallaValidacion()
    {
        var vm = CrearVm();
        vm.EspecieId = "esp-001";
        vm.PesoMuestraKg = -10;

        var result = _validator.Validate(vm);

        result.Errors.Should().Contain(e => e.PropertyName == nameof(vm.PesoMuestraKg));
    }

    [Fact]
    public void PesoMuestraKg_Nulo_NoGeneraError()
    {
        var vm = CrearVm();
        vm.EspecieId = "esp-001";
        vm.PesoMuestraKg = null;

        var result = _validator.Validate(vm);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(vm.PesoMuestraKg));
    }

    [Fact]
    public void PesoMuestraKg_Positivo_Valido()
    {
        var vm = CrearVm();
        vm.EspecieId = "esp-001";
        vm.PesoMuestraKg = 0.5;

        var result = _validator.Validate(vm);

        result.IsValid.Should().BeTrue();
    }
}
