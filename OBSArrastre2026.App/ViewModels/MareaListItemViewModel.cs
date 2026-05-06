using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OBSArrastre2026.App.Data.Entities;
using OBSArrastre2026.App.Services;

namespace OBSArrastre2026.App.ViewModels;

public sealed class MareaListItemViewModel : ObservableObject
{
    private readonly IActiveMareaManager _activeMareaManager;
    private readonly IMareaValidationService _validationService;
    private readonly IMareaReportService _reportService;
    private readonly IMareaSummaryService _summaryService;

    public MareaListItemViewModel(
        Marea marea, 
        IActiveMareaManager activeMareaManager,
        IMareaValidationService validationService,
        IMareaReportService reportService,
        IMareaSummaryService summaryService)
    {
        Marea = marea;
        _activeMareaManager = activeMareaManager;
        _validationService = validationService;
        _reportService = reportService;
        _summaryService = summaryService;

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
            string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Resumen_Marea_{Marea.Buque?.Nombre ?? "Marea"}_{Marea.NumeroInidep}_{Marea.AnioInidep}.pdf");
            await System.IO.File.WriteAllBytesAsync(tempPath, pdfBytes);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = tempPath,
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

        var result = System.Windows.MessageBox.Show(
            $"¿Desea iniciar el proceso de auditoría para la marea {CodigoDisplay}?\n\nEste proceso puede tardar unos segundos.",
            "Confirmar Auditoría",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        IsValidating = true;
        StatusText = "Iniciando...";
        try
        {
            var pdfResult = await Task.Run(async () => 
            {
                StatusText = "Conectando a base de datos...";
                var report = await _validationService.ValidateExistingMareaAsync(Marea.ID);
                
                StatusText = "Reglas de validación aplicadas. Generando PDF...";
                var pdfBytes = await _reportService.GenerateValidationPdfAsync(report);
                
                StatusText = "PDF generado. Finalizando...";
                return new { Bytes = pdfBytes, Report = report };
            });

            StatusText = "Guardando archivo temporal...";
            string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Reporte_Validacion_{Marea.Buque?.Nombre ?? "Marea"}_{Marea.NumeroInidep}_{Marea.AnioInidep}.pdf");
            await System.IO.File.WriteAllBytesAsync(tempPath, pdfResult.Bytes);

            StatusText = "Abriendo reporte...";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = tempPath,
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
