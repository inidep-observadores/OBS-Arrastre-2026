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
    private string? _comentarios;
    private int _origen;
    private double? _pesoMuestraGramos;
    private List<Especie> _allEspecies = new();

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
    public string? EspecieId { get => _especieId; set => SetProperty(ref _especieId, value); }
    public string? Comentarios { get => _comentarios; set => SetProperty(ref _comentarios, value); }
    public int Origen { get => _origen; set => SetProperty(ref _origen, value); }
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
                    Comentarios = muestra.Comentarios;
                    Origen = muestra.Origen;
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
        if (vm != null) FrecuenciasTallas.Remove(vm);
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
                    Comentarios = Comentarios,
                    Origen = Origen,
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
