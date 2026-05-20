using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using FluentValidation;
using OBSArrastre2026.App.Data.Entities;
using OBSArrastre2026.App.Models;
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
    private IReadOnlyList<Lance> _lancesInStage = new List<Lance>();
    
    // Initial values for change detection
    private int _initialNroLance;
    private DateTime _initialFecha;
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
    private List<(string? EspecieId, double DatoCaptura, TipoDatoCaptura TipoDatoCaptura, double DatoDescarte, TipoDatoDescarte TipoDatoDescarte)> _initialItems = new();

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
            Comentarios = lance.Comentarios;

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
        
        _initialItems = ItemsCaptura.Select(i => (
            EspecieId: i.SelectedEspecie?.ID, 
            i.DatoCaptura, 
            i.TipoDatoCaptura,
            i.DatoDescarte,
            i.TipoDatoDescarte)).ToList();
    }

    private bool HasChanges()
    {
        if (_initialNroLance != NroLance) return true;
        if (_initialFecha != Fecha) return true;
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
        
        if (_initialItems.Count != ItemsCaptura.Count) return true;
        for (int i = 0; i < _initialItems.Count; i++)
        {
            var initial = _initialItems[i];
            var current = ItemsCaptura[i];
            if (initial.EspecieId != current.SelectedEspecie?.ID || 
                initial.DatoCaptura != current.DatoCaptura || 
                initial.TipoDatoCaptura != current.TipoDatoCaptura ||
                initial.DatoDescarte != current.DatoDescarte ||
                initial.TipoDatoDescarte != current.TipoDatoDescarte) return true;
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
