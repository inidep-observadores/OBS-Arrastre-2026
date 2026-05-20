using Microsoft.EntityFrameworkCore;
using ControlMareas.App.Data;
using ControlMareas.App.ViewModels;

namespace ControlMareas.App.Services;

public interface IBuqueService
{
    Task<IReadOnlyList<BuqueListItemViewModel>> GetBuquesAsync(bool onlyWithMareas = false, CancellationToken cancellationToken = default);
}

public sealed class BuqueService(IDbContextFactory<AppDbContext> dbContextFactory) : IBuqueService
{
    public async Task<IReadOnlyList<BuqueListItemViewModel>> GetBuquesAsync(bool onlyWithMareas = false, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var query = dbContext.Buques.AsNoTracking();

        if (onlyWithMareas)
        {
            var buqueIdsConMareas = await dbContext.Mareas
                .Select(m => m.BuqueID)
                .Distinct()
                .ToListAsync(cancellationToken);

            query = query.Where(b => buqueIdsConMareas.Contains(b.Id));
        }

        return await query
            .OrderBy(x => x.Nombre)
            .Select(x => new BuqueListItemViewModel(
                x.Id,
                x.Nombre,
                x.Matricula,
                x.IdRadial,
                x.IMO,
                x.MMSI))
            .ToListAsync(cancellationToken);
    }
}
