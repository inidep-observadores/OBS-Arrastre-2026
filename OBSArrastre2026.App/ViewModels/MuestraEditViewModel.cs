using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using FluentValidation;
using OBSArrastre2026.App.Data.Entities;
using OBSArrastre2026.App.Services;

namespace OBSArrastre2026.App.ViewModels;

public sealed class MuestraEditViewModel : ValidatableViewModelBase<MuestraEditViewModel>
{
    private readonly IMuestraService _muestraService;
    private readonly ILanceService _lanceService;
    private readonly Action _onClose;
    private readonly string _lanceId;
    private string? _muestraId;
    private bool _isLoading;

    private string? _especieId;
    private double? _pesoMuestraGramos;
    private List<Especie> _allEspecies = new();
    
    private bool _isExpanded;
    private string _searchText = string.Empty;
    private Especie? _selectedEspecie;

    public MuestraEditViewModel(
        Action onClose,
        IValidator<MuestraEditViewModel> validator,
        IMuestraService muestraService,
        ILanceService lanceService,
        string lanceId,
        string? muestraId = null) : base(validator)
    {
        _onClose = onClose;
        _muestraService = muestraService;
        _lanceService = lanceService;
        _lanceId = lanceId;
        _muestraId = muestraId;

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(Cancel);
        AddFrecuenciaCommand = new RelayCommand(AddFrecuencia);
        RemoveFrecuenciaCommand = new RelayCommand<FrecuenciaTallaViewModel>(RemoveFrecuencia);

        _ = InitializeAsync();
    }

    public ObservableCollection<FrecuenciaTallaViewModel> FrecuenciasTallas { get; } = new();

    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public List<Especie> Especies => _allEspecies;
    
    public string? EspecieId 
    { 
        get => _especieId; 
        set 
        {
            if (SetProperty(ref _especieId, value))
            {
                if (value != null && SelectedEspecie?.ID != value)
                {
                    SelectedEspecie = _allEspecies.FirstOrDefault(e => e.ID == value);
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
                    string currentFull = SelectedEspecie != null ? $"{SelectedEspecie.NombreVulgar} ({SelectedEspecie.NombreCientifico})" : string.Empty;
                    
                    if (value != currentName && value != currentFull)
                    {
                        IsExpanded = true;
                    }
                }
            }
        }
    }

    public Especie? SelectedEspecie
    {
        get => _selectedEspecie;
        set
        {
            if (SetProperty(ref _selectedEspecie, value))
            {
                _especieId = value?.ID;
                OnPropertyChanged(nameof(EspecieId));
                OnPropertyChanged(nameof(EsLangostino));
                
                if (value != null)
                {
                    _searchText = value.NombreVulgar;
                    OnPropertyChanged(nameof(SearchText));
                    IsExpanded = false;
                }
            }
        }
    }

    public bool EsLangostino => SelectedEspecie?.CodigoInidep == "5139030101";

    public IEnumerable<Especie> FilteredEspecies
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SearchText) || (SelectedEspecie != null && SearchText == $"{SelectedEspecie.NombreVulgar} ({SelectedEspecie.NombreCientifico})"))
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

    public double? PesoMuestraGramos { get => _pesoMuestraGramos; set => SetProperty(ref _pesoMuestraGramos, value); }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand AddFrecuenciaCommand { get; }
    public ICommand RemoveFrecuenciaCommand { get; }

    private async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            _allEspecies = (await _lanceService.GetEspeciesAsync()).ToList();
            OnPropertyChanged(nameof(Especies));

            if (_muestraId != null)
            {
                var muestra = await _muestraService.GetMuestraAsync(_muestraId);
                if (muestra != null)
                {
                    EspecieId = muestra.EspecieID;
                    PesoMuestraGramos = muestra.PesoMuestra_PesoGramos;

                    FrecuenciasTallas.Clear();
                    foreach (var f in muestra.FrecuenciasTallas.OrderBy(x => x.Talla))
                    {
                        FrecuenciasTallas.Add(new FrecuenciaTallaViewModel(f));
                    }
                }
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void AddFrecuencia()
    {
        var lastTalla = FrecuenciasTallas.LastOrDefault()?.Talla ?? 0;
        FrecuenciasTallas.Add(new FrecuenciaTallaViewModel { Talla = lastTalla + 1 });
    }

    private void RemoveFrecuencia(FrecuenciaTallaViewModel? vm)
    {
        if (vm == null) return;

        var result = System.Windows.MessageBox.Show(
            "¿Desea eliminar esta frecuencia de talla?",
            "Confirmar eliminación",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            FrecuenciasTallas.Remove(vm);
        }
    }

    private async Task SaveAsync()
    {
        if (ValidateAll())
        {
            IsLoading = true;
            try
            {
                var muestra = new Muestra
                {
                    ID = _muestraId ?? Guid.NewGuid().ToString(),
                    LanceID = _lanceId,
                    EspecieID = EspecieId,
                    PesoMuestra_PesoGramos = PesoMuestraGramos
                };

                foreach (var fVm in FrecuenciasTallas)
                {
                    var f = fVm.ToEntity();
                    f.MuestraID = muestra.ID;
                    muestra.FrecuenciasTallas.Add(f);
                }

                await _muestraService.SaveMuestraAsync(muestra);
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
