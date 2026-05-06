using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OBSArrastre2026.App.Data;
using OBSArrastre2026.App.Data.Entities;
using OBSArrastre2026.App.Models.Reports;

namespace OBSArrastre2026.App.Services;

public class MareaSummaryService(IDbContextFactory<AppDbContext> dbContextFactory) : IMareaSummaryService
{
    public async Task<MareaSummaryReport> GetMareaSummaryAsync(string mareaId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var marea = await dbContext.Mareas
            .Include(m => m.Buque)
            .Include(m => m.Etapas)
            .FirstOrDefaultAsync(m => m.ID == mareaId);

        if (marea == null) throw new Exception("Marea no encontrada");

        var report = new MareaSummaryReport
        {
            Barco = marea.Buque?.Nombre ?? "Sin Buque",
            Marea = marea.NumeroInidep.ToString(),
            Anio = marea.AnioInidep,
            BuqueCodigo = marea.BuqueCodigo,
            ObservadorNombre = marea.ObservadorNombre,
            ObservadorApellido = marea.ObservadorApellido,
            ObservadorCodigo = marea.ObservadorCodigo,
            FechaInicioMarea = marea.FechaInicio,
            FechaFinMarea = marea.FechaFin
        };

        // Obtener todos los datos necesarios
        var stageIds = marea.Etapas.Select(e => e.ID).ToList();
        
        var lances = await dbContext.Lances
            .Include(l => l.ItemsCaptura).ThenInclude(i => i.Especie)
            .Include(l => l.Muestras).ThenInclude(m => m.Especie)
            .Include(l => l.Muestras).ThenInclude(m => m.FrecuenciasTallas)
            .Include(l => l.Muestras).ThenInclude(m => m.ItemsSubmuestras)
            .Where(l => stageIds.Contains(l.MareaEtapaId))
            .ToListAsync();

        var produccion = await dbContext.RegistrosProduccion
            .Include(p => p.Especie)
            .Where(p => stageIds.Contains(p.MareaEtapaId))
            .ToListAsync();

        // 1. Resumen General
        report.ResumenGeneral = CreateSection("RESUMEN GENERAL DE MAREA", lances, produccion, marea.Etapas);

        // 2. Resumen por Etapa (si hay más de una)
        if (marea.Etapas.Count > 1)
        {
            var sortedEtapas = marea.Etapas.OrderBy(e => e.FechaZarpada).ToList();
            for (int i = 0; i < sortedEtapas.Count; i++)
            {
                var etapa = sortedEtapas[i];
                int nroEtapa = i + 1;
                var lancesEtapa = lances.Where(l => l.MareaEtapaId == etapa.ID).ToList();
                var produccionEtapa = produccion.Where(p => p.MareaEtapaId == etapa.ID).ToList();
                var section = CreateSection($"RESUMEN ETAPA {nroEtapa}", lancesEtapa, produccionEtapa, new List<MareaEtapa> { etapa });
                section.EsEtapa = true;
                section.NumeroEtapa = nroEtapa;
                report.ResumenEtapas.Add(section);
            }
        }

        return report;
    }

