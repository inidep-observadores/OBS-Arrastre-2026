using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using OBSArrastre2026.App.Models;
using OBSArrastre2026.App.Services;

namespace OBSArrastre2026.App.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly IMockShellDataService _mockShellDataService;
    private readonly IThemeService _themeService;
    private readonly IMareaService _mareaService;
    private readonly IBuqueService _buqueService;
    private readonly Func<Action, MareaEditViewModel> _mareaEditFactory;
    private NavigationItemViewModel? _selectedNavigationItem;
    private string _pageTitle = string.Empty;
    private string _pageDescription = string.Empty;
    private string _pageEyebrow = string.Empty;
    private string _primaryActionLabel = string.Empty;
    private bool _isDashboardVisible;
    private AppThemeMode _currentThemeMode;
    private object? _currentEditViewModel;

    // Filtros de Mareas
    private int? _mareasFilterAnio;
    private BuqueListItemViewModel? _mareasFilterBuque;
    private DateTime? _mareasFilterFechaDesde;
    private DateTime? _mareasFilterFechaHasta;
    private string _mareasSearchText = string.Empty;

    public MainWindowViewModel(
        IMockShellDataService mockShellDataService, 
        IThemeService themeService,
        IMareaService mareaService,
        IBuqueService buqueService,
        Func<Action, MareaEditViewModel> mareaEditFactory)
    {
        _mockShellDataService = mockShellDataService;
        _themeService = themeService;
        _mareaService = mareaService;
        _buqueService = buqueService;
        _mareaEditFactory = mareaEditFactory;

        SearchPlaceholder = "Buscar...";
        SetSystemThemeCommand = new RelayCommand(() => ApplyTheme(AppThemeMode.System));
        SetLightThemeCommand = new RelayCommand(() => ApplyTheme(AppThemeMode.Light));
        SetDarkThemeCommand = new RelayCommand(() => ApplyTheme(AppThemeMode.Dark));
        PrimaryActionCommand = new RelayCommand(OpenNewMareaForm);
        ApplyMareaFiltersCommand = new AsyncCommand(LoadMareasAsync);

        _mareasFilterAnio = DateTime.Today.Year;

        foreach (var navigationItem in _mockShellDataService.GetNavigationItems())
        {
            NavigationItems.Add(navigationItem);
        }

        _currentThemeMode = _themeService.CurrentMode;
        SelectedNavigationItem = NavigationItems.FirstOrDefault();

        _ = LoadFilterDataAsync();
    }

    public string Title => "OBS Arrastre 2026";

    public string SearchPlaceholder { get; }

    public ObservableCollection<NavigationItemViewModel> NavigationItems { get; } = [];

    public ObservableCollection<KpiCardViewModel> DashboardCards { get; } = [];

    public ObservableCollection<InsightCardViewModel> InsightCards { get; } = [];

    public ObservableCollection<TrendPointViewModel> TrendPoints { get; } = [];

    public ObservableCollection<string> ActiveFilters { get; } = [];

    public ObservableCollection<object> Records { get; } = [];

    public ObservableCollection<BuqueListItemViewModel> Buques { get; } = [];

    public ObservableCollection<int> Anios { get; } = [];

    public ICommand SetSystemThemeCommand { get; }

    public ICommand SetLightThemeCommand { get; }

    public ICommand SetDarkThemeCommand { get; }

    public ICommand PrimaryActionCommand { get; }

    public NavigationItemViewModel? SelectedNavigationItem
    {
        get => _selectedNavigationItem;
        set
        {
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
        set => SetProperty(ref _mareasFilterAnio, value);
    }

    public BuqueListItemViewModel? MareasFilterBuque
    {
        get => _mareasFilterBuque;
        set => SetProperty(ref _mareasFilterBuque, value);
    }

    public DateTime? MareasFilterFechaDesde
    {
        get => _mareasFilterFechaDesde;
        set => SetProperty(ref _mareasFilterFechaDesde, value);
    }

    public DateTime? MareasFilterFechaHasta
    {
        get => _mareasFilterFechaHasta;
        set => SetProperty(ref _mareasFilterFechaHasta, value);
    }

    public string MareasSearchText
    {
        get => _mareasSearchText;
        set => SetProperty(ref _mareasSearchText, value);
    }

    public object? CurrentEditViewModel
    {
        get => _currentEditViewModel;
        private set => SetProperty(ref _currentEditViewModel, value);
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
            var buques = await _buqueService.GetBuquesAsync();
            Buques.Clear();
            foreach (var buque in buques)
            {
                Buques.Add(buque);
            }
        }
        catch (Exception ex)
        {
            // Podríamos loguear el error aquí
            System.Diagnostics.Debug.WriteLine($"Error cargando datos de filtro: {ex.Message}");
        }
    }
        {
            // Log error
        }
    }

    private async Task LoadMareasAsync()
    {
        try
        {
            var mareas = await _mareaService.GetMareasAsync(
                _mareasFilterAnio,
                _mareasFilterBuque?.ID,
                _mareasFilterFechaDesde,
                _mareasFilterFechaHasta,
                _mareasSearchText);

            var viewModels = mareas.Select(m => new MareaListItemViewModel(m)).ToList();
            
            Records.Clear();
            foreach (var vm in viewModels)
            {
                Records.Add(vm);
            }

            // Actualizar filtros activos visuales
            ActiveFilters.Clear();
            if (_mareasFilterAnio.HasValue) ActiveFilters.Add($"Año: {_mareasFilterAnio}");
            if (_mareasFilterBuque != null) ActiveFilters.Add($"Buque: {_mareasFilterBuque.Nombre}");
        }
        catch (Exception)
        {
            // Log error
        }
    }

    private void OpenNewMareaForm()
    {
        CurrentEditViewModel = _mareaEditFactory(() => CurrentEditViewModel = null);
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
