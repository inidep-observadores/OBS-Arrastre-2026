using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ControlMareas.App.Services.Internal;
using ControlMareas.App.Data;
using ControlMareas.App.Data.Entities;
using ControlMareas.App.Models.Reports;

namespace ControlMareas.App.Services;

public class MareaSummaryService(IDbContextFactory<AppDbContext> dbContextFactory) : IMareaSummaryService
{
    public async Task<MareaSummaryReport> GetMareaSummaryAsync(string mareaId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var marea = await dbContext.Mareas
            .Include(m => m.Buque)
            .Include(m => m.Etapas).ThenInclude(e => e.EspecieObjetivo)
            .FirstOrDefaultAsync(m => m.ID == mareaId);

        if (marea == null) throw new Exception("Marea no encontrada");

        var meta = MareaMetadataHelper.GetMetadata(marea);

        var report = new MareaSummaryReport
        {
            Barco = marea.Buque?.Nombre ?? "Sin Buque",
            Marea = marea.NumeroInidep.ToString(),
            Anio = marea.AnioInidep,
            BuqueCodigo = meta.BuqueCodigo,
            ObservadorNombre = meta.ObservadorNombre,
            ObservadorApellido = meta.ObservadorApellido,
            ObservadorCodigo = meta.ObservadorCodigo,
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
            foreach (var etapa in marea.Etapas.OrderBy(e => e.FechaZarpada))
            {
                var lancesEtapa = lances.Where(l => string.Equals(l.MareaEtapaId, etapa.ID, StringComparison.OrdinalIgnoreCase)).ToList();
                var produccionEtapa = produccion.Where(p => string.Equals(p.MareaEtapaId, etapa.ID, StringComparison.OrdinalIgnoreCase)).ToList();
                var section = CreateSection($"RESUMEN ETAPA {etapa.NumeroEtapa}", lancesEtapa, produccionEtapa, new List<MareaEtapa> { etapa });
                section.EsEtapa = true;
                section.NumeroEtapa = etapa.NumeroEtapa;
                report.ResumenEtapas.Add(section);
            }
        }

        // 3. Datos para la narrativa textual
        BuildNarrativaData(report, marea, lances, produccion);

        return report;
    }

