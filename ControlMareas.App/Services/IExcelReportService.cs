using System.Collections.Generic;
using System.Threading.Tasks;
using ControlMareas.App.Data.Entities;

namespace ControlMareas.App.Services
{
    public interface IExcelReportService
    {
        Task GenerateTablasExcelAsync(Marea marea, IEnumerable<Lance> lances, IEnumerable<RegistroProduccion> produccion, string outputPath, byte[]? mapImage = null);
    }
}
