using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ControlMareas.App.Data;
using ControlMareas.App.Data.Entities;

namespace ControlMareas.App.Services;

public sealed class SubmuestraService : ISubmuestraService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public SubmuestraService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<Muestra>> GetMuestrasConSubmuestrasAsync(string mareaId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Muestras
            .Include(m => m.Lance)
                .ThenInclude(l => l.MareaEtapa)
                    .ThenInclude(e => e.Marea)
                        .ThenInclude(m => m!.Etapas)
            .Include(m => m.Especie)
            .Include(m => m.ItemsSubmuestras)
            .Where(m => m.Lance!.MareaEtapa.MareaID == mareaId && m.ItemsSubmuestras.Any())
            .OrderBy(m => m.Lance!.NroLance)
            .ToListAsync();
    }

    public async Task<List<ItemSubmuestra>> GetSubmuestrasByMuestraIdAsync(string muestraId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ItemsSubmuestras
            .Where(s => s.MuestraID == muestraId)
            .OrderBy(s => s.NroEjemplar)
            .ToListAsync();
    }

    public async Task<ItemSubmuestra?> GetSubmuestraByIdAsync(string id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ItemsSubmuestras
            .Include(s => s.Muestra)
                .ThenInclude(m => m!.Lance)
            .Include(s => s.Muestra)
                .ThenInclude(m => m!.Especie)
            .Include(s => s.ContenidosGastricos)
            .FirstOrDefaultAsync(s => s.ID == id);
    }

    public async Task SaveSubmuestraAsync(ItemSubmuestra submuestra)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.ItemsSubmuestras.AnyAsync(s => s.ID == submuestra.ID);

        if (existing)
        {
            context.ItemsSubmuestras.Update(submuestra);
        }
        else
        {
            context.ItemsSubmuestras.Add(submuestra);
        }

        await context.SaveChangesAsync();
    }

    public async Task DeleteSubmuestraAsync(string id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var submuestra = await context.ItemsSubmuestras.FindAsync(id);
        if (submuestra != null)
        {
            context.ItemsSubmuestras.Remove(submuestra);
            await context.SaveChangesAsync();
        }
    }
}
