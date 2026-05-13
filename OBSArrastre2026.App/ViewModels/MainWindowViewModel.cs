using System;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OBSArrastre2026.App.Models;
using OBSArrastre2026.App.Services;
using OBSArrastre2026.App.Data.Entities;
using Microsoft.EntityFrameworkCore;
using OBSArrastre2026.App.Data;
using OBSArrastre2026.App.Services.Internal;
using GMap.NET;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Data;
using OBSArrastre2026.App.Models.Reports;

namespace OBSArrastre2026.App.ViewModels;

public class MainWindowViewModel : ObservableObject
{
    private readonly IMockShellDataService _mockShellDataService;
    private readonly IThemeService _themeService;
    private readonly IMareaService _mareaService;
    private readonly IBuqueService _buqueService;
    private readonly ILanceService _lanceService;
    private readonly IMuestraService _muestraService;
    private readonly Func<Action, string?, MareaEditViewModel> _mareaEditFactory;
    private readonly Func<Action, string, string?, LanceEditViewModel> _lanceEditFactory;
    private readonly Func<Action, string, string?, MuestraEditViewModel> _muestraEditFactory;
    private readonly Func<Action, string, SubmuestraEditViewModel> _submuestraEditFactory;
    private readonly Func<Action, string?, ProduccionEditViewModel> _produccionEditFactory;
    private readonly ISubmuestraService _submuestraService;
    private readonly IActiveMareaManager _activeMareaManager;
    private readonly IUserSettingsService _userSettingsService;
    private readonly IMareaValidationService _validationService;
    private readonly IMareaReportService _reportService;
    private readonly IProduccionService _produccionService;
    private readonly IJsonImportService _jsonImportService;
    private readonly IMareaImportService _mareaImportService;
    private readonly IMapRenderingService _mapRenderingService;
    private readonly IExcelReportService _excelReportService;
    private readonly IMareaSummaryService _mareaSummaryService;
    private readonly IDbfExporterService _dbfExporterService;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private NavigationItemViewModel? _selectedNavigationItem;
    private string _pageTitle = string.Empty;
    private string _pageDescription = string.Empty;
    private string _pageEyebrow = string.Empty;
    private string _primaryActionLabel = string.Empty;
    private AppThemeMode _currentThemeMode;
    private object? _currentEditViewModel;
    private object? _activeDialog;
    private ProcesosViewModel? _procesosVM;
    private ConfiguracionViewModel? _configuracionVM;
    private HashSet<string> _commonRayaIds = new();
    private const string RayaGenericVirtualId = "RAYA_GENERICA_GRUPO";
    
    private bool IsRaya(Especie? e)
    {
        var codigo = e?.CodigoInidep?.Trim();
        return codigo != null && codigo.Length >= 5 && codigo.StartsWith("71090");
    }

    private bool IsGenericRaya(Especie? e)
    {
        var codigo = e?.CodigoInidep?.Trim();
        return codigo == "7109000000" || codigo == "71090000000";
    }
    
    // Datos para GMap.NET
    public List<MareaTracking> CurrentTrack { get; private set; } = new();
    public List<Lance> CurrentLances { get; private set; } = new();
    public event Action? MapUpdateRequested;
    public event Action<List<PointLatLng>>? MapFocusRequested;

    private object? _selectedRecord;
    private ControlProduccionListItemViewModel? _selectedControlItem;

    public ControlProduccionListItemViewModel? SelectedControlItem
    {
        get => _selectedControlItem;
        set => SetProperty(ref _selectedControlItem, value);
    }

    private string? _controlProduccionSelectedEspecieId;
    public string? ControlProduccionSelectedEspecieId
    {
        get => _controlProduccionSelectedEspecieId;
        private set
        {
            if (SetProperty(ref _controlProduccionSelectedEspecieId, value))
            {
                OnPropertyChanged(nameof(IsControlProduccionDetailVisible));
            }
        }
    }

    private Especie? _controlProduccionSelectedEspecie;
    public Especie? ControlProduccionSelectedEspecie
    {
        get => _controlProduccionSelectedEspecie;
        private set => SetProperty(ref _controlProduccionSelectedEspecie, value);
    }

    public bool IsControlProduccionDetailVisible => SelectedNavigationItem?.Section == NavigationSection.ControlProduccion && !string.IsNullOrEmpty(ControlProduccionSelectedEspecieId);

    public ObservableCollection<ControlLanceDetailViewModel> ControlLanceDetails { get; } = new();

    private double _totalCapturaKg;
    public double TotalCapturaKg
    {
        get => _totalCapturaKg;
        private set => SetProperty(ref _totalCapturaKg, value);
    }

    private double _totalDescarteKg;
    public double TotalDescarteKg
    {
        get => _totalDescarteKg;
        private set => SetProperty(ref _totalDescarteKg, value);
    }

    private double _totalRetenidaKg;
    public double TotalRetenidaKg
    {
        get => _totalRetenidaKg;
        private set => SetProperty(ref _totalRetenidaKg, value);
    }


    // Filtros de Mareas
    private int? _mareasFilterAnio;
    private BuqueListItemViewModel? _mareasFilterBuque;
    private DateTime? _mareasFilterFechaDesde;
    private DateTime? _mareasFilterFechaHasta;
    private string _mareasSearchText = string.Empty;

    // Filtros de Control Producción
    private bool _controlSoloConDiferencias;
    public bool ControlSoloConDiferencias
    {
        get => _controlSoloConDiferencias;
        set
        {
            if (SetProperty(ref _controlSoloConDiferencias, value))
            {
                RecordsView.Refresh();
            }
        }
    }
    
    // Filtros de Lances
    private bool _showTrackLine = true;
    public bool ShowTrackLine
    {
        get => _showTrackLine;
        set
        {
            if (SetProperty(ref _showTrackLine, value))
            {
                MapUpdateRequested?.Invoke();
                OnPropertyChanged(nameof(IsTrackVisibilitySliderEnabled));
            }
        }
    }

    private bool _showTrackPoints = true;
    public bool ShowTrackPoints
    {
        get => _showTrackPoints;
        set
        {
            if (SetProperty(ref _showTrackPoints, value))
            {
                MapUpdateRequested?.Invoke();
                OnPropertyChanged(nameof(IsTrackVisibilitySliderEnabled));
            }
        }
    }

    private double _trackVisibilityWindowDays = 0;
    public double TrackVisibilityWindowDays
    {
        get => _trackVisibilityWindowDays;
        set
        {
            if (SetProperty(ref _trackVisibilityWindowDays, value))
            {
                MapUpdateRequested?.Invoke();
                OnPropertyChanged(nameof(TrackVisibilityWindowDaysLabel));
            }
        }
    }

    private int _trackVisibilitySliderValue = 0;
    public int TrackVisibilitySliderValue
    {
        get => _trackVisibilitySliderValue;
        set
        {
            if (SetProperty(ref _trackVisibilitySliderValue, value))
            {
                // Mapeo: 0->0, 1->0.25, 2->0.5, 3->1, 4->2, 5->3, 6->5
                TrackVisibilityWindowDays = value switch
                {
                    1 => 0.25,
                    2 => 0.5,
                    3 => 1.0,
                    4 => 2.0,
                    5 => 3.0,
                    6 => 5.0,
                    _ => 0.0
                };
            }
        }
    }

    public string TrackVisibilityWindowDaysLabel
    {
        get
        {
            if (TrackVisibilityWindowDays == 0) return "(Todo)";
            if (TrackVisibilityWindowDays < 1)
            {
                double horas = TrackVisibilityWindowDays * 24;
                return $"{horas:G29} hs";
            }
            string unit = TrackVisibilityWindowDays == 1 ? "día" : "días";
            return $"{TrackVisibilityWindowDays:G29} {unit}";
        }
    }

    public bool IsTrackVisibilitySliderEnabled => ShowTrackLine || ShowTrackPoints;

    private bool _shouldZoomOnNextUpdate;
    public bool ShouldZoomOnNextUpdate
    {
        get => _shouldZoomOnNextUpdate;
        set => SetProperty(ref _shouldZoomOnNextUpdate, value);
    }

    private DateTime? _lancesFilterFechaDesde;
    private DateTime? _lancesFilterFechaHasta;
    private int? _lancesFilterNroLance;
    private string _lancesFilterEspecie = string.Empty;
    private int _currentTrackPointIndex = -1;
    private bool _isPlaying;
    private System.Windows.Threading.DispatcherTimer? _playbackTimer;
    private string _selectedTrackPointInfo = string.Empty;
    private DateTime? _selectedPlaybackDate;
    
    private double? _mouseLatitude;
    public double? MouseLatitude
    {
        get => _mouseLatitude;
        set
        {
            if (SetProperty(ref _mouseLatitude, value))
            {
                OnPropertyChanged(nameof(MouseLatitudeDisplay));
            }
        }
    }

    private double? _mouseLongitude;
    public double? MouseLongitude
    {
        get => _mouseLongitude;
        set
        {
            if (SetProperty(ref _mouseLongitude, value))
            {
                OnPropertyChanged(nameof(MouseLongitudeDisplay));
            }
        }
    }

    public string MouseLatitudeDisplay => FormatCoordinate(MouseLatitude, true);
    public string MouseLongitudeDisplay => FormatCoordinate(MouseLongitude, false);


