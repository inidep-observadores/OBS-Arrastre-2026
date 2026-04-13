using Microsoft.EntityFrameworkCore;
using OBSArrastre2026.App.Data;

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

        // Asegurar que la base de datos esté actualizada con la última migración
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
