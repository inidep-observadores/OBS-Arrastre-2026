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

public sealed class LanceEditViewModel : ValidatableViewModelBase<LanceEditViewModel>
{
    private readonly ILanceService _lanceService;
    private readonly Action _onClose;
    private string? _lanceId;
    private bool _isLoading;
    private readonly string _mareaEtapaId;
    private List<Especie> _allEspecies = new();

    private int _nroLance;
    private DateTime _fecha;
    private string? _horaInicio;
    private string? _horaFinal;
    private double? _latitudInicio;
    private double? _longitudInicio;
    private double? _latitudFinal;
    private double? _longitudFinal;
    private int? _profundidadInicio;
    private int? _profundidadFinal;
    private int? _vientoDireccion;
    private int? _vientoFuerza;
    private double? _tempAire;
    private double? _tempRed;
    private int? _presionHpa;
    private double? _capturaTotalKg;
    private double? _velocidadArrastre;
    private int? _rumbo;
    private int? _mallaCopo;
    private int? _mallaAlas;
    private int? _cableFilado;
    private double? _aberturaVertical;
    private double? _distanciaAlas;
    private double? _distanciaPortones;
    private int? _estadoTiempo;
    private int? _estadoMar;
    private bool _selectividad;
    private string? _comentarios;
    private CatchItemViewModel? _selectedCatchItem;

    public LanceEditViewModel(
        Action onClose,
        IValidator<LanceEditViewModel> validator,
        ILanceService lanceService,
        string mareaEtapaId,
        string? lanceId = null) : base(validator)
    {
        _onClose = onClose;
        _lanceService = lanceService;
        _mareaEtapaId = mareaEtapaId;
        _lanceId = lanceId;
        _fecha = DateTime.Today;

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(Cancel);
        AddCatchItemCommand = new RelayCommand(AddCatchItem);

        _ = InitializeAsync();
    }

    public ObservableCollection<CatchItemViewModel> ItemsCaptura { get; } = new();

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string Title
    {
        get
        {
            return $"Lance N° {NroLance} del {Fecha:dd/MM/yyyy} a las {HoraInicio ?? "--:--" }";
        }
    }

    public int NroLance { get => _nroLance; set { if (SetProperty(ref _nroLance, value)) OnPropertyChanged(nameof(Title)); } }
    public DateTime Fecha { get => _fecha; set { if (SetProperty(ref _fecha, value)) OnPropertyChanged(nameof(Title)); } }
    public string? HoraInicio { get => _horaInicio; set { if (SetProperty(ref _horaInicio, value)) OnPropertyChanged(nameof(Title)); } }
    public string? HoraFinal { get => _horaFinal; set => SetProperty(ref _horaFinal, value); }
    
    public double? LatitudInicioDecimal { get => _latitudInicio; set => SetProperty(ref _latitudInicio, value); }
    public double? LongitudInicioDecimal { get => _longitudInicio; set => SetProperty(ref _longitudInicio, value); }
    public double? LatitudFinalDecimal { get => _latitudFinal; set => SetProperty(ref _latitudFinal, value); }
    public double? LongitudFinalDecimal { get => _longitudFinal; set => SetProperty(ref _longitudFinal, value); }

    public int? ProfundidadInicioM { get => _profundidadInicio; set => SetProperty(ref _profundidadInicio, value); }
    public int? ProfundidadFinalM { get => _profundidadFinal; set => SetProperty(ref _profundidadFinal, value); }

    public int? VientoDireccionGrados { get => _vientoDireccion; set => SetProperty(ref _vientoDireccion, value); }
    public int? VientoFuerzaBeaufort { get => _vientoFuerza; set => SetProperty(ref _vientoFuerza, value); }

    public double? TemperaturaAireC { get => _tempAire; set => SetProperty(ref _tempAire, value); }
    public double? TemperaturaRedC { get => _tempRed; set => SetProperty(ref _tempRed, value); }

