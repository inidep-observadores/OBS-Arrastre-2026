using Microsoft.EntityFrameworkCore;
using OBSArrastre2026.App.Data;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.Services;

public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

public sealed class DatabaseInitializer(IDbContextFactory<AppDbContext> dbContextFactory) : IDatabaseInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        await dbContext.Database.MigrateAsync(cancellationToken);

        if (await dbContext.Buques.AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.Buques.AddRange(
            new Buque
            {
                Id = Guid.NewGuid().ToString(),
                Nombre = "Mar Azul",
                Matricula = 101,
                IdRadial = 5001,
                IMO = 9321456,
                MMSI = 701000001
            },
            new Buque
            {
                Id = Guid.NewGuid().ToString(),
                Nombre = "Nuevo Horizonte",
                Matricula = 102,
                IdRadial = 5002,
                IMO = 9456781,
                MMSI = 701000002
            });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
