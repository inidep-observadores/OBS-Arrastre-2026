using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OBSArrastre2026.App.Models;
using OBSArrastre2026.App.Services;

namespace OBSArrastre2026.App.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly IMockShellDataService _mockShellDataService;
    private readonly IThemeService _themeService;
    private readonly IMareaService _mareaService;
    private readonly IBuqueService _buqueService;
    private readonly ILanceService _lanceService;
    private readonly Func<Action, string?, MareaEditViewModel> _mareaEditFactory;
    private readonly Func<Action, string, string?, LanceEditViewModel> _lanceEditFactory;
    private readonly IActiveMareaManager _activeMareaManager;
    private readonly IMareaValidationService _validationService;
    private readonly IMareaReportService _reportService;
    private NavigationItemViewModel? _selectedNavigationItem;
    private string _pageTitle = string.Empty;
    private string _pageDescription = string.Empty;
    private string _pageEyebrow = string.Empty;
    private string _primaryActionLabel = string.Empty;
    private bool _isDashboardVisible;
    private AppThemeMode _currentThemeMode;
    private object? _currentEditViewModel;
    private object? _activeDialog;

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

    public MainWindowViewModel(
        IMockShellDataService mockShellDataService, 
        IThemeService themeService,
        IMareaService mareaService,
        IBuqueService buqueService,
        ILanceService lanceService,
        IActiveMareaManager activeMareaManager,
        Func<Action, string?, MareaEditViewModel> mareaEditFactory,
        Func<Action, string, string?, LanceEditViewModel> lanceEditFactory,
        IMareaValidationService validationService,
        IMareaReportService reportService)
    {
        _mockShellDataService = mockShellDataService;
        _themeService = themeService;
        _mareaService = mareaService;
        _buqueService = buqueService;
        _lanceService = lanceService;
        _activeMareaManager = activeMareaManager;
        _mareaEditFactory = mareaEditFactory;
        _lanceEditFactory = lanceEditFactory;
        _validationService = validationService;
        _reportService = reportService;

        SearchPlaceholder = "Buscar...";
        SetSystemThemeCommand = new RelayCommand(() => ApplyTheme(AppThemeMode.System));
        SetLightThemeCommand = new RelayCommand(() => ApplyTheme(AppThemeMode.Light));
        SetDarkThemeCommand = new RelayCommand(() => ApplyTheme(AppThemeMode.Dark));
        PrimaryActionCommand = new RelayCommand(OpenCreateMareaForm);
        ApplyMareaFiltersCommand = new AsyncCommand(LoadMareasAsync);
        EditMareaCommand = new RelayCommand<MareaListItemViewModel>(OpenEditMareaForm);
        
        ApplyLanceFiltersCommand = new AsyncCommand(LoadLancesAsync);
        EditLanceCommand = new RelayCommand<LanceListItemViewModel>(OpenEditLanceForm);
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

        UpdateNavigationState();

        _activeMareaManager.PropertyChanged += (s, e) => 
        {
            if (e.PropertyName == nameof(IActiveMareaManager.ActiveMareaId))
            {
                OnPropertyChanged(nameof(ActiveMareaManager));
                UpdateNavigationState();
                _ = RefreshCurrentSectionAsync();
            }
        };
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

    public ICommand PrimaryActionCommand { get; }

    public ICommand EditMareaCommand { get; }
    public ICommand ApplyLanceFiltersCommand { get; }
    public ICommand EditLanceCommand { get; }
    public ICommand ClearMareaFiltersCommand { get; }
    public ICommand ClearLanceFiltersCommand { get; }

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

    public ICommand ApplyMareaFiltersCommand { get; }
    
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
            
            SetColumnHeaders(
                lanceSection.Column1Header,
                lanceSection.Column2Header,
                lanceSection.Column3Header,
                lanceSection.Column4Header,
                lanceSection.Column5Header);

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

            var viewModels = lances.Select(l => new LanceListItemViewModel(l)).ToList();
            
            Records.Clear();
            foreach (var vm in viewModels) Records.Add(vm);

            ActiveFilters.Clear();
            if (LancesFilterFechaDesde.HasValue) ActiveFilters.Add($"Desde: {LancesFilterFechaDesde.Value:dd/MM/yyyy}");
            if (LancesFilterNroLance.HasValue) ActiveFilters.Add($"Lance: {LancesFilterNroLance}");
            if (!string.IsNullOrWhiteSpace(LancesFilterEspecie)) ActiveFilters.Add($"Especie: {LancesFilterEspecie}");
            if (_activeMareaManager.ActiveMarea != null) ActiveFilters.Add($"Marea Activa: {_activeMareaManager.ActiveMarea.NumeroInidep}/{_activeMareaManager.ActiveMarea.AnioInidep}");
        }
        catch (Exception) { /* Log error */ }
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

    private void SetColumnHeaders(string column1, string column2, string column3, string column4, string column5)
    {
        Column1Header = column1;
        Column2Header = column2;
        Column3Header = column3;
        Column4Header = column4;
        Column5Header = column5;

        OnPropertyChanged(nameof(Column1Header));
        OnPropertyChanged(nameof(Column2Header));
        OnPropertyChanged(nameof(Column3Header));
        OnPropertyChanged(nameof(Column4Header));
        OnPropertyChanged(nameof(Column5Header));
    }

    private static void ReplaceItems<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();

        foreach (var item in source)
        {
            target.Add(item);
        }
    }
}
