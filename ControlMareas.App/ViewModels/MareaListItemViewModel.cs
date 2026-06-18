using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlMareas.App.Data.Entities;
using ControlMareas.App.Services;

namespace ControlMareas.App.ViewModels;

public sealed class MareaListItemViewModel : ObservableObject
{
    private readonly IActiveMareaManager _activeMareaManager;
    private readonly IMareaValidationService _validationService;
    private readonly IMareaReportService _reportService;
    private readonly IMareaSummaryService _summaryService;
    private readonly IUserSettingsService _userSettingsService;

    public Action<object>? ShowCustomDialog { get; set; }

    public MareaListItemViewModel(
        Marea marea, 
        IActiveMareaManager activeMareaManager,
        IMareaValidationService validationService,
        IMareaReportService reportService,
        IMareaSummaryService summaryService,
        IUserSettingsService userSettingsService)
    {
        Marea = marea;
        _activeMareaManager = activeMareaManager;
        _validationService = validationService;
        _reportService = reportService;
        _summaryService = summaryService;
        _userSettingsService = userSettingsService;

        SetActiveCommand = new AsyncRelayCommand(() => _activeMareaManager.SetActiveMareaAsync(Marea.ID));
        ValidateCommand = new AsyncRelayCommand(ValidateAsync);
        GenerateSummaryCommand = new AsyncRelayCommand(GenerateSummaryAsync);
    }

    public Marea Marea { get; }

    public string CodigoDisplay => $"{Marea.NumeroInidep}/{Marea.AnioInidep % 100:D2}";
    
    public string BuqueNombre => Marea.Buque?.Nombre ?? "Sin buque";
    
    public string FechaInicioDisplay => Marea.FechaInicio.ToString("dd/MM/yyyy");
    
    public string FechaFinDisplay => Marea.FechaFin?.ToString("dd/MM/yyyy") ?? "-";
    
    public string Comentarios => Marea.Comentarios ?? string.Empty;
    
    public string Estado => Marea.FechaFin == null ? "En curso" : "Cerrada";
    
    public string ID => Marea.ID;

    public bool IsActive => _activeMareaManager.ActiveMareaId == Marea.ID;

    public ICommand SetActiveCommand { get; }
    public ICommand ValidateCommand { get; }
    public ICommand GenerateSummaryCommand { get; }

    private bool _isValidating;
    public bool IsValidating
    {
        get => _isValidating;
        set => SetProperty(ref _isValidating, value);
    }

    private string _statusText = string.Empty;
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    private async Task GenerateSummaryAsync()
    {
        if (IsValidating) return;

        // Estimar y verificar previamente si el archivo de destino está libre
        string fileName = $"Resumen_Marea_{Marea.Buque?.Nombre ?? "Marea"}_{Marea.NumeroInidep}_{Marea.AnioInidep}.pdf";
        string importFolder = MareaMetadataHelper.GetImportFolder(Marea.Metadata);
        string savePath;

        if (!string.IsNullOrEmpty(importFolder))
        {
            savePath = System.IO.Path.Combine(importFolder, "Reportes", fileName);
        }
        else
        {
            savePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), fileName);
        }

        if (!Services.FileHelper.IsFileWritable(savePath))
        {
            System.Windows.MessageBox.Show(
                $"No se puede guardar el resumen de marea en:\n\"{savePath}\"\n\nEl archivo ya está abierto por otra aplicación (por ejemplo, Acrobat Reader). Por favor, cierre el documento e inténtelo nuevamente.",
                "Archivo Bloqueado",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            return;
        }

        IsValidating = true;
        StatusText = "Generando resumen...";
        try
        {
            var pdfBytes = await Task.Run(async () =>
            {
                StatusText = "Recolectando datos de la marea...";
                var report = await _summaryService.GetMareaSummaryAsync(Marea.ID);
                StatusText = "Generando PDF...";
                return await _reportService.GenerateMareaSummaryPdfAsync(report);
            });

            StatusText = "Abriendo reporte...";
            if (!string.IsNullOrEmpty(importFolder))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(savePath)!);
            }

            await System.IO.File.WriteAllBytesAsync(savePath, pdfBytes);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = savePath,
                UseShellExecute = true
            });
        }
        catch (System.Exception ex)
        {
            System.Windows.MessageBox.Show($"Error al generar el resumen: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsValidating = false;
            StatusText = string.Empty;
        }
    }

    private async Task ValidateAsync()
    {
        if (IsValidating) return;

        // Estimar y verificar previamente si el archivo de destino está libre
        string fileName = $"Reporte_Validacion_{Marea.Buque?.Nombre ?? "Marea"}_{Marea.NumeroInidep}_{Marea.AnioInidep}.pdf";
        string importFolder = MareaMetadataHelper.GetImportFolder(Marea.Metadata);
        string savePath;

        if (!string.IsNullOrEmpty(importFolder))
        {
            savePath = System.IO.Path.Combine(importFolder, "Reportes", fileName);
        }
        else
        {
            savePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), fileName);
        }

        if (!Services.FileHelper.IsFileWritable(savePath))
        {
            System.Windows.MessageBox.Show(
                $"No se puede guardar el reporte de validación en:\n\"{savePath}\"\n\nEl archivo ya está abierto por otra aplicación (por ejemplo, Acrobat Reader). Por favor, cierre el documento e inténtelo nuevamente.",
                "Archivo Bloqueado",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            return;
        }

        bool omitirProduccion = false;

        if (ShowCustomDialog != null)
        {
            var dialogVm = new ValidarMareaDialogViewModel(_userSettingsService);
            ShowCustomDialog(dialogVm);

            bool result = await dialogVm.DialogResult.Task;
            ShowCustomDialog(null);

            if (!result) return;
            omitirProduccion = dialogVm.OmitirValidacionCapturaProduccion;
        }
        else
        {
            var result = System.Windows.MessageBox.Show(
                $"¿Desea iniciar el proceso de auditoría para la marea {CodigoDisplay}?\n\nEste proceso puede tardar unos segundos.",
                "Confirmar Auditoría",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result != System.Windows.MessageBoxResult.Yes) return;
        }

        IsValidating = true;
        StatusText = "Iniciando...";
        try
        {
            var pdfResult = await Task.Run(async () => 
            {
                StatusText = "Conectando a base de datos...";
                var report = await _validationService.ValidateExistingMareaAsync(Marea.ID, omitirProduccion);
                
                StatusText = "Reglas de validación aplicadas. Generando PDF...";
                var pdfBytes = await _reportService.GenerateValidationPdfAsync(report);
                
                StatusText = "PDF generado. Finalizando...";
                return new { Bytes = pdfBytes, Report = report };
            });

            StatusText = "Guardando reporte...";
            if (!string.IsNullOrEmpty(importFolder))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(savePath)!);
            }

            await System.IO.File.WriteAllBytesAsync(savePath, pdfResult.Bytes);

            StatusText = "Abriendo reporte...";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = savePath,
                UseShellExecute = true
            });

            System.Windows.MessageBox.Show(
                "El proceso de auditoría ha finalizado correctamente. El reporte se abrirá en su visor de PDF predeterminado.",
                "Proceso Finalizado",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        catch (System.Exception ex)
        {
            System.Windows.MessageBox.Show($"Error durante la auditoría: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsValidating = false;
            StatusText = string.Empty;
        }
    }
}
