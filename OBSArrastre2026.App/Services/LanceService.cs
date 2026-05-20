using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OBSArrastre2026.App.Data;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.Services;

public sealed class LanceService(IDbContextFactory<AppDbContext> dbContextFactory) : ILanceService
{
    public async Task<IReadOnlyList<Lance>> GetLancesAsync(
        string? mareaEtapaId = null,
        string? mareaId = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? nroLance = null,
        string? especieBusqueda = null,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var query = dbContext.Lances
            .Include(x => x.MareaEtapa)
                .ThenInclude(e => e.Marea)
                    .ThenInclude(m => m.Buque)
            .Include(x => x.ItemsCaptura)
                .ThenInclude(i => i.Especie)
            .Include(x => x.Muestras)
                .ThenInclude(m => m.FrecuenciasTallas)
            .Include(x => x.Muestras)
                .ThenInclude(m => m.Especie)
            .AsNoTracking();

        if (!string.IsNullOrEmpty(mareaEtapaId))
        {
            query = query.Where(x => x.MareaEtapaId == mareaEtapaId);
        }

        if (!string.IsNullOrEmpty(mareaId))
        {
            query = query.Where(x => x.MareaEtapa!.MareaID == mareaId);
        }

        if (fechaDesde.HasValue)
        {
            var fDesde = fechaDesde.Value.ToString("yyyy-MM-dd");
            query = query.Where(x => x.Fecha.CompareTo(fDesde) >= 0);
        }

        if (fechaHasta.HasValue)
        {
            var fHasta = fechaHasta.Value.ToString("yyyy-MM-dd");
            query = query.Where(x => x.Fecha.CompareTo(fHasta) <= 0);
        }

        if (nroLance.HasValue)
        {
            query = query.Where(x => x.NroLance == nroLance.Value);
        }

        if (!string.IsNullOrWhiteSpace(especieBusqueda))
        {
            var search = especieBusqueda.Trim().ToLower();
            query = query.Where(l => l.ItemsCaptura.Any(i => 
                (i.Especie != null && i.Especie.NombreVulgar!.ToLower().Contains(search)) ||
                (i.Especie != null && i.Especie.NombreCientifico!.ToLower().Contains(search))));
        }

        return await query
            .OrderBy(x => x.NroLance)
            .ToListAsync(cancellationToken);
    }

    public async Task<Lance?> GetLanceAsync(string id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        return await dbContext.Lances
            .Include(x => x.ItemsCaptura)
                .ThenInclude(i => i.Especie)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task SaveLanceAsync(Lance lance, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var existing = await dbContext.Lances
            .Include(x => x.ItemsCaptura)
            .FirstOrDefaultAsync(x => x.Id == lance.Id, cancellationToken);

        if (existing == null)
        {
            // Evitar re-insertar especies de catálogo
            foreach (var item in lance.ItemsCaptura)
            {
                item.Especie = null;
            }
            await dbContext.Lances.AddAsync(lance, cancellationToken);
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(lance);
            
            // Sincronizar ItemsCaptura
            // 1. Eliminar ítems que ya no están
            foreach (var existingItem in existing.ItemsCaptura.ToList())
            {
                if (!lance.ItemsCaptura.Any(i => i.ID == existingItem.ID))
                {
                    dbContext.ItemsCaptura.Remove(existingItem);
                }
            }

            // 2. Actualizar o añadir ítems
            foreach (var item in lance.ItemsCaptura)
            {
                var existingItem = existing.ItemsCaptura.FirstOrDefault(i => i.ID == item.ID);
                if (existingItem == null)
                {
                    // Desacoplar especie para que EF Core no intente insertarla
                    item.Especie = null;
                    existing.ItemsCaptura.Add(item);
                }
                else
                {
                    item.Especie = null;
                    dbContext.Entry(existingItem).CurrentValues.SetValues(item);
                    existingItem.EspecieID = item.EspecieID; // Asegurar FK
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteLanceAsync(string id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var lance = await dbContext.Lances.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (lance != null)
        {
            dbContext.Lances.Remove(lance);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<Especie>> GetEspeciesAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Especies
            .AsNoTracking()
            .OrderByDescending(e => e.Frecuente)
            .ThenBy(e => e.NombreVulgar)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RegistroProduccion>> GetProduccionAsync(string mareaEtapaId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.RegistrosProduccion
            .Include(x => x.Producto)
            .Include(x => x.Especie)
            .Where(x => x.MareaEtapaId == mareaEtapaId)
            .OrderBy(x => x.Fecha)
            .ThenBy(x => x.Especie!.NombreVulgar)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
