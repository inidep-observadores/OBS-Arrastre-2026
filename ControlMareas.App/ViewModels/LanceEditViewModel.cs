using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using FluentValidation;
using ControlMareas.App.Data.Entities;
using ControlMareas.App.Models;
using ControlMareas.App.Services;

namespace ControlMareas.App.ViewModels;

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
    private DateTime? _fechaFin;
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
    private IReadOnlyList<Lance> _lancesInStage = new List<Lance>();

    // Palangre fields
    private bool _esPalangre;
    private string? _identificacion;
    private string? _tipoPalangre;
    private string? _horaInicioFilado;
    private string? _horaFinFilado;
    private double? _latitudInicioFilado;
    private double? _longitudInicioFilado;
    private double? _latitudFinFilado;
    private double? _longitudFinFilado;
    private int? _profundidadInicioFilado;
    private int? _profundidadFinFilado;
    private DateTime? _fechaVirado;
    private string? _horaInicioVirado;
    private string? _horaFinVirado;
    private double? _latitudInicioVirado;
    private double? _longitudInicioVirado;
    private double? _latitudFinVirado;
    private double? _longitudFinVirado;
    private int? _profundidadInicioVirado;
    private int? _profundidadFinVirado;
    private int? _rumboVirado;
    private string? _material;
    private int? _lmLargo;
    private int? _lmTramos;
    private int? _brzLargo;
    private int? _brzSeparacion;
    private int? _brzNxTramo;
    private string? _anzuelosTipo;
    private int? _anzuelosLargo;
    private int? _anzuelosAncho;
    private int? _anzuelosTotal;
    private int? _anzuelosConCarnada;
    private int? _anzuelosSinCarnada;
    private string? _carnada;
    
    // Initial values for change detection
    private int _initialNroLance;
    private DateTime _initialFecha;
    private DateTime? _initialFechaFin;
    private string? _initialHoraInicio;
    private string? _initialHoraFinal;
    private double? _initialLatitudInicio;
    private double? _initialLongitudInicio;
    private double? _initialLatitudFinal;
    private double? _initialLongitudFinal;
    private int? _initialProfundidadInicio;
    private int? _initialProfundidadFinal;
    private int? _initialVientoDireccion;
    private int? _initialVientoFuerza;
    private double? _initialTempAire;
    private double? _initialTempRed;
    private int? _initialPresionHpa;
    private double? _initialCapturaTotalKg;
    private double? _initialVelocidadArrastre;
    private int? _initialRumbo;
    private int? _initialMallaCopo;
    private int? _initialMallaAlas;
    private int? _initialCableFilado;
    private double? _initialAberturaVertical;
    private double? _initialDistanciaAlas;
    private double? _initialDistanciaPortones;
    private int? _initialEstadoTiempo;
    private int? _initialEstadoMar;
    private bool _initialSelectividad;
    private string? _initialComentarios;

    // Initial palangre values
    private string? _initialIdentificacion;
    private string? _initialTipoPalangre;
    private string? _initialHoraInicioFilado;
    private string? _initialHoraFinFilado;
    private double? _initialLatitudInicioFilado;
    private double? _initialLongitudInicioFilado;
    private double? _initialLatitudFinFilado;
    private double? _initialLongitudFinFilado;
    private int? _initialProfundidadInicioFilado;
    private int? _initialProfundidadFinFilado;
    private DateTime? _initialFechaVirado;
    private string? _initialHoraInicioVirado;
    private string? _initialHoraFinVirado;
    private double? _initialLatitudInicioVirado;
    private double? _initialLongitudInicioVirado;
    private double? _initialLatitudFinVirado;
    private double? _initialLongitudFinVirado;
    private int? _initialProfundidadInicioVirado;
    private int? _initialProfundidadFinVirado;
    private int? _initialRumboVirado;
    private string? _initialMaterial;
    private int? _initialLmLargo;
    private int? _initialLmTramos;
    private int? _initialBrzLargo;
    private int? _initialBrzSeparacion;
    private int? _initialBrzNxTramo;
    private string? _initialAnzuelosTipo;
    private int? _initialAnzuelosLargo;
    private int? _initialAnzuelosAncho;
    private int? _initialAnzuelosTotal;
    private int? _initialAnzuelosConCarnada;
    private int? _initialAnzuelosSinCarnada;
    private string? _initialCarnada;

    private List<(string? EspecieId, double DatoCaptura, TipoDatoCaptura TipoDatoCaptura, double DatoDescarte, TipoDatoDescarte TipoDatoDescarte, int? Piezas)> _initialItems = new();

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
        CancelCommand = new AsyncRelayCommand(CancelAsync);
        AddCatchItemCommand = new RelayCommand(AddCatchItem);
        MovePreviousCommand = new AsyncRelayCommand(MovePreviousAsync, () => CanMovePrevious);
        MoveNextCommand = new AsyncRelayCommand(MoveNextAsync, () => CanMoveNext);

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
    public DateTime? FechaFin { get => _fechaFin; set => SetProperty(ref _fechaFin, value); }
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

    // Propiedades de Palangre
    public bool EsPalangre { get => _esPalangre; set => SetProperty(ref _esPalangre, value); }
    public string? Identificacion { get => _identificacion; set => SetProperty(ref _identificacion, value); }
    public string? TipoPalangre { get => _tipoPalangre; set => SetProperty(ref _tipoPalangre, value); }

    public string? HoraInicioFilado 
    { 
        get => _horaInicioFilado; 
        set 
        { 
            if (SetProperty(ref _horaInicioFilado, value)) 
            { 
                HoraInicio = value; 
                OnPropertyChanged(nameof(Title)); 
            } 
        } 
    }

    public string? HoraFinFilado { get => _horaFinFilado; set => SetProperty(ref _horaFinFilado, value); }

    public double? LatitudInicioFiladoDecimal 
    { 
        get => _latitudInicioFilado; 
        set 
        { 
            if (SetProperty(ref _latitudInicioFilado, value)) 
                LatitudInicioDecimal = value; 
        } 
    }

    public double? LongitudInicioFiladoDecimal 
    { 
        get => _longitudInicioFilado; 
        set 
        { 
            if (SetProperty(ref _longitudInicioFilado, value)) 
                LongitudInicioDecimal = value; 
        } 
    }

    public double? LatitudFinFiladoDecimal { get => _latitudFinFilado; set => SetProperty(ref _latitudFinFilado, value); }
    public double? LongitudFinFiladoDecimal { get => _longitudFinFilado; set => SetProperty(ref _longitudFinFilado, value); }
    public int? ProfundidadInicioFiladoM { get => _profundidadInicioFilado; set => SetProperty(ref _profundidadInicioFilado, value); }
    public int? ProfundidadFinFiladoM { get => _profundidadFinFilado; set => SetProperty(ref _profundidadFinFilado, value); }

    public DateTime? FechaVirado 
    { 
        get => _fechaVirado; 
        set 
        { 
            if (SetProperty(ref _fechaVirado, value)) 
                FechaFin = value; 
        } 
    }

    public string? HoraInicioVirado { get => _horaInicioVirado; set => SetProperty(ref _horaInicioVirado, value); }

    public string? HoraFinVirado 
    { 
        get => _horaFinVirado; 
        set 
        { 
            if (SetProperty(ref _horaFinVirado, value)) 
                HoraFinal = value; 
        } 
    }

    public double? LatitudInicioViradoDecimal { get => _latitudInicioVirado; set => SetProperty(ref _latitudInicioVirado, value); }
    public double? LongitudInicioViradoDecimal { get => _longitudInicioVirado; set => SetProperty(ref _longitudInicioVirado, value); }

    public double? LatitudFinViradoDecimal 
    { 
        get => _latitudFinVirado; 
        set 
        { 
            if (SetProperty(ref _latitudFinVirado, value)) 
                LatitudFinalDecimal = value; 
        } 
    }

    public double? LongitudFinViradoDecimal 
    { 
        get => _longitudFinVirado; 
        set 
        { 
            if (SetProperty(ref _longitudFinVirado, value)) 
                LongitudFinalDecimal = value; 
        } 
    }

    public int? ProfundidadInicioViradoM 
    { 
        get => _profundidadInicioVirado; 
        set 
        { 
            if (SetProperty(ref _profundidadInicioVirado, value)) 
                RecalcularProfundidadResumen(); 
        } 
    }

    public int? ProfundidadFinViradoM 
    { 
        get => _profundidadFinVirado; 
        set 
        { 
            if (SetProperty(ref _profundidadFinVirado, value)) 
                RecalcularProfundidadResumen(); 
        } 
    }

    private void RecalcularProfundidadResumen()
    {
        if (_profundidadInicioVirado.HasValue && _profundidadFinVirado.HasValue)
        {
            var avg = (int)Math.Round((_profundidadInicioVirado.Value + _profundidadFinVirado.Value) / 2.0);
            ProfundidadInicioM = avg;
            ProfundidadFinalM = avg;
        }
        else if (_profundidadFinVirado.HasValue)
        {
            ProfundidadInicioM = _profundidadFinVirado;
            ProfundidadFinalM = _profundidadFinVirado;
        }
        else if (_profundidadInicioVirado.HasValue)
        {
            ProfundidadInicioM = _profundidadInicioVirado;
            ProfundidadFinalM = _profundidadInicioVirado;
        }
    }

    public int? RumboViradoGrados 
    { 
        get => _rumboVirado; 
        set 
        { 
            if (SetProperty(ref _rumboVirado, value)) 
                RumboGrados = value; 
        } 
    }

    public string? Material { get => _material; set => SetProperty(ref _material, value); }
    public int? LmLargoM { get => _lmLargo; set => SetProperty(ref _lmLargo, value); }
    public int? LmTramos { get => _lmTramos; set => SetProperty(ref _lmTramos, value); }
    public int? BrzLargoM { get => _brzLargo; set => SetProperty(ref _brzLargo, value); }
    public int? BrzSeparacionM { get => _brzSeparacion; set => SetProperty(ref _brzSeparacion, value); }
    public int? BrzNxTramo { get => _brzNxTramo; set => SetProperty(ref _brzNxTramo, value); }
    public string? AnzuelosTipo { get => _anzuelosTipo; set => SetProperty(ref _anzuelosTipo, value); }
    public int? AnzuelosLargoMm { get => _anzuelosLargo; set => SetProperty(ref _anzuelosLargo, value); }
    public int? AnzuelosAnchoMm { get => _anzuelosAncho; set => SetProperty(ref _anzuelosAncho, value); }
    public int? AnzuelosTotal { get => _anzuelosTotal; set => SetProperty(ref _anzuelosTotal, value); }
    public int? AnzuelosConCarnada { get => _anzuelosConCarnada; set => SetProperty(ref _anzuelosConCarnada, value); }
    public int? AnzuelosSinCarnada { get => _anzuelosSinCarnada; set => SetProperty(ref _anzuelosSinCarnada, value); }
    public string? Carnada { get => _carnada; set => SetProperty(ref _carnada, value); }

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
    public ICommand MovePreviousCommand { get; }
    public ICommand MoveNextCommand { get; }

    private bool _canMovePrevious;
    public bool CanMovePrevious { get => _canMovePrevious; set => SetProperty(ref _canMovePrevious, value); }

    private bool _canMoveNext;
    public bool CanMoveNext { get => _canMoveNext; set => SetProperty(ref _canMoveNext, value); }

    public IEnumerable<Especie> AllEspecies => _allEspecies;

    public Action<object?>? ShowCustomDialog { get; set; }
    public Func<string, string, Task<bool?>>? ShowConfirmation { get; set; }
    public Func<string, string, Task<bool?>>? ShowChoice { get; set; }
    public Action<string, string, string?, MessageDialogType>? ShowMessage { get; set; }

    private async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            var tipoMarea = await _lanceService.GetTipoMareaByEtapaIdAsync(_mareaEtapaId);
            EsPalangre = tipoMarea == TipoMarea.Palangre;

            _allEspecies = (await _lanceService.GetEspeciesAsync()).ToList();
            _lancesInStage = (await _lanceService.GetLancesAsync(mareaEtapaId: _mareaEtapaId))
                .OrderBy(l => l.NroLance)
                .ToList();

            if (_lanceId != null)
            {
                await LoadLanceDataAsync(_lanceId);
            }
            else
            {
                UpdateNavigationState();
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadLanceDataAsync(string lanceId)
    {
        var lance = await _lanceService.GetLanceAsync(lanceId);
        if (lance != null)
        {
            _lanceId = lanceId;
            if (lance.MareaEtapa?.Marea != null)
            {
                EsPalangre = lance.MareaEtapa.Marea.TipoMarea == TipoMarea.Palangre;
            }
            if (lance.DetallePalangre != null)
            {
                EsPalangre = true;
            }

            NroLance = lance.NroLance;
            if (DateTime.TryParse(lance.Fecha, out var date)) Fecha = date;
            FechaFin = lance.FechaFin;
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
            Comentarios = lance.Comentarios;

            // Cargar datos específicos de Palangre si existen
            if (lance.DetallePalangre != null)
            {
                var dp = lance.DetallePalangre;
                Identificacion = dp.Identificacion;
                TipoPalangre = dp.TipoPalangre;

                HoraInicioFilado = dp.HoraInicioFilado ?? lance.HoraInicio;
                HoraFinFilado = dp.HoraFinFilado;
                LatitudInicioFiladoDecimal = dp.LatitudInicioFiladoDecimal ?? lance.LatitudInicioDecimal;
                LongitudInicioFiladoDecimal = dp.LongitudInicioFiladoDecimal ?? lance.LongitudInicioDecimal;
                LatitudFinFiladoDecimal = dp.LatitudFinFiladoDecimal;
                LongitudFinFiladoDecimal = dp.LongitudFinFiladoDecimal;
                ProfundidadInicioFiladoM = dp.ProfundidadInicioFiladoM;
                ProfundidadFinFiladoM = dp.ProfundidadFinFiladoM;

                if (!string.IsNullOrEmpty(dp.FechaVirado) && DateTime.TryParse(dp.FechaVirado, out var dtVir))
                    FechaVirado = dtVir;
                else
                    FechaVirado = lance.FechaFin ?? Fecha;

                HoraInicioVirado = dp.HoraInicioVirado;
                HoraFinVirado = dp.HoraFinVirado ?? lance.HoraFinal;
                LatitudInicioViradoDecimal = dp.LatitudInicioViradoDecimal;
                LongitudInicioViradoDecimal = dp.LongitudInicioViradoDecimal;
                LatitudFinViradoDecimal = dp.LatitudFinViradoDecimal ?? lance.LatitudFinalDecimal;
                LongitudFinViradoDecimal = dp.LongitudFinViradoDecimal ?? lance.LongitudFinalDecimal;
                ProfundidadInicioViradoM = dp.ProfundidadInicioViradoM;
                ProfundidadFinViradoM = dp.ProfundidadFinViradoM;
                RumboViradoGrados = dp.RumboViradoGrados ?? lance.RumboGrados;

                Material = dp.MaterialLineaMadre;
                LmLargoM = dp.LargoLineaMadreM;
                LmTramos = dp.TramosLineaMadre;
                BrzLargoM = dp.LargoBrazoladaCm;
                BrzSeparacionM = dp.SeparacionBrazoladasM;
                BrzNxTramo = dp.NumeroBrazoladasPorTramo;
                AnzuelosTipo = dp.TipoAnzuelo;
                AnzuelosLargoMm = dp.LargoAnzueloMm;
                AnzuelosAnchoMm = dp.AnchoAnzueloMm;
                AnzuelosTotal = dp.AnzuelosTotal;
                AnzuelosConCarnada = dp.AnzuelosConCarnada;
                AnzuelosSinCarnada = dp.AnzuelosSinCarnada;
                Carnada = dp.Carnada;
            }
            else if (EsPalangre)
            {
                HoraInicioFilado = lance.HoraInicio;
                LatitudInicioFiladoDecimal = lance.LatitudInicioDecimal;
                LongitudInicioFiladoDecimal = lance.LongitudInicioDecimal;
                FechaVirado = lance.FechaFin ?? Fecha;
                HoraFinVirado = lance.HoraFinal;
                LatitudFinViradoDecimal = lance.LatitudFinalDecimal;
                LongitudFinViradoDecimal = lance.LongitudFinalDecimal;
                RumboViradoGrados = lance.RumboGrados;
            }

            ItemsCaptura.Clear();
            foreach (var item in lance.ItemsCaptura.OrderBy(i => i.NumeroOrden))
            {
                var vm = new CatchItemViewModel(item, _allEspecies);
                vm.RequestDeletion = HandleCatchItemDeletion;
                vm.NotifyParentOfWeightChange = NotificarCambioPesosEnItems;
                ItemsCaptura.Add(vm);
            }
            SincronizarOrden();
            
            CaptureInitialState();
            UpdateNavigationState();
        }
    }

    private void CaptureInitialState()
    {
        _initialNroLance = NroLance;
        _initialFecha = Fecha;
        _initialFechaFin = FechaFin;
        _initialHoraInicio = HoraInicio;
        _initialHoraFinal = HoraFinal;
        _initialLatitudInicio = LatitudInicioDecimal;
        _initialLongitudInicio = LongitudInicioDecimal;
        _initialLatitudFinal = LatitudFinalDecimal;
        _initialLongitudFinal = LongitudFinalDecimal;
        _initialProfundidadInicio = ProfundidadInicioM;
        _initialProfundidadFinal = ProfundidadFinalM;
        _initialVientoDireccion = VientoDireccionGrados;
        _initialVientoFuerza = VientoFuerzaBeaufort;
        _initialTempAire = TemperaturaAireC;
        _initialTempRed = TemperaturaRedC;
        _initialPresionHpa = PresionHpa;
        _initialCapturaTotalKg = CapturaTotalKg;
        _initialVelocidadArrastre = VelocidadArrastreNudos;
        _initialRumbo = RumboGrados;
        _initialMallaCopo = MallaCopoMm;
        _initialMallaAlas = MallaAlasMm;
        _initialCableFilado = CableFiladoM;
        _initialAberturaVertical = AberturaVerticalM;
        _initialDistanciaAlas = DistanciaAlasM;
        _initialDistanciaPortones = DistanciaPortonesM;
        _initialEstadoTiempo = EstadoTiempoCodigo;
        _initialEstadoMar = EstadoMarCodigo;
        _initialSelectividad = Selectividad;
        _initialComentarios = Comentarios;

        // Palangre
        _initialIdentificacion = Identificacion;
        _initialTipoPalangre = TipoPalangre;
        _initialHoraInicioFilado = HoraInicioFilado;
        _initialHoraFinFilado = HoraFinFilado;
        _initialLatitudInicioFilado = LatitudInicioFiladoDecimal;
        _initialLongitudInicioFilado = LongitudInicioFiladoDecimal;
        _initialLatitudFinFilado = LatitudFinFiladoDecimal;
        _initialLongitudFinFilado = LongitudFinFiladoDecimal;
        _initialProfundidadInicioFilado = ProfundidadInicioFiladoM;
        _initialProfundidadFinFilado = ProfundidadFinFiladoM;
        _initialFechaVirado = FechaVirado;
        _initialHoraInicioVirado = HoraInicioVirado;
        _initialHoraFinVirado = HoraFinVirado;
        _initialLatitudInicioVirado = LatitudInicioViradoDecimal;
        _initialLongitudInicioVirado = LongitudInicioViradoDecimal;
        _initialLatitudFinVirado = LatitudFinViradoDecimal;
        _initialLongitudFinVirado = LongitudFinViradoDecimal;
        _initialProfundidadInicioVirado = ProfundidadInicioViradoM;
        _initialProfundidadFinVirado = ProfundidadFinViradoM;
        _initialRumboVirado = RumboViradoGrados;
        _initialMaterial = Material;
        _initialLmLargo = LmLargoM;
        _initialLmTramos = LmTramos;
        _initialBrzLargo = BrzLargoM;
        _initialBrzSeparacion = BrzSeparacionM;
        _initialBrzNxTramo = BrzNxTramo;
        _initialAnzuelosTipo = AnzuelosTipo;
        _initialAnzuelosLargo = AnzuelosLargoMm;
        _initialAnzuelosAncho = AnzuelosAnchoMm;
        _initialAnzuelosTotal = AnzuelosTotal;
        _initialAnzuelosConCarnada = AnzuelosConCarnada;
        _initialAnzuelosSinCarnada = AnzuelosSinCarnada;
        _initialCarnada = Carnada;
        
        _initialItems = ItemsCaptura.Select(i => (
            EspecieId: i.SelectedEspecie?.ID, 
            i.DatoCaptura, 
            i.TipoDatoCaptura,
            i.DatoDescarte,
            i.TipoDatoDescarte,
            i.Piezas)).ToList();
    }

    private bool HasChanges()
    {
        if (_initialNroLance != NroLance) return true;
        if (_initialFecha != Fecha) return true;
        if (_initialFechaFin != FechaFin) return true;
        if (_initialHoraInicio != HoraInicio) return true;
        if (_initialHoraFinal != HoraFinal) return true;
        if (_initialLatitudInicio != LatitudInicioDecimal) return true;
        if (_initialLongitudInicio != LongitudInicioDecimal) return true;
        if (_initialLatitudFinal != LatitudFinalDecimal) return true;
        if (_initialLongitudFinal != LongitudFinalDecimal) return true;
        if (_initialProfundidadInicio != ProfundidadInicioM) return true;
        if (_initialProfundidadFinal != ProfundidadFinalM) return true;
        if (_initialVientoDireccion != VientoDireccionGrados) return true;
        if (_initialVientoFuerza != VientoFuerzaBeaufort) return true;
        if (_initialTempAire != TemperaturaAireC) return true;
        if (_initialTempRed != TemperaturaRedC) return true;
        if (_initialPresionHpa != PresionHpa) return true;
        if (_initialCapturaTotalKg != CapturaTotalKg) return true;
        if (_initialVelocidadArrastre != VelocidadArrastreNudos) return true;
        if (_initialRumbo != RumboGrados) return true;
        if (_initialMallaCopo != MallaCopoMm) return true;
        if (_initialMallaAlas != MallaAlasMm) return true;
        if (_initialCableFilado != CableFiladoM) return true;
        if (_initialAberturaVertical != AberturaVerticalM) return true;
        if (_initialDistanciaAlas != DistanciaAlasM) return true;
        if (_initialDistanciaPortones != DistanciaPortonesM) return true;
        if (_initialEstadoTiempo != EstadoTiempoCodigo) return true;
        if (_initialEstadoMar != EstadoMarCodigo) return true;
        if (_initialSelectividad != Selectividad) return true;
        if (_initialComentarios != Comentarios) return true;

        if (EsPalangre)
        {
            if (_initialIdentificacion != Identificacion) return true;
            if (_initialTipoPalangre != TipoPalangre) return true;
            if (_initialHoraInicioFilado != HoraInicioFilado) return true;
            if (_initialHoraFinFilado != HoraFinFilado) return true;
            if (_initialLatitudInicioFilado != LatitudInicioFiladoDecimal) return true;
            if (_initialLongitudInicioFilado != LongitudInicioFiladoDecimal) return true;
            if (_initialLatitudFinFilado != LatitudFinFiladoDecimal) return true;
            if (_initialLongitudFinFilado != LongitudFinFiladoDecimal) return true;
            if (_initialProfundidadInicioFilado != ProfundidadInicioFiladoM) return true;
            if (_initialProfundidadFinFilado != ProfundidadFinFiladoM) return true;
            if (_initialFechaVirado != FechaVirado) return true;
            if (_initialHoraInicioVirado != HoraInicioVirado) return true;
            if (_initialHoraFinVirado != HoraFinVirado) return true;
            if (_initialLatitudInicioVirado != LatitudInicioViradoDecimal) return true;
            if (_initialLongitudInicioVirado != LongitudInicioViradoDecimal) return true;
            if (_initialLatitudFinVirado != LatitudFinViradoDecimal) return true;
            if (_initialLongitudFinVirado != LongitudFinViradoDecimal) return true;
            if (_initialProfundidadInicioVirado != ProfundidadInicioViradoM) return true;
            if (_initialProfundidadFinVirado != ProfundidadFinViradoM) return true;
            if (_initialRumboVirado != RumboViradoGrados) return true;
            if (_initialMaterial != Material) return true;
            if (_initialLmLargo != LmLargoM) return true;
            if (_initialLmTramos != LmTramos) return true;
            if (_initialBrzLargo != BrzLargoM) return true;
            if (_initialBrzSeparacion != BrzSeparacionM) return true;
            if (_initialBrzNxTramo != BrzNxTramo) return true;
            if (_initialAnzuelosTipo != AnzuelosTipo) return true;
            if (_initialAnzuelosLargo != AnzuelosLargoMm) return true;
            if (_initialAnzuelosAncho != AnzuelosAnchoMm) return true;
            if (_initialAnzuelosTotal != AnzuelosTotal) return true;
            if (_initialAnzuelosConCarnada != AnzuelosConCarnada) return true;
            if (_initialAnzuelosSinCarnada != AnzuelosSinCarnada) return true;
            if (_initialCarnada != Carnada) return true;
        }
        
        if (_initialItems.Count != ItemsCaptura.Count) return true;
        for (int i = 0; i < _initialItems.Count; i++)
        {
            var initial = _initialItems[i];
            var current = ItemsCaptura[i];
            if (initial.EspecieId != current.SelectedEspecie?.ID || 
                initial.DatoCaptura != current.DatoCaptura || 
                initial.TipoDatoCaptura != current.TipoDatoCaptura ||
                initial.DatoDescarte != current.DatoDescarte ||
                initial.TipoDatoDescarte != current.TipoDatoDescarte ||
                initial.Piezas != current.Piezas) return true;
        }
        
        return false;
    }

    private void UpdateNavigationState()
    {
        if (string.IsNullOrEmpty(_lanceId))
        {
            CanMovePrevious = false;
            CanMoveNext = false;
        }
        else
        {
            var currentIndex = _lancesInStage.ToList().FindIndex(l => l.Id == _lanceId);
            CanMovePrevious = currentIndex > 0;
            CanMoveNext = currentIndex >= 0 && currentIndex < _lancesInStage.Count - 1;
        }
        
        (MovePreviousCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
        (MoveNextCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
    }

    private async Task MovePreviousAsync()
    {
        if (!await PromptSaveIfDirtyAsync()) return;

        var currentIndex = _lancesInStage.ToList().FindIndex(l => l.Id == _lanceId);
        if (currentIndex > 0)
        {
            IsLoading = true;
            try
            {
                await LoadLanceDataAsync(_lancesInStage[currentIndex - 1].Id);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    private async Task MoveNextAsync()
    {
        if (!await PromptSaveIfDirtyAsync()) return;

        var currentIndex = _lancesInStage.ToList().FindIndex(l => l.Id == _lanceId);
        if (currentIndex >= 0 && currentIndex < _lancesInStage.Count - 1)
        {
            IsLoading = true;
            try
            {
                await LoadLanceDataAsync(_lancesInStage[currentIndex + 1].Id);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    private async Task<bool> PromptSaveIfDirtyAsync()
    {
        if (!HasChanges()) return true;

        var confirm = await (ShowConfirmation?.Invoke("Cambios pendientes", "El lance actual tiene cambios sin guardar. ¿Desea guardarlos antes de cambiar de registro?") ?? Task.FromResult<bool?>(false));
        if (confirm == true)
        {
            return await SaveInternalAsync();
        }
        
        return true; // Continuar sin guardar si el usuario elige no guardar (o si el diálogo falla)
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
        if (await SaveInternalAsync())
        {
            _onClose();
        }
    }

    private async Task<bool> SaveInternalAsync()
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
                    FechaFin = FechaFin,
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
                    SelectividadSiNo = Selectividad ? 1 : 0,
                    Comentarios = Comentarios
                };

                if (EsPalangre)
                {
                    lance.DetallePalangre = new LancePalangre
                    {
                        LanceId = lance.Id,
                        Identificacion = Identificacion,
                        TipoPalangre = TipoPalangre,
                        HoraInicioFilado = HoraInicioFilado ?? HoraInicio,
                        HoraFinFilado = HoraFinFilado,
                        LatitudInicioFiladoDecimal = LatitudInicioFiladoDecimal ?? LatitudInicioDecimal,
                        LongitudInicioFiladoDecimal = LongitudInicioFiladoDecimal ?? LongitudInicioDecimal,
                        LatitudFinFiladoDecimal = LatitudFinFiladoDecimal,
                        LongitudFinFiladoDecimal = LongitudFinFiladoDecimal,
                        ProfundidadInicioFiladoM = ProfundidadInicioFiladoM,
                        ProfundidadFinFiladoM = ProfundidadFinFiladoM,
                        FechaVirado = FechaVirado?.ToString("yyyy-MM-dd") ?? FechaFin?.ToString("yyyy-MM-dd"),
                        HoraInicioVirado = HoraInicioVirado,
                        HoraFinVirado = HoraFinVirado ?? HoraFinal,
                        LatitudInicioViradoDecimal = LatitudInicioViradoDecimal,
                        LongitudInicioViradoDecimal = LongitudInicioViradoDecimal,
                        LatitudFinViradoDecimal = LatitudFinViradoDecimal ?? LatitudFinalDecimal,
                        LongitudFinViradoDecimal = LongitudFinViradoDecimal ?? LongitudFinalDecimal,
                        ProfundidadInicioViradoM = ProfundidadInicioViradoM,
                        ProfundidadFinViradoM = ProfundidadFinViradoM,
                        RumboViradoGrados = RumboViradoGrados ?? RumboGrados,
                        MaterialLineaMadre = Material,
                        LargoLineaMadreM = LmLargoM,
                        TramosLineaMadre = LmTramos,
                        LargoBrazoladaCm = BrzLargoM,
                        SeparacionBrazoladasM = BrzSeparacionM,
                        NumeroBrazoladasPorTramo = BrzNxTramo,
                        TipoAnzuelo = AnzuelosTipo,
                        LargoAnzueloMm = AnzuelosLargoMm,
                        AnchoAnzueloMm = AnzuelosAnchoMm,
                        AnzuelosTotal = AnzuelosTotal,
                        AnzuelosConCarnada = AnzuelosConCarnada,
                        AnzuelosSinCarnada = AnzuelosSinCarnada,
                        Carnada = Carnada,
                        Observaciones = Comentarios
                    };

                    lance.HoraInicio = HoraInicioFilado ?? HoraInicio;
                    lance.HoraFinal = HoraFinVirado ?? HoraFinal;
                    lance.LatitudInicioDecimal = LatitudInicioFiladoDecimal ?? LatitudInicioDecimal;
                    lance.LongitudInicioDecimal = LongitudInicioFiladoDecimal ?? LongitudInicioDecimal;
                    lance.LatitudFinalDecimal = LatitudFinViradoDecimal ?? LatitudFinalDecimal;
                    lance.LongitudFinalDecimal = LongitudFinViradoDecimal ?? LongitudFinalDecimal;
                    lance.FechaFin = FechaVirado ?? FechaFin;
                    if (ProfundidadInicioViradoM.HasValue && ProfundidadFinViradoM.HasValue)
                    {
                        var avgProf = (int)Math.Round((ProfundidadInicioViradoM.Value + ProfundidadFinViradoM.Value) / 2.0);
                        lance.ProfundidadInicioM = avgProf;
                        lance.ProfundidadFinalM = avgProf;
                    }
                }

                foreach (var itemVm in ItemsCaptura)
                {
                    var item = itemVm.ToEntity();
                    item.LanceID = lance.Id;
                    lance.ItemsCaptura.Add(item);
                }

                await _lanceService.SaveLanceAsync(lance);
                
                // Refresh internal state after save
                _lanceId = lance.Id;
                CaptureInitialState();
                
                return true;
            }
            catch (Exception ex)
            {
                var message = ex.Message;
                if (ex.InnerException != null)
                {
                    message += $"\n\nDetalle técnico: {ex.InnerException.Message}";
                }
                ShowMessage?.Invoke("Error", $"No se pudo guardar el lance: {message}", null, MessageDialogType.Error);
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }
        return false;
    }

    private async Task CancelAsync()
    {
        if (HasChanges())
        {
            var result = await (ShowChoice?.Invoke("Cambios pendientes", "¿Desea guardar los cambios realizados en el lance antes de salir?") ?? Task.FromResult<bool?>(null));
            
            if (result == true) // Guardar
            {
                if (await SaveInternalAsync())
                {
                    _onClose();
                }
            }
            else if (result == false) // No Guardar
            {
                _onClose();
            }
            // else (null) -> Volver, no hacer nada
        }
        else
        {
            _onClose();
        }
    }
}
