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

        // Asegurar que la base de datos esté actualizada con la última migración
        await dbContext.Database.MigrateAsync(cancellationToken);

        // Sembrar datos de catálogo Largo-Peso
        await SeedLargoPesoAsync(dbContext, cancellationToken);
    }

    private async Task SeedLargoPesoAsync(AppDbContext dbContext, CancellationToken ct)
    {
        if (await dbContext.EspeciesLargoPeso.AnyAsync(ct)) return;

        var seedData = new[]
        {
            // Merluza común (M. hubbsi) - 7210040101
            new { Code = "7210040101", Sexo = 1, A = 0.01124, B = 2.8340, Medida = "LT", Obs = "Stock Patagónico" },
            new { Code = "7210040101", Sexo = 2, A = 0.00985, B = 2.8920, Medida = "LT", Obs = "Stock Patagónico" },
            new { Code = "7210040101", Sexo = 0, A = 0.01050, B = 2.8630, Medida = "LT", Obs = "Promedio general" },
            
            // Abadejo (G. blacodes) - 7226030101
            new { Code = "7226030101", Sexo = 1, A = 0.00225, B = 3.2150, Medida = "LT", Obs = "Datos consolidados" },
            new { Code = "7226030101", Sexo = 2, A = 0.00238, B = 3.2080, Medida = "LT", Obs = "Datos consolidados" },
            new { Code = "7226030101", Sexo = 0, A = 0.00231, B = 3.2115, Medida = "LT", Obs = "Promedio general" },

            // Merluza de cola (M. magellanicus) - 7210040201
            new { Code = "7210040201", Sexo = 1, A = 0.00791, B = 2.8150, Medida = "LT", Obs = "Campañas de invierno" },
            new { Code = "7210040201", Sexo = 2, A = 0.00745, B = 2.8280, Medida = "LT", Obs = "Campañas de invierno" },

            // Polaca (M. australis) - 7210030201
            new { Code = "7210030201", Sexo = 1, A = 0.00455, B = 3.1250, Medida = "LT", Obs = "Área Sur" },
            new { Code = "7210030201", Sexo = 2, A = 0.00472, B = 3.1180, Medida = "LT", Obs = "Área Sur" },

            // Merluza negra (D. eleginoides) - 7218320101
            new { Code = "7218320101", Sexo = 1, A = 0.01105, B = 3.0250, Medida = "LT", Obs = "Largo Total (LT)" },
            new { Code = "7218320101", Sexo = 2, A = 0.01142, B = 2.9980, Medida = "LT", Obs = "Largo Total (LT)" },

            // Corvina Rubia (M. furnieri) - 7218160101
            new { Code = "7218160101", Sexo = 1, A = 0.00715, B = 3.0910, Medida = "LT", Obs = "Especie costera" },
            new { Code = "7218160101", Sexo = 2, A = 0.00748, B = 3.0820, Medida = "LT", Obs = "Especie costera" },

            // Pescadilla (Cynoscion guatucupa) - 7218160501
            new { Code = "7218160501", Sexo = 1, A = 0.00552, B = 3.1310, Medida = "LT", Obs = "Región Bonaerense" },
            new { Code = "7218160501", Sexo = 2, A = 0.00571, B = 3.1190, Medida = "LT", Obs = "Región Bonaerense" },

            // Savorín (S. porosa) - 7218350201
            new { Code = "7218350201", Sexo = 0, A = 0.01250, B = 3.0500, Medida = "LT", Obs = "Poca diferenciación sexual" },

            // Granadero (M. holotrachys) - 7210090401
            new { Code = "7210090401", Sexo = 0, A = 0.00040, B = 3.5200, Medida = "LPA", Obs = "Largo Preanal" },

            // Calamar (I. argentinus) - 5702150101
            new { Code = "5702150101", Sexo = 0, A = 0.01100, B = 3.1500, Medida = "LM", Obs = "Largo de Manto" }
        };

        foreach (var data in seedData)
        {
            var especie = await dbContext.Especies.FirstOrDefaultAsync(e => e.CodigoInidep == data.Code, ct);
            if (especie != null)
            {
                dbContext.EspeciesLargoPeso.Add(new EspecieLargoPeso
                {
                    EspecieId = especie.ID,
                    Sexo = data.Sexo,
                    ParamA = data.A,
                    ParamB = data.B,
                    TipoMedida = data.Medida,
                    Observaciones = data.Obs
                });
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }
}
