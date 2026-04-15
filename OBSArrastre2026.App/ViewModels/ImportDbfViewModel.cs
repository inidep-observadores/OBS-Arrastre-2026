using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using OBSArrastre2026.App.Services;

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
    private readonly IMareaImportService _importService;
    private readonly int _marea;
    private readonly int _anio;
    private readonly string _pattern;
    private readonly string _barco;
    private bool _isBusy;

    public ImportDbfViewModel(
        int marea, 
        int anio, 
        IMareaImportService importService,
        string barco,
        Action<IEnumerable<string>?> onFinished)
    {
        _marea = marea;
        _anio = anio;
        _importService = importService;
        _barco = barco;
        _onFinished = onFinished;
        _pattern = $"{marea}{anio % 100:D2}";

        AddFilesCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(AddFiles, () => !IsBusy);
        AcceptCommand = new AsyncRelayCommand(AcceptAsync, () => !IsBusy && SelectedFiles.Count > 0);
        CancelCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() => _onFinished(null), () => !IsBusy);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set 
        {
            if (SetProperty(ref _isBusy, value))
            {
                (AddFilesCommand as IRelayCommand)?.NotifyCanExecuteChanged();
                (AcceptCommand as IRelayCommand)?.NotifyCanExecuteChanged();
                (CancelCommand as IRelayCommand)?.NotifyCanExecuteChanged();
            }
        }
    }

    public string PatternNote => $"Patrón esperado: *{_pattern}*.dbf";
    
    public ObservableCollection<DbfFileItem> SelectedFiles { get; } = [];

    public ICommand AddFilesCommand { get; }
    public ICommand AcceptCommand { get; }
    public ICommand CancelCommand { get; }

    public Action<string, string, MessageDialogType>? ShowMessage { get; set; }

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
                
                if (!fileName.Contains(_pattern, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

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
            (AcceptCommand as IRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    private async Task AcceptAsync()
    {
        if (SelectedFiles.Count == 0) return;

        IsBusy = true;
        try
        {
            // La carpeta base es la del primer archivo seleccionado
            string basePath = Path.GetDirectoryName(SelectedFiles[0].FullPath) ?? string.Empty;

            var report = await _importService.ProcessMareaImportAsync(basePath, _barco, _marea, _anio);

            if (report.HasFatalErrors)
            {
                // Abrir PDF de auditoría
                string reportPath = Path.Combine(basePath, "Reports", $"Audit_{_barco}_{_marea}_{_anio}.pdf");
                
                if (File.Exists(reportPath))
                {
                    Process.Start(new ProcessStartInfo(reportPath) { UseShellExecute = true });
                    ShowMessage?.Invoke("Errores de Validación", "Se detectaron errores graves que impiden la importación. Se ha abierto el reporte PDF con el detalle.", MessageDialogType.Error);
                }
                else
                {
                    ShowMessage?.Invoke("Errores de Validación", "Se detectaron errores graves, pero no se pudo localizar el archivo de reporte.", MessageDialogType.Error);
                }
                
                // El diálogo permanece abierto para correcciones
            }
            else
            {
                // Éxito
                _onFinished(SelectedFiles.Select(f => f.FullPath));
            }
        }
        catch (Exception ex)
        {
            ShowMessage?.Invoke("Error de Importación", $"Ocurrió un error inesperado: {ex.Message}", MessageDialogType.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private string FormatSize(long bytes)
    {
        if (bytes >= 1024 * 1024)
            return $"{(double)bytes / (1024 * 1024):F2} MB";
        return $"{(double)bytes / 1024:F2} KB";
    }
}
