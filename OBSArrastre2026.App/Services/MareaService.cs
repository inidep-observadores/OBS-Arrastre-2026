using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OBSArrastre2026.App.Data;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.Services;

public sealed class MareaService(IDbContextFactory<AppDbContext> dbContextFactory) : IMareaService
{
    public async Task<IReadOnlyList<int>> GetAniosExistentesAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        return await dbContext.Mareas
            .Select(x => x.AnioInidep)
            .Distinct()
            .OrderByDescending(x => x)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Marea>> GetMareasAsync(
        int? anio = null,
        string? buqueId = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        string? busquedaTextual = null,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var query = dbContext.Mareas
            .Include(x => x.Buque)
            .AsNoTracking();

        if (anio.HasValue)
        {
            query = query.Where(x => x.AnioInidep == anio.Value);
        }

        if (!string.IsNullOrEmpty(buqueId))
        {
            query = query.Where(x => x.BuqueID == buqueId);
        }

        if (fechaDesde.HasValue)
        {
            query = query.Where(x => x.FechaInicio >= fechaDesde.Value);
        }

        if (fechaHasta.HasValue)
        {
            query = query.Where(x => x.FechaInicio <= fechaHasta.Value);
        }

        if (!string.IsNullOrWhiteSpace(busquedaTextual))
        {
            var search = busquedaTextual.Trim().ToLower();
            query = query.Where(x => 
                (x.Codigo != null && x.Codigo.ToLower().Contains(search)) ||
                (x.Comentarios != null && x.Comentarios.ToLower().Contains(search)) ||
                (x.NumeroInidep.ToString() + "/" + x.AnioInidep.ToString()).Contains(search));
        }

        // Ordenar: Las "En curso" (FechaFin null) arriba, luego por FechaFin desc, luego por FechaInicio desc
        return await query
            .OrderByDescending(x => x.FechaFin == null)
            .ThenByDescending(x => x.FechaFin)
            .ThenByDescending(x => x.FechaInicio)
            .ToListAsync(cancellationToken);
    }
}
