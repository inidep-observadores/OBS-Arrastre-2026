using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using OBSArrastre2026.App.Data.Entities;
using OBSArrastre2026.App.Services;

namespace OBSArrastre2026.App.ViewModels;

public class ExportarRecursosViewModel : ObservableObject
{
    private readonly ILanceService _lanceService;
    private readonly IMapRenderingService _mapService;
    private readonly IExcelReportService _excelService;
    private readonly Marea _marea;
    private readonly List<Lance> _lances;
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

    public TaskCompletionSource<bool> DialogResult { get; } = new();

    public ExportarRecursosViewModel(Marea marea, List<Lance> lances, ILanceService lanceService, IMapRenderingService mapService, IExcelReportService excelService)
    {
        _marea = marea;
        _lances = lances;
        _lanceService = lanceService;
        _mapService = mapService;
        _excelService = excelService;

        AcceptCommand = new AsyncRelayCommand(ExecuteExportAsync, () => CanAccept);
        CloseCommand = new RelayCommand(() => DialogResult.TrySetResult(false), () => !IsBusy);
        BrowseCommand = new RelayCommand(Browse, () => !IsBusy);
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

    private async Task ExecuteExportAsync()
    {
        IsBusy = true;
        try
        {
            var etapas = _marea.Etapas.OrderBy(e => e.FechaZarpada).ToList();
            bool multipleEtapas = etapas.Count > 1;

            for (int i = 0; i < etapas.Count; i++)
            {
                string prefix = multipleEtapas ? $"Etapa{i + 1}-" : "";
                await ExportEtapaAsync(etapas[i], ExportPath, prefix);
            }

            DialogResult.TrySetResult(true);
        }
        catch (Exception)
        {
            // Error handling ignored as per request for now
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExportEtapaAsync(MareaEtapa? etapa, string targetFolder, string prefix)
    {
        if (etapa == null) return;

        var etapaLances = _lances.Where(l => l.MareaEtapaId == etapa.ID).ToList();
        if (!etapaLances.Any()) return;

        // 1. Guardar mapa (si hay coordenadas)
        var lancesConCoord = etapaLances.Where(l => l.LatitudInicioDecimal.HasValue && l.LongitudInicioDecimal.HasValue).ToList();
        if (lancesConCoord.Any())
        {
            var lats = lancesConCoord.Select(l => l.LatitudInicioDecimal!.Value);
            var lons = lancesConCoord.Select(l => l.LongitudInicioDecimal!.Value);
            string mapPath = Path.Combine(targetFolder, $"{prefix}Mapa.png");
            await _mapService.RenderMapAsync(lats, lons, mapPath);
        }

        // 2. Guardar Tablas Excel
        var etapaProduccion = await _lanceService.GetProduccionAsync(etapa.ID);
        string excelPath = Path.Combine(targetFolder, $"{prefix}Tablas.xlsx");
        await _excelService.GenerateTablasExcelAsync(etapaLances, etapaProduccion, excelPath);
    }
}
