using FluentAssertions;
using NSubstitute;
using ControlMareas.App.Data.Entities;
using ControlMareas.App.Services;
using ControlMareas.App.ViewModels;

namespace ControlMareas.Tests.Validators;

public sealed class ProduccionEditViewModelValidatorTests
{
    private readonly ProduccionEditViewModelValidator _validator = new();

    private ProduccionEditViewModel CrearVm()
    {
        var produccionService = Substitute.For<IProduccionService>();
        var productoService = Substitute.For<IProductoService>();
        productoService.GetProductosAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Producto>>(Array.Empty<Producto>()));
        productoService.GetEspeciesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Especie>>(Array.Empty<Especie>()));

        var activeMareaManager = Substitute.For<IActiveMareaManager>();
        activeMareaManager.ActiveMarea.Returns((Marea?)null);

        return new ProduccionEditViewModel(
            () => { },
            _validator,
            produccionService,
            productoService,
            activeMareaManager);
    }

    [Fact]
    public void Fecha_Nula_FallaValidacion()
    {
        var vm = CrearVm();
        vm.Fecha = null;

        var result = _validator.Validate(vm);

        result.Errors.Should().Contain(e => e.PropertyName == nameof(vm.Fecha));
    }

    [Fact]
    public void Fecha_Valida_NoGeneraError()
    {
        var vm = CrearVm();
        vm.Fecha = DateTime.Today;

        var result = _validator.Validate(vm);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(vm.Fecha));
    }

    [Fact]
    public void SelectedProducto_Nulo_FallaValidacion()
    {
        var vm = CrearVm();
        vm.SelectedProducto = null;

        var result = _validator.Validate(vm);

        result.Errors.Should().Contain(e => e.PropertyName == nameof(vm.SelectedProducto));
    }

    [Fact]
    public void SelectedEtapa_Nula_FallaValidacion()
    {
        var vm = CrearVm();
        vm.SelectedEtapa = null;

        var result = _validator.Validate(vm);

        result.Errors.Should().Contain(e => e.PropertyName == nameof(vm.SelectedEtapa));
    }

    [Fact]
    public void Kg_Cero_FallaValidacion()
    {
        var vm = CrearVm();
        vm.Kg = 0;

        var result = _validator.Validate(vm);

        result.Errors.Should().Contain(e => e.PropertyName == nameof(vm.Kg));
    }

    [Fact]
    public void Kg_Negativo_FallaValidacion()
    {
        var vm = CrearVm();
        vm.Kg = -100;

        var result = _validator.Validate(vm);

        result.Errors.Should().Contain(e => e.PropertyName == nameof(vm.Kg));
    }

    [Fact]
    public void Kg_Nulo_FallaValidacion()
    {
        var vm = CrearVm();
        vm.Kg = null;

        var result = _validator.Validate(vm);

        result.Errors.Should().Contain(e => e.PropertyName == nameof(vm.Kg));
    }

    [Fact]
    public void Factor_Negativo_CuandoEstaPresente_FallaValidacion()
    {
        var vm = CrearVm();
        vm.Factor = -0.5;

        var result = _validator.Validate(vm);

        result.Errors.Should().Contain(e => e.PropertyName == nameof(vm.Factor));
    }

    [Fact]
    public void Factor_Nulo_NoGeneraError()
    {
        var vm = CrearVm();
        vm.Factor = null;

        var result = _validator.Validate(vm);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(vm.Factor));
    }

    [Fact]
    public void VmCompleto_Valido()
    {
        var vm = CrearVm();
        vm.Fecha = DateTime.Today;
        vm.SelectedProducto = new Producto { Id = "prod-1", Descripcion = "Merluza entera" };
        vm.SelectedEtapa = new MareaEtapa { ID = "etapa-1", MareaID = "marea-1" };
        vm.Kg = 1500;
        vm.Factor = 1.2;

        var result = _validator.Validate(vm);

        result.IsValid.Should().BeTrue();
    }
}
