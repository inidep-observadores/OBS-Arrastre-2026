using OBSArrastre2026.App.Models.Import;
using OBSArrastre2026.App.Services.Internal;
using OBSArrastre2026.App.Data;
using OBSArrastre2026.App.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text;

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

    private string? ResolveFilePath(string basePath, char prefix, int marea, int anio)
    {
        if (!Directory.Exists(basePath)) return null;

        string yearSuffix = $"{(anio % 100):D2}.DBF";
        string mareaStr = marea.ToString();

        // Buscar todos los archivos que empiecen con el prefijo y terminen con el año
        var candidateFiles = Directory.GetFiles(basePath, $"{prefix}*.DBF");

        foreach (var path in candidateFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(path).ToUpper();
            if (fileName.Length < 3) continue; // Mínimo "X" + Marea + "YY"

            // El nombre debe empezar con el prefijo
            if (fileName[0] != char.ToUpper(prefix)) continue;

            // El nombre debe terminar con el año (2 dígitos)
            if (!fileName.EndsWith(yearSuffix.Replace(".DBF", ""))) continue;

            // La parte central debe coincidir numéricamente con la marea
            string middlePart = fileName.Substring(1, fileName.Length - 3);
            if (int.TryParse(middlePart, out int foundMarea) && foundMarea == marea)
            {
                return path;
            }
        }

        return null;
    }

    public async Task<MareaValidationReport> ProcessMareaImportAsync(string basePath, string barco, int marea, int anio, IEnumerable<MareaEtapa> etapas)
    {
        // 1. Resolver rutas de forma flexible (Marea 3 Año 2026 -> S326, S0326, S00326, etc.)
        string? cPath = ResolveFilePath(basePath, 'C', marea, anio);
        string? mPath = ResolveFilePath(basePath, 'M', marea, anio);
        string? xPath = ResolveFilePath(basePath, 'X', marea, anio);
        string? sPath = ResolveFilePath(basePath, 'S', marea, anio);
        string? lPath = ResolveFilePath(basePath, 'L', marea, anio);
        string? tPath = ResolveFilePath(basePath, 'T', marea, anio);
        string? pPath = ResolveFilePath(basePath, 'P', marea, anio);

        var report = new MareaValidationReport();

        // Verificar archivos obligatorios (usando nombres amigables para el reporte si no se encuentran)
        string suffix = $"{marea:D2}{anio % 100:D2}.DBF"; // Nombre sugerido para el error
        if (cPath == null) report.AddIssue(ValidationLevel.Error, "Sistema", $"El archivo de CAPTURA obligatorio no se encuentra (esperado C*{suffix})");
        if (mPath == null) report.AddIssue(ValidationLevel.Error, "Sistema", $"El archivo de MUESTRA obligatorio no se encuentra (esperado M*{suffix})");
        if (sPath == null) report.AddIssue(ValidationLevel.Warning, "Sistema", $"El archivo de SUBMUESTRA no se encuentra (esperado S*{suffix})");
        if (pPath == null) report.AddIssue(ValidationLevel.Error, "Sistema", $"El archivo de PRODUCCIÓN obligatorio no se encuentra (esperado P*{suffix})");

        // 2. Extraer datos (si los paths fueron resueltos)
        var capturas = cPath != null ? await _extractor.ReadCapturasAsync(cPath) ?? new() : new();
        var muestras = mPath != null ? await _extractor.ReadMuestrasAsync(mPath) ?? new() : new();
        var submuestras = sPath != null ? await _extractor.ReadSubmuestrasAsync(sPath) ?? new() : new();
        var lgs = lPath != null ? await _extractor.ReadLgAsync(lPath) ?? new() : new();
        var produccion = pPath != null ? await _extractor.ReadProduccionAsync(pPath) ?? new() : new();
        
        // Registro de archivos encontrados para la UI
        var archivosEncontrados = new List<string>();
        if (cPath != null) archivosEncontrados.Add(Path.GetFileName(cPath));
        if (mPath != null) archivosEncontrados.Add(Path.GetFileName(mPath));
        if (sPath != null) archivosEncontrados.Add(Path.GetFileName(sPath));
        if (lPath != null) archivosEncontrados.Add(Path.GetFileName(lPath));
        if (pPath != null) archivosEncontrados.Add(Path.GetFileName(pPath));

        // 3. Lógica de fusión X*
        if (File.Exists(xPath))
        {
            archivosEncontrados.Add(Path.GetFileName(xPath));
            var extensiones = await _extractor.ReadMuestrasAsync(xPath) ?? new();
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
                if (ext != null)
                {
                    muestras.Add(ext);
                }
            }
        }

        // 3.b Carga de Tracking (Opcional)
        var tracking = new List<LegacyTracking>();
        if (File.Exists(tPath))
        {
            archivosEncontrados.Add(Path.GetFileName(tPath));
            tracking = await _extractor.ReadTrackingAsync(tPath) ?? new();
        }

        // 4. Validar (se llamará de nuevo tras cargar el catálogo en el paso 5)
        report.ArchivosProcesados = archivosEncontrados;

        // 4b. Validar asignación a etapas y existencia de especies
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var especiesExistentes = await dbContext.Especies
            .Select(e => e.CodigoInidep)
            .Where(c => c != null)
            .ToListAsync();
        var setEspeciesExistentes = new HashSet<string>(especiesExistentes!);

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

        // 5. Validar y Guardar datos en el reporte para el paso de commit
        var etapasFechas = etapas.Select(e => (Inicio: e.FechaZarpada, Fin: e.FechaArribo ?? e.FechaZarpada)).ToList();

        var largoPesoDB = await dbContext.EspeciesLargoPeso
            .Include(lp => lp.Especie)
            .ToListAsync();

        var largoPesoCatalogo = largoPesoDB
            .Where(lp => lp.Especie?.CodigoInidep != null)
            .ToDictionary(
                lp => (EspecieId: lp.Especie!.CodigoInidep!, Sexo: lp.Sexo),
                lp => (A: lp.ParamA, B: lp.ParamB)
            );

        report = _validator.ValidateMarea(barco, anio, marea, etapasFechas, capturas, muestras, submuestras, lgs, tracking, produccion, especiesDict, largoPesoCatalogo);
        report.ArchivosProcesados = archivosEncontrados;

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

        // Cargar catálogo de especies con múltiples índices para resolución flexible
        var especiesCatalogo = await dbContext.Especies.ToListAsync();
        
        var especieByCodigoMap = especiesCatalogo
            .Where(e => !string.IsNullOrEmpty(e.CodigoInidep))
            .GroupBy(e => e.CodigoInidep!)
            .ToDictionary(g => g.Key, g => g.First().ID);

        var especieByCientificoMap = especiesCatalogo
            .Where(e => !string.IsNullOrEmpty(e.NombreCientifico))
            .GroupBy(e => e.NombreCientifico!.Trim().ToUpper().Normalize(NormalizationForm.FormC))
            .ToDictionary(g => g.Key, g => g.First().ID);

        // Map Capturas -> Lances
        var lanceMap = new Dictionary<double, Lance>();
        foreach (var c in report.Capturas)
        {
            var lanceTime = GetLanceDateTime(c);
            var etapa = marea.Etapas.FirstOrDefault(e => lanceTime >= e.FechaZarpada && lanceTime <= (e.FechaArribo ?? DateTime.MaxValue));
            
            if (etapa == null) continue;

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
            };

            // Items de Captura (Especies por código)
            foreach (var kvp in c.Especies)
            {
                if (kvp.Value > 0 && especieByCodigoMap.TryGetValue(kvp.Key.ToString(), out var especieId))
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
            if (lanceMap.TryGetValue(rm.Lance, out var lance))
            {
                // Resolución de especie: Priorizar código, luego nombre científico
                string? especieId = null;
                if (rm.CodEspec > 0) especieByCodigoMap.TryGetValue(rm.CodEspec.ToString(), out especieId);
                
                if (especieId == null && !string.IsNullOrEmpty(rm.Especie))
                {
                    var normName = rm.Especie.Trim().ToUpper().Normalize(NormalizationForm.FormC);
                    especieByCientificoMap.TryGetValue(normName, out especieId);
                }

                if (especieId != null)
                {
                    var muestra = new Muestra
                    {
                        Lance = lance,
                        EspecieID = especieId,
                        PesoMuestra_PesoGramos = rm.PesoMues * 1000,
                        Intervalo = rm.Intervalo,
                    };

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
                    
                    // La clave de vinculación con submuestras DEBE ser por Nombre Científico normalizado
                    // ya que es el único campo confiable en ambos archivos (M y S) según el usuario.
                    string speciesKey = rm.Especie.Trim().ToUpper().Normalize(NormalizationForm.FormC);
                    string key = $"{rm.Lance}_{speciesKey}";
                    muestraMap[key] = muestra;
                }
            }
        }

        // Map Submuestras
        foreach (var rs in report.Submuestras)
        {
            string speciesKey = rs.Especie.Trim().ToUpper().Normalize(NormalizationForm.FormC);
            if (muestraMap.TryGetValue($"{rs.Lance}_{speciesKey}", out var muestra))
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

        // Map Tracking (Conversión UTC -> UTC-3)
        if (report.Tracking.Any())
        {
            foreach (var rt in report.Tracking)
            {
                var utcDate = rt.GetUtcDateTime();
                if (utcDate == DateTime.MinValue) continue;

                dbContext.TrackingPoints.Add(new MareaTracking
                {
                    MareaID = mareaId,
                    FechaHora = utcDate.AddHours(-3), // Conversión solicitada
                    Latitud = rt.Latitud,
                    Longitud = rt.Longitud,
                    Velocidad = rt.Velocidad,
                    Rumbo = rt.Rumbo,
                    Matricula = rt.Matricula
                });
            }
            await dbContext.SaveChangesAsync();
        }

        // Map Producción (Creación dinámica de productos)
        if (report.Produccion.Any())
        {
            var existingProducts = await dbContext.Productos.ToDictionaryAsync(p => p.Codigo, p => p.Id);
            var existenteEspecies = await dbContext.Especies
                .Select(e => new { e.ID, e.NombreVulgar })
                .Where(e => e.NombreVulgar != null)
                .ToListAsync();

            var especieNameMap = existenteEspecies
                .GroupBy(e => e.NombreVulgar!.Trim().ToUpper().Normalize(NormalizationForm.FormC))
                .ToDictionary(
                    g => g.Key, 
                    g => g.First().ID);

            foreach (var rp in report.Produccion)
            {
                if (!existingProducts.TryGetValue(rp.Producto, out var productGuid))
                {
                    var newProduct = new Producto
                    {
                        Codigo = rp.Producto?.Trim() ?? string.Empty,
                        Descripcion = $"{rp.Especie?.Trim()} - {rp.Producto?.Trim()}".Trim(' ', '-'),
                        Orden = 99 // Al final
                    };
                    dbContext.Productos.Add(newProduct);
                    await dbContext.SaveChangesAsync(); 
                    productGuid = newProduct.Id;
                    existingProducts[rp.Producto] = productGuid;
                }

                // Encontrar etapa de marea por fecha
                var regDate = rp.Fecha.Date;
                var etapa = marea.Etapas.FirstOrDefault(e => 
                    regDate >= e.FechaZarpada.Date && 
                    regDate <= (e.FechaArribo?.Date ?? DateTime.MaxValue));

                if (etapa != null)
                {
                    string? speciesId = null;
                    if (!string.IsNullOrEmpty(rp.Especie))
                    {
                        var searchName = rp.Especie.Trim().ToUpper().Normalize(NormalizationForm.FormC);
                        especieNameMap.TryGetValue(searchName, out speciesId);
                    }

                    dbContext.RegistrosProduccion.Add(new RegistroProduccion
                    {
                        MareaEtapaId = etapa.ID,
                        Fecha = rp.Fecha.ToString("yyyy-MM-dd"),
                        IdProducto = productGuid,
                        Categoria = rp.Categoria,
                        EspecieId = speciesId,
                        Factor = rp.Factor,
                        Operarios = rp.Operarios,
                        Kg = rp.Kilos,
                        Comentarios = $"Importado de legacy"
                    });
                }
            }
            await dbContext.SaveChangesAsync();
        }
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

        bool hasTracking = await dbContext.TrackingPoints.AnyAsync(t => t.MareaID == mareaId);

        return hasLances || hasProduccion || hasTracking;
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

        // Eliminar tracking
        var tracking = await dbContext.TrackingPoints
            .Where(t => t.MareaID == mareaId)
            .ToListAsync();
        dbContext.TrackingPoints.RemoveRange(tracking);

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
