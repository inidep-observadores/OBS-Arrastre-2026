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
    private Muestra? _originalMuestra;

    private string? _especieId;
    private double? _pesoMuestraKg;
    private List<Especie> _allEspecies = new();
    private IReadOnlyList<EspecieLargoPeso> _parametrosActuales = new List<EspecieLargoPeso>();
    
    private bool _isExpanded;
    private string _searchText = string.Empty;
    private Especie? _selectedEspecie;
    private int _tipoMuestra = 1;
    private FrecuenciaTallaViewModel? _selectedFrecuencia;

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
        GoToNextCommand = new RelayCommand(GoToNext, () => CanGoToNext);
        GoToPreviousCommand = new RelayCommand(GoToPrevious, () => CanGoToPrevious);

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
                    _ = LoadParametrosAsync(value.ID);
                }
            }
        }
    }

    private async Task LoadParametrosAsync(string? especieId)
    {
        if (string.IsNullOrEmpty(especieId))
        {
            _parametrosActuales = new List<EspecieLargoPeso>();
        }
        else
        {
            _parametrosActuales = await _muestraService.GetParametrosAlometricosAsync(especieId);
        }

        foreach (var f in FrecuenciasTallas)
        {
            f.SetParametros(_parametrosActuales);
        }
    }

    public FrecuenciaTallaViewModel? SelectedFrecuencia
    {
        get => _selectedFrecuencia;
        set
        {
            if (SetProperty(ref _selectedFrecuencia, value))
            {
                (GoToNextCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (GoToPreviousCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public int TipoMuestra { get => _tipoMuestra; set => SetProperty(ref _tipoMuestra, value); }

    public List<KeyValuePair<int, string>> SampleTypes { get; } = new()
    {
        new(1, "Estándar"),
        new(2, "Descarte")
    };

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

    public double? PesoMuestraKg { get => _pesoMuestraKg; set => SetProperty(ref _pesoMuestraKg, value); }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand AddFrecuenciaCommand { get; }
    public ICommand RemoveFrecuenciaCommand { get; }
    public ICommand GoToNextCommand { get; }
    public ICommand GoToPreviousCommand { get; }

    public bool CanGoToNext => SelectedFrecuencia != null && FrecuenciasTallas.IndexOf(SelectedFrecuencia) < FrecuenciasTallas.Count - 1;
    public bool CanGoToPrevious => SelectedFrecuencia != null && FrecuenciasTallas.IndexOf(SelectedFrecuencia) > 0;

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
                    _originalMuestra = muestra;
                    EspecieId = muestra.EspecieID;
                    PesoMuestraKg = muestra.PesoMuestra_PesoGramos.HasValue ? Math.Round(muestra.PesoMuestra_PesoGramos.Value / 1000.0, 2) : null;
                    TipoMuestra = muestra.TipoMuestra;

                    FrecuenciasTallas.Clear();
                    foreach (var f in muestra.FrecuenciasTallas.OrderBy(x => x.Talla))
                    {
                        var vm = new FrecuenciaTallaViewModel(f);
                        vm.SetParametros(_parametrosActuales);
                        FrecuenciasTallas.Add(vm);
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
        var next = new FrecuenciaTallaViewModel { Talla = lastTalla + 1 };
        next.SetParametros(_parametrosActuales);
        FrecuenciasTallas.Add(next);
        SelectedFrecuencia = next;
    }

    private void GoToNext()
    {
        if (SelectedFrecuencia == null) return;
        int index = FrecuenciasTallas.IndexOf(SelectedFrecuencia);
        if (index < FrecuenciasTallas.Count - 1)
        {
            SelectedFrecuencia = FrecuenciasTallas[index + 1];
        }
    }

    private void GoToPrevious()
    {
        if (SelectedFrecuencia == null) return;
        int index = FrecuenciasTallas.IndexOf(SelectedFrecuencia);
        if (index > 0)
        {
            SelectedFrecuencia = FrecuenciasTallas[index - 1];
        }
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
                Muestra muestra;
                if (_originalMuestra != null)
                {
                    muestra = _originalMuestra;
                    muestra.EspecieID = EspecieId;
                    muestra.PesoMuestra_PesoGramos = PesoMuestraKg.HasValue ? Math.Round(PesoMuestraKg.Value * 1000.0, 2) : null;
                    muestra.TipoMuestra = TipoMuestra;
                    muestra.FrecuenciasTallas.Clear();
                }
                else
                {
                    muestra = new Muestra
                    {
                        ID = Guid.NewGuid().ToString(),
                        LanceID = _lanceId,
                        EspecieID = EspecieId,
                        PesoMuestra_PesoGramos = PesoMuestraKg.HasValue ? Math.Round(PesoMuestraKg.Value * 1000.0, 2) : null,
                        TipoMuestra = TipoMuestra,
                        
                        // Valores obligatorios para muestra nueva
                        Intervalo = 1.0, // por defecto 1 cm
                        UnidadMedidaTalla = 1, // CM
                        ModoMedicionTalla = 1, // LT
                        Origen = 1, // Muestreo de Captura
                        NumeroOrden = 1 // Por defecto
                    };
                }

                // Recalcular Flags de Sexo y Totales para la muestra
                int totalEjemplares = 0;
                bool tieneMachosOHembras = false;
                bool tieneIndeterminados = false;

                foreach (var fVm in FrecuenciasTallas)
                {
                    var f = fVm.ToEntity();
                    f.MuestraID = muestra.ID;
                    muestra.FrecuenciasTallas.Add(f);

                    totalEjemplares += f.NroTotal;
                    if (f.NroMachos > 0 || f.NroHembras > 0) tieneMachosOHembras = true;
                    if (f.NroIndeterminados > 0) tieneIndeterminados = true;
                }

                muestra.DiscriminaSexo = tieneMachosOHembras ? 1 : 0;
                muestra.HayIndeterminados = tieneIndeterminados ? 1 : 0;

                if (PesoMuestraKg.HasValue && PesoMuestraKg.Value > 0)
                {
                    muestra.EjemplaresPorKg = (int)Math.Round(totalEjemplares / PesoMuestraKg.Value);
                }
                else
                {
                    muestra.EjemplaresPorKg = 0;
                }

                await _muestraService.SaveMuestraAsync(muestra);
                _onClose();
            }
            catch (Exception ex)
            {
                var message = "No se pudo guardar la muestra: " + ex.Message;
                if (ex.InnerException != null)
                {
                    message += "\n\nDetalle técnico: " + ex.InnerException.Message;
                }
                System.Windows.MessageBox.Show(message, "Error al guardar", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    private void Cancel() => _onClose();
}
