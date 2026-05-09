using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OBSArrastre2026.App.ViewModels;

public partial class ProcesosViewModel : ObservableObject
{
    public ICommand GenerarInformeCommand { get; }
    public ICommand ExportarDbfCommand { get; }
    public ICommand CambiarUnidadDescarteCommand { get; }

    public ProcesosViewModel(
        ICommand generarInformeCommand,
        ICommand exportarDbfCommand,
        ICommand cambiarUnidadDescarteCommand)
    {
        GenerarInformeCommand = generarInformeCommand;
        ExportarDbfCommand = exportarDbfCommand;
        CambiarUnidadDescarteCommand = cambiarUnidadDescarteCommand;
    }
}
