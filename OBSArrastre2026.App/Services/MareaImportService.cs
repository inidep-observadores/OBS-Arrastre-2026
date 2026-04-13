using OBSArrastre2026.App.Models.Import;
using OBSArrastre2026.App.Services.Internal;
using System.IO;

namespace OBSArrastre2026.App.Services;

public interface IMareaImportService
{
    Task<MareaValidationReport> ProcessMareaImportAsync(string basePath, string barco, int marea, int anio);
}

public class MareaImportService : IMareaImportService
{
    private readonly IDbfExtractorService _extractor;
    private readonly MareaValidationEngine _validator;
    private readonly IMareaReportService _reporter;

    public MareaImportService(
        IDbfExtractorService extractor, 
        IMareaReportService reporter)
    {
        _extractor = extractor;
        _validator = new MareaValidationEngine();
        _reporter = reporter;
    }

    public async Task<MareaValidationReport> ProcessMareaImportAsync(string basePath, string barco, int marea, int anio)
    {
        // 1. Determinar nombres de archivos (Ej: C15225.DBF)
        string suffix = $"{marea}{anio % 100:D2}.DBF";
        string cPath = Path.Combine(basePath, $"C{suffix}");
        string mPath = Path.Combine(basePath, $"M{suffix}");
        string xPath = Path.Combine(basePath, $"X{suffix}");
        string sPath = Path.Combine(basePath, $"S{suffix}");
        string lPath = Path.Combine(basePath, $"L{suffix}");

        // 2. Extraer datos
        var capturas = await _extractor.ReadCapturasAsync(cPath);
        var muestras = await _extractor.ReadMuestrasAsync(mPath);
        var submuestras = await _extractor.ReadSubmuestrasAsync(sPath);
        var lgs = await _extractor.ReadLgAsync(lPath);

        // 3. Lógica de fusión X* (Muestras extendidas)
        if (File.Exists(xPath))
        {
            var extensiones = await _extractor.ReadMuestrasAsync(xPath);
            foreach (var ext in extensiones)
            {
                var baseM = muestras.FirstOrDefault(m => 
                    (int)m.Lance == (int)ext.Lance && 
                    m.CodEspec == ext.CodEspec);
                
                if (baseM != null)
                {
                    LegacyDecoder.MergeExtendedMuestras(baseM, ext);
                }
                else
                {
                    // Si no hay base, se trata como una muestra independiente (raro pero posible)
                    muestras.Add(ext);
                }
            }
        }

        // 4. Validar
        var report = _validator.ValidateMarea(barco, anio, marea, capturas, muestras, submuestras);

        // 5. Generar Reporte PDF
        var pdfBytes = _reporter.GenerateValidationPdf(report);
        string reportPath = Path.Combine(basePath, "Reports", $"Audit_{barco}_{marea}_{anio}.pdf");
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        await File.WriteAllBytesAsync(reportPath, pdfBytes);

        return report;
    }
}
