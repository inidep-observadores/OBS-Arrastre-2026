using System;
using System.Linq;
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
using GMap.NET;

namespace OBSArrastre2026.App.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
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
    private readonly IMareaValidationService _validationService;
    private readonly IMareaReportService _reportService;
    private readonly IProduccionService _produccionService;
    private readonly IJsonImportService _jsonImportService;
    private readonly IMareaImportService _mareaImportService;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private NavigationItemViewModel? _selectedNavigationItem;
    private string _pageTitle = string.Empty;
    private string _pageDescription = string.Empty;
    private string _pageEyebrow = string.Empty;
    private string _primaryActionLabel = string.Empty;
    private bool _isDashboardVisible;
    private AppThemeMode _currentThemeMode;
    private object? _currentEditViewModel;
    private object? _activeDialog;
    
    // Datos para GMap.NET
    public List<MareaTracking> CurrentTrack { get; private set; } = new();
    public List<Lance> CurrentLances { get; private set; } = new();
    public event Action? MapUpdateRequested;
    public event Action<List<PointLatLng>>? MapFocusRequested;

    private object? _selectedRecord;

    // Filtros de Mareas
    private int? _mareasFilterAnio;
    private BuqueListItemViewModel? _mareasFilterBuque;
    private DateTime? _mareasFilterFechaDesde;
    private DateTime? _mareasFilterFechaHasta;
    private string _mareasSearchText = string.Empty;
    
    // Filtros de Lances
    private DateTime? _lancesFilterFechaDesde;
    private DateTime? _lancesFilterFechaHasta;
    private int? _lancesFilterNroLance;
    private string _lancesFilterEspecie = string.Empty;
    private int _currentTrackPointIndex = -1;
    private bool _isPlaying;
    private System.Windows.Threading.DispatcherTimer? _playbackTimer;
    private string _selectedTrackPointInfo = string.Empty;
    private DateTime? _selectedPlaybackDate;

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
        IDbContextFactory<AppDbContext> dbContextFactory)
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

        _mareasFilterAnio = DateTime.Today.Year;

        foreach (var navigationItem in _mockShellDataService.GetNavigationItems())
        {
            NavigationItems.Add(navigationItem);
        }

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

    public string Title => "OBS Arrastre 2026";

    public string SearchPlaceholder { get; }

    public ObservableCollection<NavigationItemViewModel> NavigationItems { get; } = [];

    public ObservableCollection<KpiCardViewModel> DashboardCards { get; } = [];

    public ObservableCollection<InsightCardViewModel> InsightCards { get; } = [];

    public ObservableCollection<TrendPointViewModel> TrendPoints { get; } = [];

    public ObservableCollection<string> ActiveFilters { get; } = [];

    public ObservableCollection<object> Records { get; } = [];

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

    private void OpenEditSubmuestraForm(MuestraListItemViewModel vm)
    {
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

    public int? MareasFilterAnio
    {
        get => _mareasFilterAnio;
        set
        {
            if (SetProperty(ref _mareasFilterAnio, value))
            {
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

    public int CurrentTrackPointIndex
    {
        get => _currentTrackPointIndex;
        set
        {
            if (SetProperty(ref _currentTrackPointIndex, value))
            {
                UpdateSelectedTrackPoint();
                
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

    public bool IsDashboardVisible
    {
        get => _isDashboardVisible;
        private set
        {
            if (!SetProperty(ref _isDashboardVisible, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsTableVisible));
        }
    }

    public bool IsTableVisible => !IsDashboardVisible;

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
    }

    private void LoadSection(NavigationSection section)
    {
        if (section == NavigationSection.Inicio)
        {
            LoadDashboard();
            return;
        }

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

            ClearDashboardCollections();
            IsDashboardVisible = false;
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

            ClearDashboardCollections();
            IsDashboardVisible = false;
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

            ClearDashboardCollections();
            IsDashboardVisible = false;
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

            ClearDashboardCollections();
            IsDashboardVisible = false;
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
                "Factor",
                "Kg");

            ClearDashboardCollections();
            IsDashboardVisible = false;
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
        ClearDashboardCollections();
        IsDashboardVisible = false;
    }

    private void LoadDashboard()
    {
        var dashboard = _mockShellDataService.GetDashboard();

        PageEyebrow = dashboard.Eyebrow;
        PageTitle = dashboard.Title;
        PageDescription = dashboard.Description;
        PrimaryActionLabel = dashboard.PrimaryActionLabel;

        ReplaceItems(DashboardCards, dashboard.Cards);
        ReplaceItems(InsightCards, dashboard.Insights);
        ReplaceItems(TrendPoints, dashboard.Trends);
        ActiveFilters.Clear();
        Records.Clear();
        SetColumnHeaders(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        IsDashboardVisible = true;
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

            var viewModels = mareas.Select(m => new MareaListItemViewModel(m, _activeMareaManager, _validationService, _reportService)).ToList();
            
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
            _ = LoadMareasAsync(); // Recargar lista al cerrar
        }, null);
        vm.ShowCustomDialog = diag => ActiveDialog = diag;
        vm.ShowMessage = (t, m, d, type) => ShowMessage(t, m, d, type);
        vm.ShowConfirmation = (t, m) => ShowConfirmationAsync(t, m);
        CurrentEditViewModel = vm;
    }

    private void OpenEditMareaForm(MareaListItemViewModel? item)
    {
        if (item == null) return;
        
        var vm = _mareaEditFactory(() => 
        {
            CurrentEditViewModel = null;
            _ = LoadMareasAsync(); // Recargar lista al cerrar
        }, item.ID);
        vm.ShowCustomDialog = diag => ActiveDialog = diag;
        vm.ShowMessage = (t, m, d, type) => ShowMessage(t, m, d, type);
        vm.ShowConfirmation = (t, m) => ShowConfirmationAsync(t, m);
        CurrentEditViewModel = vm;
    }

    private async Task LoadLancesAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_activeMareaManager.ActiveMareaId))
            {
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

            // 2. Obtener Track
            CurrentTrack = await dbContext.TrackingPoints
                .Where(t => t.MareaID == _activeMareaManager.ActiveMareaId)
                .OrderBy(t => t.FechaHora)
                .ToListAsync();

            // Notificar a la vista para que actualice GMap.NET
            MapUpdateRequested?.Invoke();
            
            OnPropertyChanged(nameof(TotalTrackPoints));
            CurrentTrackPointIndex = -1;
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

    private void OpenCreateLanceForm()
    {
        ShowMessage("Nuevo Lance", "Para crear un nuevo lance, debe hacerlo desde la sección de Mareas > Etapas para mantener la consistencia de datos.", null, MessageDialogType.Info);
    }

    private void ApplyTheme(AppThemeMode mode)
    {
        _themeService.ApplyTheme(mode);
        CurrentThemeMode = mode;
    }

    private void ClearDashboardCollections()
    {
        DashboardCards.Clear();
        InsightCards.Clear();
        TrendPoints.Clear();
    }

    private void SetColumnHeaders(string column1, string column2, string column3, string column4, string column5, string column6 = "", string column7 = "")
    {
        Column1Header = column1;
        Column2Header = column2;
        Column3Header = column3;
        Column4Header = column4;
        Column5Header = column5;
        Column6Header = column6;
        Column7Header = column7;

        OnPropertyChanged(nameof(Column1Header));
        OnPropertyChanged(nameof(Column2Header));
        OnPropertyChanged(nameof(Column3Header));
        OnPropertyChanged(nameof(Column4Header));
        OnPropertyChanged(nameof(Column5Header));
        OnPropertyChanged(nameof(Column6Header));
        OnPropertyChanged(nameof(Column7Header));
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
            
        // Formato GGº MM,M' C (C= cuadrante N,S,E,O)
        return $"{degrees}º {minutes:00.1}' {quadrant}".Replace('.', ',');
    }

    private void OnRecordSelected(object? record)
    {
        // 1. Notificar a la vista que debe redibujar para aplicar el resaltado de color
        MapUpdateRequested?.Invoke();

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

    private static void ReplaceItems<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();

        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    private async Task ImportMareaAsync()
    {
        var importVm = new ImportDbfViewModel(
            null, // Nueva marea
            0,
            DateTime.Today.Year,
            _mareaImportService,
            _jsonImportService,
            _mareaService,
            "Sin Nombre",
            [],
            async files => 
            {
                ActiveDialog = null;
                if (files != null)
                {
                    await LoadMareasAsync();
                    await LoadFilterDataAsync();
                    ShowMessage("Importación Exitosa", "La marea y sus datos han sido importados correctamente.", null, MessageDialogType.Success);
                }
            });

        importVm.ShowMessage = (title, msg, details, type) => ShowMessage(title, msg, details, type);
        importVm.ShowConfirmation = (title, msg) => ShowConfirmationAsync(title, msg);

        ActiveDialog = importVm;
    }
}
