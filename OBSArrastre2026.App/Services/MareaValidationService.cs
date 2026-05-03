using Microsoft.EntityFrameworkCore;
using OBSArrastre2026.App.Data;
using OBSArrastre2026.App.Data.Entities;
using OBSArrastre2026.App.Models.Import;
using OBSArrastre2026.App.Services.Internal;
using System.Text;

namespace OBSArrastre2026.App.Services;

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

        // Obtener catálogo de especies para validación de producción
        var especiesDB = await dbContext.Especies
            .Where(e => e.CodigoInidep != null && (e.NombreVulgar != null || e.NombreCientifico != null))
            .ToListAsync();

        var especiesDict = new Dictionary<string, long>();
        foreach (var esp in especiesDB)
        {
            if (long.TryParse(esp.CodigoInidep, out long code))
            {
                if (!string.IsNullOrEmpty(esp.NombreVulgar))
                    especiesDict[esp.NombreVulgar.Trim().ToUpper()] = code;
                if (!string.IsNullOrEmpty(esp.NombreCientifico))
                    especiesDict[esp.NombreCientifico.Trim().ToUpper()] = code;
            }
        }

        var especiesViejasDB = await dbContext.EspeciesViejas
            .Where(e => e.CodigoInidep != null)
            .ToListAsync();

        var especiesViejasDict = new Dictionary<string, long>();
        foreach (var esp in especiesViejasDB)
        {
            if (long.TryParse(esp.CodigoInidep, out long code))
            {
                if (!string.IsNullOrEmpty(esp.NombreVulgar))
                    especiesViejasDict[esp.NombreVulgar.Trim().ToUpper()] = code;
                if (!string.IsNullOrEmpty(esp.NombreCientifico))
                    especiesViejasDict[esp.NombreCientifico.Trim().ToUpper()] = code;
            }
        }

        var especiesCodigosValidos = new HashSet<long>(especiesDict.Values);

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
            FechaStr = t.FechaHora.AddHours(3).ToString("yyyy-MM-dd HH:mm:ss"), // Volver a UTC para que el motor reste 3
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
                    if (long.TryParse(ic.Especie?.CodigoInidep, out long cod))
                    {
                        cap.Especies[cod] = ic.DatoCaptura;
                        cap.DescartesPorEspecie[cod] = ic.DatoDescarte;
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
                        CodEspec = long.TryParse(m.Especie?.CodigoInidep, out long c) ? c : 0,
                        Intervalo = (int)m.Intervalo,
                        PesoMues = Math.Round((m.PesoMuestra_PesoGramos ?? 0) / 1000.0, 2),
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
                lp => {
                    string rawId = lp.Especie!.CodigoInidep!.Trim();
                    return (EspecieId: long.TryParse(rawId, out long n) ? n.ToString() : rawId, Sexo: lp.Sexo);
                },
                lp => (A: lp.ParamA, B: lp.ParamB)
            );

        var report = _validator.ValidateMarea(
            marea.Buque?.Nombre ?? "",
            marea.AnioInidep,
            marea.NumeroInidep,
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
            largoPesoCatalogo
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
