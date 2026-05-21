using Microsoft.EntityFrameworkCore;
using ControlMareas.App.Data;
using ControlMareas.App.Data.Entities;
using ControlMareas.App.Models.Import;
using ControlMareas.App.Services.Internal;
using System.Text;

namespace ControlMareas.App.Services;

public interface IMareaValidationService
{
    Task<MareaValidationReport> ValidateExistingMareaAsync(string mareaId);
}

public class MareaValidationService : IMareaValidationService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly MareaValidationEngine _validator;

    public MareaValidationService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
        _validator = new MareaValidationEngine();
    }

    public async Task<MareaValidationReport> ValidateExistingMareaAsync(string mareaId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var marea = await dbContext.Mareas
            .Include(m => m.Buque)
            .Include(m => m.Etapas)
                .ThenInclude(e => e.Lances)
                    .ThenInclude(l => l.ItemsCaptura)
                        .ThenInclude(ic => ic.Especie)
            .Include(m => m.Etapas)
                .ThenInclude(e => e.Lances)
                    .ThenInclude(l => l.Muestras)
                        .ThenInclude(m => m.Especie)
            .Include(m => m.Etapas)
                .ThenInclude(e => e.Lances)
                    .ThenInclude(l => l.Muestras)
                        .ThenInclude(m => m.FrecuenciasTallas)
            .Include(m => m.Etapas)
                .ThenInclude(e => e.Lances)
                    .ThenInclude(l => l.Muestras)
                        .ThenInclude(m => m.ItemsSubmuestras)
            .Include(m => m.Etapas)
                .ThenInclude(e => e.RegistrosProduccion)
                    .ThenInclude(rp => rp.Especie)
            .Include(m => m.Etapas)
                .ThenInclude(e => e.RegistrosProduccion)
                    .ThenInclude(rp => rp.Producto)
            .AsSplitQuery() // Evita el producto cartesiano masivo de múltiples colecciones
            .FirstOrDefaultAsync(m => m.ID == mareaId);

        if (marea == null) throw new InvalidOperationException("Marea no encontrada");

        // Obtener puntos de tracking por separado ya que no hay navegación en la entidad
        var trackingPoints = await dbContext.TrackingPoints
            .Where(t => t.MareaID == mareaId)
            .OrderBy(t => t.FechaHora)
            .ToListAsync();

        // Aplicar offset UTC-3 para consistencia con lances (que están en hora local)
        foreach (var p in trackingPoints)
        {
            p.FechaHora = p.FechaHora.AddHours(-3);
        }

        // Obtener catálogo de especies para validación de producción ordenado por Frecuente descendente
        var especiesDB = await dbContext.Especies
            .Where(e => e.CodigoInidep != null && (e.NombreVulgar != null || e.NombreCientifico != null))
            .OrderByDescending(e => e.Frecuente)
            .ToListAsync();

        var especiesDict = new Dictionary<string, string>();
        foreach (var esp in especiesDB)
        {
            string code = esp.CodigoInidep ?? esp.ID; // Usar código si existe, sino ID interno
            if (!string.IsNullOrEmpty(esp.NombreVulgar))
                especiesDict.TryAdd(esp.NombreVulgar.Trim().ToUpper(), code);
            if (!string.IsNullOrEmpty(esp.NombreCientifico))
                especiesDict.TryAdd(esp.NombreCientifico.Trim().ToUpper(), code);
        }

        // También para especies viejas ordenado por Frecuente descendente
        var especiesViejasDB = await dbContext.EspeciesViejas
            .Where(e => e.CodigoInidep != null)
            .OrderByDescending(e => e.Frecuente)
            .ToListAsync();

        var especiesViejasDict = new Dictionary<string, string>();
        foreach (var esp in especiesViejasDB)
        {
            string code = esp.CodigoInidep ?? esp.ID;
            if (!string.IsNullOrEmpty(esp.NombreVulgar))
                especiesViejasDict.TryAdd(esp.NombreVulgar.Trim().ToUpper(), code);
            if (!string.IsNullOrEmpty(esp.NombreCientifico))
                especiesViejasDict.TryAdd(esp.NombreCientifico.Trim().ToUpper(), code);
        }

        var especiesCodigosValidos = new HashSet<string>(especiesDict.Values);

        // Extraer rangos de fechas de las etapas
        var etapasFechas = marea.Etapas
            .Select(e => (Inicio: e.FechaZarpada, Fin: e.FechaArribo ?? e.FechaZarpada))
            .ToList();

        // Mapear a modelos Legacy para reutilizar el motor de validación
        var capturas = new List<LegacyCaptura>();
        var muestrasList = new List<LegacyMuestra>();
        var submuestrasList = new List<LegacySubmuestra>();
        var produccionList = new List<LegacyProduccion>();
        var trackingList = trackingPoints.Select(t => new LegacyTracking
        {
            Buque = marea.Buque?.Nombre ?? "",
            Matricula = t.Matricula ?? "",
            FechaStr = t.FechaHora.ToString("yyyy-MM-dd HH:mm:ss"), // Ya está en hora local en la base de datos
            Latitud = t.Latitud,
            Longitud = t.Longitud,
            Velocidad = t.Velocidad,
            Rumbo = t.Rumbo
        }).ToList();

        foreach (var etapa in marea.Etapas)
        {
            foreach (var lance in etapa.Lances)
            {
                var cap = new LegacyCaptura
                {
                    Barco = marea.Buque?.Nombre ?? "",
                    Marea = marea.NumeroInidep,
                    Lance = lance.NroLance,
                    Fecha = DateTime.Parse(lance.Fecha),
                    FechaFin = lance.FechaFin,
                    HoraInic = EncodeTime(lance.HoraInicio),
                    HoraFinal = EncodeTime(lance.HoraFinal),
                    LatInic = EncodeCoordinate(lance.LatitudInicioDecimal),
                    LongInic = EncodeCoordinate(lance.LongitudInicioDecimal),
                    LatFinal = EncodeCoordinate(lance.LatitudFinalDecimal),
                    LongFinal = EncodeCoordinate(lance.LongitudFinalDecimal),
                    ProfInic = lance.ProfundidadInicioM ?? 0,
                    ProfFinal = lance.ProfundidadFinalM ?? 0,
                    CaptTotal = lance.CapturaTotalKg ?? 0,
                    Descarte = lance.DescarteTotalKg ?? 0
                };

                foreach (var ic in lance.ItemsCaptura)
                {
                    if (ic.Especie != null)
                    {
                        string cod = ic.Especie.CodigoInidep ?? ic.Especie.ID;
                        if (!cap.Especies.ContainsKey(cod)) cap.Especies[cod] = 0;
                        if (!cap.DescartesPorEspecie.ContainsKey(cod)) cap.DescartesPorEspecie[cod] = 0;

                        cap.Especies[cod] += ic.CapturaTotalKgCalculado;
                        cap.DescartesPorEspecie[cod] += ic.PesoDescarteCalculado;
                    }
                }
                capturas.Add(cap);

                foreach (var m in lance.Muestras)
                {
                    var lm = new LegacyMuestra
                    {
                        Barco = marea.Buque?.Nombre ?? "",
                        Marea = marea.NumeroInidep,
                        Lance = lance.NroLance,
                        Fecha = DateTime.Parse(lance.Fecha),
                        Especie = m.Especie?.NombreCientifico ?? "",
                        CodEspec = m.Especie?.CodigoInidep ?? m.Especie?.ID ?? "",
                        Intervalo = (int)m.Intervalo,
                        PesoMues = Math.Round((m.PesoMuestra_PesoGramos ?? 0) / 1000.0, 2),
                        TipoMuestra = m.TipoMuestra,
                        Area = _validator.CalculateArea(lance.LatitudInicioDecimal ?? 0, lance.LongitudInicioDecimal ?? 0)
                    };

                    if (m.FrecuenciasTallas.Any())
                    {
                        lm.PrimTalla = (int)m.FrecuenciasTallas.Min(f => f.Talla);
                        lm.UltTalla = (int)m.FrecuenciasTallas.Max(f => f.Talla);
                        foreach (var f in m.FrecuenciasTallas)
                        {
                            lm.Tallies.Add(new DecodedTally((int)f.Talla, f.NroMachos, f.NroHembras, f.NroIndeterminados, f.NroMachos + f.NroHembras + f.NroIndeterminados));
                        }
                    }
                    muestrasList.Add(lm);

                    foreach (var s in m.ItemsSubmuestras)
                    {
                        submuestrasList.Add(new LegacySubmuestra
                        {
                            Barco = marea.Buque?.Nombre ?? "",
                            Marea = marea.NumeroInidep,
                            Lance = lance.NroLance,
                            Fecha = DateTime.Parse(lance.Fecha),
                            Especie = m.Especie?.NombreCientifico ?? "",
                            NEjemplar = s.NroEjemplar,
                            LargoTot = s.LargoTotalMm ?? 0,
                            LargoSta = s.LargoEstandarMm ?? 0,
                            PesoTot = (s.PesoTotalGramos ?? 0) / 1000.0,
                            Sexo = s.Sexo ?? 0,
                            Estadio = s.Estadio ?? 0
                        });
                    }
                }
            }

            foreach (var rp in etapa.RegistrosProduccion)
            {
                produccionList.Add(new LegacyProduccion
                {
                    Barco = marea.Buque?.Nombre ?? "",
                    Marea = marea.NumeroInidep,
                    Fecha = DateTime.Parse(rp.Fecha),
                    Especie = rp.Especie?.NombreVulgar ?? "",
                    Producto = rp.Producto?.Codigo ?? "",
                    Categoria = rp.Categoria ?? "",
                    Operarios = rp.Operarios ?? 0,
                    Factor = rp.Factor ?? 0,
                    Kilos = rp.Kg ?? 0
                });
            }
        }

        // Obtener catálogo Largo-Peso para cálculos automáticos de peso de muestra
        var largoPesoDB = await dbContext.EspeciesLargoPeso
            .Include(lp => lp.Especie)
            .ToListAsync();
            
        var largoPesoCatalogo = largoPesoDB
            .Where(lp => lp.Especie?.CodigoInidep != null)
            .ToDictionary(
                lp => (EspecieId: lp.Especie?.CodigoInidep ?? lp.Especie?.ID ?? "", Sexo: lp.Sexo),
                lp => (A: lp.ParamA, B: lp.ParamB)
            );

        var meta = MareaMetadataHelper.GetMetadata(marea);
        var report = _validator.ValidateMarea(
            marea.Buque?.Nombre ?? "",
            marea.AnioInidep,
            marea.NumeroInidep,
            meta.BuqueCodigo,
            meta.ObservadorNombre,
            meta.ObservadorApellido,
            meta.ObservadorCodigo,
            etapasFechas,
            capturas,
            muestrasList,
            submuestrasList,
            new List<LegacyLg>(),
            trackingList,
            produccionList,
            especiesDict,
            especiesViejasDict,
            especiesCodigosValidos,
            largoPesoCatalogo,
            skipConsensusHeuristic: true
        );

        // PERSISTIR CORRECCIONES: Si el motor corrigió totales, los guardamos en la DB
        bool hayCambios = false;
        foreach (var etapa in marea.Etapas)
        {
            foreach (var lance in etapa.Lances)
            {
                var legacyCaptura = capturas.FirstOrDefault(c => (int)c.Lance == lance.NroLance);
                if (legacyCaptura != null)
                {
                    // Si el valor en el objeto legacy (corregido) difiere del de la DB, actualizamos
                    if (Math.Abs((lance.CapturaTotalKg ?? 0) - legacyCaptura.CaptTotal) > 0.01)
                    {
                        lance.CapturaTotalKg = legacyCaptura.CaptTotal;
                        hayCambios = true;
                    }
                    if (Math.Abs((lance.DescarteTotalKg ?? 0) - legacyCaptura.Descarte) > 0.01)
                    {
                        lance.DescarteTotalKg = legacyCaptura.Descarte;
                        hayCambios = true;
                    }
                }

                foreach (var muestra in lance.Muestras)
                {
                    // Buscar la muestra legacy correspondiente (por especie)
                    var legacyMuestra = muestrasList.FirstOrDefault(m => 
                        (int)m.Lance == lance.NroLance && 
                        (m.CodEspec == (muestra.Especie?.CodigoInidep ?? muestra.Especie?.ID) || m.Especie == muestra.Especie?.NombreCientifico));

                    if (legacyMuestra != null)
                    {
                        double dbPesoGramos = muestra.PesoMuestra_PesoGramos ?? 0;
                        double legacyPesoGramos = legacyMuestra.PesoMues * 1000;

                        if (Math.Abs(dbPesoGramos - legacyPesoGramos) > 1.0) // Diferencia mayor a 1 gramo
                        {
                            muestra.PesoMuestra_PesoGramos = legacyPesoGramos;
                            
                            // Recalcular ejemplares por kg si tenemos el dato
                            int totalEjemplares = muestra.FrecuenciasTallas.Sum(f => f.NroTotal);
                            if (legacyMuestra.PesoMues > 0)
                            {
                                muestra.EjemplaresPorKg = (int)Math.Round(totalEjemplares / legacyMuestra.PesoMues);
                            }
                            
                            hayCambios = true;
                        }
                    }
                }
            }
        }

        if (hayCambios)
        {
            await dbContext.SaveChangesAsync();
        }

        report.ArchivosProcesados = new List<string> { "Datos de Base de Datos (Auditados y Corregidos)" };
        return report;
    }

    private double EncodeCoordinate(double? decimalDegrees)
    {
        if (!decimalDegrees.HasValue) return 0;
        double absVal = Math.Abs(decimalDegrees.Value);
        int degrees = (int)Math.Truncate(absVal);
        double minutesPart = (absVal - degrees) * 60;
        return degrees + (minutesPart / 100.0);
    }

    private double EncodeTime(string? timeStr)
    {
        if (string.IsNullOrEmpty(timeStr)) return 0;
        if (TimeSpan.TryParse(timeStr, out var ts))
        {
            return ts.Hours + (ts.Minutes / 100.0);
        }
        return 0;
    }
}