    public MainWindowViewModel(
        IMockShellDataService mockShellDataService, 
        IThemeService themeService,
        IMareaService mareaService,
        IBuqueService buqueService,
        ILanceService lanceService,
        IMuestraService muestraService,
        IActiveMareaManager activeMareaManager,
        Func<Action, string?, MareaEditViewModel> mareaEditFactory,
        Func<Action, string, string?, LanceEditViewModel> lanceEditFactory,
        Func<Action, string, string?, MuestraEditViewModel> muestraEditFactory,
        Func<Action, string, SubmuestraEditViewModel> submuestraEditFactory,
        ISubmuestraService submuestraService,
        IProduccionService produccionService,
        Func<Action, string?, ProduccionEditViewModel> produccionEditFactory,
        IMareaValidationService validationService,
        IMareaReportService reportService,
        IJsonImportService jsonImportService,
        IMareaImportService mareaImportService,
        IMapRenderingService mapRenderingService,
        IExcelReportService excelReportService,
        IMareaSummaryService mareaSummaryService,
        IUserSettingsService userSettingsService,
        IDbfExporterService dbfExporterService,
        IDbContextFactory<AppDbContext> dbContextFactory,
        ConfiguracionViewModel configuracionViewModel)
    {
        _mockShellDataService = mockShellDataService;
        _themeService = themeService;
        _mareaService = mareaService;
        _buqueService = buqueService;
        _lanceService = lanceService;
        _muestraService = muestraService;
        _activeMareaManager = activeMareaManager;
        _mareaEditFactory = mareaEditFactory;
        _lanceEditFactory = lanceEditFactory;
        _muestraEditFactory = muestraEditFactory;
        _submuestraEditFactory = submuestraEditFactory;
        _validationService = validationService;
        _reportService = reportService;
        _dbContextFactory = dbContextFactory;
        _submuestraService = submuestraService;
        _produccionService = produccionService;
        _produccionEditFactory = produccionEditFactory;
        _jsonImportService = jsonImportService;
        _mareaImportService = mareaImportService;
        _mapRenderingService = mapRenderingService;
        _excelReportService = excelReportService;
        _mareaSummaryService = mareaSummaryService;
        _userSettingsService = userSettingsService;
        _dbfExporterService = dbfExporterService;
        _configuracionVM = configuracionViewModel;

        SearchPlaceholder = "Buscar...";
        SetSystemThemeCommand = new RelayCommand(() => ApplyTheme(AppThemeMode.System));
        SetLightThemeCommand = new RelayCommand(() => ApplyTheme(AppThemeMode.Light));
        SetDarkThemeCommand = new RelayCommand(() => ApplyTheme(AppThemeMode.Dark));
        PrimaryActionCommand = new RelayCommand(OpenCreateMareaForm);
        ApplyMareaFiltersCommand = new AsyncCommand(LoadMareasAsync);
        EditMareaCommand = new RelayCommand<MareaListItemViewModel>(OpenEditMareaForm);
        ImportMareaCommand = new AsyncRelayCommand(ImportMareaAsync);
        
        ApplyLanceFiltersCommand = new AsyncCommand(LoadLancesAsync);
        EditLanceCommand = new RelayCommand<LanceListItemViewModel>(OpenEditLanceForm);
        EditMuestraCommand = new RelayCommand<MuestraListItemViewModel>(OpenEditMuestraForm);
        EditSubmuestraCommand = new RelayCommand<MuestraListItemViewModel>(OpenEditSubmuestraForm);
        OpenSelectedRecordEditCommand = new RelayCommand(OpenSelectedRecordEdit);
        ClearMareaFiltersCommand = new AsyncCommand(ClearMareaFiltersAsync);
        ClearLanceFiltersCommand = new AsyncCommand(ClearLanceFiltersAsync);
        DeleteRecordCommand = new AsyncRelayCommand<object>(DeleteRecordAsync);

        NavigateToControlProduccionDetailCommand = new AsyncRelayCommand<ControlProduccionListItemViewModel>(NavigateToControlProduccionDetailAsync);
        BackControlProduccionCommand = new RelayCommand(BackToControlProduccionSummary);
        EditLanceFromDetailCommand = new RelayCommand<ControlLanceDetailViewModel>(OpenEditLanceFromDetail);
        ExportControlProduccionPdfCommand = new AsyncRelayCommand(ExportControlProduccionPdfAsync);

        var settings = _userSettingsService.GetSettings();
        _mareasFilterAnio = settings.LastSelectedMareaAnio ?? DateTime.Today.Year;
        TrackVisibilitySliderValue = 1; // 0.25 días por defecto

        foreach (var navigationItem in _mockShellDataService.GetNavigationItems())
        {
            NavigationItems.Add(navigationItem);
        }

        NavigationItems.Add(new NavigationItemViewModel(NavigationSection.Separator, "", "", ""));
        NavigationItems.Add(new NavigationItemViewModel(NavigationSection.Procesos, "Procesos", "Lanzador de procesos", "⚡", true));
        NavigationItems.Add(new NavigationItemViewModel(NavigationSection.Separator, "", "", ""));
        NavigationItems.Add(ConfiguracionNavigationItem);

        _procesosVM = new ProcesosViewModel(
            new AsyncRelayCommand(OpenGenerarRecursosInformeAsync),
            new AsyncRelayCommand(OpenGenerarRecibiPdfAsync),
            new AsyncRelayCommand(OpenExportarDbfAsync),
            new AsyncRelayCommand(OpenConfigurarUnidadDescarteAsync)
        );

        _currentThemeMode = _themeService.CurrentMode;
        SelectedNavigationItem = NavigationItems.FirstOrDefault();

        _ = LoadFilterDataAsync();

        _activeMareaManager.PropertyChanged += (s, e) => 
        {
            if (e.PropertyName == nameof(IActiveMareaManager.ActiveMareaId))
            {
                OnPropertyChanged(nameof(ActiveMareaManager));
                UpdateNavigationState();
                _ = RefreshCurrentSectionAsync();
                _ = UpdateMapDataAsync();
                StopPlayback();
            }
        };

        PlayCommand = new RelayCommand(StartPlayback, () => !IsPlaying && CurrentTrack.Any());
        PauseCommand = new RelayCommand(PausePlayback, () => IsPlaying);
        StopCommand = new RelayCommand(StopPlayback, () => CurrentTrack.Any());
        NextPointCommand = new RelayCommand(() => CurrentTrackPointIndex++, () => CurrentTrack.Any() && CurrentTrackPointIndex < CurrentTrack.Count - 1);
        PrevPointCommand = new RelayCommand(() => CurrentTrackPointIndex--, () => CurrentTrack.Any() && CurrentTrackPointIndex > 0);
        GoToStartCommand = new RelayCommand(() => CurrentTrackPointIndex = 0, () => CurrentTrack.Any());
        GoToEndCommand = new RelayCommand(() => CurrentTrackPointIndex = CurrentTrack.Count - 1, () => CurrentTrack.Any());

        UpdateNavigationState();
        RecordsView = CollectionViewSource.GetDefaultView(Records);
    }


    private void UpdateNavigationState()
    {
        bool hasActiveMarea = !string.IsNullOrEmpty(_activeMareaManager.ActiveMareaId);

        foreach (var item in NavigationItems)
        {
            if (item.RequiresActiveMarea)
            {
                item.IsEnabled = hasActiveMarea;
            }
        }

        // Si estamos en una sección que ahora está deshabilitada, redirigir a Mareas
        if (!hasActiveMarea && SelectedNavigationItem != null && SelectedNavigationItem.RequiresActiveMarea)
        {
            SelectedNavigationItem = NavigationItems.FirstOrDefault(x => x.Section == NavigationSection.Mareas);
        }
    }

    public IActiveMareaManager ActiveMareaManager => _activeMareaManager;

    public string Title => "Control de mareas";

    public ICommand DeleteRecordCommand { get; }

    public string SearchPlaceholder { get; }

    public ObservableCollection<NavigationItemViewModel> NavigationItems { get; } = [];

    public NavigationItemViewModel ConfiguracionNavigationItem { get; } = new(NavigationSection.Configuracion, "Configuración", "Preferencias del sistema", "⚙", false);

    public ObservableCollection<string> ActiveFilters { get; } = [];

    public ObservableCollection<object> Records { get; } = [];
    public ICollectionView RecordsView { get; }

    public ObservableCollection<BuqueListItemViewModel> Buques { get; } = [];

    public ObservableCollection<int?> Anios { get; } = [];

    public ICommand SetSystemThemeCommand { get; }

    public ICommand SetLightThemeCommand { get; }

    public ICommand SetDarkThemeCommand { get; }

    public ICommand PrimaryActionCommand { get; private set; }

    public ICommand EditMareaCommand { get; }
    public ICommand ApplyMareaFiltersCommand { get; }
    public ICommand ApplyLanceFiltersCommand { get; }
    public ICommand EditLanceCommand { get; }
    public ICommand EditMuestraCommand { get; }
    public ICommand EditSubmuestraCommand { get; }
    public ICommand OpenSelectedRecordEditCommand { get; }
    public ICommand ClearMareaFiltersCommand { get; }
    public ICommand ClearLanceFiltersCommand { get; }
    public ICommand ImportMareaCommand { get; }
    public ICommand NavigateToControlProduccionDetailCommand { get; }
    public ICommand BackControlProduccionCommand { get; }
    public ICommand EditLanceFromDetailCommand { get; }
    public ICommand ExportControlProduccionPdfCommand { get; }
    
    public ICommand PlayCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand NextPointCommand { get; }
    public ICommand PrevPointCommand { get; }
    public ICommand GoToStartCommand { get; }
    public ICommand GoToEndCommand { get; }

    private async Task LoadMuestrasAsync()
    {
        var activeMarea = _activeMareaManager.ActiveMarea;
        if (activeMarea == null)
        {
            Records.Clear();
            return;
        }

        // Obtener lances de la marea activa para filtrar muestras
        var lances = await _lanceService.GetLancesAsync(mareaId: activeMarea.ID);
        var samples = new List<Muestra>();
        foreach (var lance in lances)
        {
            var lanceSamples = await _muestraService.GetMuestrasAsync(lance.Id);
            samples.AddRange(lanceSamples);
        }

        var viewModels = samples.Select(m => new MuestraListItemViewModel(m)).ToList();
        
        RecordsView.GroupDescriptions.Clear();
        Records.Clear();
        foreach (var vm in viewModels)
        {
            Records.Add(vm);
        }
    }

    private async Task LoadProduccionAsync()
    {
        var activeMarea = _activeMareaManager.ActiveMarea;
        if (activeMarea == null)
        {
            Records.Clear();
            return;
        }

        // Obtener registros de producción de todas las etapas de la marea activa
        var produccion = new List<RegistroProduccion>();
        foreach (var etapa in activeMarea.Etapas)
        {
            var etapaProduccion = await _produccionService.GetRegistrosProduccionAsync(etapa.ID);
            produccion.AddRange(etapaProduccion);
        }

        var viewModels = produccion
            .OrderBy(p => p.Fecha)
            .Select(p => new ProduccionListItemViewModel(p))
            .ToList();
        
        RecordsView.GroupDescriptions.Clear();
        Records.Clear();
        foreach (var vm in viewModels)
        {
            Records.Add(vm);
        }
    }

    private void OpenSelectedRecordEdit()
    {
        if (SelectedRecord is MareaListItemViewModel mareaVm)
            OpenEditMareaForm(mareaVm);
        else if (SelectedRecord is LanceListItemViewModel lanceVm)
            OpenEditLanceForm(lanceVm);
        else if (SelectedRecord is MuestraListItemViewModel muestraVm)
        {
            if (SelectedNavigationItem?.Section == NavigationSection.Submuestras)
                OpenEditSubmuestraForm(muestraVm);
            else
                OpenEditMuestraForm(muestraVm);
        }
        else if (SelectedRecord is ProduccionListItemViewModel prodVm)
        {
            OpenEditProduccionForm(prodVm);
            return;
        }
    }

    private void OpenEditSubmuestraForm(MuestraListItemViewModel? vm)
    {
        if (vm == null) return;
        CurrentEditViewModel = _submuestraEditFactory(() => { CurrentEditViewModel = null; _ = LoadSubmuestrasAsync(); }, vm.Muestra.ID);
    }

    private void OpenCreateProduccionForm()
    {
        CurrentEditViewModel = _produccionEditFactory(() => {
            CurrentEditViewModel = null;
            _ = LoadProduccionAsync();
        }, null);
    }

    private void OpenEditProduccionForm(ProduccionListItemViewModel vm)
    {
        CurrentEditViewModel = _produccionEditFactory(() => {
            CurrentEditViewModel = null;
            _ = LoadProduccionAsync();
        }, vm.Registro.Id);
    }

    private async Task LoadSubmuestrasAsync()
    {
        var activeMareaId = _activeMareaManager.ActiveMareaId;
        if (string.IsNullOrEmpty(activeMareaId))
        {
            Records.Clear();
            return;
        }

        var muestras = await _submuestraService.GetMuestrasConSubmuestrasAsync(activeMareaId);
        var viewModels = muestras.Select(m => new MuestraListItemViewModel(m)).ToList();

        Records.Clear();
        foreach (var vm in viewModels)
        {
            Records.Add(vm);
        }
    }

    private void OpenEditMuestraForm(MuestraListItemViewModel? vm)
    {
        if (vm == null) return;
        
        if (SelectedNavigationItem?.Section == NavigationSection.Submuestras)
        {
            OpenEditSubmuestraForm(vm);
            return;
        }

        CurrentEditViewModel = _muestraEditFactory(() => { CurrentEditViewModel = null; _ = LoadMuestrasAsync(); }, vm.Muestra.LanceID!, vm.Muestra.ID);
    }

