using System.Collections.ObjectModel;
using System.Windows.Input;
using OBSArrastre2026.App.Models;
using OBSArrastre2026.App.Services;

namespace OBSArrastre2026.App.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly IMockShellDataService _mockShellDataService;
    private readonly IThemeService _themeService;
    private NavigationItemViewModel? _selectedNavigationItem;
    private string _pageTitle = string.Empty;
    private string _pageDescription = string.Empty;
    private string _pageEyebrow = string.Empty;
    private string _primaryActionLabel = string.Empty;
    private bool _isDashboardVisible;
    private AppThemeMode _currentThemeMode;

    public MainWindowViewModel(IMockShellDataService mockShellDataService, IThemeService themeService)
    {
        _mockShellDataService = mockShellDataService;
        _themeService = themeService;

        SearchPlaceholder = "Buscar en la maqueta...";
        SetSystemThemeCommand = new RelayCommand(() => ApplyTheme(AppThemeMode.System));
        SetLightThemeCommand = new RelayCommand(() => ApplyTheme(AppThemeMode.Light));
        SetDarkThemeCommand = new RelayCommand(() => ApplyTheme(AppThemeMode.Dark));
        PrimaryActionCommand = new RelayCommand(() => { });

        foreach (var navigationItem in _mockShellDataService.GetNavigationItems())
        {
            NavigationItems.Add(navigationItem);
        }

        _currentThemeMode = _themeService.CurrentMode;
        SelectedNavigationItem = NavigationItems.FirstOrDefault();
    }

    public string Title => "OBS Arrastre 2026";

    public string SearchPlaceholder { get; }

    public ObservableCollection<NavigationItemViewModel> NavigationItems { get; } = [];

    public ObservableCollection<KpiCardViewModel> DashboardCards { get; } = [];

    public ObservableCollection<InsightCardViewModel> InsightCards { get; } = [];

    public ObservableCollection<TrendPointViewModel> TrendPoints { get; } = [];

    public ObservableCollection<string> ActiveFilters { get; } = [];

    public ObservableCollection<MockRecordRowViewModel> Records { get; } = [];

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

    private void LoadSection(NavigationSection section)
    {
        if (section == NavigationSection.Inicio)
        {
            LoadDashboard();
            return;
        }

        var listSection = _mockShellDataService.GetListSection(section);

        PageEyebrow = listSection.Eyebrow;
        PageTitle = listSection.Title;
        PageDescription = listSection.Description;
        PrimaryActionLabel = listSection.PrimaryActionLabel;
        Column1Header = listSection.Column1Header;
        Column2Header = listSection.Column2Header;
        Column3Header = listSection.Column3Header;
        Column4Header = listSection.Column4Header;
        Column5Header = listSection.Column5Header;

        ActiveFilters.Clear();
        foreach (var filter in listSection.Filters)
        {
            ActiveFilters.Add(filter);
        }

        Records.Clear();
        foreach (var row in listSection.Rows)
        {
            Records.Add(row);
        }

        DashboardCards.Clear();
        InsightCards.Clear();
        TrendPoints.Clear();
        IsDashboardVisible = false;

        OnPropertyChanged(nameof(Column1Header));
        OnPropertyChanged(nameof(Column2Header));
        OnPropertyChanged(nameof(Column3Header));
        OnPropertyChanged(nameof(Column4Header));
        OnPropertyChanged(nameof(Column5Header));
    }

    private void LoadDashboard()
    {
        var dashboard = _mockShellDataService.GetDashboard();

        PageEyebrow = dashboard.Eyebrow;
        PageTitle = dashboard.Title;
        PageDescription = dashboard.Description;
        PrimaryActionLabel = dashboard.PrimaryActionLabel;

        DashboardCards.Clear();
        foreach (var card in dashboard.Cards)
        {
            DashboardCards.Add(card);
        }

        InsightCards.Clear();
        foreach (var card in dashboard.Insights)
        {
            InsightCards.Add(card);
        }

        TrendPoints.Clear();
        foreach (var point in dashboard.Trends)
        {
            TrendPoints.Add(point);
        }

        ActiveFilters.Clear();
        Records.Clear();
        Column1Header = string.Empty;
        Column2Header = string.Empty;
        Column3Header = string.Empty;
        Column4Header = string.Empty;
        Column5Header = string.Empty;
        IsDashboardVisible = true;

        OnPropertyChanged(nameof(Column1Header));
        OnPropertyChanged(nameof(Column2Header));
        OnPropertyChanged(nameof(Column3Header));
        OnPropertyChanged(nameof(Column4Header));
        OnPropertyChanged(nameof(Column5Header));
    }

    private void ApplyTheme(AppThemeMode mode)
    {
        _themeService.ApplyTheme(mode);
        CurrentThemeMode = mode;
    }
}
