using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlMareas.App.Models;

namespace ControlMareas.App.ViewModels;

public class ConfigurarUnidadDescarteViewModel : ObservableObject
{
    private TipoDatoDescarte _tipoSeleccionado;
    public TipoDatoDescarte TipoSeleccionado
    {
        get => _tipoSeleccionado;
        set => SetProperty(ref _tipoSeleccionado, value);
    }

    public ICommand AcceptCommand { get; }
    public ICommand CancelCommand { get; }

    public TaskCompletionSource<bool> DialogResult { get; } = new();

    public ConfigurarUnidadDescarteViewModel()
    {
        _tipoSeleccionado = TipoDatoDescarte.Kilogramos;
        AcceptCommand = new RelayCommand(() => DialogResult.TrySetResult(true));
        CancelCommand = new RelayCommand(() => DialogResult.TrySetResult(false));
    }
}
