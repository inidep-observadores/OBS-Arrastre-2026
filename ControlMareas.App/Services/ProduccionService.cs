using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ControlMareas.App.Data;
using ControlMareas.App.Data.Entities;

namespace ControlMareas.App.Services;

public sealed class ProduccionService(IDbContextFactory<AppDbContext> dbContextFactory) : IProduccionService
{
    public async Task<IReadOnlyList<RegistroProduccion>> GetRegistrosProduccionAsync(string mareaEtapaId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        return await dbContext.RegistrosProduccion
            .Include(r => r.Especie)
            .Include(r => r.Producto)
            .Where(r => r.MareaEtapaId == mareaEtapaId)
            .OrderBy(r => r.Fecha)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<RegistroProduccion?> GetRegistroProduccionAsync(string id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        return await dbContext.RegistrosProduccion
            .Include(r => r.Especie)
            .Include(r => r.Producto)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task SaveRegistroProduccionAsync(RegistroProduccion registro, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var existing = await dbContext.RegistrosProduccion
            .FirstOrDefaultAsync(r => r.Id == registro.Id, cancellationToken);

        if (existing == null)
        {
            await dbContext.RegistrosProduccion.AddAsync(registro, cancellationToken);
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(registro);
            existing.EspecieId = registro.EspecieId;
            existing.IdProducto = registro.IdProducto;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteRegistroProduccionAsync(string id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var existing = await dbContext.RegistrosProduccion.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (existing != null)
        {
            dbContext.RegistrosProduccion.Remove(existing);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
