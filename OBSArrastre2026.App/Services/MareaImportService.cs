using OBSArrastre2026.App.Models.Import;
using OBSArrastre2026.App.Services.Internal;
using OBSArrastre2026.App.Data;
using OBSArrastre2026.App.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace OBSArrastre2026.App.Services;

public interface IMareaImportService
{
    Task<MareaValidationReport> ProcessMareaImportAsync(string basePath, string barco, int marea, int anio, IEnumerable<MareaEtapa> etapas);
    Task ImportAsync(string mareaId, MareaValidationReport report);
    Task<bool> HasDataAsync(string mareaId);
    Task ClearMareaDataAsync(string mareaId);
}

public class MareaImportService : IMareaImportService
{
    private readonly IDbfExtractorService _extractor;
    private readonly MareaValidationEngine _validator;
    private readonly IMareaReportService _reporter;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public MareaImportService(
        IDbfExtractorService extractor, 
        IMareaReportService reporter,
        IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _extractor = extractor;
        _validator = new MareaValidationEngine();
        _reporter = reporter;
        _dbContextFactory = dbContextFactory;
    }

    public async Task<MareaValidationReport> ProcessMareaImportAsync(string basePath, string barco, int marea, int anio, IEnumerable<MareaEtapa> etapas)
    {
        // 1. Determinar nombres de archivos
        string suffix = $"{marea}{anio % 100:D2}.DBF";
        string cPath = Path.Combine(basePath, $"C{suffix}");
        string mPath = Path.Combine(basePath, $"M{suffix}");
        string xPath = Path.Combine(basePath, $"X{suffix}");
        string sPath = Path.Combine(basePath, $"S{suffix}");
        string lPath = Path.Combine(basePath, $"L{suffix}");

        // 2. Extraer datos
        var capturas = await _extractor.ReadCapturasAsync(cPath);
        var muestras = await _extractor.ReadMuestrasAsync(mPath);
        var submuestras = await _extractor.ReadSubmuestrasAsync(sPath);
        var lgs = await _extractor.ReadLgAsync(lPath);

        // 3. Lógica de fusión X*
        if (File.Exists(xPath))
        {
            var extensiones = await _extractor.ReadMuestrasAsync(xPath);
            foreach (var ext in extensiones)
            {
                var baseM = muestras.FirstOrDefault(m => 
                    (int)m.Lance == (int)ext.Lance && 
                    m.CodEspec == ext.CodEspec);
                
                if (baseM != null)
                {
                    LegacyDecoder.MergeExtendedMuestras(baseM, ext);
                }
                else
                {
                    muestras.Add(ext);
                }
            }
        }

        // 4. Validar
        var report = _validator.ValidateMarea(barco, anio, marea, capturas, muestras, submuestras);
        
        // 4b. Validar asignación a etapas y existencia de especies
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var especiesExistentes = await dbContext.Especies
            .Select(e => e.CodigoInidep)
            .Where(c => c != null)
            .ToListAsync();
        var setEspeciesExistentes = new HashSet<string>(especiesExistentes!);

        foreach (var c in capturas)
        {
            var lanceTime = GetLanceDateTime(c);
            var etapa = etapas.FirstOrDefault(e => lanceTime >= e.FechaZarpada && lanceTime <= (e.FechaArribo ?? DateTime.MaxValue));
            if (etapa == null)
            {
                report.AddIssue(ValidationLevel.Fatal, "Etapa", $"El lance {c.Lance} ({lanceTime:g}) no cae dentro de ninguna etapa definida de la marea.", $"Lance {c.Lance}");
            }

            foreach (var spCode in c.Especies.Keys)
            {
                if (!setEspeciesExistentes.Contains(spCode.ToString()))
                {
                    report.AddIssue(ValidationLevel.Fatal, "Catálogo Especies", $"La especie legado con código '{spCode}' no existe en la base de datos local.", $"Captura Lance {c.Lance}");
                }
            }
        }

        foreach (var m in muestras)
        {
            if (!setEspeciesExistentes.Contains(m.CodEspec.ToString()))
            {
                report.AddIssue(ValidationLevel.Fatal, "Catálogo Especies", $"La especie legado con código '{m.CodEspec}' no existe en la base de datos local.", $"Muestra Lance {m.Lance}");
            }
        }

        // 5. Guardar datos en el reporte para el paso de commit
        report.Capturas = capturas;
        report.Muestras = muestras;
        report.Submuestras = submuestras;
        report.Lgs = lgs;

        // 6. Generar Reporte PDF
        var pdfBytes = _reporter.GenerateValidationPdf(report);
        string reportPath = Path.Combine(basePath, "Reports", $"Audit_{barco}_{marea}_{anio}.pdf");
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        await File.WriteAllBytesAsync(reportPath, pdfBytes);

        return report;
    }

    public async Task ImportAsync(string mareaId, MareaValidationReport report)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        
        var marea = await dbContext.Mareas
            .Include(m => m.Etapas)
            .FirstOrDefaultAsync(m => m.ID == mareaId);

        if (marea == null) throw new InvalidOperationException("Marea no encontrada");

        // Cargar catálogo de especies para resolución de IDs (GUIDs)
        var especieMap = await dbContext.Especies
            .Where(e => e.CodigoInidep != null)
            .ToDictionaryAsync(e => e.CodigoInidep!, e => e.ID);

