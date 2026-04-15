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

    public MareaEditViewModel(
        Action onClose, 
        IValidator<MareaEditViewModel> validator,
        IMareaService mareaService,
        IBuqueService buqueService,
        string? mareaId = null) : base(validator)
    {
        _onClose = onClose;
        _mareaService = mareaService;
        _buqueService = buqueService;
        _mareaId = mareaId;
        
        _fechaInicio = DateTime.Today;

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(Cancel);
        AddEtapaCommand = new RelayCommand(AddEtapa);
        ImportDbfCommand = new RelayCommand(ImportDbf);

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
    public ICommand ImportDbfCommand { get; }

    public Action<object?>? ShowCustomDialog { get; set; }

    private void ImportDbf()
    {
        if (AnioInidep < 2000 || NumeroInidep <= 0)
        {
            // Podríamos mostrar un mensaje de que se requiere año y número
            return;
        }

        var importVm = new ImportDbfViewModel(NumeroInidep, AnioInidep, files => 
        {
            ShowCustomDialog?.Invoke(null); // Cerrar diálogos
            if (files != null)
            {
                // TODO: En la siguiente etapa se implementará la lógica de procesamiento
            }
        });

        ShowCustomDialog?.Invoke(importVm);
    }

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
                var marea = new Marea
                {
                    ID = _mareaId ?? Guid.NewGuid().ToString(),
                    AnioInidep = AnioInidep,
                    NumeroInidep = NumeroInidep,
                    Comentarios = Comentarios,
                    FechaInicio = FechaInicio,
                    FechaFin = FechaFin,
                    BuqueID = SelectedBuque?.ID
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
