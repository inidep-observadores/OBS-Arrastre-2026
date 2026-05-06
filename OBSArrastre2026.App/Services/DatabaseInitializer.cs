using Microsoft.EntityFrameworkCore;
using OBSArrastre2026.App.Data;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.Services;

public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task SeedCatalogsAsync(CancellationToken cancellationToken = default);
}

public sealed class DatabaseInitializer(IDbContextFactory<AppDbContext> dbContextFactory) : IDatabaseInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        // Asegurar que la base de datos esté actualizada con la última migración
        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    public async Task SeedCatalogsAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        // Sembrar datos de catálogo Largo-Peso
        await SeedLargoPesoAsync(dbContext, cancellationToken);
    }

    private async Task SeedLargoPesoAsync(AppDbContext dbContext, CancellationToken ct)
    {
        if (await dbContext.EspeciesLargoPeso.AnyAsync(ct)) return;

        var seedData = new[]
        {
            // Merluza común (M. hubbsi) - 7210040101
            new { Code = "7210040101", Sexo = 0, A = 0.009, B = 2.92527, Medida = "LT", Obs = "Oficial 2026 - Total" },
            new { Code = "7210040101", Sexo = 1, A = 0.01048, B = 2.87878, Medida = "LT", Obs = "Oficial 2026 - Machos" },
            new { Code = "7210040101", Sexo = 2, A = 0.00855, B = 2.94086, Medida = "LT", Obs = "Oficial 2026 - Hembras" },
            
            // Abadejo (G. blacodes) - 7226030101
            new { Code = "7226030101", Sexo = 0, A = 0.00096, B = 3.352, Medida = "LT", Obs = "Oficial 2026 - Total" },

            // Merluza de cola (M. magellanicus) - 7210040201
            new { Code = "7210040201", Sexo = 0, A = 0.027, B = 3.03142, Medida = "LT", Obs = "Oficial 2026 - Total" },
            new { Code = "7210040201", Sexo = 1, A = 0.029, B = 3.01028, Medida = "LT", Obs = "Oficial 2026 - Machos" },
            new { Code = "7210040201", Sexo = 2, A = 0.025, B = 3.04753, Medida = "LT", Obs = "Oficial 2026 - Hembras" },

            // Merluza Austral (M. australis) - 7210040102
            new { Code = "7210040102", Sexo = 0, A = 0.025, B = 3.2702, Medida = "LT", Obs = "Oficial 2026 - Total" },

            // Polaca (M. australis) - 7210030201
            new { Code = "7210030201", Sexo = 0, A = 0.024, B = 3.248, Medida = "LT", Obs = "Oficial 2026 - Total" },
            new { Code = "7210030201", Sexo = 1, A = 0.021, B = 3.2846, Medida = "LT", Obs = "Oficial 2026 - Machos" },
            new { Code = "7210030201", Sexo = 2, A = 0.026, B = 3.2278, Medida = "LT", Obs = "Oficial 2026 - Hembras" },

            // Merluza negra (D. eleginoides) - 7218320101
            new { Code = "7218320101", Sexo = 0, A = 0.0042, B = 3.19385, Medida = "LT", Obs = "Oficial 2026 - Total" },

            // Pescadilla (Cynoscion guatucupa) - 7218160501
            new { Code = "7218160501", Sexo = 0, A = 0.00552, B = 3.1310, Medida = "LT", Obs = "Región Bonaerense" },

            // Savorín (S. porosa) - 7218350201
            new { Code = "7218350201", Sexo = 0, A = 0.004, B = 3.1989, Medida = "LT", Obs = "Oficial 2026 - Total" },

            // Granadero Grande (Macruronus spp.) - 7210090401
            new { Code = "7210090401", Sexo = 0, A = 0.0044, B = 3.0251, Medida = "LPA", Obs = "Oficial 2026 - Grande" },

            // Granadero Chico (C. fasciatus) - 7210090601
            new { Code = "7210090601", Sexo = 0, A = 0.0038, B = 3.071, Medida = "LPA", Obs = "Oficial 2026 - Chico" },

            // Bacalao Austral / Salilota (S. australis) - 7210020101
            new { Code = "7210020101", Sexo = 0, A = 0.026, B = 2.7262, Medida = "LT", Obs = "Oficial 2026 - Total" },
            new { Code = "7210020101", Sexo = 1, A = 0.03, B = 2.6755, Medida = "LT", Obs = "Oficial 2026 - Machos" },
            new { Code = "7210020101", Sexo = 2, A = 0.024, B = 2.7518, Medida = "LT", Obs = "Oficial 2026 - Hembras" },

            // Gatuzo (Mustelus schmitti) - 7105010101
            new { Code = "7105010101", Sexo = 0, A = 0.002, B = 3.1913, Medida = "LT", Obs = "Oficial 2026 - Total" },

            // Pez palo (Percophis brasiliensis) - 7218250101
            new { Code = "7218250101", Sexo = 0, A = 0.00313, B = 3.082422, Medida = "LT", Obs = "Oficial 2026 - Sur" },

            // Nototenia (Patagonotothen ramsayi) - 7218280105
            new { Code = "7218280105", Sexo = 0, A = 0.0039, B = 3.32, Medida = "LT", Obs = "Oficial 2026 - Total" },

            // Caballa (Scomber colias) - 7218360101
            new { Code = "7218360101", Sexo = 0, A = 0.0007, B = 3.4959, Medida = "LT", Obs = "Oficial 2026 - Total" },

            // Raya (Zearaja brevicaudata) - 7109010105
            new { Code = "7109010105", Sexo = 1, A = 0.001, B = 3.2575, Medida = "LT", Obs = "Oficial 2026 - Machos" },
            new { Code = "7109010105", Sexo = 2, A = 0.001, B = 3.2573, Medida = "LT", Obs = "Oficial 2026 - Hembras" },

            // Calamar (I. argentinus) - 5702150101
            new { Code = "5702150101", Sexo = 0, A = 0.011, B = 3.15, Medida = "LM", Obs = "Largo de Manto" }
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
