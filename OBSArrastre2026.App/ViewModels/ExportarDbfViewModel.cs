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
    private string _exportPath = string.Empty;
    private bool _isBusy;

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
                (AcceptCommand as IRelayCommand)?.NotifyCanExecuteChanged();
                (CloseCommand as IRelayCommand)?.NotifyCanExecuteChanged();
                (BrowseCommand as IRelayCommand)?.NotifyCanExecuteChanged();
            }
        }
    }

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
        BrowseCommand = new RelayCommand(Browse, () => !IsBusy);

        // Carpeta por defecto: Documentos
        ExportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "INIDEP_Export");
    }

    private void Browse()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Seleccionar carpeta de exportación DBF",
            Multiselect = false
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
            await _exporterService.ExportMareaToDbfAsync(_marea, ExportPath);
            DialogResult.TrySetResult(true);
        }
        catch (Exception ex)
        {
            IsBusy = false;
            if (ShowMessage != null) 
                await ShowMessage("Error de Exportación", "Ocurrió un error al generar los archivos DBF.", ex.ToString(), MessageDialogType.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
