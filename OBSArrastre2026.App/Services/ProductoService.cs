using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OBSArrastre2026.App.Data;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.Services;

public sealed class ProductoService(IDbContextFactory<AppDbContext> dbContextFactory) : IProductoService
{
    public async Task<IReadOnlyList<Producto>> GetProductosAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Productos
            .OrderBy(p => p.Orden)
            .ThenBy(p => p.Descripcion)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Especie>> GetEspeciesAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Especies
            .OrderByDescending(e => e.Frecuente)
            .ThenBy(e => e.NombreVulgar)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveProductoAsync(Producto producto, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var existing = await dbContext.Productos.FirstOrDefaultAsync(p => p.Id == producto.Id, cancellationToken);
        if (existing == null)
        {
            await dbContext.Productos.AddAsync(producto, cancellationToken);
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(producto);
        }
        
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
