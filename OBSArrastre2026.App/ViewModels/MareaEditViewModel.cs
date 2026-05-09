using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentValidation;
using OBSArrastre2026.App.Data.Entities;
using OBSArrastre2026.App.Services;

namespace OBSArrastre2026.App.ViewModels;

public sealed partial class MareaEditViewModel : ValidatableViewModelBase<MareaEditViewModel>
{
    private readonly IMareaService _mareaService;
    private readonly IBuqueService _buqueService;
    private readonly IMareaImportService _mareaImportService;
    private readonly IJsonImportService _jsonImportService;
    private readonly IActiveMareaManager _activeMareaManager;
    private readonly Action _onClose;
    private string? _mareaId;
    private int _anioInidep;
    private int _numeroInidep;
    private string? _buqueID;
    private DateTime _fechaInicio;
    private DateTime? _fechaFin;
    private string? _comentarios;
    private BuqueListItemViewModel? _selectedBuque;
    private bool _isLoading;
    private string? _metadata;

    public MareaEditViewModel(
        Action onClose, 
        IValidator<MareaEditViewModel> validator,
        IMareaService mareaService,
        IBuqueService buqueService,
        IMareaImportService mareaImportService,
        IJsonImportService jsonImportService,
        IActiveMareaManager activeMareaManager,
        string? mareaId = null) : base(validator)
    {
        _onClose = onClose;
        _mareaService = mareaService;
        _buqueService = buqueService;
        _mareaImportService = mareaImportService;
        _jsonImportService = jsonImportService;
        _activeMareaManager = activeMareaManager;
        _mareaId = mareaId;
        
        _fechaInicio = DateTime.Today;

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(Cancel);
        AddEtapaCommand = new RelayCommand(AddEtapa);
        ImportDbfCommand = new AsyncRelayCommand(ImportDbf);

        _ = InitializeAsync();
    }

    public ObservableCollection<MareaEtapaItemViewModel> Etapas { get; } = [];
    public ObservableCollection<BuqueListItemViewModel> Buques { get; } = [];

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string CodigoDisplay => $"{NumeroInidep}/{AnioInidep % 100:D2}";

    public string? Comentarios
    {
        get => _comentarios;
        set => SetProperty(ref _comentarios, value);
    }

    public int AnioInidep
    {
        get => _anioInidep;
        set 
        {
            if (SetProperty(ref _anioInidep, value))
            {
                ValidatePropertyWithFluent(value, nameof(AnioInidep));
                OnPropertyChanged(nameof(CodigoDisplay));
            }
        }
    }

    public int NumeroInidep
    {
        get => _numeroInidep;
        set 
        {
            if (SetProperty(ref _numeroInidep, value))
            {
                ValidatePropertyWithFluent(value, nameof(NumeroInidep));
                OnPropertyChanged(nameof(CodigoDisplay));
            }
        }
    }

    public string? BuqueID
    {
        get => _buqueID;
        set => SetProperty(ref _buqueID, value);
    }

    public DateTime FechaInicio
    {
        get => _fechaInicio;
        set 
        {
            if (SetProperty(ref _fechaInicio, value))
                ValidatePropertyWithFluent(value, nameof(FechaInicio));
        }
    }

    public DateTime? FechaFin
    {
        get => _fechaFin;
        set 
        {
            if (SetProperty(ref _fechaFin, value))
            {
                ValidatePropertyWithFluent(value, nameof(FechaFin));
                ValidatePropertyWithFluent(FechaInicio, nameof(FechaInicio));
            }
        }
    }

