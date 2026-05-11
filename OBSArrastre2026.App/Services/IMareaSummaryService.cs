using System.Threading.Tasks;
using OBSArrastre2026.App.Models.Reports;

namespace OBSArrastre2026.App.Services;

public interface IMareaSummaryService
{
    Task<MareaSummaryReport> GetMareaSummaryAsync(string mareaId);
    Task<RecibiProyectoReport> GetRecibiProyectoReportAsync(string mareaId);
}
