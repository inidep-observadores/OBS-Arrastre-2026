using FluentAssertions;
using NSubstitute;
using ControlMareas.App.Services;
using ControlMareas.App.ViewModels;

namespace ControlMareas.Tests.Validators;

public sealed class MuestraEditViewModelValidatorTests
{
    private readonly MuestraEditViewModelValidator _validator = new();

    private MuestraEditViewModel CrearVm()
    {
        var muestraService = Substitute.For<IMuestraService>();
        var lanceService = Substitute.For<ILanceService>();
        lanceService.GetEspeciesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ControlMareas.App.Data.Entities.Especie>>(Array.Empty<ControlMareas.App.Data.Entities.Especie>()));

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

    [Fact]
    public void FrecuenciasTallas_MachoMadurosSuperaMachosTotales_FallaValidacion()
    {
        var vm = CrearVm();
        vm.EspecieId = "esp-001";
        vm.FrecuenciasTallas.Add(new FrecuenciaTallaViewModel
        {
            Talla = 10,
            NroMachos = 5,
            NroLangostinosMachoMaduros = 6
        });

        var result = _validator.Validate(vm);

        result.Errors.Should().Contain(e => e.PropertyName.Contains("NroLangostinosMachoMaduros"));
    }

    [Fact]
    public void FrecuenciasTallas_HembrasMadurasEImpregnadasSuperaHembrasTotales_FallaValidacion()
    {
        var vm = CrearVm();
        vm.EspecieId = "esp-001";
        vm.FrecuenciasTallas.Add(new FrecuenciaTallaViewModel
        {
            Talla = 10,
            NroHembras = 5,
            NroLangostinosHembraMaduras = 3,
            NroLangostinosHembraImpregnadas = 3
        });

        var result = _validator.Validate(vm);

        result.Errors.Should().Contain(e => e.PropertyName.Contains("NroLangostinosHembraMaduras") || e.PropertyName.Contains("FrecuenciasTallas"));
    }
}
