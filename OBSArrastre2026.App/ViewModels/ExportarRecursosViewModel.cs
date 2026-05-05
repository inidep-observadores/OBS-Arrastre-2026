using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace OBSArrastre2026.App.ViewModels;

public class ExportarRecursosViewModel : ObservableObject
{
    private string _exportPath = string.Empty;
    public string ExportPath
    {
        get => _exportPath;
        set => SetProperty(ref _exportPath, value);
    }

    public ICommand AcceptCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand BrowseCommand { get; }

    public TaskCompletionSource<bool> DialogResult { get; } = new();

    public ExportarRecursosViewModel()
    {
        AcceptCommand = new RelayCommand(() => DialogResult.TrySetResult(true));
        CloseCommand = new RelayCommand(() => DialogResult.TrySetResult(false));
        BrowseCommand = new RelayCommand(Browse);
    }

    private void Browse()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Seleccionar carpeta de exportación",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            ExportPath = dialog.FolderName;
        }
    }
}
