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

public sealed class SubmuestraEditViewModel : ValidatableViewModelBase<SubmuestraEditViewModel>
{
    private readonly ISubmuestraService _submuestraService;
    private readonly IMuestraService _muestraService;
    private readonly IActiveMareaManager _activeMareaManager;
    private readonly Action _onClose;
    private string? _selectedMuestraId;
    private readonly List<string> _deletedIds = new();
    private bool _isLoading;

    private string _especieNombre = string.Empty;
    private string _pesoMuestraDisplay = string.Empty;
    private ItemSubmuestraRowViewModel? _selectedSubmuestra;

    public SubmuestraEditViewModel(
        Action onClose,
        IValidator<SubmuestraEditViewModel> validator,
        ISubmuestraService submuestraService,
        IMuestraService muestraService,
        IActiveMareaManager activeMareaManager,
        string? muestraId) : base(validator)
    {
        _onClose = onClose;
        _submuestraService = submuestraService;
        _muestraService = muestraService;
        _activeMareaManager = activeMareaManager;
        _selectedMuestraId = muestraId;

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(Cancel);
        AddItemCommand = new RelayCommand(AddItem, () => !string.IsNullOrEmpty(SelectedMuestraId));
        RemoveItemCommand = new RelayCommand<ItemSubmuestraRowViewModel>(RemoveItem);
        GoToNextCommand = new RelayCommand(GoToNext, () => CanGoToNext);
        GoToPreviousCommand = new RelayCommand(GoToPrevious, () => CanGoToPrevious);

        _ = InitializeAsync();
    }

    public ObservableCollection<ItemSubmuestraRowViewModel> Submuestras { get; } = new();
    public ObservableCollection<MuestraSelectionOption> MuestrasDisponibles { get; } = new();
    
    public List<SexoOption> SexoOptions { get; } = new()
    {
        new SexoOption(1, "Macho"),
        new SexoOption(2, "Hembra"),
        new SexoOption(3, "Indet.")
    };

    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public string EspecieNombre { get => _especieNombre; set => SetProperty(ref _especieNombre, value); }
    public string PesoMuestraDisplay { get => _pesoMuestraDisplay; set => SetProperty(ref _pesoMuestraDisplay, value); }

