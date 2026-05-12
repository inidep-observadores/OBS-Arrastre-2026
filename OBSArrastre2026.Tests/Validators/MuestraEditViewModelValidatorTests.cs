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
    public void PesoMuestraGramos_Cero_CuandoEstaPresente_FallaValidacion()
    {
        var vm = CrearVm();
        vm.EspecieId = "esp-001";
        vm.PesoMuestraGramos = 0;

        var result = _validator.Validate(vm);

        result.Errors.Should().Contain(e => e.PropertyName == nameof(vm.PesoMuestraGramos));
    }

    [Fact]
    public void PesoMuestraGramos_Negativo_FallaValidacion()
    {
        var vm = CrearVm();
        vm.EspecieId = "esp-001";
        vm.PesoMuestraGramos = -10;

        var result = _validator.Validate(vm);

        result.Errors.Should().Contain(e => e.PropertyName == nameof(vm.PesoMuestraGramos));
    }

    [Fact]
    public void PesoMuestraGramos_Nulo_NoGeneraError()
    {
        var vm = CrearVm();
        vm.EspecieId = "esp-001";
        vm.PesoMuestraGramos = null;

        var result = _validator.Validate(vm);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(vm.PesoMuestraGramos));
    }

    [Fact]
    public void PesoMuestraGramos_Positivo_Valido()
    {
        var vm = CrearVm();
        vm.EspecieId = "esp-001";
        vm.PesoMuestraGramos = 500;

        var result = _validator.Validate(vm);

        result.IsValid.Should().BeTrue();
    }
}
