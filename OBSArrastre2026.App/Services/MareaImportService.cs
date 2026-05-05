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

    private string? ResolveFilePath(string basePath, string prefix, int marea, int anio)
    {
        if (!Directory.Exists(basePath)) return null;

        string yearSuffix = $"{(anio % 100):D2}.DBF";
        string mareaStr = marea.ToString();

        // Buscar todos los archivos que empiecen con el prefijo y terminen con el año
        var candidateFiles = Directory.GetFiles(basePath, $"{prefix}*.DBF");

        foreach (var path in candidateFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(path).ToUpper();
            if (fileName.Length < prefix.Length + 2) continue; 

            // El nombre debe empezar con el prefijo
            if (!fileName.StartsWith(prefix.ToUpper())) continue;

            // El nombre debe terminar con el año (2 dígitos)
            if (!fileName.EndsWith(yearSuffix.Replace(".DBF", ""))) continue;

            // La parte central debe coincidir numéricamente con la marea
            string middlePart = fileName.Substring(prefix.Length, fileName.Length - (prefix.Length + 2));
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
        string? cPath = ResolveFilePath(basePath, "C", marea, anio);
        string? mPath = ResolveFilePath(basePath, "M", marea, anio);
        string? mdPath = ResolveFilePath(basePath, "MD", marea, anio); // Tallas de descarte
        string? xPath = ResolveFilePath(basePath, "X", marea, anio);
        string? sPath = ResolveFilePath(basePath, "S", marea, anio);
        string? lPath = ResolveFilePath(basePath, "L", marea, anio);
        string? tPath = ResolveFilePath(basePath, "T", marea, anio);
        string? pPath = ResolveFilePath(basePath, "P", marea, anio);

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

        if (mdPath != null)
        {
            var muestrasDescarte = await _extractor.ReadMuestrasAsync(mdPath) ?? new();
            foreach (var md in muestrasDescarte)
            {
                md.TipoMuestra = 2; // Descarte
                muestras.Add(md);
            }
        }
        
        // Registro de archivos encontrados para la UI
        var archivosEncontrados = new List<string>();
        if (cPath != null) archivosEncontrados.Add(Path.GetFileName(cPath));
        if (mPath != null) archivosEncontrados.Add(Path.GetFileName(mPath));
        if (mdPath != null) archivosEncontrados.Add(Path.GetFileName(mdPath));
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
        var setEspeciesExistentes = new HashSet<string>(especiesExistentes.Select(c => c!.Trim()));

        var especiesDB = await dbContext.Especies
            .Where(e => e.CodigoInidep != null && (e.NombreVulgar != null || e.NombreCientifico != null))
            .ToListAsync();

        var especiesDict = new Dictionary<string, string>();
        foreach (var esp in especiesDB)
        {
            var codeStr = NormalizeInidepCode(esp.CodigoInidep);
            if (!string.IsNullOrEmpty(codeStr))
            {
                if (!string.IsNullOrEmpty(esp.NombreVulgar))
                    especiesDict[esp.NombreVulgar.Trim().ToUpper().Normalize(NormalizationForm.FormC)] = codeStr;
                if (!string.IsNullOrEmpty(esp.NombreCientifico))
                    especiesDict[esp.NombreCientifico.Trim().ToUpper().Normalize(NormalizationForm.FormC)] = codeStr;
            }
        }

        var especiesViejasDB = await dbContext.EspeciesViejas
            .Where(e => e.CodigoInidep != null)
            .ToListAsync();

        var especiesViejasDict = new Dictionary<string, string>();
        var setEspeciesViejasExistentes = new HashSet<string>();
        foreach (var esp in especiesViejasDB)
        {
            var codeStr = NormalizeInidepCode(esp.CodigoInidep);
            setEspeciesViejasExistentes.Add(codeStr);
            if (!string.IsNullOrEmpty(codeStr))
            {
                if (!string.IsNullOrEmpty(esp.NombreVulgar))
                    especiesViejasDict[esp.NombreVulgar.Trim().ToUpper().Normalize(NormalizationForm.FormC)] = codeStr;
                if (!string.IsNullOrEmpty(esp.NombreCientifico))
                    especiesViejasDict[esp.NombreCientifico.Trim().ToUpper().Normalize(NormalizationForm.FormC)] = codeStr;
            }
        }

        var especiesCodigosValidos = new HashSet<string>(especiesDict.Values);

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
                if (!setEspeciesExistentes.Contains(spCode) && !setEspeciesViejasExistentes.Contains(spCode))
                {
                    report.AddIssue(ValidationLevel.Fatal, "Catálogo Especies", $"La especie legado con código '{spCode}' no existe ni en el catálogo actual ni en el histórico.", $"Captura Lance {c.Lance}");
                }
            }
        }

        foreach (var m in muestras)
        {
            if (!setEspeciesExistentes.Contains(m.CodEspec) && !setEspeciesViejasExistentes.Contains(m.CodEspec))
            {
                report.AddIssue(ValidationLevel.Fatal, "Catálogo Especies", $"La especie legado con código '{m.CodEspec}' no existe ni en el catálogo actual ni en el histórico.", $"Muestra Lance {m.Lance}");
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

        report = _validator.ValidateMarea(barco, anio, marea, etapasFechas, capturas, muestras, submuestras, lgs, tracking, produccion, especiesDict, especiesViejasDict, especiesCodigosValidos, largoPesoCatalogo);
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
            .GroupBy(e => NormalizeInidepCode(e.CodigoInidep))
            .ToDictionary(g => g.Key, g => g.First().ID);

        var especieByNombreMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in especiesCatalogo)
        {
            if (!string.IsNullOrEmpty(e.NombreVulgar))
                especieByNombreMap[e.NombreVulgar.Trim().ToUpper().Normalize(NormalizationForm.FormC)] = e.ID;
            if (!string.IsNullOrEmpty(e.NombreCientifico))
                especieByNombreMap[e.NombreCientifico.Trim().ToUpper().Normalize(NormalizationForm.FormC)] = e.ID;
        }
 
        // LÓGICA DE PUENTE CON ESPECIES VIEJAS:

        // LÓGICA DE PUENTE CON ESPECIES VIEJAS:
        // Usamos la tabla especies_viejas como puente para encontrar el código que mapea a la tabla especies actual.
        var especiesViejasCatalogo = await dbContext.EspeciesViejas.ToListAsync();
        foreach (var ev in especiesViejasCatalogo)
        {
            var code = NormalizeInidepCode(ev.CodigoInidep);
            if (string.IsNullOrEmpty(code)) continue;

            // Si el código de la vieja existe en la nueva, mapeamos los nombres viejos al ID de la nueva
            if (especieByCodigoMap.TryGetValue(code, out var newId))
            {
                if (!string.IsNullOrEmpty(ev.NombreVulgar))
                    especieByNombreMap.TryAdd(ev.NombreVulgar.Trim().ToUpper().Normalize(NormalizationForm.FormC), newId);
                if (!string.IsNullOrEmpty(ev.NombreCientifico))
                    especieByNombreMap.TryAdd(ev.NombreCientifico.Trim().ToUpper().Normalize(NormalizationForm.FormC), newId);
            }
        }


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
                CapturaTotalKg = c.CaptTotal != 0 ? c.CaptTotal : (c.Especies.Values.Sum() > 0 ? c.Especies.Values.Sum() : 0),
                DescarteTotalKg = c.Descarte != 0 ? c.Descarte : (c.DescartesPorEspecie.Values.Sum() > 0 ? c.DescartesPorEspecie.Values.Sum() : 0),
                
                // Mapeo de campos adicionales
                EstadoTiempoCodigo = (int?)c.Tiempo,
                EstadoMarCodigo = (int?)c.Mar,
                VientoDireccionGrados = (int?)c.DirViento,
                VientoFuerzaBeaufort = (int?)c.VelViento,
                TemperaturaAireC = c.TmpASeco,
                TemperaturaRedC = c.TmpMarF,
                PresionHpa = (int?)c.PresionB,
                VelocidadArrastreNudos = c.VelArras,
                RumboGrados = (int?)c.Rumbo,
                MallaCopoMm = (int?)c.MallCopo,
                MallaAlasMm = (int?)c.MallAlas,
                CableFiladoM = (int?)c.CabFilad,
                AberturaVerticalM = c.AberVert,
                DistanciaAlasM = c.DistAlas,
                DistanciaPortonesM = c.DistEPor
            };

            // Items de Captura (Especies por código o puente)
            foreach (var kvp in c.Especies)
            {
                if (kvp.Value > 0 && especieByCodigoMap.TryGetValue(kvp.Key, out var especieId))
                {
                    lance.ItemsCaptura.Add(new ItemCaptura
                    {
                        EspecieID = especieId,
                        DatoCaptura = kvp.Value,
                        DatoDescarte = c.DescartesPorEspecie.TryGetValue(kvp.Key, out var d) ? d : 0,
                        TipoDatoDescarte = report.UnidadDescarte
                    });
                }
            }

            dbContext.Lances.Add(lance);
            lanceMap[c.Lance] = lance;
        }
        await dbContext.SaveChangesAsync();

        // Map Muestras
        var muestraMap = new Dictionary<string, Muestra>();
        var muestraByIdMap = new Dictionary<string, Muestra>();
        foreach (var rm in report.Muestras)
        {
            if (lanceMap.TryGetValue(rm.Lance, out var lance))
            {
                // Resolución de especie: Priorizar código, luego nombre (con puente incluido en especieByNombreMap)
                string? especieId = null;
                if (!string.IsNullOrEmpty(rm.CodEspec)) especieByCodigoMap.TryGetValue(rm.CodEspec.Trim(), out especieId);
                
                if (especieId == null && !string.IsNullOrEmpty(rm.Especie))
                {
                    var normName = rm.Especie.Trim().ToUpper().Normalize(NormalizationForm.FormC);
                    especieByNombreMap.TryGetValue(normName, out especieId);
                }

                if (especieId != null)
                {
                    var muestra = new Muestra
                    {
                        Lance = lance,
                        EspecieID = especieId,
                        PesoMuestra_PesoGramos = rm.PesoMues * 1000,
                        Intervalo = rm.Intervalo,
                        // Inferir Flags
                        UnidadMedidaTalla = 1, // CM por defecto en archivos M*
                        ModoMedicionTalla = 1, // LT por defecto
                        Origen = 1, // Muestreo de Captura
                        DiscriminaSexo = rm.Tallies.Any(t => t.Males > 0 || t.Females > 0) ? 1 : 0,
                        HayIndeterminados = rm.Tallies.Any(t => t.Indeterminate > 0) ? 1 : 0,
                        TipoMuestra = rm.TipoMuestra
                    };

                    int totalEjemplares = rm.Tallies.Sum(t => t.Total);
                    if (rm.PesoMues > 0)
                    {
                        muestra.EjemplaresPorKg = (int)Math.Round(totalEjemplares / rm.PesoMues);
                    }

                    foreach (var tally in rm.Tallies)
                    {
                        muestra.FrecuenciasTallas.Add(new FrecuenciaTalla
                        {
                            Talla = tally.Size,
                            NroMachos = tally.Males,
                            NroHembras = tally.Females,
                            NroIndeterminados = tally.Indeterminate,
                            NroTotal = tally.Total
                        });
                    }

                    dbContext.Muestras.Add(muestra);
                    
                    // La clave de vinculación con submuestras DEBE ser por Nombre Científico normalizado
                    // ya que es el único campo confiable en ambos archivos (M y S) según el usuario.
                    string speciesKey = rm.Especie.Trim().ToUpper().Normalize(NormalizationForm.FormC);
                    string key = $"{rm.Lance}_{speciesKey}";
                    muestraMap[key] = muestra;
                    
                    // Mapa adicional para búsqueda por ID de especie (usado por archivos L*)
                    muestraByIdMap[$"{rm.Lance}_{especieId}"] = muestra;
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

        // Map Lg (Langostinos - Madurez e Impregnación de archivos L*)
        if (report.Lgs.Any())
        {
            // El archivo "L" siempre se refiere a Langostino (Pleoticus muelleri)
            especieByCodigoMap.TryGetValue("5139030101", out var langostinoId);

            foreach (var rl in report.Lgs)
            {
                if (langostinoId != null && muestraByIdMap.TryGetValue($"{rl.Lance}_{langostinoId}", out var muestra))
                {
                    foreach (var kvp in rl.Frecuencias)
                    {
                        double talla = kvp.Key; // El índice final en TALLA_N indica la talla
                        var decoded = LegacyDecoder.DecodeMatureTally(kvp.Value);

                        // Buscar frecuencia existente
                        var frec = muestra.FrecuenciasTallas.FirstOrDefault(f => Math.Abs(f.Talla - talla) < 0.1);
                        if (frec != null)
                        {
                            frec.NroLangostinosMachoMaduros = decoded.MatureMales;
                            frec.NroLangostinosHembraMaduras = decoded.MatureFemales;
                            frec.NroLangostinosHembraImpregnadas = decoded.ImpregnatedFemales;
                        }
                    }
                }
            }
            await dbContext.SaveChangesAsync();
        }

        // Map Tracking (Conversión UTC -> UTC-3 realizada en la extracción)
        if (report.Tracking.Any())
        {
            foreach (var rt in report.Tracking)
            {
                var localDate = rt.GetDateTime();
                if (localDate == DateTime.MinValue) continue;

                dbContext.TrackingPoints.Add(new MareaTracking
                {
                    MareaID = mareaId,
                    FechaHora = localDate, 
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
            var existingProducts = await dbContext.Productos.ToDictionaryAsync(p => p.Codigo.Trim(), p => p.Id);
            
            foreach (var rp in report.Produccion)
            {
                if (string.IsNullOrEmpty(rp.Producto)) continue;
                
                if (!existingProducts.TryGetValue(rp.Producto.Trim(), out var productGuid))
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
                        
                        // REQ: Excepción Granadero solo para producción (comercial)
                        if (searchName == "GRANADERO")
                        {
                            especieByCodigoMap.TryGetValue("7210090401", out speciesId);
                        }
                        else if (!especieByNombreMap.TryGetValue(searchName, out speciesId))
                        {
                            // Fallback: Probar si el campo Especie trae el código INIDEP directamente (normalizado)
                            var searchCode = NormalizeInidepCode(rp.Especie);
                            especieByCodigoMap.TryGetValue(searchCode, out speciesId);
                        }
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
                        Comentarios = $"Importado: {rp.Especie}"
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
    private string NormalizeInidepCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return string.Empty;
        // Si el código viene como "721004.0" (común en DBFs numéricos), lo convertimos a "721004"
        if (double.TryParse(code, out double d)) return ((long)d).ToString();
        return code.Trim();
    }
}
