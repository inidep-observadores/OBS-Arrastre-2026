using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ControlMareas.App.Data;
using ControlMareas.App.Data.Entities;

namespace ControlMareas.App.Services;

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

    public async Task<IReadOnlyList<EspecieLargoPeso>> GetParametrosAlometricosAsync(string especieId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        return await dbContext.EspeciesLargoPeso
            .Where(lp => lp.EspecieId == especieId)
            .ToListAsync(cancellationToken);
    }

    public async Task RecalcularPesosMuestrasAsync(string mareaId, IProgress<(int Current, int Total, string Message)>? progress = null, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        progress?.Report((0, 100, "Cargando muestras y parámetros alométricos..."));

        var muestras = await dbContext.Muestras
            .Include(m => m.FrecuenciasTallas)
            .Include(m => m.Especie)
            .Include(m => m.Lance)
                .ThenInclude(l => l.MareaEtapa)
            .Where(m => m.Lance!.MareaEtapa!.MareaID == mareaId)
            .ToListAsync(cancellationToken);

        if (!muestras.Any())
        {
            progress?.Report((100, 100, "No hay muestras para procesar."));
            return;
        }

        var especieIds = muestras.Select(m => m.EspecieID).Distinct().ToList();
        var parametros = await dbContext.EspeciesLargoPeso
            .Where(p => especieIds.Contains(p.EspecieId))
            .ToListAsync(cancellationToken);

        // Crear un diccionario rápido por EspecieId y Sexo (0=Indet, 1=Macho, 2=Hembra, 3=Ambos)
        var paramDict = parametros.GroupBy(p => p.EspecieId)
            .ToDictionary(g => g.Key, g => g.ToList());

        int totalMuestras = muestras.Count;
        int current = 0;

        foreach (var muestra in muestras)
        {
            current++;
            string spName = muestra.Especie?.NombreVulgar ?? "Especie desconocida";
            progress?.Report((current, totalMuestras, $"Calculando muestra {current} de {totalMuestras} ({spName})"));

            if (muestra.FrecuenciasTallas == null || !muestra.FrecuenciasTallas.Any() || muestra.EspecieID == null)
            {
                continue;
            }

            // Respetar pesos cargados a mano (no automáticos) si ya tienen un valor > 0
            if (!muestra.Automatica && muestra.PesoMuestra_PesoGramos > 0)
            {
                continue;
            }

            if (!paramDict.TryGetValue(muestra.EspecieID, out var pList) || !pList.Any())
            {
                continue; // No hay parámetros para esta especie
            }

            // Función local para obtener parámetros
            (double A, double B) GetParams(int sex)
            {
                var p = pList.FirstOrDefault(x => x.Sexo == sex);
                if (p == null && sex != 0) p = pList.FirstOrDefault(x => x.Sexo == 3); // Fallback a general
                if (p == null && sex != 0) p = pList.FirstOrDefault(x => x.Sexo == 0); // Fallback a indeterminado
                if (p == null) p = pList.FirstOrDefault(); // Cualquier registro
                return p != null ? (p.ParamA, p.ParamB) : (0, 0);
            }

            var pM = GetParams(1);
            var pH = GetParams(2);
            var pI = GetParams(0);

            double totalWeightGramos = 0;

            foreach (var f in muestra.FrecuenciasTallas)
            {
                double tallaCm = f.Talla;
                
                // Si está discriminado
                if (f.NroMachos > 0 || f.NroHembras > 0)
                {
                    if (f.NroMachos > 0 && pM.A > 0) totalWeightGramos += (f.NroMachos * (pM.A * Math.Pow(tallaCm, pM.B)));
                    if (f.NroHembras > 0 && pH.A > 0) totalWeightGramos += (f.NroHembras * (pH.A * Math.Pow(tallaCm, pH.B)));
                    if (f.NroIndeterminados > 0 && pI.A > 0) totalWeightGramos += (f.NroIndeterminados * (pI.A * Math.Pow(tallaCm, pI.B)));
                }
                else if (f.NroIndeterminados > 0 || f.NroTotal > 0)
                {
                    // No discriminado
                    if (pI.A > 0)
                    {
                        int sum = f.NroTotal > 0 ? f.NroTotal : (f.NroMachos + f.NroHembras + f.NroIndeterminados);
                        totalWeightGramos += (sum * (pI.A * Math.Pow(tallaCm, pI.B)));
                    }
                }
            }

            if (totalWeightGramos > 0)
            {
                muestra.PesoMuestra_PesoGramos = Math.Round(totalWeightGramos, 2);
            }
        }

        progress?.Report((totalMuestras, totalMuestras, "Guardando cambios en la base de datos..."));
        await dbContext.SaveChangesAsync(cancellationToken);
        progress?.Report((totalMuestras, totalMuestras, "Proceso completado."));
    }
}
