using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.Services;

public interface IProductoService
{
    Task<IReadOnlyList<Producto>> GetProductosAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Especie>> GetEspeciesAsync(CancellationToken cancellationToken = default);
    Task SaveProductoAsync(Producto producto, CancellationToken cancellationToken = default);
}
