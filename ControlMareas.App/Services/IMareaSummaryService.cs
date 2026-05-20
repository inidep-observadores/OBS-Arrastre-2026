using System.Threading.Tasks;
using ControlMareas.App.Models.Reports;

namespace ControlMareas.App.Services;

public interface IMareaSummaryService
{
    Task<MareaSummaryReport> GetMareaSummaryAsync(string mareaId);
    Task<RecibiProyectoReport> GetRecibiProyectoReportAsync(string mareaId);
}
