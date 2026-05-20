using FluentAssertions;
using NSubstitute;
using ControlMareas.App.Features.Lances;
using ControlMareas.App.Services;
using ControlMareas.App.ViewModels;

namespace ControlMareas.Tests.Validators;

public sealed class LanceValidatorTests
{
    private readonly LanceValidator _validator = new();

    private LanceEditViewModel CrearVm()
    {
        var lanceService = Substitute.For<ILanceService>();
        lanceService.GetEspeciesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ControlMareas.App.Data.Entities.Especie>>(Array.Empty<ControlMareas.App.Data.Entities.Especie>()));

        return new LanceEditViewModel(
            () => { },
            _validator,
            lanceService,
            mareaEtapaId: "etapa-1");
    }

    [Fact]
    public void NroLance_Cero_FallaValidacion()
    {
        var vm = CrearVm();
        vm.NroLance = 0;

        var result = _validator.Validate(vm);

        result.Errors.Should().Contain(e => e.PropertyName == nameof(vm.NroLance));
    }

    [Fact]
    public void NroLance_Negativo_FallaValidacion()
    {
        var vm = CrearVm();
        vm.NroLance = -1;

        var result = _validator.Validate(vm);

        result.Errors.Should().Contain(e => e.PropertyName == nameof(vm.NroLance));
    }

    [Fact]
    public void NroLance_Positivo_Valido()
    {
        var vm = CrearVm();
        vm.NroLance = 1;
        vm.Fecha = DateTime.Today;

        var result = _validator.Validate(vm);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(vm.NroLance));
    }

    [Fact]
    public void Fecha_Default_FallaValidacion()
    {
        var vm = CrearVm();
        vm.NroLance = 1;
        vm.Fecha = default;

        var result = _validator.Validate(vm);

        result.Errors.Should().Contain(e => e.PropertyName == nameof(vm.Fecha));
    }

    [Fact]
    public void Fecha_Valida_NoGeneraError()
    {
        var vm = CrearVm();
        vm.NroLance = 1;
        vm.Fecha = new DateTime(2025, 5, 1);

        var result = _validator.Validate(vm);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(vm.Fecha));
    }

    [Fact]
    public void VmCompleto_Valido()
    {
        var vm = CrearVm();
        vm.NroLance = 5;
        vm.Fecha = new DateTime(2025, 3, 20);

        var result = _validator.Validate(vm);

        result.IsValid.Should().BeTrue();
    }
}
