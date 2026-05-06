using System.Collections.Generic;
using System.Threading.Tasks;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.Services
{
    public interface IExcelReportService
    {
        Task GenerateTablasExcelAsync(IEnumerable<Lance> lances, string outputPath);
    }
}
