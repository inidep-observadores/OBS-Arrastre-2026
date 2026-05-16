using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OBSArrastre2026.App.Data;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.Services;

public sealed class MuestraService(IDbContextFactory<AppDbContext> dbContextFactory) : IMuestraService
{
    public async Task<IReadOnlyList<Muestra>> GetMuestrasPorMareaAsync(string mareaId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Muestras
            .Include(m => m.Especie)
            .Include(m => m.Lance)
                .ThenInclude(l => l.MareaEtapa)
                    .ThenInclude(e => e.Marea)
                        .ThenInclude(m => m!.Etapas)
            .Where(m => m.Lance!.MareaEtapa!.MareaID == mareaId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Muestra>> GetMuestrasAsync(string lanceId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        return await dbContext.Muestras
            .Include(m => m.Especie)
            .Include(m => m.Lance)
                .ThenInclude(l => l.MareaEtapa)
                    .ThenInclude(e => e.Marea)
                        .ThenInclude(m => m!.Etapas)
            .Where(m => m.LanceID == lanceId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Muestra?> GetMuestraAsync(string id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        return await dbContext.Muestras
            .Include(m => m.Especie)
            .Include(m => m.FrecuenciasTallas)
            .FirstOrDefaultAsync(m => m.ID == id, cancellationToken);
    }

    public async Task SaveMuestraAsync(Muestra muestra, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var existing = await dbContext.Muestras
            .Include(m => m.FrecuenciasTallas)
            .FirstOrDefaultAsync(m => m.ID == muestra.ID, cancellationToken);

        if (existing == null)
        {
            await dbContext.Muestras.AddAsync(muestra, cancellationToken);
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(muestra);
            existing.EspecieID = muestra.EspecieID;

            // Sincronizar FrecuenciasTallas
            // 1. Eliminar
            foreach (var existingFreq in existing.FrecuenciasTallas.ToList())
            {
                if (!muestra.FrecuenciasTallas.Any(f => f.ID == existingFreq.ID))
                {
                    dbContext.FrecuenciasTallas.Remove(existingFreq);
                }
            }

            // 2. Actualizar o Añadir
            foreach (var freq in muestra.FrecuenciasTallas)
            {
                var existingFreq = existing.FrecuenciasTallas.FirstOrDefault(f => f.ID == freq.ID);
                if (existingFreq == null)
                {
                    existing.FrecuenciasTallas.Add(freq);
                }
                else
                {
                    dbContext.Entry(existingFreq).CurrentValues.SetValues(freq);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteMuestraAsync(string id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var existing = await dbContext.Muestras.FirstOrDefaultAsync(m => m.ID == id, cancellationToken);
        if (existing != null)
        {
            dbContext.Muestras.Remove(existing);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