    /// <summary>
    /// Recolecta y consolida todos los datos necesarios para armar el resumen narrativo textual de la marea.
    /// </summary>
    private void BuildNarrativaData(
        MareaSummaryReport report,
        Data.Entities.Marea marea,
        List<Lance> lances,
        List<RegistroProduccion> produccion)
    {
        var sortedEtapas = marea.Etapas.OrderBy(e => e.FechaZarpada).ToList();

        // -- Totales generales de la marea --
        report.NarrativaCapturaTotal = lances.SelectMany(l => l.ItemsCaptura).Sum(i => i.CapturaTotalKgCalculado);
        report.NarrativaDescarteTotal = lances.SelectMany(l => l.ItemsCaptura).Sum(i => i.PesoDescarteCalculado);
        report.NarrativaTotalLances = lances.Count;
        report.NarrativaTotalDiasPesca = lances.Select(l => DateTime.Parse(l.Fecha).Date).Distinct().Count();

        // -- Narrativa por etapa --
        for (int i = 0; i < sortedEtapas.Count; i++)
        {
            var etapa = sortedEtapas[i];
            var lancesEtapa = lances.Where(l => string.Equals(l.MareaEtapaId, etapa.ID, StringComparison.OrdinalIgnoreCase)).ToList();

            // Cuadrados estadísticos (ordenados, sin repetición)
            var cuadradoGroups = lancesEtapa
                .Where(l => l.LatitudInicioDecimal.HasValue && l.LongitudInicioDecimal.HasValue)
                .GroupBy(l => GetCuadricula(l))
                .Select(g => new
                {
                    Cuadrado = g.Key,
                    Lances = g.Count(),
                    CapturaKg = g.Sum(l => l.ItemsCaptura.Sum(ic => ic.CapturaTotalKgCalculado)),
                    Dias = g.Select(l => DateTime.Parse(l.Fecha).Date).Distinct().Count()
                })
                .OrderByDescending(g => g.Lances)
                .ThenByDescending(g => g.CapturaKg)
                .ToList();

            var masLances = cuadradoGroups.OrderByDescending(g => g.Lances).ThenByDescending(g => g.CapturaKg).FirstOrDefault();
            var mayorCaptura = cuadradoGroups.OrderByDescending(g => g.CapturaKg).FirstOrDefault();
            var dominante = masLances; // Para mantener compatibilidad

            var cuadradosOrdenados = cuadradoGroups.Select(g => g.Cuadrado).OrderBy(c => c).ToList();

            // Especie objetivo de la etapa
            NarrativaEspecieObjetivo? espObj = null;
            if (etapa.EspecieObjetivo != null)
            {
                var espId = etapa.EspecieObjetivoID;
                var captEsp = lancesEtapa.SelectMany(l => l.ItemsCaptura).Where(ic => ic.EspecieID == espId).ToList();
                espObj = new NarrativaEspecieObjetivo
                {
                    NombreVulgar = etapa.EspecieObjetivo.NombreVulgar ?? etapa.EspecieObjetivo.NombreCientifico ?? string.Empty,
                    NombreCientifico = etapa.EspecieObjetivo.NombreCientifico ?? string.Empty,
                    CapturaKg = captEsp.Sum(ic => ic.CapturaTotalKgCalculado),
                    DescarteKg = captEsp.Sum(ic => ic.PesoDescarteCalculado)
                };
            }

            // Producción de la etapa (para calcular el umbral del 20%)
            var produccionEtapa = produccion.Where(p => string.Equals(p.MareaEtapaId, etapa.ID, StringComparison.OrdinalIgnoreCase)).ToList();
            double prodTotalEtapa = produccionEtapa.Sum(p => p.Kg ?? 0);

            // Códigos INIDEP relevantes para la regla de negocio narrativa
            const string CodigoLangostino   = "5139030101";
            const string CodigoMerluzaHubbsi = "7210040101";

            bool objetivoEsLangostino = etapa.EspecieObjetivo?.CodigoInidep == CodigoLangostino;

            // Especies secundarias: solo las que representan ≥20% de la producción de la etapa,
            // más Merluza común (M. hubbsi) si la especie objetivo es Langostino.
            var espSecundarias = lancesEtapa
                .SelectMany(l => l.ItemsCaptura)
                .Where(ic => ic.EspecieID != etapa.EspecieObjetivoID && ic.Especie != null)
                .GroupBy(ic => ic.EspecieID)
                .Select(g =>
                {
                    var esp = g.First().Especie!;
                    double capt = g.Sum(ic => ic.CapturaTotalKgCalculado);
                    double desc = g.Sum(ic => ic.PesoDescarteCalculado);
                    double prodEsp = produccionEtapa.Where(p => p.EspecieId == esp.ID).Sum(p => p.Kg ?? 0);
                    double pctProd = prodTotalEtapa > 0 ? prodEsp * 100.0 / prodTotalEtapa : 0;

                    bool esMerluzaHubbsi = esp.CodigoInidep == CodigoMerluzaHubbsi;

                    // Incluir si supera el umbral del 20% o si es Merluza común en marea de Langostino
                    bool incluir = pctProd >= 20.0 || (objetivoEsLangostino && esMerluzaHubbsi);

                    return new
                    {
                        Especie = new NarrativaEspecieSecundaria
                        {
                            NombreVulgar    = esp.NombreVulgar ?? esp.NombreCientifico ?? string.Empty,
                            NombreCientifico = esp.NombreCientifico ?? string.Empty,
                            CapturaKg  = capt,
                            DescarteKg = desc
                        },
                        Incluir = incluir
                    };
                })
                .Where(x => x.Incluir && x.Especie.CapturaKg > 0)
                .Select(x => x.Especie)
                .OrderByDescending(e => e.CapturaKg)
                .ToList();

            var narrativaEtapa = new NarrativaEtapa
            {
                Numero = etapa.NumeroEtapa,
                TipoEtapa = etapa.TipoEtapa,
                FechaInicio = etapa.FechaZarpada,
                FechaFin = etapa.FechaArribo ?? etapa.FechaZarpada,
                TotalLances = lancesEtapa.Count,
                DiasPesca = lancesEtapa.Select(l => DateTime.Parse(l.Fecha).Date).Distinct().Count(),
                CapturaKg = lancesEtapa.SelectMany(l => l.ItemsCaptura).Sum(ic => ic.CapturaTotalKgCalculado),
                DescarteKg = lancesEtapa.SelectMany(l => l.ItemsCaptura).Sum(ic => ic.PesoDescarteCalculado),
                Cuadrados = cuadradosOrdenados,
                CuadradoMasLances = masLances?.Cuadrado,
                CuadradoMasLancesNro = masLances?.Lances ?? 0,
                CuadradoMayorCaptura = mayorCaptura?.Cuadrado,
                CuadradoMayorCapturaKg = mayorCaptura?.CapturaKg ?? 0,
                CuadradoDominante = dominante?.Cuadrado,
                CuadradoDominanteLances = dominante?.Lances ?? 0,
                CuadradoDominanteCapturaKg = dominante?.CapturaKg ?? 0,
                CuadradoDominanteDias = dominante?.Dias ?? 0,
                EspecieObjetivo = espObj,
                EspeciesSecundarias = espSecundarias
            };

            report.NarrativaEtapas.Add(narrativaEtapa);
        }

        // -- Especies objetivo únicas de toda la marea (para el párrafo introductorio) --
        // Regla: Aquellas que representen >= 20% de la producción total de la marea
        double totalProdGlobal = produccion.Sum(p => p.Kg ?? 0);
        var especiesObjetivoGlobal = produccion
            .GroupBy(p => p.EspecieId)
            .Select(g => new
            {
                EspecieId = g.Key,
                ProdKg = g.Sum(p => p.Kg ?? 0)
            })
            .Where(g => totalProdGlobal > 0 && (g.ProdKg / totalProdGlobal) >= 0.2)
            .ToList();

        report.NarrativaEspeciesObjetivo = especiesObjetivoGlobal.Select(eg => {
            var itemsCaptura = lances.SelectMany(l => l.ItemsCaptura).Where(ic => ic.EspecieID == eg.EspecieId).ToList();
            var esp = itemsCaptura.FirstOrDefault()?.Especie ?? produccion.FirstOrDefault(p => p.EspecieId == eg.EspecieId)?.Especie;
            return new NarrativaEspecieObjetivo
            {
                NombreVulgar = esp?.NombreVulgar ?? esp?.NombreCientifico ?? "Desconocida",
                NombreCientifico = esp?.NombreCientifico ?? "Desconocida",
                CapturaKg = itemsCaptura.Sum(ic => ic.CapturaTotalKgCalculado),
                DescarteKg = itemsCaptura.Sum(ic => ic.PesoDescarteCalculado)
            };
        }).OrderByDescending(e => e.CapturaKg).ToList();

        // -- Resumen de muestras por especie (párrafo de cierre) --
        var muestrasAgrupadas = lances
            .SelectMany(l => l.Muestras)
            .Where(m => m.Especie != null && (m.TipoMuestra == 1 || m.TipoMuestra == 2)) // Muestras de talla estándar y descarte
            .GroupBy(m => m.EspecieID)
            .Select(g =>
            {
                var esp = g.First().Especie!;
                return new NarrativaMuestraEspecie
                {
                    NombreVulgar = esp.NombreVulgar ?? esp.NombreCientifico ?? string.Empty,
                    NombreCientifico = esp.NombreCientifico ?? string.Empty,
                    TotalMuestras = g.Count(m => m.TipoMuestra == 1),
                    TotalMuestrasDescarte = g.Count(m => m.TipoMuestra == 2)
                };
            })
            .Where(x => x.TotalMuestras > 0 || x.TotalMuestrasDescarte > 0)
            .OrderByDescending(e => e.TotalMuestras)
            .ThenByDescending(e => e.TotalMuestrasDescarte)
            .ToList();
        report.NarrativaMuestras = muestrasAgrupadas;
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
        
        section.EspeciesMuestreadas = section.EspeciesMuestreadas.OrderByDescending(e => e.MuestrasCaptura).ThenBy(e => e.NombreCientifico).ToList();
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
        section.AreaMasLances = areaGroups.OrderByDescending(a => a.CantidadLances).ThenByDescending(a => a.CapturaKg).FirstOrDefault()?.Area ?? "-";
        section.AreaMayorCaptura = areaGroups.OrderByDescending(a => a.CapturaKg).FirstOrDefault()?.Area ?? "-";

        return section;
    }

