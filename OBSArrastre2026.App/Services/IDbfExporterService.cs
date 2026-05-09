using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.Services;

public interface IDbfExporterService
{
    /// <summary>
    /// Exporta todos los datos de una marea a un conjunto de archivos DBF institucionales.
    /// </summary>
    /// <param name="marea">La marea a exportar.</param>
    /// <param name="outputPath">La carpeta de destino.</param>
    /// <returns>Tarea que representa el proceso de exportación.</returns>
    Task ExportMareaToDbfAsync(Marea marea, string outputPath);
}
