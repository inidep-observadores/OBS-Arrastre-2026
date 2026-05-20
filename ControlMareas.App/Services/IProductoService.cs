using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ControlMareas.App.Data.Entities;

namespace ControlMareas.App.Services;

public interface IProductoService
{
    Task<IReadOnlyList<Producto>> GetProductosAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Especie>> GetEspeciesAsync(CancellationToken cancellationToken = default);
    Task SaveProductoAsync(Producto producto, CancellationToken cancellationToken = default);
}