    public int? PresionHpa { get => _presionHpa; set => SetProperty(ref _presionHpa, value); }
    public double? CapturaTotalKg 
    { 
        get => _capturaTotalKg; 
        set 
        { 
            if (SetProperty(ref _capturaTotalKg, value))
            {
                NotificarCambioPesosEnItems();
            }
        } 
    }

    public double? VelocidadArrastreNudos { get => _velocidadArrastre; set => SetProperty(ref _velocidadArrastre, value); }
    public int? RumboGrados { get => _rumbo; set => SetProperty(ref _rumbo, value); }

    public int? MallaCopoMm { get => _mallaCopo; set => SetProperty(ref _mallaCopo, value); }
    public int? MallaAlasMm { get => _mallaAlas; set => SetProperty(ref _mallaAlas, value); }
    public int? CableFiladoM { get => _cableFilado; set => SetProperty(ref _cableFilado, value); }
    public double? AberturaVerticalM { get => _aberturaVertical; set => SetProperty(ref _aberturaVertical, value); }
    public double? DistanciaAlasM { get => _distanciaAlas; set => SetProperty(ref _distanciaAlas, value); }
    public double? DistanciaPortonesM { get => _distanciaPortones; set => SetProperty(ref _distanciaPortones, value); }
    public int? EstadoTiempoCodigo { get => _estadoTiempo; set => SetProperty(ref _estadoTiempo, value); }
    public int? EstadoMarCodigo { get => _estadoMar; set => SetProperty(ref _estadoMar, value); }

    public bool Selectividad { get => _selectividad; set => SetProperty(ref _selectividad, value); }
    public string? Comentarios { get => _comentarios; set => SetProperty(ref _comentarios, value); }

    public CatchItemViewModel? SelectedCatchItem
    {
        get => _selectedCatchItem;
        set
        {
            if (SetProperty(ref _selectedCatchItem, value))
            {
                OnPropertyChanged(nameof(HasSelectedCatchItem));
            }
        }
    }

    public bool HasSelectedCatchItem => SelectedCatchItem != null;

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand AddCatchItemCommand { get; }

    public IEnumerable<Especie> AllEspecies => _allEspecies;

    public Action<object?>? ShowCustomDialog { get; set; }
    public Func<string, string, Task<bool>>? ShowConfirmation { get; set; }
    public Action<string, string, string?, MessageDialogType>? ShowMessage { get; set; }

    private async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            _allEspecies = (await _lanceService.GetEspeciesAsync()).ToList();