    public string? SelectedMuestraId
    {
        get => _selectedMuestraId;
        set
        {
            if (SetProperty(ref _selectedMuestraId, value))
            {
                _ = OnMuestraChangedAsync();
                ((RelayCommand)AddItemCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public ItemSubmuestraRowViewModel? SelectedSubmuestra
    {
        get => _selectedSubmuestra;
        set
        {
            if (SetProperty(ref _selectedSubmuestra, value))
            {
                OnPropertyChanged(nameof(CanGoToNext));
                OnPropertyChanged(nameof(CanGoToPrevious));
                (GoToNextCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (GoToPreviousCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand AddItemCommand { get; }
    public ICommand RemoveItemCommand { get; }
    public ICommand GoToNextCommand { get; }
    public ICommand GoToPreviousCommand { get; }

    private async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            var activeMareaId = _activeMareaManager.ActiveMareaId;
            if (!string.IsNullOrEmpty(activeMareaId))
            {
                var muestras = await _muestraService.GetMuestrasPorMareaAsync(activeMareaId);
                MuestrasDisponibles.Clear();
                foreach (var m in muestras.OrderBy(x => x.Lance?.NroLance).ThenBy(x => x.Especie?.NombreVulgar))
                {
                    string lanceInfo = m.Lance != null ? $"Lance {m.Lance.NroLance} ({m.Lance.Fecha})" : "Sin Lance";
                    string especieInfo = m.Especie?.NombreVulgar ?? "Sin Especie";
                    MuestrasDisponibles.Add(new MuestraSelectionOption(m.ID, $"{lanceInfo} - {especieInfo}"));
                }
            }

            if (!string.IsNullOrEmpty(_selectedMuestraId))
            {
                SelectedMuestraId = _selectedMuestraId;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task OnMuestraChangedAsync()
    {
        if (string.IsNullOrEmpty(SelectedMuestraId))
        {
            Submuestras.Clear();
            EspecieNombre = string.Empty;
            PesoMuestraDisplay = string.Empty;
            return;
        }

        IsLoading = true;
        try
        {
            await LoadMuestraDataAsync(SelectedMuestraId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadMuestraDataAsync(string muestraId)
    {
        var muestra = await _muestraService.GetMuestraAsync(muestraId);
        if (muestra != null)
        {
            EspecieNombre = muestra.Especie?.NombreVulgar ?? "Sin Especie";
            PesoMuestraDisplay = muestra.PesoMuestra_PesoGramos.HasValue 
                ? $"{(muestra.PesoMuestra_PesoGramos.Value / 1000.0):N2} kg" 
                : "0.00 kg";

            var subs = await _submuestraService.GetSubmuestrasByMuestraIdAsync(muestraId);
            Submuestras.Clear();
            _deletedIds.Clear();
            foreach (var s in subs)
            {
                Submuestras.Add(new ItemSubmuestraRowViewModel(s));
            }
        }
    }

    private void AddItem()
    {
        if (string.IsNullOrEmpty(SelectedMuestraId)) return;

        var lastNro = Submuestras.LastOrDefault()?.NroEjemplar ?? 0;
        var newItem = new ItemSubmuestra { MuestraID = SelectedMuestraId, NroEjemplar = lastNro + 1 };
        var vm = new ItemSubmuestraRowViewModel(newItem);
        Submuestras.Add(vm);
        SelectedSubmuestra = vm;
    }

    public bool CanGoToNext => SelectedSubmuestra != null && Submuestras.IndexOf(SelectedSubmuestra) < Submuestras.Count - 1;
    public bool CanGoToPrevious => SelectedSubmuestra != null && Submuestras.IndexOf(SelectedSubmuestra) > 0;

    private void GoToNext()
    {
        if (CanGoToNext)
        {
            SelectedSubmuestra = Submuestras[Submuestras.IndexOf(SelectedSubmuestra!) + 1];
        }
    }

    private void GoToPrevious()
    {
        if (CanGoToPrevious)
        {
            SelectedSubmuestra = Submuestras[Submuestras.IndexOf(SelectedSubmuestra!) - 1];
        }
    }

    private void RemoveItem(ItemSubmuestraRowViewModel? vm)
    {
        if (vm == null) return;

        var result = System.Windows.MessageBox.Show(
            $"¿Desea eliminar el ejemplar nro {vm.NroEjemplar}?",
            "Confirmar eliminación",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            if (!string.IsNullOrEmpty(vm.Item.ID))
            {
                _deletedIds.Add(vm.Item.ID);
            }
            Submuestras.Remove(vm);
        }
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrEmpty(SelectedMuestraId))
        {
            System.Windows.MessageBox.Show("Debe seleccionar una muestra.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (ValidateAll())
        {
            IsLoading = true;
            try
            {
                foreach (var id in _deletedIds)
                {
                    await _submuestraService.DeleteSubmuestraAsync(id);
                }

                foreach (var vm in Submuestras)
                {
                    // Asegurar que el MuestraID sea el correcto si se cambió en el dropdown antes de añadir ítems
                    vm.Item.MuestraID = SelectedMuestraId;
                    await _submuestraService.SaveSubmuestraAsync(vm.Item);
                }

                _onClose();
            }
            finally
            {
                IsLoading = false;
            }
        }
        else
        {
            System.Windows.MessageBox.Show(
                "Existen errores de validación en los ejemplares. Por favor, revise los datos (especialmente Repleción Gástrica debe estar entre 0 y 4).",
                "Error de Validación",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
    }

    private void Cancel() => _onClose();
}

public record SexoOption(int? Value, string Label);
public record MuestraSelectionOption(string Id, string DisplayLabel);

public sealed class ItemSubmuestraRowViewModel : ObservableObject
{
    public ItemSubmuestraRowViewModel(ItemSubmuestra item)
    {
        Item = item;
    }

    public ItemSubmuestra Item { get; }

    public int NroEjemplar
    {
        get => Item.NroEjemplar;
        set { if (Item.NroEjemplar != value) { Item.NroEjemplar = value; OnPropertyChanged(); } }
    }

    public int? Sexo
    {
        get => Item.Sexo;
        set { if (Item.Sexo != value) { Item.Sexo = value; OnPropertyChanged(); } }
    }

    public int? Estadio
    {
        get => Item.Estadio;
        set { if (Item.Estadio != value) { Item.Estadio = value; OnPropertyChanged(); } }
    }

    public int? LargoMm
    {
        get => Item.LargoTotalMm;
        set { if (Item.LargoTotalMm != value) { Item.LargoTotalMm = value; OnPropertyChanged(); } }
    }

    public double? PesoGramos
    {
        get => Item.PesoTotalGramos;
        set { if (Item.PesoTotalGramos != value) { Item.PesoTotalGramos = value; OnPropertyChanged(); } }
    }

    public int? ReplecionGastrica
    {
        get => Item.ReplecionGastrica;
        set { if (Item.ReplecionGastrica != value) { Item.ReplecionGastrica = value; OnPropertyChanged(); } }
    }

    public string? Comentarios
    {
        get => Item.Comentarios;
        set { if (Item.Comentarios != value) { Item.Comentarios = value; OnPropertyChanged(); } }
    }
}
