using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace OBSArrastre2026.App.ViewModels;

public class ProcesosViewModel : ObservableObject
{
    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    private string _busyMessage = string.Empty;
    public string BusyMessage
    {
        get => _busyMessage;
        set => SetProperty(ref _busyMessage, value);
    }

    public ICommand GenerarInformeCommand { get; }
    public ICommand GenerarRecibiCommand { get; }
    public ICommand ExportarDbfCommand { get; }
    public ICommand CambiarUnidadDescarteCommand { get; }

    public ProcesosViewModel(
        ICommand generarInformeCommand,
        ICommand generarRecibiCommand,
        ICommand exportarDbfCommand,
        ICommand cambiarUnidadDescarteCommand)
    {
        GenerarInformeCommand = generarInformeCommand;
        GenerarRecibiCommand = generarRecibiCommand;
        ExportarDbfCommand = exportarDbfCommand;
        CambiarUnidadDescarteCommand = cambiarUnidadDescarteCommand;
    }
}