    private string GetCuadricula(Lance lance)
    {
        if (!lance.LatitudInicioDecimal.HasValue || !lance.LongitudInicioDecimal.HasValue) 
            return "S/D";

        double area = LegacyDecoder.CalculateGridArea(lance.LatitudInicioDecimal.Value, lance.LongitudInicioDecimal.Value);
        // Se ignora el cuadrante (decimal) para la agrupación según requerimiento
        return Math.Truncate(area).ToString("0");
    }

    public async Task<RecibiProyectoReport> GetRecibiProyectoReportAsync(string mareaId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var marea = await dbContext.Mareas
            .Include(m => m.Buque)
            .Include(m => m.Etapas)
            .FirstOrDefaultAsync(m => m.ID == mareaId);

        if (marea == null) throw new Exception("Marea no encontrada");

        var meta = MareaMetadataHelper.GetMetadata(marea);

        var report = new RecibiProyectoReport
        {
            BuqueNombre = marea.Buque?.Nombre ?? "Sin Buque",
            MareaNumero = marea.NumeroInidep.ToString(),
            MareaAnio = marea.AnioInidep,
            ObservadorNombreCompleto = $"{(meta.ObservadorApellido ?? "").ToUpper()}, {meta.ObservadorNombre ?? ""}".Trim(',', ' '),
            FechaGeneracion = DateTime.Now,
            Otolitos = "S",
            Escamas = "N",
            Gonadas = "N"
        };

        var stageIds = marea.Etapas.Select(e => e.ID).ToList();

        var submuestras = await dbContext.ItemsSubmuestras
            .Include(s => s.Muestra).ThenInclude(m => m.Lance)
            .Include(s => s.Muestra).ThenInclude(m => m.Especie)
            .Where(s => stageIds.Contains(s.Muestra.Lance.MareaEtapaId))
            .ToListAsync();

        if (submuestras.Any())
        {
            // Agrupar por especie
            var groupedBySpecies = submuestras
                .GroupBy(s => s.Muestra!.EspecieID)
                .Select(g => new RecibiProyectoEspecieItem
                {
                    NombreEspecie = g.First().Muestra!.Especie?.NombreVulgar ?? g.First().Muestra!.Especie?.NombreCientifico ?? g.First().Muestra!.EspecieOriginal ?? "Desconocida",
                    NombreCientifico = g.First().Muestra!.Especie?.NombreCientifico ?? string.Empty,
                    Lances = g.GroupBy(s => s.Muestra!.Lance!.NroLance)
                              .Select(gl => new RecibiProyectoLanceItem
                              {
                                  NroLance = gl.Key,
                                  Area = gl.First().Muestra!.Area.HasValue 
                                            ? Math.Truncate(gl.First().Muestra!.Area.Value).ToString("0") 
                                            : GetCuadricula(gl.First().Muestra!.Lance!)
                              })
                              .OrderBy(l => l.NroLance)
                              .ToList()
                })
                .OrderBy(e => e.NombreEspecie)
                .ToList();

            report.Especies = groupedBySpecies;
        }

        return report;
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
            "7218280201" => 82, // Merluza Negra
            "7218390102" => 29, // Savorín
            "7218160501" => 30, // Pescadilla común
            "7204020101" => 9,  // Anchoíta
            "7105010101" => 56, // Gatuzo
            "7218250101" => 30, // Pez palo
            "7218360101" => 24, // Caballa
            "5139440101" => 11,  // Centolla (110mm)
            _ => 0
        };
    }
}
