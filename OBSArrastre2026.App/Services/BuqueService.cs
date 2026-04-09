using Microsoft.EntityFrameworkCore;
using OBSArrastre2026.App.Data;
using OBSArrastre2026.App.ViewModels;

namespace OBSArrastre2026.App.Services;

public interface IBuqueService
{
    Task<IReadOnlyList<BuqueListItemViewModel>> GetBuquesAsync(CancellationToken cancellationToken = default);
}

public sealed class BuqueService(IDbContextFactory<AppDbContext> dbContextFactory) : IBuqueService
{
    public async Task<IReadOnlyList<BuqueListItemViewModel>> GetBuquesAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Buques
            .AsNoTracking()
            .OrderBy(x => x.Nombre)
            .Select(x => new BuqueListItemViewModel(
                x.Nombre,
                x.Matricula,
                x.IdRadial,
                x.IMO,
                x.MMSI))
            .ToListAsync(cancellationToken);
    }
}