            if (_lanceId != null)
            {
                var lance = await _lanceService.GetLanceAsync(_lanceId);
                if (lance != null)
                {
                    NroLance = lance.NroLance;
                    if (DateTime.TryParse(lance.Fecha, out var date)) Fecha = date;
                    HoraInicio = lance.HoraInicio;
                    HoraFinal = lance.HoraFinal;
                    LatitudInicioDecimal = lance.LatitudInicioDecimal;
                    LongitudInicioDecimal = lance.LongitudInicioDecimal;
                    LatitudFinalDecimal = lance.LatitudFinalDecimal;
                    LongitudFinalDecimal = lance.LongitudFinalDecimal;
                    ProfundidadInicioM = lance.ProfundidadInicioM;
                    ProfundidadFinalM = lance.ProfundidadFinalM;
                    EstadoTiempoCodigo = lance.EstadoTiempoCodigo;
                    EstadoMarCodigo = lance.EstadoMarCodigo;
                    VientoDireccionGrados = lance.VientoDireccionGrados;
                    VientoFuerzaBeaufort = lance.VientoFuerzaBeaufort;
                    TemperaturaAireC = lance.TemperaturaAireC;
                    TemperaturaRedC = lance.TemperaturaRedC;
                    PresionHpa = lance.PresionHpa;
                    CapturaTotalKg = lance.CapturaTotalKg;
                    VelocidadArrastreNudos = lance.VelocidadArrastreNudos;
                    RumboGrados = lance.RumboGrados;
                    MallaCopoMm = lance.MallaCopoMm;
                    MallaAlasMm = lance.MallaAlasMm;
                    CableFiladoM = lance.CableFiladoM;
                    AberturaVerticalM = lance.AberturaVerticalM;
                    DistanciaAlasM = lance.DistanciaAlasM;
                    DistanciaPortonesM = lance.DistanciaPortonesM;
                    Selectividad = lance.SelectividadSiNo == 1;

                    ItemsCaptura.Clear();
                    foreach (var item in lance.ItemsCaptura.OrderBy(i => i.NumeroOrden))
                    {
                        var vm = new CatchItemViewModel(item, _allEspecies);
                        vm.RequestDeletion = HandleCatchItemDeletion;
                        vm.NotifyParentOfWeightChange = NotificarCambioPesosEnItems;
                        ItemsCaptura.Add(vm);

                    }
                    SincronizarOrden();
                }
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void AddCatchItem()
    {
        var newItem = new ItemCaptura
        {
            NumeroOrden = ItemsCaptura.Count + 1,
            TipoDatoCaptura = 0,
            TipoDatoDescarte = 0
        };
        var vm = new CatchItemViewModel(newItem, _allEspecies)
        {
            RequestDeletion = HandleCatchItemDeletion,
            NotifyParentOfWeightChange = NotificarCambioPesosEnItems
        };

        ItemsCaptura.Add(vm);
        SelectedCatchItem = vm;
        SincronizarOrden();
    }

    private void HandleCatchItemDeletion(CatchItemViewModel vm)
    {
        ItemsCaptura.Remove(vm);
        SincronizarOrden();
    }

    private void SincronizarOrden()
    {
        for (int i = 0; i < ItemsCaptura.Count; i++)
        {
            ItemsCaptura[i].NumeroOrden = i + 1;
        }
    }

    private void NotificarCambioPesosEnItems()
    {
        foreach (var item in ItemsCaptura)
        {
            item.NotifyCalculatedWeightsChanged();
        }
    }


    private async Task SaveAsync()
    {
        if (ValidateAll())
        {
            IsLoading = true;
            try
            {
                var lance = new Lance
                {
                    Id = _lanceId ?? Guid.NewGuid().ToString(),
                    MareaEtapaId = _mareaEtapaId,
                    NroLance = NroLance,
                    Fecha = Fecha.ToString("yyyy-MM-dd"),
                    HoraInicio = HoraInicio,
                    HoraFinal = HoraFinal,
                    LatitudInicioDecimal = LatitudInicioDecimal,
                    LongitudInicioDecimal = LongitudInicioDecimal,
                    LatitudFinalDecimal = LatitudFinalDecimal,
                    LongitudFinalDecimal = LongitudFinalDecimal,
                    ProfundidadInicioM = ProfundidadInicioM,
                    ProfundidadFinalM = ProfundidadFinalM,
                    EstadoTiempoCodigo = EstadoTiempoCodigo,
                    EstadoMarCodigo = EstadoMarCodigo,
                    VientoDireccionGrados = VientoDireccionGrados,
                    VientoFuerzaBeaufort = VientoFuerzaBeaufort,
                    TemperaturaAireC = TemperaturaAireC,
                    TemperaturaRedC = TemperaturaRedC,
                    PresionHpa = PresionHpa,
                    CapturaTotalKg = CapturaTotalKg,
                    VelocidadArrastreNudos = VelocidadArrastreNudos,
                    RumboGrados = RumboGrados,
                    MallaCopoMm = MallaCopoMm,
                    MallaAlasMm = MallaAlasMm,
                    CableFiladoM = CableFiladoM,
                    AberturaVerticalM = AberturaVerticalM,
                    DistanciaAlasM = DistanciaAlasM,
                    DistanciaPortonesM = DistanciaPortonesM,
                    SelectividadSiNo = Selectividad ? 1 : 0
                };

                foreach (var itemVm in ItemsCaptura)
                {
                    var item = itemVm.ToEntity();
                    item.LanceID = lance.Id;
                    lance.ItemsCaptura.Add(item);
                }

                await _lanceService.SaveLanceAsync(lance);
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