    private MareaSummarySection CreateSection(string titulo, List<Lance> lances, List<RegistroProduccion> produccion, ICollection<MareaEtapa> etapas)
    {
        var section = new MareaSummarySection { Titulo = titulo };

        if (etapas.Any())
        {
            section.FechaInicio = etapas.Min(e => e.FechaZarpada);
            section.FechaFin = etapas.Max(e => e.FechaArribo);
        }

        // Días Navegados (Días únicos entre inicio y fin de cada etapa)
        var uniqueNavDays = new HashSet<DateTime>();
        foreach (var etapa in etapas)
        {
            DateTime start = etapa.FechaZarpada.Date;
            DateTime end = (etapa.FechaArribo ?? DateTime.Now).Date;
            for (var dt = start; dt <= end; dt = dt.AddDays(1))
            {
                uniqueNavDays.Add(dt);
            }
        }
        section.DiasNavegados = uniqueNavDays.Count;

        // Días de Pesca (Días únicos con lances)
        section.DiasPesca = lances.Select(l => DateTime.Parse(l.Fecha).Date).Distinct().Count();
        section.CantidadLances = lances.Count;

        // Conteo de Muestras y Submuestras
        var todasMuestras = lances.SelectMany(l => l.Muestras).ToList();
        section.CantidadMuestrasCaptura = todasMuestras.Count(m => m.TipoMuestra == 1);
        section.CantidadMuestrasDescarte = todasMuestras.Count(m => m.TipoMuestra == 2);
        section.CantidadSubmuestras = todasMuestras.Count(m => m.ItemsSubmuestras.Any());

        // Especies Objetivo y Estadísticas por Especie
        double totalProduccionGlobal = produccion.Sum(p => p.Kg ?? 0);
        
        var especiesIds = lances.SelectMany(l => l.ItemsCaptura).Select(i => i.EspecieID)
            .Union(produccion.Select(p => p.EspecieId))
            .Distinct()
            .Where(id => id != null)
            .ToList();

        foreach (var espId in especiesIds)
        {
            var itemsCaptura = lances.SelectMany(l => l.ItemsCaptura).Where(i => i.EspecieID == espId).ToList();
            var prodEspecie = produccion.Where(p => p.EspecieId == espId).ToList();
            var muestrasEspecie = todasMuestras.Where(m => m.EspecieID == espId).ToList();
            
            double captTotal = itemsCaptura.Sum(i => i.CapturaTotalKgCalculado);
            double descTotal = itemsCaptura.Sum(i => i.PesoDescarteCalculado);
            double prodTotal = prodEspecie.Sum(p => p.Kg ?? 0);
            
            var item = new MareaSummarySpeciesItem
            {
                EspecieId = espId!,
                NombreCientifico = prodEspecie.FirstOrDefault()?.Especie?.NombreCientifico 
                                  ?? itemsCaptura.FirstOrDefault()?.Especie?.NombreCientifico 
                                  ?? muestrasEspecie.FirstOrDefault()?.Especie?.NombreCientifico 
                                  ?? "Desconocida",
                CodigoInidep = prodEspecie.FirstOrDefault()?.Especie?.CodigoInidep 
                              ?? itemsCaptura.FirstOrDefault()?.Especie?.CodigoInidep 
                              ?? muestrasEspecie.FirstOrDefault()?.Especie?.CodigoInidep 
                              ?? "",
                CapturaTotal = captTotal,
                DescarteKg = descTotal,
                ProduccionTotal = prodTotal,
                NroLances = lances.Count(l => l.ItemsCaptura.Any(i => i.EspecieID == espId)),
                NroDias = lances.Where(l => l.ItemsCaptura.Any(i => i.EspecieID == espId)).Select(l => DateTime.Parse(l.Fecha).Date).Distinct().Count(),
                EsObjetivo = totalProduccionGlobal > 0 && (prodTotal / totalProduccionGlobal) >= 0.2
            };

            // Porcentaje de Juveniles
            int cutoff = GetSpeciesCutoff(item.CodigoInidep);
            if (cutoff > 0)
            {
                var frecs = muestrasEspecie.SelectMany(m => m.FrecuenciasTallas).ToList();
                double totalMedidos = frecs.Sum(f => f.NroTotal);
                if (totalMedidos > 0)
                {
                    double juveniles = frecs.Where(f => f.Talla < cutoff).Sum(f => f.NroTotal);
                    item.PorcentajeJuveniles = (juveniles * 100.0) / totalMedidos;
                }
            }

            // Especies Muestreadas
            if (muestrasEspecie.Any())
            {
                section.EspeciesMuestreadas.Add(new MareaSummarySampledSpeciesItem
                {
                    EspecieId = espId!,
                    NombreCientifico = item.NombreCientifico,
                    MuestrasCaptura = muestrasEspecie.Count(m => m.TipoMuestra == 1),
                    MuestrasDescarte = muestrasEspecie.Count(m => m.TipoMuestra == 2),
                    MuestrasConSubmuestra = muestrasEspecie.Count(m => m.ItemsSubmuestras.Any())
                });
            }

            if (item.EsObjetivo)
            {
                section.EspeciesObjetivo.Add(item);
            }
        }
        
        section.EspeciesMuestreadas = section.EspeciesMuestreadas.OrderBy(e => e.NombreCientifico).ToList();
        section.EspeciesObjetivo = section.EspeciesObjetivo.OrderByDescending(e => e.ProduccionTotal).ToList();

        // Áreas
        var areaGroups = lances.GroupBy(l => GetCuadricula(l))
            .Select(g => new MareaSummaryAreaItem
            {
                Area = g.Key,
                CantidadLances = g.Count(),
                CapturaKg = g.Sum(l => l.ItemsCaptura.Sum(i => i.CapturaTotalKgCalculado)),
                DiasPesca = g.Select(l => DateTime.Parse(l.Fecha).Date).Distinct().Count()
            }).ToList();

        section.Areas = areaGroups.OrderBy(a => a.Area).ToList();
        section.AreaMasLances = areaGroups.OrderByDescending(a => a.CantidadLances).FirstOrDefault()?.Area ?? "-";
        section.AreaMayorCaptura = areaGroups.OrderByDescending(a => a.CapturaKg).FirstOrDefault()?.Area ?? "-";

        return section;
    }

    private string GetCuadricula(Lance lance)
    {
        if (!lance.LatitudInicioDecimal.HasValue || !lance.LongitudInicioDecimal.HasValue) 
            return "S/D";

        double lat = Math.Abs(lance.LatitudInicioDecimal.Value);
        double lon = Math.Abs(lance.LongitudInicioDecimal.Value);

        int cuad = ((int)Math.Truncate(lat) * 100) + (int)Math.Truncate(lon);
        return cuad.ToString();
    }

    private int GetSpeciesCutoff(string? codigoInidep)
    {
        if (string.IsNullOrEmpty(codigoInidep)) return 0;

        return codigoInidep switch
        {
            "7210040101" => 35, // Merluza Hubbsi
            "7210040201" => 59, // Merluza de Cola
            "7226030101" => 70, // Abadejo
            "7210030201" => 32, // Polaca
            "7210040102" => 61, // Merluza Austral
            "7210020101" => 40, // Salilota australis
            "7218320101" => 82, // Merluza Negra
            "7218350201" => 29, // Savorín
            "7218160501" => 30, // Pescadilla común
            "7204020101" => 9,  // Anchoíta
            "7105010101" => 56, // Gatuzo
            "7218250101" => 30, // Pez palo
            "7218360101" => 24, // Caballa
            "7109010105" => 78, // Raya
            "5139440101" => 7,  // Centolla (70mm)
            _ => 0
        };
    }
}
