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
    private readonly Action _onClose;
    private readonly string _muestraId;
    private bool _isLoading;

    private string _especieNombre = string.Empty;
    private string _pesoMuestraDisplay = string.Empty;

    public SubmuestraEditViewModel(
        Action onClose,
        IValidator<SubmuestraEditViewModel> validator,
        ISubmuestraService submuestraService,
        IMuestraService muestraService,
        string muestraId) : base(validator)
    {
        _onClose = onClose;
        _submuestraService = submuestraService;
        _muestraService = muestraService;
        _muestraId = muestraId;

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(Cancel);
        AddItemCommand = new RelayCommand(AddItem);
        RemoveItemCommand = new RelayCommand<ItemSubmuestraRowViewModel>(RemoveItem);

        _ = InitializeAsync();
    }

    public ObservableCollection<ItemSubmuestraRowViewModel> Submuestras { get; } = new();
    
    public List<SexoOption> SexoOptions { get; } = new()
    {
        new SexoOption(1, "Macho"),
        new SexoOption(2, "Hembra"),
        new SexoOption(3, "Indet.")
    };

    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public string EspecieNombre { get => _especieNombre; set => SetProperty(ref _especieNombre, value); }
    public string PesoMuestraDisplay { get => _pesoMuestraDisplay; set => SetProperty(ref _pesoMuestraDisplay, value); }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand AddItemCommand { get; }
    public ICommand RemoveItemCommand { get; }

    private async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            var muestra = await _muestraService.GetMuestraAsync(_muestraId);
            if (muestra != null)
            {
                EspecieNombre = muestra.Especie?.NombreVulgar ?? "Sin Especie";
                PesoMuestraDisplay = muestra.PesoMuestra_PesoGramos.HasValue 
                    ? $"{(muestra.PesoMuestra_PesoGramos.Value / 1000.0):N2} kg" 
                    : "0.00 kg";

                var subs = await _submuestraService.GetSubmuestrasByMuestraIdAsync(_muestraId);
                Submuestras.Clear();
                foreach (var s in subs)
                {
                    Submuestras.Add(new ItemSubmuestraRowViewModel(s));
                }
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void AddItem()
    {
        var lastNro = Submuestras.LastOrDefault()?.NroEjemplar ?? 0;
        var newItem = new ItemSubmuestra { MuestraID = _muestraId, NroEjemplar = lastNro + 1 };
        var vm = new ItemSubmuestraRowViewModel(newItem);
        Submuestras.Add(vm);
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
            Submuestras.Remove(vm);
        }
    }

    private async Task SaveAsync()
    {
        if (ValidateAll())
        {
            IsLoading = true;
            try
            {
                // En un escenario real, borraríamos las que ya no están y guardaríamos las nuevas.
                // Para este MVP, vamos a guardar todas las actuales.
                // Nota: El servicio debería manejar la persistencia masiva.
                
                foreach (var vm in Submuestras)
                {
                    await _submuestraService.SaveSubmuestraAsync(vm.Item);
                }

                _onClose();
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    private void Cancel() => _onClose();
}

public record SexoOption(int? Value, string Label);

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
}
