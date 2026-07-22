using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlMareas.App.Services;

namespace ControlMareas.App.ViewModels;

public class ValidarMareaDialogViewModel : ObservableObject
{
    private readonly IUserSettingsService _userSettingsService;
    private bool _filtrarDiferencias;
    private double _toleranciaFiltro;
    private bool _omitirValidacionCapturaProduccion;
    private bool _omitirValidacionMuestraSubmuestra;

    public bool OmitirValidacionMuestraSubmuestra
    {
        get => _omitirValidacionMuestraSubmuestra;
        set => SetProperty(ref _omitirValidacionMuestraSubmuestra, value);
    }

    public bool OmitirValidacionCapturaProduccion
    {
        get => _omitirValidacionCapturaProduccion;
        set 
        {
            SetProperty(ref _omitirValidacionCapturaProduccion, value);
            OnPropertyChanged(nameof(IsFiltrarHabilitado));
            OnPropertyChanged(nameof(IsToleranciaHabilitado));
        }
    }

    public bool IsFiltrarHabilitado => !OmitirValidacionCapturaProduccion;
    public bool IsToleranciaHabilitado => FiltrarDiferencias && !OmitirValidacionCapturaProduccion;

    public bool FiltrarDiferencias
    {
        get => _filtrarDiferencias;
        set 
        {
            SetProperty(ref _filtrarDiferencias, value);
            OnPropertyChanged(nameof(IsToleranciaHabilitado));
        }
    }

    public double ToleranciaFiltro
    {
        get => _toleranciaFiltro;
        set
        {
            // Validar que esté entre 0 y 100
            if (value < 0) value = 0;
            if (value > 100) value = 100;
            SetProperty(ref _toleranciaFiltro, value);
        }
    }

    public TaskCompletionSource<bool> DialogResult { get; } = new();

    public ICommand AcceptCommand { get; }
    public ICommand CancelCommand { get; }

    public ValidarMareaDialogViewModel(IUserSettingsService userSettingsService)
    {
        _userSettingsService = userSettingsService;
        
        var settings = _userSettingsService.GetSettings();
        _filtrarDiferencias = settings.FiltrarDiferenciasAuditoria;
        _toleranciaFiltro = settings.ToleranciaFiltroAuditoria;
        _omitirValidacionMuestraSubmuestra = settings.OmitirValidacionMuestraSubmuestra;

        AcceptCommand = new RelayCommand(Accept);
        CancelCommand = new RelayCommand(Cancel);
    }

    private void Accept()
    {
        _userSettingsService.UpdateSettings(s =>
        {
            s.FiltrarDiferenciasAuditoria = FiltrarDiferencias;
            s.ToleranciaFiltroAuditoria = ToleranciaFiltro;
            s.OmitirValidacionMuestraSubmuestra = OmitirValidacionMuestraSubmuestra;
        });

        DialogResult.TrySetResult(true);
    }

    private void Cancel()
    {
        DialogResult.TrySetResult(false);
    }
}
