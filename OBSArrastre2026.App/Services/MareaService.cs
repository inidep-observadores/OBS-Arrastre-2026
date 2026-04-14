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

    public async Task<Marea?> GetMareaAsync(string id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        return await dbContext.Mareas
            .Include(x => x.Buque)
            .Include(x => x.Etapas)
            .FirstOrDefaultAsync(x => x.ID == id, cancellationToken);
    }

    public async Task SaveMareaAsync(Marea marea, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var existingMarea = await dbContext.Mareas
            .Include(x => x.Etapas)
            .FirstOrDefaultAsync(x => x.ID == marea.ID, cancellationToken);

        if (existingMarea == null)
        {
            // Nueva marea
            await dbContext.Mareas.AddAsync(marea, cancellationToken);
        }
        else
        {
            // Actualizar marea existente
            dbContext.Entry(existingMarea).CurrentValues.SetValues(marea);
            existingMarea.BuqueID = marea.BuqueID;

            // Sincronizar Etapas
            // 1. ELiminar etapas que ya no están
            foreach (var existingEtapa in existingMarea.Etapas.ToList())
            {
                if (!marea.Etapas.Any(e => e.ID == existingEtapa.ID))
                {
                    dbContext.MareaEtapas.Remove(existingEtapa);
                }
            }

            // 2. Actualizar o añadir etapas
            foreach (var etapa in marea.Etapas)
            {
                var existingEtapa = existingMarea.Etapas.FirstOrDefault(e => e.ID == etapa.ID);
                if (existingEtapa == null)
                {
                    existingMarea.Etapas.Add(etapa);
                }
                else
                {
                    dbContext.Entry(existingEtapa).CurrentValues.SetValues(etapa);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
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
                (x.Comentarios != null && x.Comentarios.ToLower().Contains(search)) ||
                (x.NumeroInidep.ToString() + "/" + (x.AnioInidep % 100).ToString("D2")).Contains(search));
        }

        // Ordenar: Las "En curso" (FechaFin null) arriba, luego por FechaFin desc, luego por FechaInicio desc
        return await query
            .OrderByDescending(x => x.FechaFin == null)
            .ThenByDescending(x => x.FechaFin)
            .ThenByDescending(x => x.FechaInicio)
            .ToListAsync(cancellationToken);
    }
}
