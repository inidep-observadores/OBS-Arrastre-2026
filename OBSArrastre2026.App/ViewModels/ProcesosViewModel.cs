using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OBSArrastre2026.App.ViewModels;

public partial class ProcesosViewModel : ObservableObject
{
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
