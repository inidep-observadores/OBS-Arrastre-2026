using CommunityToolkit.Mvvm.ComponentModel;
using OBSArrastre2026.App.Models;
using OBSArrastre2026.App.Services;
using System.Collections.Generic;
using System.Windows.Input;

namespace OBSArrastre2026.App.ViewModels;

public partial class ConfiguracionViewModel : ObservableObject
{
    private readonly IThemeService _themeService;
    private readonly IUserSettingsService _settingsService;

    private string _nombreRevisor = "";
    private string _apellidoRevisor = "";
    private AppThemeMode _selectedTheme;

    public string NombreRevisor
    {
        get => _nombreRevisor;
        set
        {
            if (SetProperty(ref _nombreRevisor, value))
            {
                GuardarPreferencias();
            }
        }
    }

    public string ApellidoRevisor
    {
        get => _apellidoRevisor;
        set
        {
            if (SetProperty(ref _apellidoRevisor, value))
            {
                GuardarPreferencias();
            }
        }
    }

    public AppThemeMode SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (SetProperty(ref _selectedTheme, value))
            {
                _themeService.ApplyTheme(value);
                OnPropertyChanged(nameof(IsSystemThemeActive));
                OnPropertyChanged(nameof(IsLightThemeActive));
                OnPropertyChanged(nameof(IsDarkThemeActive));
                // El servicio de temas ya persiste el modo, pero guardamos para asegurar consistencia si fuera necesario
                GuardarPreferencias();
            }
        }
    }

    public bool IsSystemThemeActive => SelectedTheme == AppThemeMode.System;
    public bool IsLightThemeActive => SelectedTheme == AppThemeMode.Light;
    public bool IsDarkThemeActive => SelectedTheme == AppThemeMode.Dark;

    public ICommand SetSystemThemeCommand { get; }
    public ICommand SetLightThemeCommand { get; }
    public ICommand SetDarkThemeCommand { get; }

    public ConfiguracionViewModel(IThemeService themeService, IUserSettingsService settingsService)
    {
        _themeService = themeService;
        _settingsService = settingsService;

        var settings = _settingsService.GetSettings();
        _nombreRevisor = settings.RevisorNombre ?? "";
        _apellidoRevisor = settings.RevisorApellido ?? "";
        _selectedTheme = settings.ThemeMode;

        SetSystemThemeCommand = new RelayCommand(() => SelectedTheme = AppThemeMode.System);
        SetLightThemeCommand = new RelayCommand(() => SelectedTheme = AppThemeMode.Light);
        SetDarkThemeCommand = new RelayCommand(() => SelectedTheme = AppThemeMode.Dark);
    }

    private void GuardarPreferencias()
    {
        _settingsService.UpdateSettings(s =>
        {
            s.RevisorNombre = NombreRevisor;
            s.RevisorApellido = ApellidoRevisor;
            s.ThemeMode = SelectedTheme;
        });
    }
}
