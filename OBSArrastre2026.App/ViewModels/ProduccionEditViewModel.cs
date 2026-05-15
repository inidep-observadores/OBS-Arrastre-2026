using System;
using System.Collections.Generic;
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
    private bool _isLoading;
    private MareaEtapa? _selectedEtapa;
    
    private List<Especie> _allEspecies = new();
    private bool _isExpanded;
    private string _searchText = string.Empty;
    private string _productSearchText = string.Empty;

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

    public ObservableCollection<Producto> Productos { get; } = [];
    public ObservableCollection<MareaEtapa> Etapas { get; } = [];
    public IEnumerable<Especie> AllEspecies => _allEspecies;

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
        set
        {
            if (SetProperty(ref _selectedEspecie, value))
            {
                if (value != null)
                {
                    _searchText = value.NombreVulgar ?? string.Empty;
                    OnPropertyChanged(nameof(SearchText));
                    IsExpanded = false;
                }
            }
        }
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                OnPropertyChanged(nameof(FilteredEspecies));
                
                if (string.IsNullOrWhiteSpace(value))
                {
                    SelectedEspecie = null;
                    IsExpanded = false;
                }
                else
                {
                    string currentName = SelectedEspecie?.NombreVulgar ?? string.Empty;
                    if (value != currentName)
                    {
                        IsExpanded = true;
                    }
                }
            }
        }
    }

    public IEnumerable<Especie> FilteredEspecies
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SearchText) || (SelectedEspecie != null && SearchText == SelectedEspecie.NombreVulgar))
            {
                return _allEspecies.OrderByDescending(e => e.Frecuente).ThenBy(e => e.NombreVulgar).Take(20);
            }

            return _allEspecies
                .Where(e => (e.NombreVulgar?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                             (e.NombreCientifico?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false))
                .OrderByDescending(e => e.Frecuente)
                .ThenBy(e => e.NombreVulgar)
                .Take(50);
        }
    }

    public Producto? SelectedProducto
    {
        get => _selectedProducto;
        set
        {
            if (SetProperty(ref _selectedProducto, value))
            {
                if (value != null)
                {
                    ProductSearchText = value.Codigo;
                }
                ValidatePropertyWithFluent(value, nameof(SelectedProducto));
            }
        }
    }

    public string ProductSearchText
    {
        get => _productSearchText;
        set
        {
            if (SetProperty(ref _productSearchText, value))
            {
                ValidatePropertyWithFluent(SelectedProducto, nameof(SelectedProducto));
            }
        }
    }

    public Func<string, string, Task<bool>>? ShowConfirmation { get; set; }

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
            _allEspecies = species.ToList();
            OnPropertyChanged(nameof(FilteredEspecies));

            var products = await _productoService.GetProductosAsync();
            foreach (var p in products) Productos.Add(p);

            var activeMarea = _activeMareaManager.ActiveMarea;
            if (activeMarea != null)
            {
                foreach (var e in activeMarea.Etapas)
                {
                    e.Marea = activeMarea; // Asegurar referencia para NumeroEtapa
                    Etapas.Add(e);
                }
                SelectedEtapa = Etapas.FirstOrDefault();
            }

            if (_registroId != null)
            {
                var registro = await _produccionService.GetRegistroProduccionAsync(_registroId);
                if (registro != null)
                {
                    Fecha = DateTime.TryParse(registro.Fecha, out var dt) ? dt : DateTime.Today;
                    SelectedEspecie = _allEspecies.FirstOrDefault(e => e.ID == registro.EspecieId);
                    SelectedProducto = Productos.FirstOrDefault(p => p.Id == registro.IdProducto);
                    Categoria = registro.Categoria;
                    Kg = registro.Kg;
                    Factor = registro.Factor;
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

        if (SelectedEtapa == null) return;

        // Lógica de creación de producto nuevo si no existe
        if (SelectedProducto == null && !string.IsNullOrWhiteSpace(ProductSearchText))
        {
            if (ShowConfirmation != null)
            {
                var confirm = await ShowConfirmation(
                    "Producto nuevo", 
                    $"El producto '{ProductSearchText}' no existe. ¿Desea crearlo y asignarlo a este registro?");
                
                if (confirm)
                {
                    var nuevoProducto = new Producto
                    {
                        Id = Guid.NewGuid().ToString(),
                        Codigo = ProductSearchText,
                        Descripcion = ProductSearchText,
                        Orden = Productos.Count > 0 ? Productos.Max(p => p.Orden) + 1 : 1
                    };
                    
                    await _productoService.SaveProductoAsync(nuevoProducto);
                    Productos.Add(nuevoProducto);
                    SelectedProducto = nuevoProducto;
                }
                else
                {
                    return; // El usuario no quiso crear el producto
                }
            }
            else
            {
                // Si no hay forma de confirmar, no podemos seguir si no hay producto
                return;
            }
        }

        if (SelectedProducto == null) return;

        var registro = new RegistroProduccion
        {
            Id = _registroId ?? Guid.NewGuid().ToString(),
            MareaEtapaId = SelectedEtapa.ID,
            Fecha = Fecha?.ToString("yyyy-MM-dd") ?? string.Empty,
            IdProducto = SelectedProducto.Id,
            EspecieId = SelectedEspecie?.ID,
            Categoria = Categoria,
            Kg = Kg,
            Factor = Factor
        };

        await _produccionService.SaveRegistroProduccionAsync(registro);
        _onClose();
    }

    private void Cancel() => _onClose();
}