    private void OpenCreateMuestraForm()
    {
        var activeMarea = _activeMareaManager.ActiveMarea;
        if (activeMarea == null) return;

        _ = Task.Run(async () => {
            var lances = await _lanceService.GetLancesAsync(mareaId: activeMarea.ID);
            var lastLance = lances.OrderByDescending(l => l.NroLance).FirstOrDefault();
            if (lastLance != null)
            {
                App.Current.Dispatcher.Invoke(() => {
                    CurrentEditViewModel = _muestraEditFactory(() => { CurrentEditViewModel = null; _ = LoadMuestrasAsync(); }, lastLance.Id, null);
                });
            }
            else
            {
                App.Current.Dispatcher.Invoke(() => {
                    ShowMessage("Sin Lances", "Debe existir al menos un lance registrado en la marea para poder crear una muestra.", null, MessageDialogType.Warning);
                });
            }
        });
    }

    public NavigationItemViewModel? SelectedNavigationItem
    {
        get => _selectedNavigationItem;
        set
        {
            if (value != null && !value.IsEnabled)
            {
                return;
            }

            if (value?.Section == NavigationSection.Separator)
            {
                return;
            }

            if (!SetProperty(ref _selectedNavigationItem, value) || value is null)
            {
                return;
            }

            LoadSection(value.Section);
        }
    }

    public string PageTitle
    {
        get => _pageTitle;
        private set => SetProperty(ref _pageTitle, value);
    }

    public string PageDescription
    {
        get => _pageDescription;
        private set => SetProperty(ref _pageDescription, value);
    }

    public string PageEyebrow
    {
        get => _pageEyebrow;
        private set => SetProperty(ref _pageEyebrow, value);
    }

    public string PrimaryActionLabel
    {
        get => _primaryActionLabel;
        private set => SetProperty(ref _primaryActionLabel, value);
    }

    private ReemplazoEspecieViewModel? _reemplazoEspecieVM;
    public ReemplazoEspecieViewModel? ReemplazoEspecieVM
    {
        get => _reemplazoEspecieVM;
        private set => SetProperty(ref _reemplazoEspecieVM, value);
    }

    public ConfiguracionViewModel? ConfiguracionVM
    {
        get => _configuracionVM;
        private set => SetProperty(ref _configuracionVM, value);
    }

    public int? MareasFilterAnio
    {
        get => _mareasFilterAnio;
        set
        {
            if (SetProperty(ref _mareasFilterAnio, value))
            {
                _userSettingsService.UpdateSettings(s => s.LastSelectedMareaAnio = value);
                _ = LoadMareasAsync();
            }
        }
    }

    public BuqueListItemViewModel? MareasFilterBuque
    {
        get => _mareasFilterBuque;
        set
        {
            if (SetProperty(ref _mareasFilterBuque, value))
            {
                _ = LoadMareasAsync();
            }
        }
    }

    public DateTime? MareasFilterFechaDesde
    {
        get => _mareasFilterFechaDesde;
        set
        {
            if (SetProperty(ref _mareasFilterFechaDesde, value))
            {
                _ = LoadMareasAsync();
            }
        }
    }

    public DateTime? MareasFilterFechaHasta
    {
        get => _mareasFilterFechaHasta;
        set
        {
            if (SetProperty(ref _mareasFilterFechaHasta, value))
            {
                _ = LoadMareasAsync();
            }
        }
    }

    public string MareasSearchText
    {
        get => _mareasSearchText;
        set
        {
            if (SetProperty(ref _mareasSearchText, value))
            {
                _ = LoadMareasAsync();
            }
        }
    }

    public DateTime? LancesFilterFechaDesde
    {
        get => _lancesFilterFechaDesde;
        set
        {
            if (SetProperty(ref _lancesFilterFechaDesde, value))
            {
                _ = LoadLancesAsync();
            }
        }
    }

    public DateTime? LancesFilterFechaHasta
    {
        get => _lancesFilterFechaHasta;
        set
        {
            if (SetProperty(ref _lancesFilterFechaHasta, value))
            {
                _ = LoadLancesAsync();
            }
        }
    }

    public int? LancesFilterNroLance
    {
        get => _lancesFilterNroLance;
        set
        {
            if (SetProperty(ref _lancesFilterNroLance, value))
            {
                _ = LoadLancesAsync();
            }
        }
    }

    public string LancesFilterEspecie
    {
        get => _lancesFilterEspecie;
        set
        {
            if (SetProperty(ref _lancesFilterEspecie, value))
            {
                _ = LoadLancesAsync();
            }
        }
    }

    public object? SelectedRecord
    {
        get => _selectedRecord;
        set
        {
            if (SetProperty(ref _selectedRecord, value))
            {
                OnRecordSelected(value);
            }
        }
    }

    public object? CurrentEditViewModel
    {
        get => _currentEditViewModel;
        private set => SetProperty(ref _currentEditViewModel, value);
    }

    public object? ActiveDialog
    {
        get => _activeDialog;
        private set => SetProperty(ref _activeDialog, value);
    }

    public ProcesosViewModel? ProcesosVM
    {
        get => _procesosVM;
        private set => SetProperty(ref _procesosVM, value);
    }