    public BuqueListItemViewModel? SelectedBuque
    {
        get => _selectedBuque;
        set 
        {
            if (SetProperty(ref _selectedBuque, value))
            {
                BuqueID = value?.ID;
                ValidatePropertyWithFluent(value, nameof(SelectedBuque));
            }
        }
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand AddEtapaCommand { get; }
    public AsyncRelayCommand ImportDbfCommand { get; }

    public Action<object?>? ShowCustomDialog { get; set; }
    public Func<string, string, Task<bool>>? ShowConfirmation { get; set; }

    private async Task ImportDbf()
    {
        if (AnioInidep < 2000 || NumeroInidep <= 0)
        {
            if (ShowMessage != null) await ShowMessage("Validación", "Se requiere Año y Número de Marea válidos para importar.", null, MessageDialogType.Warning);
            return;
        }

        if (string.IsNullOrEmpty(_mareaId))
        {
            // Si es una marea nueva, guardamos un borrador para obtener un ID
            if (SelectedBuque == null)
            {
                if (ShowMessage != null) await ShowMessage("Validación", "Debe seleccionar un Buque antes de importar, o importar desde un JSON que lo contenga.", null, MessageDialogType.Warning);
                // Si el usuario va a importar de JSON, tal vez no necesite seleccionar buque aún.
                // Pero necesitamos un ID. Generamos uno.
                _mareaId = Guid.NewGuid().ToString();
            }
            else
            {
                await SaveAsync(); // Guarda el estado actual
                if (string.IsNullOrEmpty(_mareaId)) return; // Falló el guardado
            }
        }

        // 1. Verificar si hay datos
        bool hasData = await _mareaService.HasExistingDataAsync(_mareaId);
        if (hasData)
        {
            bool confirm = await (ShowConfirmation?.Invoke("Datos Existentes", 
                "Esta marea ya tiene lances o producción cargada. Si continúa, estos datos se borrarán para realizar una importación limpia. ¿Desea proceder?") ?? Task.FromResult(false));
            
            if (!confirm) return;

            // 2. Limpiar datos
            try 
            {
                IsLoading = true;
                await _mareaService.ClearMareaDataAsync(_mareaId);
            }
            catch (Exception ex)
            {
                if (ShowMessage != null) await ShowMessage("Error", $"No se pudo limpiar la marea: {ex.Message}", ex.ToString(), MessageDialogType.Error);
                return;
            }
            finally
            {
                IsLoading = false;
            }
        }

        // 3. Obtener datos de la marea para validación de etapas
        var mareaFull = await _mareaService.GetMareaAsync(_mareaId);
        if (mareaFull == null || !mareaFull.Etapas.Any())
        {
            if (ShowMessage != null) await ShowMessage("Sin Etapas", "No se puede importar datos si la marea no tiene al menos una etapa cargada.", null, MessageDialogType.Warning);
            return;
        }

        // 4. Abrir diálogo de selección
        var importVm = new ImportDbfViewModel(
            _mareaId,
            NumeroInidep, 
            AnioInidep, 
            _mareaImportService,
            _jsonImportService,
            _mareaService,
            SelectedBuque?.Nombre ?? "Sin Nombre",
            mareaFull.Etapas,
            async files => 
            {
                ShowCustomDialog?.Invoke(null); // Cerrar diálogos
                
                // Siempre refrescamos los detalles (por si se crearon etapas o cambió el buque)
                await RefreshDetailsAsync();

                // Si la marea que estamos editando es la activa, refrescamos el gestor global
                if (_mareaId == _activeMareaManager.ActiveMareaId)
                {
                    await _activeMareaManager.RefreshAsync();
                }

                if (files != null)
                {
                    if (ShowMessage != null) await ShowMessage("Éxito", "La importación finalizó correctamente. Los lances y muestras han sido guardados en la base de datos.", null, MessageDialogType.Success);
                }
            });

        importVm.ShowMessage = (title, msg, details, type) => ShowMessage != null ? ShowMessage(title, msg, details, type) : Task.CompletedTask;
        importVm.ShowConfirmation = (title, msg) => ShowConfirmation?.Invoke(title, msg) ?? Task.FromResult(false);
        ShowCustomDialog?.Invoke(importVm);
    }

    private async Task RefreshDetailsAsync()
    {
        await InitializeAsync();
    }

    public Func<string, string, string?, MessageDialogType, Task>? ShowMessage { get; set; }

    private async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            // Cargar buques
            var availableBuques = await _buqueService.GetBuquesAsync();
            Buques.Clear();
            foreach (var b in availableBuques) Buques.Add(b);

            if (!string.IsNullOrEmpty(_mareaId))
            {
                // Cargar marea existente
                var marea = await _mareaService.GetMareaAsync(_mareaId);
                if (marea != null)
                {
                    AnioInidep = marea.AnioInidep;
                    NumeroInidep = marea.NumeroInidep;
                    Comentarios = marea.Comentarios;
                    FechaInicio = marea.FechaInicio;
                    FechaFin = marea.FechaFin;
                    SelectedBuque = Buques.FirstOrDefault(b => b.ID == marea.BuqueID);
                    _metadata = marea.Metadata;

                    Etapas.Clear();
                    foreach (var etapa in marea.Etapas.OrderBy(e => e.FechaZarpada))
                    {
                        var vm = new MareaEtapaItemViewModel(etapa);
                        vm.RequestDeletion = HandleEtapaDeletion;
                        Etapas.Add(vm);
                    }
                }
            }
            else
            {
                // Marea nueva: valores por defecto
                AnioInidep = DateTime.Today.Year;
            }
        }
        finally
        {
            IsLoading = false;
            ValidateAll();
        }
    }

    private void AddEtapa()
    {
        foreach (var e in Etapas) e.IsExpanded = false;

        var nuevaEtapa = new MareaEtapa 
        { 
            MareaID = _mareaId,
            FechaZarpada = DateTime.Today
        };
        var vm = new MareaEtapaItemViewModel(nuevaEtapa) 
        { 
            IsExpanded = true,
            RequestDeletion = HandleEtapaDeletion
        };
        Etapas.Add(vm);
    }

    private void HandleEtapaDeletion(MareaEtapaItemViewModel vm)
    {
        Etapas.Remove(vm);
    }

    private async Task SaveAsync()
    {
        if (ValidateAll())
        {
            IsLoading = true;
            try
            {
                // Verificar duplicados si es una marea nueva
                if (string.IsNullOrEmpty(_mareaId))
                {
                    var existing = await _mareaService.FindMareaAsync(NumeroInidep, AnioInidep);
                    if (existing != null)
                    {
                        if (ShowMessage != null) await ShowMessage("Marea Duplicada", $"Ya existe una marea registrada con el código {NumeroInidep}/{AnioInidep % 100:D2} para el buque {existing.Buque?.Nombre ?? "desconocido"}.", null, MessageDialogType.Warning);
                        return;
                    }
                }

                var marea = new Marea
                {
                    ID = _mareaId ?? Guid.NewGuid().ToString(),
                    AnioInidep = AnioInidep,
                    NumeroInidep = NumeroInidep,
                    Comentarios = Comentarios,
                    FechaInicio = FechaInicio,
                    FechaFin = FechaFin,
                    BuqueID = SelectedBuque?.ID,
                    Metadata = _metadata
                };

                // Añadir etapas desde los ViewModels
                foreach (var etapaVm in Etapas)
                {
                    var etapa = etapaVm.ToEntity();
                    etapa.MareaID = marea.ID;
                    marea.Etapas.Add(etapa);
                }

                await _mareaService.SaveMareaAsync(marea);
                _onClose();
            }
            catch (Exception ex)
            {
                // Aquí se podría mostrar un mensaje de error al usuario
                System.Diagnostics.Debug.WriteLine($"Error al guardar marea: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    private void Cancel()
    {
        _onClose();
    }
}
