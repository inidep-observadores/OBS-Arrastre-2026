using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using OBSArrastre2026.App.Data.Entities;
using OBSArrastre2026.App.Services;
using OBSArrastre2026.App.Models;
using OBSArrastre2026.App.Models.Reports;

namespace OBSArrastre2026.App.ViewModels;

public class ExportarRecursosViewModel : ObservableObject
{
    private readonly ILanceService _lanceService;
    private readonly IMapRenderingService _mapService;
    private readonly IExcelReportService _excelService;
    private readonly IMareaReportService _reportService;
    private readonly IMareaSummaryService _summaryService;
    private readonly IUserSettingsService _userSettingsService;
    private readonly Marea _marea;
    private readonly List<Lance> _lances;
    private string _exportPath = string.Empty;
    private bool _isBusy;
    private bool _exportExcel = false;
    private bool _exportTemplateWord = true;

    public bool ExportExcel
    {
        get => _exportExcel;
        set => SetProperty(ref _exportExcel, value);
    }


    public bool ExportTemplateWord
    {
        get => _exportTemplateWord;
        set => SetProperty(ref _exportTemplateWord, value);
    }

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
    public Func<string, string, Task<bool?>>? ShowConfirmation { get; set; }

    public TaskCompletionSource<bool> DialogResult { get; } = new();

    public ExportarRecursosViewModel(Marea marea, List<Lance> lances, 
        ILanceService lanceService, IMapRenderingService mapService, 
        IExcelReportService excelService, IMareaReportService reportService,
        IMareaSummaryService summaryService, IUserSettingsService userSettingsService)
    {
        _marea = marea;
        _lances = lances;
        _lanceService = lanceService;
        _mapService = mapService;
        _excelService = excelService;
        _reportService = reportService;
        _summaryService = summaryService;
        _userSettingsService = userSettingsService;

        AcceptCommand = new AsyncRelayCommand(ExecuteExportAsync, () => CanAccept);
        CloseCommand = new RelayCommand(() => DialogResult.TrySetResult(false), () => !IsBusy);
        BrowseCommand = new RelayCommand(Browse, () => !IsBusy);

        // Carpeta por defecto basada en metadata
        string importFolder = MareaMetadataHelper.GetImportFolder(_marea.Metadata);
        if (!string.IsNullOrEmpty(importFolder))
        {
            ExportPath = Path.Combine(importFolder, "informe");
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
                try { initialDir = Path.GetDirectoryName(ExportPath); } catch { }
            }
        }

        var dialog = new OpenFolderDialog
        {
            Title = "Seleccionar carpeta de exportación",
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
            // Asegurar que la carpeta de destino existe
            if (!string.IsNullOrWhiteSpace(ExportPath))
            {
                Directory.CreateDirectory(ExportPath);
            }

            if (ExportExcel)
            {
                var etapas = _marea.Etapas.OrderBy(e => e.FechaZarpada).ToList();
                bool multipleEtapas = etapas.Count > 1;

                for (int i = 0; i < etapas.Count; i++)
                {
                    string prefix = multipleEtapas ? $"Etapa{i + 1}-" : "";
                    await ExportEtapaAsync(etapas[i], ExportPath, prefix);
                }
            }
            else if (ExportTemplateWord)
            {
                await ExportFullWordAsync(ExportPath, useTemplate: true);
            }

            DialogResult.TrySetResult(true);
        }
        catch (Exception ex)
        {
            IsBusy = false;
            string message = "Ocurrió un error inesperado al generar el informe.";
            
            // Detectar si el archivo está en uso
            if (ex is IOException && (ex.HResult & 0x0000FFFF) == 32)
            {
                message = "No se pudo guardar el informe porque el archivo ya está abierto por otra aplicación (ej: Excel o Word). Por favor, cierre el documento y vuelva a intentarlo.";
            }

            if (ShowMessage != null) 
                await ShowMessage("Error de Exportación", message, ex.ToString(), MessageDialogType.Error);
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

        // 1. Obtener mapa para Excel (ya no se guarda como archivo separado)
        byte[]? mapBytes = null;
        var lancesConCoord = etapaLances.Where(l => l.LatitudInicioDecimal.HasValue && l.LongitudInicioDecimal.HasValue).ToList();
        if (lancesConCoord.Any())
        {
            var lats = lancesConCoord.Select(l => l.LatitudInicioDecimal!.Value);
            var lons = lancesConCoord.Select(l => l.LongitudInicioDecimal!.Value);
            
            // Solo obtenemos bytes para incrustar en el libro de trabajo
            try { mapBytes = await _mapService.RenderMapToBytesAsync(lats, lons); } catch { }
        }

        // 2. Guardar Informe Excel
        var etapaProduccion = await _lanceService.GetProduccionAsync(etapa.ID);
        
        string aa = (_marea.AnioInidep % 100).ToString("00");
        string nn = _marea.NumeroInidep.ToString("00");
        string fileName = $"{prefix}Informe_{nn}{aa}.xlsx";
        
        string excelPath = Path.Combine(targetFolder, fileName);
        await _excelService.GenerateTablasExcelAsync(_marea, etapaLances, etapaProduccion, excelPath, mapBytes);
    }

    private async Task ExportFullWordAsync(string targetFolder, bool useTemplate)
    {
        var allProduccion = new List<RegistroProduccion>();
        var etapas = _marea.Etapas.OrderBy(e => e.FechaZarpada).ToList();
        foreach (var etapa in etapas)
        {
            var p = await _lanceService.GetProduccionAsync(etapa.ID);
            allProduccion.AddRange(p);
        }

        var summary = await _summaryService.GetMareaSummaryAsync(_marea.ID);
        
        byte[] docBytes;
        if (useTemplate)
        {
            docBytes = await _reportService.GenerateMareaReportTemplateAsync(_marea, _lances, allProduccion, summary);
        }
        else
        {
            docBytes = await _reportService.GenerateFullMareaReportWordAsync(_marea, _lances, allProduccion, summary);
        }

        string fileName;
        if (useTemplate)
        {
            // Estructura oficial: Inf_MAR_DIOYT_{AñoActual}_{ApellidoRevisor}{1InicialNombreRevisor}_{AñoMarea}_{NroMarea}_{CodigoBuque}
            var settings = _userSettingsService.GetSettings();
            string añoActual = DateTime.Now.Year.ToString();
            string apellido = (settings.RevisorApellido ?? "S_A").Replace(" ", "_");
            string inicialNombre = !string.IsNullOrEmpty(settings.RevisorNombre) ? settings.RevisorNombre[0].ToString().ToUpper() : "";
            string añoMarea = _marea.AnioInidep.ToString();
            string nroMarea = _marea.NumeroInidep.ToString("00");
            var meta = MareaMetadataHelper.GetMetadata(_marea);
            string codigoBuque = meta.BuqueCodigo?.ToString() ?? "0";

            fileName = $"Inf_MAR_DIOYT_{añoActual}_{apellido}{inicialNombre}_{añoMarea}_{nroMarea}_{codigoBuque}.docx";
        }
        else
        {
            // Estructura estándar heredada: Informe_Marea_{nroMarea}{AñoMarea2Digitos}
            string aa = (_marea.AnioInidep % 100).ToString("00");
            string nn = _marea.NumeroInidep.ToString("00");
            fileName = $"Informe_Marea_{nn}{aa}.docx";
        }

        string filePath = Path.Combine(targetFolder, fileName);
        await File.WriteAllBytesAsync(filePath, docBytes);

    }
}
