using OBSArrastre2026.App.Data.Entities;
using OBSArrastre2026.App.Models;

namespace OBSArrastre2026.App.Services;

public interface IDbfExporterService
{
    /// <summary>
    /// Exporta todos los datos de una marea a un conjunto de archivos DBF institucionales.
    /// </summary>
    /// <param name="marea">La marea a exportar.</param>
    /// <param name="outputPath">La carpeta de destino.</param>
    /// <param name="progress">Reporte de progreso opcional.</param>
    /// <returns>Resumen con los tiempos de cada etapa.</returns>
    Task<DbfExportSummary> ExportMareaToDbfAsync(Marea marea, string outputPath, IProgress<double>? progress = null);
}
