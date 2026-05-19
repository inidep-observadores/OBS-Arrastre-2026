using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using OBSArrastre2026.App.Data.Entities;
using OBSArrastre2026.App.Services;
using OBSArrastre2026.App.Models;

namespace OBSArrastre2026.App.ViewModels;

public class ExportarDbfViewModel : ObservableObject
{
    private readonly IDbfExporterService _exporterService;
    private readonly Marea _marea;
    private readonly string _importFolder = string.Empty;
    private string _exportPath = string.Empty;
    private bool _isBusy;
    private double _progressValue;
    private DbfExportSummary? _exportSummary;
    private bool _exportToCorregido = true;
    private bool _updateOriginalFiles;

    public string ExportPath
    {
        get => _exportPath;
        set 
        {
            if (SetProperty(ref _exportPath, value))
            {
                OnPropertyChanged(nameof(CanAccept));
                (AcceptCommand as IRelayCommand)?.NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set 
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(CanAccept));
                OnPropertyChanged(nameof(CanBrowse));
                (AcceptCommand as IRelayCommand)?.NotifyCanExecuteChanged();
                (CloseCommand as IRelayCommand)?.NotifyCanExecuteChanged();
                (BrowseCommand as IRelayCommand)?.NotifyCanExecuteChanged();
            }
        }
    }

    public double ProgressValue
    {
        get => _progressValue;
        set => SetProperty(ref _progressValue, value);
    }

    public DbfExportSummary? ExportSummary
    {
        get => _exportSummary;
        set => SetProperty(ref _exportSummary, value);
    }

    public bool ExportToCorregido
    {
        get => _exportToCorregido;
        set
        {
            if (SetProperty(ref _exportToCorregido, value))
            {
                if (value)
                {
                    _updateOriginalFiles = false;
                    OnPropertyChanged(nameof(UpdateOriginalFiles));
                    
                    // Restaurar ruta a "Corregido" o MyDocuments
                    if (!string.IsNullOrEmpty(_importFolder))
                    {
                        ExportPath = Path.Combine(_importFolder, "Corregido");
                    }
                    else
                    {
                        ExportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "INIDEP_Export");
                    }
                }
                OnPropertyChanged(nameof(CanBrowse));
                (BrowseCommand as IRelayCommand)?.NotifyCanExecuteChanged();
            }
        }
    }

    public bool UpdateOriginalFiles
    {
        get => _updateOriginalFiles;
        set
        {
            if (SetProperty(ref _updateOriginalFiles, value))
            {
                if (value)
                {
                    _exportToCorregido = false;
                    OnPropertyChanged(nameof(ExportToCorregido));
                    
                    // Asignar ruta de exportación a la carpeta original
                    if (!string.IsNullOrEmpty(_importFolder))
                    {
                        ExportPath = _importFolder;
                    }
                }
                OnPropertyChanged(nameof(CanBrowse));
                (BrowseCommand as IRelayCommand)?.NotifyCanExecuteChanged();
            }
        }
    }

    public bool CanUpdateOriginalFiles { get; }

    public bool CanBrowse => ExportToCorregido && !IsBusy;

    public bool CanAccept => !string.IsNullOrWhiteSpace(ExportPath) && !IsBusy;

    public ICommand AcceptCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand BrowseCommand { get; }

    public Func<string, string, string?, MessageDialogType, Task>? ShowMessage { get; set; }
    public TaskCompletionSource<bool> DialogResult { get; } = new();

    public ExportarDbfViewModel(Marea marea, IDbfExporterService exporterService)
    {
        _marea = marea;
        _exporterService = exporterService;

        AcceptCommand = new AsyncRelayCommand(ExecuteExportAsync, () => CanAccept);
        CloseCommand = new RelayCommand(() => DialogResult.TrySetResult(false), () => !IsBusy);
        BrowseCommand = new RelayCommand(Browse, () => CanBrowse);

        // Carpeta por defecto basada en metadata o Documentos
        _importFolder = MareaMetadataHelper.GetImportFolder(_marea.Metadata);
        CanUpdateOriginalFiles = !string.IsNullOrEmpty(_importFolder);

        if (CanUpdateOriginalFiles)
        {
            _exportToCorregido = true;
            _exportPath = Path.Combine(_importFolder, "Corregido");
        }
        else
        {
            _exportToCorregido = true;
            _exportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "INIDEP_Export");
        }
    }

    private void Browse()
    {
        string? initialDir = null;
        if (!string.IsNullOrEmpty(ExportPath))
        {
            if (Directory.Exists(ExportPath))
            {
                initialDir = ExportPath;
            }
            else
            {
                // Si la carpeta no existe aún (ej: subcarpeta 'Corregido'), intentamos con el padre
                try { initialDir = Path.GetDirectoryName(ExportPath); } catch { }
            }
        }

        var dialog = new OpenFolderDialog
        {
            Title = "Seleccionar carpeta de exportación DBF",
            Multiselect = false,
            InitialDirectory = Directory.Exists(initialDir) ? initialDir : null
        };

        if (dialog.ShowDialog() == true)
        {
            ExportPath = dialog.FolderName;
        }
    }

    private async Task ExecuteExportAsync()
    {
        IsBusy = true;
        try
        {
            var progress = new Progress<double>(v => ProgressValue = v);
            ExportSummary = await _exporterService.ExportMareaToDbfAsync(_marea, ExportPath, UpdateOriginalFiles, progress);
            DialogResult.TrySetResult(true);
        }
        catch (Exception ex)
        {
            if (ShowMessage != null) 
                await ShowMessage("Error de Exportación", "Ocurrió un error al generar los archivos DBF.", ex.ToString(), MessageDialogType.Error);
            DialogResult.TrySetResult(false);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
