using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentValidation;
using OBSArrastre2026.App.Data.Entities;
using OBSArrastre2026.App.Services;

namespace OBSArrastre2026.App.ViewModels;

public sealed partial class ProduccionEditViewModel : ValidatableViewModelBase<ProduccionEditViewModel>
{
    private readonly IProduccionService _produccionService;
    private readonly IProductoService _productoService;
    private readonly IActiveMareaManager _activeMareaManager;
    private readonly Action _onClose;
    private string? _registroId;
    
    private DateTime? _fecha;
    private Especie? _selectedEspecie;
    private Producto? _selectedProducto;
    private string? _categoria;
    private double? _kg;
    private double? _factor;
    private string? _comentarios;
    private bool _isLoading;
    private MareaEtapa? _selectedEtapa;

    public ProduccionEditViewModel(
        Action onClose,
        IValidator<ProduccionEditViewModel> validator,
        IProduccionService produccionService,
        IProductoService productoService,
        IActiveMareaManager activeMareaManager,
        string? registroId = null) : base(validator)
    {
        _onClose = onClose;
        _produccionService = produccionService;
        _productoService = productoService;
        _activeMareaManager = activeMareaManager;
        _registroId = registroId;

        _fecha = DateTime.Today;

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(Cancel);

        _ = InitializeAsync();
    }

    public ObservableCollection<Especie> Especies { get; } = [];
    public ObservableCollection<Producto> Productos { get; } = [];
    public ObservableCollection<MareaEtapa> Etapas { get; } = [];

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public DateTime? Fecha
    {
        get => _fecha;
        set => SetProperty(ref _fecha, value);
    }

    public Especie? SelectedEspecie
    {
        get => _selectedEspecie;
        set => SetProperty(ref _selectedEspecie, value);
    }

    public Producto? SelectedProducto
    {
        get => _selectedProducto;
        set => SetProperty(ref _selectedProducto, value);
    }

    public string? Categoria
    {
        get => _categoria;
        set => SetProperty(ref _categoria, value);
    }

    public double? Kg
    {
        get => _kg;
        set => SetProperty(ref _kg, value);
    }

    public double? Factor
    {
        get => _factor;
        set => SetProperty(ref _factor, value);
    }

    public string? Comentarios
    {
        get => _comentarios;
        set => SetProperty(ref _comentarios, value);
    }

    public MareaEtapa? SelectedEtapa
    {
        get => _selectedEtapa;
        set => SetProperty(ref _selectedEtapa, value);
    }

    private async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            var species = await _productoService.GetEspeciesAsync();
            foreach (var s in species) Especies.Add(s);

            var products = await _productoService.GetProductosAsync();
            foreach (var p in products) Productos.Add(p);

            var activeMarea = _activeMareaManager.ActiveMarea;
            if (activeMarea != null)
            {
                foreach (var e in activeMarea.Etapas) Etapas.Add(e);
                SelectedEtapa = Etapas.FirstOrDefault();
            }

            if (_registroId != null)
            {
                var registro = await _produccionService.GetRegistroProduccionAsync(_registroId);
                if (registro != null)
                {
                    Fecha = DateTime.TryParse(registro.Fecha, out var dt) ? dt : DateTime.Today;
                    SelectedEspecie = Especies.FirstOrDefault(e => e.ID == registro.EspecieId);
                    SelectedProducto = Productos.FirstOrDefault(p => p.Id == registro.IdProducto);
                    Categoria = registro.Categoria;
                    Kg = registro.Kg;
                    Factor = registro.Factor;
                    Comentarios = registro.Comentarios;
                    SelectedEtapa = Etapas.FirstOrDefault(e => e.ID == registro.MareaEtapaId);
                }
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SaveAsync()
    {
        if (!ValidateAll()) return;

        if (SelectedEtapa == null || SelectedProducto == null) return;

        var registro = new RegistroProduccion
        {
            Id = _registroId ?? Guid.NewGuid().ToString(),
            MareaEtapaId = SelectedEtapa.ID,
            Fecha = Fecha?.ToString("yyyy-MM-dd") ?? string.Empty,
            IdProducto = SelectedProducto.Id,
            EspecieId = SelectedEspecie?.ID,
            Categoria = Categoria,
            Kg = Kg,
            Factor = Factor,
            Comentarios = Comentarios
        };

        await _produccionService.SaveRegistroProduccionAsync(registro);
        _onClose();
    }

    private void Cancel() => _onClose();
}