    public int CurrentTrackPointIndex
    {
        get => _currentTrackPointIndex;
        set
        {
            if (SetProperty(ref _currentTrackPointIndex, value))
            {
                UpdateSelectedTrackPoint();
                OnPropertyChanged(nameof(CurrentPlaybackPoint));
                
                // Sincronizar fecha del DatePicker sin disparar JumpToDate de nuevo
                if (value >= 0 && value < CurrentTrack.Count)
                {
                    _selectedPlaybackDate = CurrentTrack[value].FechaHora.Date;
                    OnPropertyChanged(nameof(SelectedPlaybackDate));
                }

                ((RelayCommand)NextPointCommand).RaiseCanExecuteChanged();
                ((RelayCommand)PrevPointCommand).RaiseCanExecuteChanged();
                ((RelayCommand)GoToStartCommand).RaiseCanExecuteChanged();
                ((RelayCommand)GoToEndCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public MareaTracking? CurrentPlaybackPoint => 
        (CurrentTrackPointIndex >= 0 && CurrentTrackPointIndex < CurrentTrack.Count) 
        ? CurrentTrack[CurrentTrackPointIndex] 
        : null;

    public bool IsPlaying
    {
        get => _isPlaying;
        private set
        {
            if (SetProperty(ref _isPlaying, value))
            {
                ((RelayCommand)PlayCommand).RaiseCanExecuteChanged();
                ((RelayCommand)PauseCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string SelectedTrackPointInfo
    {
        get => _selectedTrackPointInfo;
        private set => SetProperty(ref _selectedTrackPointInfo, value);
    }

    public DateTime? SelectedPlaybackDate
    {
        get => _selectedPlaybackDate;
        set
        {
            if (SetProperty(ref _selectedPlaybackDate, value) && value.HasValue)
            {
                JumpToDate(value.Value.Date);
            }
        }
    }

    public int TotalTrackPoints => CurrentTrack.Count;

    public void ShowMessage(string title, string message, string? details = null, MessageDialogType type = MessageDialogType.Info)
    {
        ActiveDialog = new MessageDialogViewModel(title, message, details, type, () => ActiveDialog = null);
    }

    public Task ShowMessageAsync(string title, string message, string? details = null, MessageDialogType type = MessageDialogType.Info)
    {
        var tcs = new TaskCompletionSource();
        ActiveDialog = new MessageDialogViewModel(title, message, details, type, () => 
        {
            ActiveDialog = null;
            tcs.SetResult();
        });
        return tcs.Task;
    }

    public Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var tcs = new TaskCompletionSource<bool>();
        ActiveDialog = new ConfirmationDialogViewModel(title, message, result => 
        {
            ActiveDialog = null;
            tcs.SetResult(result);
        });
        return tcs.Task;
    }

    public bool IsTableVisible => true;

    public AppThemeMode CurrentThemeMode
    {
        get => _currentThemeMode;
        private set
        {
            if (!SetProperty(ref _currentThemeMode, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsSystemThemeActive));
            OnPropertyChanged(nameof(IsLightThemeActive));
            OnPropertyChanged(nameof(IsDarkThemeActive));
        }
    }

    private async Task OpenConfigurarUnidadDescarteAsync()
    {
        var activeMarea = _activeMareaManager.ActiveMarea;
        if (activeMarea == null)
        {
            ShowMessage("Sin marea activa", "Debe seleccionar una marea activa para realizar esta acción.", null, MessageDialogType.Warning);
            return;
        }

        var viewModel = new ConfigurarUnidadDescarteViewModel();
        ActiveDialog = viewModel;

        bool result = await viewModel.DialogResult.Task;
        ActiveDialog = null;

        if (result)
        {
            try 
            {
                await _mareaService.SetTipoDatoDescarteMasivoAsync(activeMarea.ID, viewModel.TipoSeleccionado);
                await RefreshCurrentSectionAsync();
                ShowMessage("Éxito", "Se ha actualizado la unidad de descarte en todos los registros de la marea.");
            }
            catch (Exception ex)
            {
                ShowMessage("Error", "No se pudo actualizar la unidad de descarte.", ex.Message, MessageDialogType.Error);
            }
        }
    }

    private async Task OpenGenerarRecursosInformeAsync()
    {
        var activeMarea = _activeMareaManager.ActiveMarea;
        if (activeMarea == null)
        {
            ShowMessage("Sin marea activa", "Debe seleccionar una marea activa para realizar esta acción.", null, MessageDialogType.Warning);
            return;
        }

        var lances = await _lanceService.GetLancesAsync(mareaId: activeMarea.ID);
        var viewModel = new ExportarRecursosViewModel(activeMarea, lances.ToList(), _lanceService, _mapRenderingService, _excelReportService, _reportService, _mareaSummaryService, _userSettingsService);
        viewModel.ShowMessage = (title, msg, details, type) => { ShowMessage(title, msg, details, type); return Task.CompletedTask; };
        viewModel.ShowConfirmation = (title, msg) => ShowConfirmationAsync(title, msg);
        ActiveDialog = viewModel;

        bool result = await viewModel.DialogResult.Task;
        ActiveDialog = null;

        if (result)
        {
            bool openFolder = await ShowConfirmationAsync("Exportación completada", "¿Desea abrir la carpeta donde se generaron los archivos?");
            if (openFolder)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = viewModel.ExportPath,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
                catch (Exception ex)
                {
                    ShowMessage("Error", $"No se pudo abrir la carpeta: {ex.Message}", null, MessageDialogType.Error);
                }
            }
        }
    }

    private async Task OpenGenerarRecibiPdfAsync()
    {
        var activeMareaId = _activeMareaManager.ActiveMareaId;
        if (string.IsNullOrEmpty(activeMareaId))
        {
            ShowMessage("Sin marea activa", "Debe seleccionar una marea activa para realizar esta acción.", null, MessageDialogType.Warning);
            return;
        }

        try
        {
            // 1. Obtener los datos base del reporte (todas las especies con submuestra)
            var reportData = await _mareaSummaryService.GetRecibiProyectoReportAsync(activeMareaId);
            
            if (reportData.Especies == null || !reportData.Especies.Any())
            {
                ShowMessage("Sin datos", "No se encontraron especies con submuestras para esta marea.", null, MessageDialogType.Info);
                return;
            }

            // 2. Mostrar el diálogo de selección
            var selectionVm = new RecibiProyectoSelectionViewModel(reportData.Especies);
            ActiveDialog = selectionVm;

            // 3. Esperar a que el usuario confirme o cancele
            var selectedEspecies = await selectionVm.SelectionTask;
            ActiveDialog = null;

            // 4. Si el usuario confirmó (no es null) y hay especies seleccionadas
            if (selectedEspecies != null)
            {
                if (!selectedEspecies.Any())
                {
                    ShowMessage("Selección vacía", "Debe seleccionar al menos una especie para generar el reporte.", null, MessageDialogType.Warning);
                    return;
                }

                // Actualizar los datos del reporte con la selección filtrada
                reportData.Especies = selectedEspecies;

                var pdfBytes = await _reportService.GenerateRecibiProyectoPdfAsync(reportData);

                string fileName = $"Recibi_{reportData.BuqueNombre.Replace(" ", "_")}_{reportData.MareaNumero}_{reportData.MareaAnio}.pdf";
                string importFolder = MareaMetadataHelper.GetImportFolder(_activeMareaManager.ActiveMarea!.Metadata);
                string savePath;

                if (!string.IsNullOrEmpty(importFolder))
                {
                    savePath = Path.Combine(importFolder, "reportes", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
                }
                else
                {
                    savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);
                }

                await File.WriteAllBytesAsync(savePath, pdfBytes);

                Process.Start(new ProcessStartInfo
                {
                    FileName = savePath,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            ShowMessage("Error", "No se pudo generar el reporte PDF.", ex.Message, MessageDialogType.Error);
        }
    }

    private async Task OpenExportarDbfAsync()
    {
        var activeMarea = _activeMareaManager.ActiveMarea;
        if (activeMarea == null)
        {
            ShowMessage("Sin marea activa", "Debe seleccionar una marea activa para realizar esta acción.", null, MessageDialogType.Warning);
            return;
        }

        var viewModel = new ExportarDbfViewModel(activeMarea, _dbfExporterService);
        viewModel.ShowMessage = (title, msg, details, type) => { ShowMessage(title, msg, details, type); return Task.CompletedTask; };
        ActiveDialog = viewModel;

        bool result = await viewModel.DialogResult.Task;
        ActiveDialog = null;

        if (result)
        {
            var summary = viewModel.ExportSummary;
            var timeDetails = new StringBuilder();
            timeDetails.AppendLine("Se han generado los archivos DBF con éxito.");
            timeDetails.AppendLine();
            timeDetails.AppendLine($"Tiempo Total: {summary?.TotalTime.TotalSeconds:F2}s");
            timeDetails.AppendLine("-----------------------------------");
            if (summary?.StageTimings != null)
            {
                foreach (var stage in summary.StageTimings)
                {
                    timeDetails.AppendLine($"{stage.Key}: {stage.Value.TotalSeconds:F2}s");
                }
            }
            timeDetails.AppendLine();
            timeDetails.AppendLine("¿Desea abrir la carpeta de destino?");

            bool openFolder = await ShowConfirmationAsync("Exportación completada", timeDetails.ToString());
            if (openFolder)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = viewModel.ExportPath,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
                catch (Exception ex)
                {
                    ShowMessage("Error", $"No se pudo abrir la carpeta: {ex.Message}", null, MessageDialogType.Error);
                }
            }
        }
    }

    public bool IsSystemThemeActive => CurrentThemeMode == AppThemeMode.System;

    public bool IsLightThemeActive => CurrentThemeMode == AppThemeMode.Light;

    public bool IsDarkThemeActive => CurrentThemeMode == AppThemeMode.Dark;

    public string Column1Header { get; private set; } = string.Empty;
    public string Column2Header { get; private set; } = string.Empty;
    public string Column3Header { get; private set; } = string.Empty;
    public string Column4Header { get; private set; } = string.Empty;
    public string Column5Header { get; private set; } = string.Empty;
    public string Column6Header { get; private set; } = string.Empty;
    public string Column7Header { get; private set; } = string.Empty;
    public string Column8Header { get; private set; } = string.Empty;
    public string Column9Header { get; private set; } = string.Empty;

    public ICommand ClearActiveMareaCommand => new AsyncRelayCommand(() => _activeMareaManager.SetActiveMareaAsync(null));

    private async Task RefreshCurrentSectionAsync()
    {
        if (SelectedNavigationItem == null) return;
        
        if (SelectedNavigationItem.Section == NavigationSection.Mareas)
        {
            await LoadMareasAsync();
        }
        else if (SelectedNavigationItem.Section == NavigationSection.Lances)
        {
            await LoadLancesAsync();
        }
        else if (SelectedNavigationItem.Section == NavigationSection.Muestras)
        {
            await LoadMuestrasAsync();
        }
        else if (SelectedNavigationItem.Section == NavigationSection.Submuestras)
        {
            await LoadSubmuestrasAsync();
        }
        else if (SelectedNavigationItem.Section == NavigationSection.ControlProduccion)
        {
            await LoadControlProduccionAsync();
        }
        else if (SelectedNavigationItem.Section == NavigationSection.ReemplazoEspecie)
        {
            if (ReemplazoEspecieVM != null)
            {
                await ReemplazoEspecieVM.LoadDataAsync();
            }
        }
    }

    private void LoadSection(NavigationSection section)
    {
        if (section == NavigationSection.Mareas)
        {
            _ = LoadMareasAsync();
            
            // Configurar metadatos básicos de la marea para la shell
            var mareaSection = _mockShellDataService.GetListSection(section);
            PageEyebrow = mareaSection.Eyebrow;
            PageTitle = mareaSection.Title;
            PageDescription = mareaSection.Description;
            PrimaryActionLabel = mareaSection.PrimaryActionLabel;
            
            SetColumnHeaders(
                mareaSection.Column1Header,
                mareaSection.Column2Header,
                mareaSection.Column3Header,
                mareaSection.Column4Header,
                mareaSection.Column5Header);

            return;
        }

        if (section == NavigationSection.Lances)
        {
            _ = LoadLancesAsync();
            var lanceSection = _mockShellDataService.GetListSection(section);
            PageEyebrow = lanceSection.Eyebrow;
            PageTitle = lanceSection.Title;
            PageDescription = lanceSection.Description;
            PrimaryActionLabel = lanceSection.PrimaryActionLabel;
            PrimaryActionCommand = new RelayCommand(OpenCreateLanceForm);
            
            SetColumnHeaders(
                lanceSection.Column1Header,
                lanceSection.Column2Header,
                lanceSection.Column3Header,
                lanceSection.Column4Header,
                lanceSection.Column5Header,
                "Inicio",
                "Fin");

            return;
        }

        if (section == NavigationSection.Muestras)
        {
            _ = LoadMuestrasAsync();
            var muestraSection = _mockShellDataService.GetListSection(section);
            PageEyebrow = muestraSection.Eyebrow;
            PageTitle = muestraSection.Title;
            PageDescription = muestraSection.Description;
            PrimaryActionLabel = muestraSection.PrimaryActionLabel;
            PrimaryActionCommand = new RelayCommand(OpenCreateMuestraForm);
            
            SetColumnHeaders(
                "Nro. Lance",
                "Fecha",
                "Hora Virada",
                "Especie",
                "Peso");

            return;
        }

        if (section == NavigationSection.Submuestras)
        {
            _ = LoadSubmuestrasAsync();
            var subSection = _mockShellDataService.GetListSection(section);
            PageEyebrow = subSection.Eyebrow;
            PageTitle = subSection.Title;
            PageDescription = subSection.Description;
            PrimaryActionLabel = subSection.PrimaryActionLabel;
            PrimaryActionCommand = new RelayCommand(() => ShowMessage("En desarrollo", "La creación de submuestras individuales se realiza desde la edición de muestras."));

            SetColumnHeaders(
                "Nro. Lance",
                "Fecha",
                "Hora Virada",
                "Especie",
                "Peso");

            return;
        }

        if (section == NavigationSection.Produccion)
        {
            _ = LoadProduccionAsync();
            var prodSection = _mockShellDataService.GetListSection(section);
            PageEyebrow = prodSection.Eyebrow;
            PageTitle = prodSection.Title;
            PageDescription = prodSection.Description;
            PrimaryActionLabel = prodSection.PrimaryActionLabel;
            PrimaryActionCommand = new RelayCommand(OpenCreateProduccionForm);

            SetColumnHeaders(
                "Fecha",
                "Especie",
                "Producto",
                "Categoría",
                "Factor",
                "Kg");

            return;
        }

        if (section == NavigationSection.ControlProduccion)
        {
            _ = LoadControlProduccionAsync();
            var controlSection = _mockShellDataService.GetListSection(section);
            PageEyebrow = controlSection.Eyebrow;
            PageTitle = controlSection.Title;
            PageDescription = controlSection.Description;
            PageEyebrow = controlSection.Eyebrow;
            PrimaryActionLabel = controlSection.PrimaryActionLabel;
            PrimaryActionCommand = new AsyncCommand(LoadControlProduccionAsync);

            // Los encabezados se manejan dinámicamente en LoadControlProduccionAsync
            // según si es vista resumen o detalle.

            return;
        }

        // Limpiar filtro al cambiar a otras secciones
        if (RecordsView != null)
        {
            RecordsView.Filter = null;
        }

        if (section == NavigationSection.ReemplazoEspecie)
        {
            ReemplazoEspecieVM ??= new ReemplazoEspecieViewModel(_dbContextFactory, _activeMareaManager)
            {
                ShowMessage = (t, m, d, type) => ShowMessage(t, m, d, type),
                ShowConfirmation = (t, m) => ShowConfirmationAsync(t, m)
            };
            _ = ReemplazoEspecieVM.LoadDataAsync();

            PageEyebrow = "Herramientas de marea";
            PageTitle = "Reemplazar especie";
            PageDescription = "Permite reidentificar especies de forma masiva en todos los registros de la marea activa.";
            PrimaryActionLabel = "";

            return;
        }

        if (section == NavigationSection.Procesos)
        {
            PageEyebrow = "Centro de control";
            PageTitle = "Procesos";
            PageDescription = "Panel centralizado para la ejecución de tareas de exportación y configuración masiva.";
            PrimaryActionLabel = "";

            return;
        }

        if (section == NavigationSection.Configuracion)
        {
            PageEyebrow = "Ajustes";
            PageTitle = "Configuración";
            PageDescription = "Preferencias de usuario y configuración del sistema.";
            PrimaryActionLabel = "";

            return;
        }

        var listSection = _mockShellDataService.GetListSection(section);

        PageEyebrow = listSection.Eyebrow;
        PageTitle = listSection.Title;
        PageDescription = listSection.Description;
        PrimaryActionLabel = listSection.PrimaryActionLabel;
        SetColumnHeaders(
            listSection.Column1Header,
            listSection.Column2Header,
            listSection.Column3Header,
            listSection.Column4Header,
            listSection.Column5Header);

        ReplaceItems(ActiveFilters, listSection.Filters);
        ReplaceItems(Records, listSection.Rows);
    }


    private async Task LoadFilterDataAsync()
    {
        try
        {
            // Cargar años únicos desde las mareas existentes
            var anios = await _mareaService.GetAniosExistentesAsync();
            Anios.Clear();
            Anios.Add(null);
            foreach (var anio in anios)
            {
                Anios.Add(anio);
            }

            // Si no hay años pero tenemos un filtro por defecto (año actual), 
            // nos aseguramos de que el año actual esté en la lista para que sea seleccionable.
            if (Anios.Count == 0 || !Anios.Contains(DateTime.Today.Year))
            {
                // Opcional: Podríamos añadir el año actual siempre, o solo si el usuario
                // quiere poder filtrar por "el presente" aun sin datos.
                // Anios.Add(DateTime.Today.Year); 
                // Pero respetando el pedido: "extraer años únicos". 
                // Si la lista está vacía, es porque no hay mareas.
            }

            // Cargar buques
            var buques = await _buqueService.GetBuquesAsync(onlyWithMareas: true);
            Buques.Clear();
            Buques.Add(new BuqueListItemViewModel(null!, "(Todos)", 0, 0, null, null));
            foreach (var buque in buques)
            {
                Buques.Add(buque);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cargando datos de filtro: {ex.Message}");
            // Si falla la carga de buques, reintentamos una vez tras un breve delay 
            // por si la sincronización inicial estaba terminando
            _ = Task.Delay(2000).ContinueWith(async _ => 
            {
                try {
                    var b = await _buqueService.GetBuquesAsync(onlyWithMareas: true);
                    if (b.Any()) {
                        App.Current.Dispatcher.Invoke(() => {
                            Buques.Clear();
                            Buques.Add(new BuqueListItemViewModel(null!, "(Todos)", 0, 0, null, null));
                            foreach(var x in b) Buques.Add(x);
                        });
                    }
                } catch { /* Ignorar reintento silencioso */ }
            });
        }
    }

    private async Task LoadMareasAsync()
    {
        try
        {
            DateTime? filterDesde = _mareasFilterFechaDesde;
            DateTime? filterHasta = _mareasFilterFechaHasta;

            // Validación estricta: fecha fin >= fecha inicio. 
            // Si ambas están presentes y el rango es inválido, advertimos y cancelamos la búsqueda.
            if (filterDesde.HasValue && filterHasta.HasValue && filterHasta.Value < filterDesde.Value)
            {
                ShowMessage(
                    "Rango de Fechas Inválido",
                    "La fecha de fin (" + filterHasta.Value.ToShortDateString() + ") no puede ser anterior a la de inicio (" + filterDesde.Value.ToShortDateString() + ").",
                    null,
                    MessageDialogType.Warning);
                return;
            }

            SelectedRecord = null;

            var mareas = await _mareaService.GetMareasAsync(
                _mareasFilterAnio,
                _mareasFilterBuque?.ID,
                filterDesde,
                filterHasta,
                _mareasSearchText);

            var viewModels = mareas.Select(m => new MareaListItemViewModel(m, _activeMareaManager, _validationService, _reportService, _mareaSummaryService)).ToList();
            
            RecordsView.GroupDescriptions.Clear();
            Records.Clear();
            foreach (var vm in viewModels)
            {
                Records.Add(vm);
            }

            // Actualizar filtros activos visuales
            ActiveFilters.Clear();
            if (_mareasFilterAnio.HasValue) ActiveFilters.Add($"Año: {_mareasFilterAnio}");
            if (_mareasFilterBuque != null) ActiveFilters.Add($"Buque: {_mareasFilterBuque.Nombre}");
            if (_activeMareaManager.ActiveMarea != null) ActiveFilters.Add($"Marea Activa: {_activeMareaManager.ActiveMarea.NumeroInidep}/{_activeMareaManager.ActiveMarea.AnioInidep}");
        }
        catch (Exception)
        {
            // Log error
        }
    }

    private async Task ClearMareaFiltersAsync()
    {
        MareasFilterAnio = null;
        MareasFilterBuque = null;
        MareasFilterFechaDesde = null;
        MareasFilterFechaHasta = null;
        MareasSearchText = string.Empty;
        await LoadMareasAsync();
    }

    private void OpenCreateMareaForm()
    {
        var vm = _mareaEditFactory(() => 
        {
            CurrentEditViewModel = null;
            _ = _activeMareaManager.RefreshAsync();
            _ = RefreshCurrentSectionAsync();
        }, null);
        vm.ShowCustomDialog = diag => ActiveDialog = diag;
        vm.ShowMessage = (t, m, d, type) => ShowMessageAsync(t, m, d, type);
        vm.ShowConfirmation = (t, m) => ShowConfirmationAsync(t, m);
        CurrentEditViewModel = vm;
    }

    private void OpenEditMareaForm(MareaListItemViewModel? item)
    {
        if (item == null) return;
        
        var vm = _mareaEditFactory(() => 
        {
            CurrentEditViewModel = null;
            _ = _activeMareaManager.RefreshAsync();
            _ = RefreshCurrentSectionAsync();
        }, item.ID);
        vm.ShowCustomDialog = diag => ActiveDialog = diag;
        vm.ShowMessage = (t, m, d, type) => ShowMessageAsync(t, m, d, type);
        vm.ShowConfirmation = (t, m) => ShowConfirmationAsync(t, m);
        CurrentEditViewModel = vm;
    }

    private async Task LoadLancesAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_activeMareaManager.ActiveMareaId))
            {
                RecordsView.GroupDescriptions.Clear();
                Records.Clear();
                ActiveFilters.Clear();
                return;
            }

            var lances = await _lanceService.GetLancesAsync(
                null, 
                _activeMareaManager.ActiveMareaId,
                LancesFilterFechaDesde, 
                LancesFilterFechaHasta, 
                LancesFilterNroLance, 
                LancesFilterEspecie);

            SelectedRecord = null;

            var viewModels = lances.Select(l => new LanceListItemViewModel(l)).ToList();
            
            RecordsView.GroupDescriptions.Clear();
            Records.Clear();
            foreach (var vm in viewModels) Records.Add(vm);

            ActiveFilters.Clear();
            if (LancesFilterFechaDesde.HasValue) ActiveFilters.Add($"Desde: {LancesFilterFechaDesde.Value:dd/MM/yyyy}");
            if (LancesFilterNroLance.HasValue) ActiveFilters.Add($"Lance: {LancesFilterNroLance}");
            if (!string.IsNullOrWhiteSpace(LancesFilterEspecie)) ActiveFilters.Add($"Especie: {LancesFilterEspecie}");
            if (_activeMareaManager.ActiveMarea != null) ActiveFilters.Add($"Marea Activa: {_activeMareaManager.ActiveMarea.NumeroInidep}/{_activeMareaManager.ActiveMarea.AnioInidep}");

            _ = UpdateMapDataAsync();
        }
        catch (Exception) { /* Log error */ }
    }

    private async Task UpdateMapDataAsync()
    {
        if (string.IsNullOrEmpty(_activeMareaManager.ActiveMareaId))
        {
            CurrentTrack.Clear();
            CurrentLances.Clear();
            MapUpdateRequested?.Invoke();
            return;
        }

        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            // 1. Obtener Lances
            CurrentLances = await dbContext.Lances
                .Where(l => l.MareaEtapa.MareaID == _activeMareaManager.ActiveMareaId)
                .ToListAsync();

            var trackPoints = await dbContext.TrackingPoints
                .Where(t => t.MareaID == _activeMareaManager.ActiveMareaId)
                .OrderBy(t => t.FechaHora)
                .ToListAsync();

            // Conversión explícita a UTC-3 para el ploteo en el mapa (Hora Local Argentina)
            foreach (var p in trackPoints)
            {
                p.FechaHora = p.FechaHora.AddHours(-3);
            }

            CurrentTrack = trackPoints;

            // Notificar a la vista para que actualice GMap.NET
            ShouldZoomOnNextUpdate = true;
            MapUpdateRequested?.Invoke();
            
            OnPropertyChanged(nameof(TotalTrackPoints));
            CurrentTrackPointIndex = CurrentTrack.Any() ? 0 : -1;
            ((RelayCommand)PlayCommand).RaiseCanExecuteChanged();
            ((RelayCommand)StopCommand).RaiseCanExecuteChanged();
            ((RelayCommand)GoToStartCommand).RaiseCanExecuteChanged();
            ((RelayCommand)GoToEndCommand).RaiseCanExecuteChanged();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error actualizando datos de mapa: {ex.Message}");
        }
    }

    private void UpdateSelectedTrackPoint()
    {
        if (CurrentTrackPointIndex >= 0 && CurrentTrackPointIndex < CurrentTrack.Count)
        {
            var point = CurrentTrack[CurrentTrackPointIndex];
            SelectedTrackPointInfo = $"{point.FechaHora:dd/MM/yyyy HH:mm}";
        }
        else
        {
            SelectedTrackPointInfo = string.Empty;
        }
        
        // Notificar a la vista que el marcador del buque debe moverse
        MapUpdateRequested?.Invoke();
    }

    private void StartPlayback()
    {
        if (CurrentTrackPointIndex >= CurrentTrack.Count - 1)
        {
            CurrentTrackPointIndex = 0;
        }
        else if (CurrentTrackPointIndex < 0)
        {
            CurrentTrackPointIndex = 0;
        }

        IsPlaying = true;
        
        if (_playbackTimer == null)
        {
            _playbackTimer = new System.Windows.Threading.DispatcherTimer();
            _playbackTimer.Interval = TimeSpan.FromMilliseconds(500);
            _playbackTimer.Tick += (s, e) =>
            {
                if (CurrentTrackPointIndex < CurrentTrack.Count - 1)
                {
                    CurrentTrackPointIndex++;
                }
                else
                {
                    StopPlayback();
                }
            };
        }
        
        _playbackTimer.Start();
    }

    private void PausePlayback()
    {
        IsPlaying = false;
        _playbackTimer?.Stop();
    }

    private void StopPlayback()
    {
        IsPlaying = false;
        _playbackTimer?.Stop();
        CurrentTrackPointIndex = -1;
    }

    public void SeekTrackToTime(DateTime? targetTime)
    {
        if (targetTime == null || CurrentTrack == null || !CurrentTrack.Any()) return;

        // Buscar el punto de track más cercano en tiempo
        var nearest = CurrentTrack
            .Select((p, index) => new { Point = p, Index = index, Diff = Math.Abs((p.FechaHora - targetTime.Value).Ticks) })
            .OrderBy(x => x.Diff)
            .FirstOrDefault();

        if (nearest != null)
        {
            CurrentTrackPointIndex = nearest.Index;
        }
    }

    public DateTime? GetLanceDateTime(Lance lance, bool isStart)
    {
        string? hora = isStart ? lance.HoraInicio : lance.HoraFinal;
        if (string.IsNullOrEmpty(lance.Fecha) || string.IsNullOrEmpty(hora)) return null;

        if (DateTime.TryParse($"{lance.Fecha} {hora}", out var dt))
        {
            return dt;
        }
        return null;
    }

    private void JumpToDate(DateTime targetDate)
    {
        if (!CurrentTrack.Any()) return;

        var closest = CurrentTrack
            .Select((point, index) => new { point, index })
            .OrderBy(item => Math.Abs((item.point.FechaHora - targetDate).Ticks))
            .FirstOrDefault();

        if (closest != null)
        {
            CurrentTrackPointIndex = closest.index;
        }
    }

    private async Task ClearLanceFiltersAsync()
    {
        LancesFilterFechaDesde = null;
        LancesFilterFechaHasta = null;
        LancesFilterNroLance = null;
        LancesFilterEspecie = string.Empty;
        await LoadLancesAsync();
    }

    private void OpenEditLanceForm(LanceListItemViewModel? item)
    {
        if (item == null) return;
        
        var vm = _lanceEditFactory(() => 
        {
            CurrentEditViewModel = null;
            _ = LoadLancesAsync();
        }, item.Lance.MareaEtapaId, item.ID);
        
        vm.ShowCustomDialog = diag => ActiveDialog = diag;
        vm.ShowMessage = (t, m, d, type) => ShowMessage(t, m, d, type);
        vm.ShowConfirmation = (t, m) => ShowConfirmationAsync(t, m);
        CurrentEditViewModel = vm;
    }

    private void OpenEditLanceFromDetail(ControlLanceDetailViewModel? vm)
    {
        if (vm == null) return;
        
        var editVm = _lanceEditFactory(() => 
        {
            CurrentEditViewModel = null;
            // Refrescamos la lista principal, lo cual disparará también el refresco del detalle
            // si logramos preservar la selección.
            _ = LoadControlProduccionAsync();
        }, vm.MareaEtapaId, vm.LanceId);
        
        editVm.ShowCustomDialog = diag => ActiveDialog = diag;
        editVm.ShowMessage = (t, m, d, type) => ShowMessage(t, m, d, type);
        editVm.ShowConfirmation = (t, m) => ShowConfirmationAsync(t, m);
        CurrentEditViewModel = editVm;
    }

    private void OpenCreateLanceForm()
    {
        ShowMessage("Nuevo Lance", "Para crear un nuevo lance, debe hacerlo desde la sección de Mareas > Etapas para mantener la consistencia de datos.", null, MessageDialogType.Info);
    }

    private void ApplyTheme(AppThemeMode mode)
    {
        _themeService.ApplyTheme(mode);
        CurrentThemeMode = mode;
    }


    private void SetColumnHeaders(string column1, string column2, string column3, string column4, string column5, string column6 = "", string column7 = "", string column8 = "", string column9 = "")
    {
        Column1Header = column1;
        Column2Header = column2;
        Column3Header = column3;
        Column4Header = column4;
        Column5Header = column5;
        Column6Header = column6;
        Column7Header = column7;
        Column8Header = column8;
        Column9Header = column9;

        OnPropertyChanged(nameof(Column1Header));
        OnPropertyChanged(nameof(Column2Header));
        OnPropertyChanged(nameof(Column3Header));
        OnPropertyChanged(nameof(Column4Header));
        OnPropertyChanged(nameof(Column5Header));
        OnPropertyChanged(nameof(Column6Header));
        OnPropertyChanged(nameof(Column7Header));
        OnPropertyChanged(nameof(Column8Header));
        OnPropertyChanged(nameof(Column9Header));
    }

    private string FormatCoordinate(double? value, bool isLatitude)
    {
        if (!value.HasValue) return "-";
        
        double absolute = Math.Abs(value.Value);
        int degrees = (int)absolute;
        double minutes = (absolute - degrees) * 60;
        
        string quadrant = isLatitude 
            ? (value.Value >= 0 ? "N" : "S") 
            : (value.Value >= 0 ? "E" : "O");
            
        // Formato GGº MM,MM' C (C= cuadrante N,S,E,O) - Usamos 2 decimales para precisión en HUD
        return $"{degrees}º {minutes:00.00}' {quadrant}".Replace('.', ',');
    }

    private void OnRecordSelected(object? record)
    {
        // 1. Notificar a la vista que debe redibujar para aplicar el resaltado de color
        MapUpdateRequested?.Invoke();

        if (record is ControlProduccionListItemViewModel controlItem)
        {
            SelectedControlItem = controlItem;
            _ = LoadControlLanceDetailsAsync(controlItem);
        }
        
        // 2. Si es un lance, pedir el foco (encuadre de inicio y fin)
        if (record is LanceListItemViewModel lanceVm)
        {
            var lance = lanceVm.Lance;
            var points = new List<PointLatLng>();

            if (lance.LatitudInicioDecimal.HasValue && lance.LongitudInicioDecimal.HasValue)
                points.Add(new PointLatLng(lance.LatitudInicioDecimal.Value, lance.LongitudInicioDecimal.Value));

            if (lance.LatitudFinalDecimal.HasValue && lance.LongitudFinalDecimal.HasValue)
                points.Add(new PointLatLng(lance.LatitudFinalDecimal.Value, lance.LongitudFinalDecimal.Value));

            if (points.Count > 0)
            {
                MapFocusRequested?.Invoke(points);
            }
        }
    }

    private async Task LoadControlLanceDetailsAsync(ControlProduccionListItemViewModel controlItem)
    {
        ControlLanceDetails.Clear();
        var activeMarea = _activeMareaManager.ActiveMarea;
        if (activeMarea == null) return;

        using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        
        // Obtener lances de la marea
        var lances = await dbContext.Lances
            .Include(l => l.MareaEtapa)
            .Include(l => l.ItemsCaptura)
                .ThenInclude(i => i.Especie)
            .Where(l => l.MareaEtapa.MareaID == activeMarea.ID)
            .ToListAsync();

        foreach (var lance in lances)
        {
            foreach (var item in lance.ItemsCaptura)
            {
                item.Lance = lance;
            }
        }

        var filteredLances = lances
            .Where(l => controlItem.IsSummaryView || (DateTime.TryParse(l.Fecha, out var ld) && ld.Date == controlItem.Fecha.Date))
            .OrderBy(l => l.NroLance)
            .ToList();

        bool isRayaGenericGroup = controlItem.EspecieId == RayaGenericVirtualId;

        foreach (var lance in filteredLances)
        {
            var catchItems = lance.ItemsCaptura.Where(c => 
            {
                if (c.EspecieID == controlItem.EspecieId) return true;

                if (isRayaGenericGroup)
                {
                    if (IsRaya(c.Especie) && (IsGenericRaya(c.Especie) || !_commonRayaIds.Contains(c.EspecieID!)))
                    {
                        return true;
                    }
                }
                return false;
            });

            var catchItemsList = catchItems.ToList();
            double catchKg = 0;
            double discardKg = 0;

            foreach (var catchItem in catchItemsList)
            {
                catchKg += catchItem.CapturaTotalKgCalculado;
                discardKg += catchItem.PesoDescarteCalculado;
            }

            if (catchKg > 0 || discardKg > 0)
            {
                var especiesNombres = catchItemsList
                    .Select(c => c.Especie?.FullDisplayName ?? "Desconocida")
                    .Where(n => n != null)
                    .Distinct()
                    .ToList();

                ControlLanceDetails.Add(new ControlLanceDetailViewModel
                {
                    LanceId = lance.Id,
                    MareaEtapaId = lance.MareaEtapaId,
                    NroLance = lance.NroLance,
                    CapturaKg = catchKg,
                    DescarteKg = discardKg,
                    EspecieDetalle = string.Join(", ", especiesNombres)
                });
            }
        }

        TotalCapturaKg = ControlLanceDetails.Sum(d => d.CapturaKg);
        TotalDescarteKg = ControlLanceDetails.Sum(d => d.DescarteKg);
        TotalRetenidaKg = ControlLanceDetails.Sum(d => d.NetaKg);
    }

    private static void ReplaceItems<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();

        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    private async Task DeleteRecordAsync(object? record)
    {
        if (record == null) return;

        string entityName = "el registro";
        string identifier = "";

        if (record is MareaListItemViewModel marea)
        {
            entityName = "la MAREA";
            identifier = marea.CodigoDisplay;
        }
        else if (record is LanceListItemViewModel lance)
        {
            entityName = "el LANCE";
            identifier = lance.NroLance.ToString();
        }
        else if (record is MuestraListItemViewModel muestra)
        {
            entityName = "la MUESTRA";
            identifier = muestra.EspecieDisplay;
        }
        else if (record is ProduccionListItemViewModel prod)
        {
            entityName = "el registro de PRODUCCIÓN";
            identifier = $"{prod.EspecieDisplay} ({prod.FechaDisplay})";
        }

        var result = await ShowConfirmationAsync(
            "Confirmar Borrado",
            $"¿Está seguro de que desea eliminar {entityName} '{identifier}'?\n\nEsta acción es permanente y eliminará todos los datos asociados en cascada.");

        if (!result) return;

        try
        {
            if (record is MareaListItemViewModel mareaVm)
            {
                await _mareaService.DeleteMareaAsync(mareaVm.ID);
                if (_activeMareaManager.ActiveMareaId == mareaVm.ID)
                {
                    await _activeMareaManager.SetActiveMareaAsync(null);
                }
            }
            else if (record is LanceListItemViewModel lanceVm)
            {
                await _lanceService.DeleteLanceAsync(lanceVm.ID);
            }
            else if (record is MuestraListItemViewModel muestraVm)
            {
                await _muestraService.DeleteMuestraAsync(muestraVm.Muestra.ID);
            }
            else if (record is ProduccionListItemViewModel prodVm)
            {
                await _produccionService.DeleteRegistroProduccionAsync(prodVm.Registro.Id);
            }

            await RefreshCurrentSectionAsync();
            ShowMessage("Borrado Exitoso", "El registro ha sido eliminado correctamente.", null, MessageDialogType.Success);
        }
        catch (Exception ex)
        {
            ShowMessage("Error al Borrar", $"No se pudo eliminar el registro: {ex.Message}", null, MessageDialogType.Error);
        }
    }

    private async Task ImportMareaAsync()
    {
        ImportDbfViewModel? importVm = null;
        importVm = new ImportDbfViewModel(
            null, // Nueva marea
            0,
            DateTime.Today.Year,
            _mareaImportService,
            _jsonImportService,
            _mareaService,
            "Sin Nombre",
            [],
            async (files, mareaId) => 
            {
                ActiveDialog = null;
                importVm.Dispose(); // Limpiar archivos temporales si los hubiera
                
                // Refrescamos siempre la lista (pedido por el usuario)
                await LoadMareasAsync();
                await LoadFilterDataAsync();
                
                // Si la importación fue exitosa y tenemos ID, la activamos automáticamente
                if (files != null && !string.IsNullOrEmpty(mareaId))
                {
                    await _activeMareaManager.SetActiveMareaAsync(mareaId);
                }

                // Si hay una marea activa (que puede ser la recién activada), refrescamos todo
                if (!string.IsNullOrEmpty(_activeMareaManager.ActiveMareaId))
                {
                    await _activeMareaManager.RefreshAsync();
                    await RefreshCurrentSectionAsync();
                }
                else 
                {
                    await RefreshCurrentSectionAsync();
                }

                if (files != null)
                {
                    ShowMessage("Importación Exitosa", "La marea y sus datos han sido importados correctamente.", null, MessageDialogType.Success);
                }
            });

        importVm.ShowMessage = (title, msg, details, type) => ShowMessageAsync(title, msg, details, type);
        importVm.ShowConfirmation = (title, msg) => ShowConfirmationAsync(title, msg);

        ActiveDialog = importVm;
    }

    private async Task NavigateToControlProduccionDetailAsync(ControlProduccionListItemViewModel? vm)
    {
        if (vm == null || IsControlProduccionDetailVisible) return;

        ControlProduccionSelectedEspecieId = vm.EspecieId;
        
        if (vm.EspecieId == RayaGenericVirtualId)
        {
            ControlProduccionSelectedEspecie = new Especie 
            { 
                ID = RayaGenericVirtualId, 
                NombreVulgar = "Rayas", 
                NombreCientifico = "Rajidae" 
            };
        }
        else
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            ControlProduccionSelectedEspecie = await dbContext.Especies.FirstOrDefaultAsync(e => e.ID == vm.EspecieId);
        }

        await LoadControlProduccionAsync();
    }

    private void BackToControlProduccionSummary()
    {
        ControlProduccionSelectedEspecieId = null;
        ControlProduccionSelectedEspecie = null;
        _ = LoadControlProduccionAsync();
    }

    private async Task ExportControlProduccionPdfAsync()
    {
        if (_activeMareaManager.ActiveMarea == null) return;
        
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            
            var meta = MareaMetadataHelper.GetMetadata(_activeMareaManager.ActiveMarea);
            var report = new ControlProduccionReport
            {
                Barco = _activeMareaManager.ActiveMarea.Buque?.Nombre ?? "S/D",
                Marea = _activeMareaManager.ActiveMarea.NumeroInidep.ToString(),
                Anio = _activeMareaManager.ActiveMarea.AnioInidep,
                BuqueCodigo = meta.BuqueCodigo,
                ObservadorNombre = meta.ObservadorNombre,
                ObservadorApellido = meta.ObservadorApellido,
                ObservadorCodigo = meta.ObservadorCodigo,
                FechaInicioMarea = _activeMareaManager.ActiveMarea.FechaInicio,
                FechaFinMarea = _activeMareaManager.ActiveMarea.FechaFin
            };

            var etapasOrdenadas = _activeMareaManager.ActiveMarea.Etapas.OrderBy(e => e.FechaZarpada).ToList();
            
            // Calcular rayas comunes para toda la marea (consistente con la UI)
            var todasEtapaIds = etapasOrdenadas.Select(e => e.ID).ToList();
            var todasProduccion = await dbContext.RegistrosProduccion
                .Include(rp => rp.Especie)
                .Where(rp => todasEtapaIds.Contains(rp.MareaEtapaId))
                .ToListAsync();
            var todosLances = await dbContext.Lances
                .Include(l => l.ItemsCaptura)
                    .ThenInclude(ic => ic.Especie)
                .Where(l => todasEtapaIds.Contains(l.MareaEtapaId))
                .ToListAsync();

            var rayaIdsEnMareaProd = todasProduccion
                .Where(p => IsRaya(p.Especie))
                .Select(p => p.EspecieId!)
                .Distinct()
                .ToHashSet();

            var rayaIdsEnMareaCatch = todosLances
                .SelectMany(l => l.ItemsCaptura)
                .Where(c => IsRaya(c.Especie))
                .Select(c => c.EspecieID!)
                .Distinct()
                .ToHashSet();

            var commonRayaIdsMarea = rayaIdsEnMareaProd.Intersect(rayaIdsEnMareaCatch).ToHashSet();

            for (int i = 0; i < etapasOrdenadas.Count; i++)
            {
                var etapa = etapasOrdenadas[i];
                // 1. Obtener lances de la etapa
                var etapaLances = await dbContext.Lances
                    .Include(l => l.ItemsCaptura)
                        .ThenInclude(ic => ic.Especie)
                    .Where(l => l.MareaEtapaId == etapa.ID)
                    .ToListAsync();

                foreach (var lance in etapaLances)
                {
                    foreach (var item in lance.ItemsCaptura) item.Lance = lance;
                }

                // 2. Obtener producción de la etapa
                var etapaProduccion = await dbContext.RegistrosProduccion
                    .Include(rp => rp.Especie)
                    .Include(rp => rp.Producto)
                    .Where(rp => rp.MareaEtapaId == etapa.ID)
                    .ToListAsync();

                var etapaReport = new ControlProduccionEtapaReport
                {
                    NumeroEtapa = i + 1,
                    FechaInicio = etapa.FechaZarpada,
                    FechaFin = etapa.FechaArribo ?? DateTime.Now,
                    Lats = etapaLances.Where(l => l.LatitudInicioDecimal.HasValue).Select(l => l.LatitudInicioDecimal!.Value).ToList(),
                    Lons = etapaLances.Where(l => l.LongitudInicioDecimal.HasValue).Select(l => l.LongitudInicioDecimal!.Value).ToList()
                };

                // 3. Balance de masa (Items) para la etapa
                var prodSummary = etapaProduccion
                    .GroupBy(p => (IsRaya(p.Especie) && !IsGenericRaya(p.Especie) && !commonRayaIdsMarea.Contains(p.EspecieId!)) 
                        ? RayaGenericVirtualId 
                        : (p.EspecieId ?? p.Especie?.FullDisplayName ?? p.Comentarios?.Replace("Importado: ", "") ?? "Desconocida"))
                    .ToDictionary(g => g.Key, g => new {
                        EspecieNombre = g.Key == RayaGenericVirtualId ? "Rayas (Rajidae - Otras/Genérico)" : (g.First().Especie?.FullDisplayName ?? g.First().Comentarios?.Replace("Importado: ", "") ?? "Desconocida"),
                        ProduccionTotal = g.Sum(p => p.Kg ?? 0),
                        CapturaReconstruida = g.Sum(p => (p.Kg ?? 0) * (p.Factor ?? 1.0))
                    });

                var catchSummary = etapaLances.SelectMany(l => l.ItemsCaptura)
                    .GroupBy(c => (IsRaya(c.Especie) && !IsGenericRaya(c.Especie) && !commonRayaIdsMarea.Contains(c.EspecieID!)) 
                        ? RayaGenericVirtualId 
                        : (c.EspecieID ?? c.Especie?.FullDisplayName ?? "Desconocida"))
                    .ToDictionary(g => g.Key, g => new {
                        EspecieNombre = g.Key == RayaGenericVirtualId ? "Rayas (Rajidae - Otras/Genérico)" : (g.First().Especie?.FullDisplayName ?? "Desconocida"),
                        CapturaBruta = g.Sum(c => c.CapturaTotalKgCalculado),
                        DescarteKg = g.Sum(c => c.PesoDescarteCalculado),
                        CapturaRetenida = g.Sum(c => c.CapturaTotalKgCalculado - c.PesoDescarteCalculado)
                    });

                var allSpeciesKeys = prodSummary.Keys.Union(catchSummary.Keys).ToList();
                foreach (var key in allSpeciesKeys)
                {
                    prodSummary.TryGetValue(key, out var pData);
                    catchSummary.TryGetValue(key, out var cData);

                    var itemVm = new ControlProduccionListItemViewModel
                    {
                        Especie = pData?.EspecieNombre ?? cData?.EspecieNombre ?? "Desconocida",
                        ProduccionTotal = pData?.ProduccionTotal ?? 0,
                        CapturaReconstruida = pData?.CapturaReconstruida ?? 0,
                        CapturaBruta = cData?.CapturaBruta ?? 0,
                        DescarteKg = cData?.DescarteKg ?? 0,
                        CapturaRetenida = cData?.CapturaRetenida ?? 0
                    };

                    etapaReport.Items.Add(new ControlProduccionReportItem
                    {
                        Especie = itemVm.Especie,
                        ProduccionTotal = itemVm.ProduccionTotal,
                        CapturaReconstruida = itemVm.CapturaReconstruida,
                        CapturaBruta = itemVm.CapturaBruta,
                        DescarteKg = itemVm.DescarteKg,
                        CapturaRetenida = itemVm.CapturaRetenida,
                        DiferenciaKg = itemVm.DiferenciaKg,
                        DiferenciaPorcentaje = itemVm.DiferenciaPorcentajeDisplay,
                        HasDiferenciaSignificativa = itemVm.HasDiferenciaSignificativa
                    });
                }
                etapaReport.Items = etapaReport.Items.OrderByDescending(i => i.CapturaBruta).ToList();

                // 4. Especies predominantes en la ETAPA (>= 20% de la producción de la etapa)
                double totalEtapaProduccion = etapaProduccion.Sum(p => p.Kg ?? 0);
                var predominantInEtapa = prodSummary
                    .Where(x => totalEtapaProduccion > 0 && (x.Value.ProduccionTotal / totalEtapaProduccion) >= 0.20)
                    .Select(x => x.Key)
                    .ToList();

                foreach (var key in predominantInEtapa)
                {
                    prodSummary.TryGetValue(key, out var pData);
                    var spName = pData?.EspecieNombre ?? "Desconocida";
                    var spLances = etapaLances.Where(l => l.ItemsCaptura.Any(ic => 
                    {
                        bool isRaya = IsRaya(ic.Especie);
                        bool isGeneric = isRaya && !IsGenericRaya(ic.Especie) && !commonRayaIdsMarea.Contains(ic.EspecieID!);
                        string icKey = isGeneric ? RayaGenericVirtualId : (ic.EspecieID ?? ic.Especie?.FullDisplayName ?? "Desconocida");
                        return icKey == key;
                    })).ToList();
                    var groupedByArea = spLances
                        .GroupBy(l => Math.Truncate(LegacyDecoder.CalculateGridArea(l.LatitudInicioDecimal ?? 0, l.LongitudInicioDecimal ?? 0)).ToString("0"))
                        .Select(g => 
                        {
                            double totalHoras = 0;
                            foreach(var l in g)
                            {
                                if (TimeSpan.TryParse(l.HoraInicio, out var tsI) && TimeSpan.TryParse(l.HoraFinal, out var tsF))
                                {
                                    if (tsF < tsI) tsF = tsF.Add(TimeSpan.FromDays(1));
                                    totalHoras += (tsF - tsI).TotalHours;
                                }
                            }

                            return new ControlProduccionAreaSummary
                            {
                                Especie = spName,
                                Area = g.Key,
                                CapturaKg = g.Sum(l => l.ItemsCaptura.Where(ic => 
                                {
                                    bool isR = IsRaya(ic.Especie);
                                    bool isG = isR && !IsGenericRaya(ic.Especie) && !commonRayaIdsMarea.Contains(ic.EspecieID!);
                                    string icK = isG ? RayaGenericVirtualId : (ic.EspecieID ?? ic.Especie?.FullDisplayName ?? "Desconocida");
                                    return icK == key;
                                }).Sum(ic => ic.CapturaTotalKgCalculado)),
                                DescarteKg = g.Sum(l => l.ItemsCaptura.Where(ic => 
                                {
                                    bool isR = IsRaya(ic.Especie);
                                    bool isG = isR && !IsGenericRaya(ic.Especie) && !commonRayaIdsMarea.Contains(ic.EspecieID!);
                                    string icK = isG ? RayaGenericVirtualId : (ic.EspecieID ?? ic.Especie?.FullDisplayName ?? "Desconocida");
                                    return icK == key;
                                }).Sum(ic => ic.PesoDescarteCalculado)),
                                TotalHoras = totalHoras,
                                CantidadLances = g.Count(),
                                DiasPesca = g.Select(l => l.Fecha).Distinct().Count()
                            };
                        })
                        .OrderBy(a => a.Area)
                        .ToList();
                    etapaReport.AreaSummaries.AddRange(groupedByArea);
                }

                // 5. Detalle producción de la etapa (AGRUPADO)
                etapaReport.ProduccionDetalle = etapaProduccion
                    .GroupBy(p => new { 
                        Especie = p.Especie?.FullDisplayName ?? p.Comentarios?.Replace("Importado: ", "") ?? "Desconocida",
                        Producto = p.Producto?.Codigo ?? "S/D",
                        Categoria = p.Categoria ?? ""
                    })
                    .Select(g => new ControlProduccionDetalleItem
                    {
                        Especie = g.Key.Especie,
                        Producto = g.Key.Producto,
                        Categoria = g.Key.Categoria,
                        Kilos = g.Sum(p => p.Kg ?? 0)
                    })
                    .OrderBy(p => p.Especie).ThenBy(p => p.Producto).ThenBy(p => p.Categoria)
                    .ToList();

                report.Etapas.Add(etapaReport);
            }

            var pdfBytes = await _reportService.GenerateControlProduccionPdfAsync(report);
            string fileName = $"Control_Produccion_{report.Barco}_{report.Marea}_{report.Anio}.pdf";
            string importFolder = MareaMetadataHelper.GetImportFolder(_activeMareaManager.ActiveMarea.Metadata);
            string savePath;

            if (!string.IsNullOrEmpty(importFolder))
            {
                savePath = Path.Combine(importFolder, "reportes", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
            }
            else
            {
                savePath = Path.Combine(Path.GetTempPath(), fileName);
            }

            await File.WriteAllBytesAsync(savePath, pdfBytes);

            Process.Start(new ProcessStartInfo
            {
                FileName = savePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error al generar el PDF: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task LoadControlProduccionAsync()
    {
        try
        {
            RecordsView.Filter = item => 
            {
                if (!_controlSoloConDiferencias) return true;
                if (item is ControlProduccionListItemViewModel cpItem)
                {
                    // Se ocultan las filas donde la diferencia es 0 (o despreciable)
                    return Math.Abs(cpItem.DiferenciaPorcentaje) > 0.0001;
                }
                return true;
            };
            var activeMarea = _activeMareaManager.ActiveMarea;
            if (activeMarea == null)
            {
                Records.Clear();
                return;
            }

            // Obtener lances (incluyendo ítems de captura) y producción
            using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var lances = await dbContext.Lances
                .Include(l => l.ItemsCaptura)
                    .ThenInclude(i => i.Especie)
                .Where(l => l.MareaEtapa!.MareaID == activeMarea.ID)
                .AsNoTracking()
                .ToListAsync();

            // Asegurar que cada ítem de captura tenga la referencia al lance para los cálculos de propiedades [NotMapped]
            foreach (var lance in lances)
            {
                foreach (var item in lance.ItemsCaptura)
                {
                    item.Lance = lance;
                }
            }

            var produccion = new List<RegistroProduccion>();
            foreach (var etapa in activeMarea.Etapas)
            {
                var etapaProduccion = await _produccionService.GetRegistrosProduccionAsync(etapa.ID);
                produccion.AddRange(etapaProduccion);
            }

            var rayaIdsInProd = produccion
                .Where(p => IsRaya(p.Especie))
                .Select(p => p.EspecieId!)
                .Distinct()
                .ToHashSet();

            var rayaIdsInCatch = lances
                .SelectMany(l => l.ItemsCaptura)
                .Where(c => IsRaya(c.Especie))
                .Select(c => c.EspecieID!)
                .Distinct()
                .ToHashSet();

            _commonRayaIds = rayaIdsInProd.Intersect(rayaIdsInCatch).ToHashSet();

            if (string.IsNullOrEmpty(ControlProduccionSelectedEspecieId))
            {
                // VISTA AGRUPADA POR ETAPA Y ESPECIE
                PageTitle = "Control Capt./Prod.";
                PageDescription = "Balance de masa por etapa para la marea activa.";
                
                SetColumnHeaders(
                    "Especie",
                    "Prod. Total",
                    "Capt. Recon.",
                    "Captura",
                    "Descarte",
                    "Capt. Retenida",
                    "Dif. Kg",
                    "Dif. %",
                    "");

                Records.Clear();
                var etapasOrdenadas = activeMarea.Etapas.OrderBy(e => e.FechaZarpada).ToList();
                
                for (int i = 0; i < etapasOrdenadas.Count; i++)
                {
                    var etapa = etapasOrdenadas[i];
                    var etapaLances = lances.Where(l => l.MareaEtapaId == etapa.ID).ToList();
                    var etapaProduccion = produccion.Where(p => p.MareaEtapaId == etapa.ID).ToList();
                    var etapaNombre = $"Etapa {i + 1} ({etapa.FechaZarpada:dd/MM} - {etapa.FechaArribo?.ToString("dd/MM") ?? "Act."})";

                    // Agrupar producción de la etapa
                    var prodSummary = etapaProduccion
                        .GroupBy(p => (IsRaya(p.Especie) && (IsGenericRaya(p.Especie) || !_commonRayaIds.Contains(p.EspecieId!))) ? RayaGenericVirtualId : (p.EspecieId ?? "S/D"))
                        .ToDictionary(g => g.Key, g => new {
                            EspecieNombre = g.Key == RayaGenericVirtualId ? "Rayas (Rajidae - Otras/Genérico)" : (g.First().Especie?.FullDisplayName ?? g.First().Comentarios?.Replace("Importado: ", "") ?? "Desconocida"),
                            PesoProcesadoTotal = g.Sum(p => p.Kg ?? 0),
                            CapturaReconstruida = g.Sum(p => (p.Kg ?? 0) * (p.Factor ?? 1.0))
                        });

                    // Agrupar capturas de la etapa
                    var catchSummary = etapaLances.SelectMany(l => l.ItemsCaptura)
                        .GroupBy(c => (IsRaya(c.Especie) && (IsGenericRaya(c.Especie) || !_commonRayaIds.Contains(c.EspecieID!))) ? RayaGenericVirtualId : (c.EspecieID ?? "S/D"))
                        .ToDictionary(g => g.Key, g => new {
                            EspecieNombre = g.Key == RayaGenericVirtualId ? "Rayas (Rajidae - Otras/Genérico)" : (g.First().Especie?.FullDisplayName ?? "Desconocida"),
                            CapturaBruta = g.Sum(c => c.CapturaTotalKgCalculado),
                            DescarteKg = g.Sum(c => c.PesoDescarteCalculado),
                            CapturaRetenida = g.Sum(c => c.CapturaTotalKgCalculado - c.PesoDescarteCalculado)
                        });

                    var allSpeciesIds = prodSummary.Keys.Union(catchSummary.Keys)
                        .OrderByDescending(spId => {
                            catchSummary.TryGetValue(spId, out var c);
                            return c?.CapturaBruta ?? 0;
                        })
                        .ToList();
                        
                    foreach (var spId in allSpeciesIds)
                    {
                        prodSummary.TryGetValue(spId, out var pData);
                        catchSummary.TryGetValue(spId, out var cData);

                        Records.Add(new ControlProduccionListItemViewModel
                        {
                            NumeroEtapa = i + 1,
                            EtapaDisplay = etapaNombre,
                            Especie = pData?.EspecieNombre ?? cData?.EspecieNombre ?? "Desconocida",
                            EspecieId = spId,
                            ProduccionTotal = pData?.PesoProcesadoTotal ?? 0,
                            CapturaReconstruida = pData?.CapturaReconstruida ?? 0,
                            CapturaBruta = cData?.CapturaBruta ?? 0,
                            DescarteKg = cData?.DescarteKg ?? 0,
                            CapturaRetenida = cData?.CapturaRetenida ?? 0,
                            IsSummaryView = true
                        });
                    }
                }

                // Aplicar agrupación en la vista
                RecordsView.GroupDescriptions.Clear();
                RecordsView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ControlProduccionListItemViewModel.EtapaDisplay)));
            }
            else
            {
                // VISTA DETALLE POR FECHA (filtrada por especie)
                if (ControlProduccionSelectedEspecie != null)
                {
                    PageTitle = $"{ControlProduccionSelectedEspecie.NombreVulgar} ({ControlProduccionSelectedEspecie.NombreCientifico})";
                }
                else
                {
                    PageTitle = "Detalle por Especie";
                }
                PageDescription = "Evolución diaria del balance de masa para la especie seleccionada.";

                SetColumnHeaders(
                    "Fecha",
                    "Prod. Total",
                    "Capt. Recon.",
                    "Captura",
                    "Descarte",
                    "Capt. Retenida",
                    "Dif. Kg",
                    "Dif. %",
                    "");

                bool isRayaGenericGroup = ControlProduccionSelectedEspecieId == RayaGenericVirtualId;

                // Agrupar producción filtrada por fecha
                var prodByDate = produccion
                    .Where(p => isRayaGenericGroup 
                        ? (IsRaya(p.Especie) && (IsGenericRaya(p.Especie) || !_commonRayaIds.Contains(p.EspecieId!))) 
                        : p.EspecieId == ControlProduccionSelectedEspecieId)
                    .Where(p => DateTime.TryParse(p.Fecha, out _))
                    .GroupBy(p => DateTime.Parse(p.Fecha).Date)
                    .ToDictionary(
                        g => g.Key,
                        g => new
                        {
                            EspecieNombre = isRayaGenericGroup ? "Rayas (Rajidae - Otras/Genérico)" : (g.First().Especie?.NombreVulgar 
                                ?? g.First().Especie?.NombreCientifico 
                                ?? g.First().Comentarios?.Replace("Importado: ", "") 
                                ?? "Desconocida"),
                            PesoProcesadoTotal = g.Sum(p => p.Kg ?? 0),
                            CapturaReconstruida = g.Sum(p => (p.Kg ?? 0) * (p.Factor ?? 1.0))
                        });

                // Agrupar capturas filtradas por fecha
                var catchByDate = lances
                    .Where(l => DateTime.TryParse(l.Fecha, out _))
                    .GroupBy(l => DateTime.Parse(l.Fecha).Date)
                    .Select(g => new
                    {
                        Fecha = g.Key,
                        CapturaBruta = g.SelectMany(l => l.ItemsCaptura)
                            .Where(c => isRayaGenericGroup 
                                ? (IsRaya(c.Especie) && (IsGenericRaya(c.Especie) || !_commonRayaIds.Contains(c.EspecieID!))) 
                                : c.EspecieID == ControlProduccionSelectedEspecieId)
                            .Sum(c => c.CapturaTotalKgCalculado),
                        DescarteKg = g.SelectMany(l => l.ItemsCaptura)
                            .Where(c => isRayaGenericGroup 
                                ? (IsRaya(c.Especie) && (IsGenericRaya(c.Especie) || !_commonRayaIds.Contains(c.EspecieID!))) 
                                : c.EspecieID == ControlProduccionSelectedEspecieId)
                            .Sum(c => c.PesoDescarteCalculado),
                        CapturaRetenida = g.SelectMany(l => l.ItemsCaptura)
                            .Where(c => isRayaGenericGroup 
                                ? (IsRaya(c.Especie) && (IsGenericRaya(c.Especie) || !_commonRayaIds.Contains(c.EspecieID!))) 
                                : c.EspecieID == ControlProduccionSelectedEspecieId)
                            .Sum(c => c.CapturaTotalKgCalculado - c.PesoDescarteCalculado)
                    })
                    .Where(x => x.CapturaRetenida > 0 || x.CapturaBruta > 0)
                    .ToDictionary(x => x.Fecha, x => x);

                // Unir fechas
                var allDates = prodByDate.Keys.Union(catchByDate.Keys).OrderBy(d => d).ToList();
                var results = new List<ControlProduccionListItemViewModel>();

                foreach (var date in allDates)
                {
                    prodByDate.TryGetValue(date, out var pData);
                    catchByDate.TryGetValue(date, out var cData);

                    results.Add(new ControlProduccionListItemViewModel
                    {
                        Fecha = date,
                        Especie = pData?.EspecieNombre ?? (isRayaGenericGroup ? "Rayas (Rajidae - Otras/Genérico)" : ControlProduccionSelectedEspecie?.FullDisplayName ?? "Desconocida"),
                        EspecieId = ControlProduccionSelectedEspecieId!,
                        ProduccionTotal = pData?.PesoProcesadoTotal ?? 0,
                        CapturaReconstruida = pData?.CapturaReconstruida ?? 0,
                        CapturaBruta = cData?.CapturaBruta ?? 0,
                        DescarteKg = cData?.DescarteKg ?? 0,
                        CapturaRetenida = cData?.CapturaRetenida ?? 0,
                        IsSummaryView = false
                    });
                }

                var viewModels = results.ToList(); // Ya vienen ordenados por fecha por allDates
                Records.Clear();
                foreach (var vm in viewModels)
                {
                    Records.Add(vm);
                }

                // No agrupamos en vista detalle
                RecordsView.GroupDescriptions.Clear();
            }

            // Intentar restaurar la selección previa para mantener el contexto del usuario
            var prevSelected = SelectedControlItem;
            ControlProduccionListItemViewModel? newSelected = null;

            if (prevSelected != null)
            {
                newSelected = Records.Cast<ControlProduccionListItemViewModel>()
                    .FirstOrDefault(r => 
                        r.IsSummaryView == prevSelected.IsSummaryView &&
                        r.EspecieId == prevSelected.EspecieId &&
                        r.NumeroEtapa == prevSelected.NumeroEtapa &&
                        (r.IsSummaryView || r.Fecha.Date == prevSelected.Fecha.Date));
            }

            if (newSelected != null)
            {
                SelectedRecord = newSelected;
            }
            else if (Records.Count > 0)
            {
                // Si no se encontró el previo o no había, seleccionar el primero
                SelectedRecord = Records[0];
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error al cargar el control de producción: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}
