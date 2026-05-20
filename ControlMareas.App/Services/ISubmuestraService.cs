using System.Collections.Generic;
using System.Threading.Tasks;
using ControlMareas.App.Data.Entities;

namespace ControlMareas.App.Services;

public interface ISubmuestraService
{
    Task<List<Muestra>> GetMuestrasConSubmuestrasAsync(string mareaId);
    Task<List<ItemSubmuestra>> GetSubmuestrasByMuestraIdAsync(string muestraId);
    Task<ItemSubmuestra?> GetSubmuestraByIdAsync(string id);
    Task SaveSubmuestraAsync(ItemSubmuestra submuestra);
    Task DeleteSubmuestraAsync(string id);
}
