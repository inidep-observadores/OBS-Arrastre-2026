using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using RelayCommand = CommunityToolkit.Mvvm.Input.RelayCommand;

namespace OBSArrastre2026.App.ViewModels;

public sealed class DbfFileItem : ObservableObject
{
    public string Name { get; init; } = string.Empty;
    public string FullPath { get; init; } = string.Empty;
    public string SizeDisplay { get; init; } = string.Empty;
    public string DateDisplay { get; init; } = string.Empty;
}

public sealed partial class ImportDbfViewModel : ObservableObject
{
    private readonly Action<IEnumerable<string>?> _onFinished;
    private readonly int _marea;
    private readonly int _anio;
    private readonly string _pattern;

    public ImportDbfViewModel(int marea, int anio, Action<IEnumerable<string>?> onFinished)
    {
        _marea = marea;
        _anio = anio;
        _onFinished = onFinished;
        _pattern = $"{marea}{anio % 100:D2}";

        AddFilesCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(AddFiles);
        AcceptCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(Accept, () => SelectedFiles.Count > 0);
        CancelCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() => _onFinished(null));
    }

    public string PatternNote => $"Patrón esperado: *{_pattern}*.dbf";
    
    public ObservableCollection<DbfFileItem> SelectedFiles { get; } = [];

    public ICommand AddFilesCommand { get; }
    public IRelayCommand AcceptCommand { get; }
    public ICommand CancelCommand { get; }

    private void AddFiles()
    {
        var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Filter = $"Archivos de Marea (*{_pattern}*.dbf)|*{_pattern}*.dbf|Todos los archivos (*.*)|*.*",
            Title = "Seleccionar Archivos DBF de Marea"
        };

        if (dialog.ShowDialog() == true)
        {
            foreach (var filePath in dialog.FileNames)
            {
                var fileName = Path.GetFileName(filePath);
                
                // Validación estricta secundaria
                if (!fileName.Contains(_pattern, StringComparison.OrdinalIgnoreCase))
                {
                    continue; // O podrías mostrar un aviso
                }

                // Evitar duplicados
                if (SelectedFiles.Any(f => f.FullPath.Equals(filePath, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var info = new FileInfo(filePath);
                SelectedFiles.Add(new DbfFileItem
                {
                    Name = fileName,
                    FullPath = filePath,
                    SizeDisplay = FormatSize(info.Length),
                    DateDisplay = info.LastWriteTime.ToString("g")
                });
            }
            AcceptCommand.NotifyCanExecuteChanged();
        }
    }

    private void Accept()
    {
        _onFinished(SelectedFiles.Select(f => f.FullPath));
    }

    private string FormatSize(long bytes)
    {
        if (bytes >= 1024 * 1024)
            return $"{(double)bytes / (1024 * 1024):F2} MB";
        return $"{(double)bytes / 1024:F2} KB";
    }
}