        // Map Capturas -> Lances
        var lanceMap = new Dictionary<double, Lance>();
        foreach (var c in report.Capturas)
        {
            var lanceTime = GetLanceDateTime(c);
            var etapa = marea.Etapas.FirstOrDefault(e => lanceTime >= e.FechaZarpada && lanceTime <= (e.FechaArribo ?? DateTime.MaxValue));
            
            if (etapa == null) continue; // No debería pasar tras validación

            var lance = new Lance
            {
                MareaEtapaId = etapa.ID,
                NroLance = (int)c.Lance,
                Fecha = c.Fecha.ToString("yyyy-MM-dd"),
                HoraInicio = FormatTime(c.HoraInic),
                HoraFinal = FormatTime(c.HoraFinal),
                LatitudInicioDecimal = LegacyDecoder.DecodeCoordinate(c.LatInic),
                LongitudInicioDecimal = LegacyDecoder.DecodeCoordinate(c.LongInic),
                LatitudFinalDecimal = LegacyDecoder.DecodeCoordinate(c.LatFinal),
                LongitudFinalDecimal = LegacyDecoder.DecodeCoordinate(c.LongFinal),
                ProfundidadInicioM = (int)c.ProfInic,
                ProfundidadFinalM = (int)c.ProfFinal,
                CapturaTotalKg = c.CaptTotal,
                // Otros mapeos...
            };

            // Items de Captura (Especies)
            foreach (var kvp in c.Especies)
            {
                if (kvp.Value > 0 && especieMap.TryGetValue(kvp.Key.ToString(), out var especieId))
                {
                    lance.ItemsCaptura.Add(new ItemCaptura
                    {
                        EspecieID = especieId,
                        DatoCaptura = kvp.Value,
                        DatoDescarte = c.DescartesPorEspecie.TryGetValue(kvp.Key, out var d) ? d : 0
                    });
                }
            }

            dbContext.Lances.Add(lance);
            lanceMap[c.Lance] = lance;
        }

        // Map Muestras
        var muestraMap = new Dictionary<string, Muestra>();
        foreach (var rm in report.Muestras)
        {
            if (lanceMap.TryGetValue(rm.Lance, out var lance) && 
                especieMap.TryGetValue(rm.CodEspec.ToString(), out var especieId))
            {
                var muestra = new Muestra
                {
                    Lance = lance,
                    EspecieID = especieId,
                    PesoMuestra_PesoGramos = rm.PesoMues * 1000,
                    Intervalo = rm.Intervalo,
                    // Otros mapeos...
                };

                // Tallas
                foreach (var tally in rm.Tallies)
                {
                    muestra.FrecuenciasTallas.Add(new FrecuenciaTalla
                    {
                        Talla = tally.Size,
                        NroMachos = tally.Males,
                        NroHembras = tally.Females,
                        NroIndeterminados = tally.Indeterminate
                    });
                }

                dbContext.Muestras.Add(muestra);
                string key = $"{rm.Lance}_{rm.CodEspec}";
                muestraMap[key] = muestra;
            }
        }

        // Map Submuestras
        foreach (var rs in report.Submuestras)
        {
            // Intentar encontrar la muestra correspondiente por Lance + Especie
            // rs.Especie suele contener el código en formato string en estos DBF
            if (muestraMap.TryGetValue($"{rs.Lance}_{rs.Especie}", out var muestra))
            {
                muestra.ItemsSubmuestras.Add(new ItemSubmuestra
                {
                    NroEjemplar = rs.NEjemplar,
                    Sexo = rs.Sexo,
                    Estadio = rs.Estadio,
                    PesoTotalGramos = rs.PesoTot * 1000,
                    LargoTotalMm = rs.LargoTot,
                    LargoEstandarMm = rs.LargoSta
                });
            }
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task<bool> HasDataAsync(string mareaId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        
        // Verificamos si hay lances o producciones en cualquiera de las etapas de la marea
        bool hasLances = await dbContext.Lances
            .AnyAsync(l => dbContext.MareaEtapas
                .Where(e => e.MareaID == mareaId)
                .Select(e => e.ID)
                .Contains(l.MareaEtapaId));

        bool hasProduccion = await dbContext.RegistrosProduccion
            .AnyAsync(p => dbContext.MareaEtapas
                .Where(e => e.MareaID == mareaId)
                .Select(e => e.ID)
                .Contains(p.MareaEtapaId));

        return hasLances || hasProduccion;
    }

    public async Task ClearMareaDataAsync(string mareaId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        
        var etapaIds = await dbContext.MareaEtapas
            .Where(e => e.MareaID == mareaId)
            .Select(e => e.ID)
            .ToListAsync();

        // Eliminar lances (Cascada se encarga de muestras, frecuencias y subs)
        var lances = await dbContext.Lances
            .Where(l => etapaIds.Contains(l.MareaEtapaId))
            .ToListAsync();
        dbContext.Lances.RemoveRange(lances);

        // Eliminar producción
        var prod = await dbContext.RegistrosProduccion
            .Where(p => etapaIds.Contains(p.MareaEtapaId))
            .ToListAsync();
        dbContext.RegistrosProduccion.RemoveRange(prod);

        await dbContext.SaveChangesAsync();
    }

    private DateTime GetLanceDateTime(LegacyCaptura c)
    {
        var ts = ParseLegacyTime(c.HoraInic);
        return c.Fecha.Date.Add(ts);
    }

    private TimeSpan ParseLegacyTime(double time)
    {
        return LegacyDecoder.DecodeTime(time);
    }

    private string FormatTime(double time)
    {
        var ts = ParseLegacyTime(time);
        return ts.ToString(@"hh\:mm");
    }
}
