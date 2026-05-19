using OBSArrastre2026.App.Models.Import;
using OBSArrastre2026.App.Services.Internal;
using OBSArrastre2026.App.Data;
using OBSArrastre2026.App.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text;
using System.Text.Json;

namespace OBSArrastre2026.App.Services;

public interface IMareaImportService
{
    Task<MareaValidationReport> ProcessMareaImportAsync(string basePath, IEnumerable<string> selectedFiles, Marea marea, bool procesarSubmuestrasSinMuestraTalla = false);
    Task ImportAsync(string mareaId, MareaValidationReport report);
    Task<bool> HasDataAsync(string mareaId);
    Task ClearMareaDataAsync(string mareaId);
    Task ClearTrackingDataAsync(string mareaId);
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

    private string? ResolveFilePath(IEnumerable<string> selectedFiles, string prefix, int marea, int anio)
    {
        if (selectedFiles == null) return null;

        string yearSuffix = $"{(anio % 100):D2}.DBF";
        string mareaStr = marea.ToString();

        // Buscar todos los archivos en la lista seleccionada
        foreach (var path in selectedFiles)
        {
            string extension = Path.GetExtension(path).ToUpper();
            if (extension != ".DBF") continue;

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

    public async Task<MareaValidationReport> ProcessMareaImportAsync(string basePath, IEnumerable<string> selectedFiles, Marea marea, bool procesarSubmuestrasSinMuestraTalla = false)
    {
        string barco = marea.Buque?.Nombre ?? "S/D";
        int mareaNum = marea.NumeroInidep;
        int anio = marea.AnioInidep;
        var etapas = marea.Etapas;

        // Estimar y verificar previamente si el reporte de auditoría es escribible
        string safeBarco = barco.Replace("/", "-").Replace("\\", "-");
        string reportPath = Path.Combine(basePath, "Reportes", $"Audit_{safeBarco}_{mareaNum}_{anio}.pdf");
        if (!FileHelper.IsFileWritable(reportPath))
        {
            throw new IOException($"No se puede iniciar el proceso de importación porque el reporte de auditoría PDF de destino ya está abierto o bloqueado en:\n\"{reportPath}\"\n\nPor favor, cierre el documento e inténtelo nuevamente.");
        }

        // 1. Resolver rutas de forma flexible (Marea 3 Año 2026 -> S326, S0326, S00326, etc.) limitando a los archivos seleccionados
        string? cPath = ResolveFilePath(selectedFiles, "C", mareaNum, anio);
        string? mPath = ResolveFilePath(selectedFiles, "M", mareaNum, anio);
        string? mdPath = ResolveFilePath(selectedFiles, "MD", mareaNum, anio); // Tallas de descarte
        string? xPath = ResolveFilePath(selectedFiles, "X", mareaNum, anio);
        string? sPath = ResolveFilePath(selectedFiles, "S", mareaNum, anio);
        string? lPath = ResolveFilePath(selectedFiles, "L", mareaNum, anio);
        string? tPath = ResolveFilePath(selectedFiles, "T", mareaNum, anio);
        string? pPath = ResolveFilePath(selectedFiles, "P", mareaNum, anio);

        var report = new MareaValidationReport();
        bool isTrackingOnly = (tPath != null || cPath == null) && cPath == null && mPath == null && pPath == null;
        report.IsTrackingOnly = isTrackingOnly;
        
        var initialIssues = new List<ValidationIssue>();

        // Verificar archivos obligatorios (usando nombres amigables para el reporte si no se encuentran)
        string suffix = $"{mareaNum:D2}{anio % 100:D2}.DBF"; // Nombre sugerido para el error
        if (!isTrackingOnly)
        {
            if (cPath == null) initialIssues.Add(new ValidationIssue(ValidationLevel.Fatal, "Sistema", $"El archivo de CAPTURA obligatorio no se encuentra (esperado C*{suffix})"));
            if (mPath == null) initialIssues.Add(new ValidationIssue(ValidationLevel.Fatal, "Sistema", $"El archivo de MUESTRA obligatorio no se encuentra (esperado M*{suffix})"));
            if (sPath == null) initialIssues.Add(new ValidationIssue(ValidationLevel.Warning, "Sistema", $"El archivo de SUBMUESTRA no se encuentra (esperado S*{suffix})"));
            if (pPath == null) initialIssues.Add(new ValidationIssue(ValidationLevel.Fatal, "Sistema", $"El archivo de PRODUCCIÓN obligatorio no se encuentra (esperado P*{suffix})"));
        }
        else
        {
            initialIssues.Add(new ValidationIssue(ValidationLevel.Info, "Sistema", "Importación parcial detectada: Sólo se actualizarán metadatos y seguimiento satelital."));
        }
        
        foreach (var issue in initialIssues) { report.Issues.Add(issue); }
        // 1.5 Guardar metadatos (carpeta de importación, encoding detectado y nombres de archivos originales)
        var meta = MareaMetadataHelper.GetMetadata(marea.Metadata);
        meta.ImportFolder = basePath;

        meta.OriginalFilenames.Clear();
        if (cPath != null) meta.OriginalFilenames["C"] = Path.GetFileName(cPath);
        if (mPath != null) meta.OriginalFilenames["M"] = Path.GetFileName(mPath);
        if (mdPath != null) meta.OriginalFilenames["MD"] = Path.GetFileName(mdPath);
        if (xPath != null) meta.OriginalFilenames["X"] = Path.GetFileName(xPath);
        if (sPath != null) meta.OriginalFilenames["S"] = Path.GetFileName(sPath);
        if (lPath != null) meta.OriginalFilenames["L"] = Path.GetFileName(lPath);
        if (pPath != null) meta.OriginalFilenames["P"] = Path.GetFileName(pPath);

        if (pPath != null)
        {
            var detectedEnc = await _extractor.DetectEncodingSmartAsync(pPath);
            meta.EncodingCodePage = detectedEnc?.CodePage ?? 1252;
        }
        else if (cPath != null)
        {
             var detectedEnc = await _extractor.DetectEncodingSmartAsync(cPath);
             meta.EncodingCodePage = detectedEnc?.CodePage ?? 1252;
        }
        marea.Metadata = JsonSerializer.Serialize(meta);
        report.MareaMetadata = marea.Metadata;

        // 2. Extraer datos (si los paths fueron resueltos)
        var capturas = cPath != null ? await _extractor.ReadCapturasAsync(cPath) ?? new() : new();

        // Si el buque de la marea es "S/D" (marea nueva) y tenemos lances extraídos del DBF,
        // intentamos usar el nombre del barco real registrado por el observador en el archivo.
        if ((barco == "S/D" || string.IsNullOrWhiteSpace(barco) || barco == "Sin Nombre") && capturas.Any())
        {
            var primerCapturaBarco = capturas.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.Barco))?.Barco;
            if (!string.IsNullOrWhiteSpace(primerCapturaBarco))
            {
                barco = primerCapturaBarco.Trim().ToUpper();
            }
        }
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
            .OrderByDescending(e => e.Frecuente)
            .ToListAsync();

        var productos = await dbContext.Productos.ToListAsync();

        var especiesDict = new Dictionary<string, string>();
        var especiesSinAcentosDict = new Dictionary<string, string>();
        foreach (var esp in especiesDB)
        {
            var codeStr = NormalizeInidepCode(esp.CodigoInidep);
            if (!string.IsNullOrEmpty(codeStr))
            {
                if (!string.IsNullOrEmpty(esp.NombreVulgar))
                {
                    var name = esp.NombreVulgar.Trim().ToUpper().Normalize(NormalizationForm.FormC);
                    especiesDict.TryAdd(name, codeStr);
                    especiesSinAcentosDict.TryAdd(RemoveAccents(name), codeStr);
                }
                if (!string.IsNullOrEmpty(esp.NombreCientifico))
                {
                    var name = esp.NombreCientifico.Trim().ToUpper().Normalize(NormalizationForm.FormC);
                    especiesDict.TryAdd(name, codeStr);
                    especiesSinAcentosDict.TryAdd(RemoveAccents(name), codeStr);
                }
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
            var etapa = etapas.FirstOrDefault(e => lanceTime >= e.FechaZarpada && lanceTime < (e.FechaArribo?.Date.AddDays(1) ?? DateTime.MaxValue));
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

        // Validar productos (P*)
        var productosNombres = new HashSet<string>(productos.Select(p => p.Codigo.Trim().ToUpper()), StringComparer.OrdinalIgnoreCase);
        foreach (var p in produccion)
        {
            if (!string.IsNullOrEmpty(p.Producto) && !productosNombres.Contains(p.Producto.Trim().ToUpper()))
            {
                report.AddIssue(ValidationLevel.Error, "Catálogo Productos", $"El producto '{p.Producto}' no se encuentra en el catálogo local. Se importará como comentario.", $"Fecha {p.Fecha:dd/MM/yyyy}");
            }
        }

        // 5. Validar y Guardar datos en el reporte para el paso de commit
        var etapasFechas = etapas.Select(e => (Inicio: e.FechaZarpada, Fin: e.FechaArribo ?? DateTime.MaxValue)).ToList();

        var largoPesoDB = await dbContext.EspeciesLargoPeso
            .Include(lp => lp.Especie)
            .ToListAsync();

        var largoPesoCatalogo = largoPesoDB
            .Where(lp => lp.Especie?.CodigoInidep != null)
            .ToDictionary(
                lp => (EspecieId: lp.Especie!.CodigoInidep!, Sexo: lp.Sexo),
                lp => (A: lp.ParamA, B: lp.ParamB)
            );

        report = _validator.ValidateMarea(
            barco, 
            anio, 
            mareaNum, 
            meta.BuqueCodigo,
            meta.ObservadorNombre,
            meta.ObservadorApellido,
            meta.ObservadorCodigo,
            etapasFechas, 
            capturas, 
            muestras, 
            submuestras, 
            lgs, 
            tracking, 
            produccion, 
            especiesDict, 
            especiesViejasDict, 
            especiesCodigosValidos, 
            largoPesoCatalogo,
            procesarSubmuestrasSinMuestraTalla);
            
        // Restaurar atributos del reporte original que se perdían al instanciar uno nuevo
        report.IsTrackingOnly = isTrackingOnly;
        report.ArchivosProcesados = archivosEncontrados;
        report.ImportPath = basePath;
        report.MareaMetadata = marea.Metadata;
        foreach (var issue in initialIssues) 
        { 
            report.Issues.Insert(0, issue); 
        }

        // 6. Generar Reporte PDF
        var pdfBytes = await _reporter.GenerateValidationPdfAsync(report);
        safeBarco = barco.Replace("/", "-").Replace("\\", "-");
        reportPath = Path.Combine(basePath, "Reportes", $"Audit_{safeBarco}_{mareaNum}_{anio}.pdf");
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

        // Restaurar metadatos capturados durante la validación (encoding, etc.)
        if (!string.IsNullOrEmpty(report.MareaMetadata))
        {
            marea.Metadata = report.MareaMetadata;
        }

        // Asegurar carpeta de importación en metadata si se pasó explícitamente
        if (!string.IsNullOrEmpty(report.ImportPath))
        {
            marea.Metadata = MareaMetadataHelper.SetImportFolder(marea.Metadata, report.ImportPath);
        }

        // Cargar catálogo de especies con múltiples índices para resolución flexible ordenado por Frecuente descendente
        var especiesCatalogo = await dbContext.Especies
            .OrderByDescending(e => e.Frecuente)
            .ToListAsync();

        var largoPesoDB = await dbContext.EspeciesLargoPeso
            .Include(lp => lp.Especie)
            .ToListAsync();

        var largoPesoCatalogo = largoPesoDB
            .Where(lp => lp.Especie?.CodigoInidep != null)
            .ToDictionary(
                lp => (EspecieId: NormalizeInidepCode(lp.Especie!.CodigoInidep!), Sexo: lp.Sexo),
                lp => (A: lp.ParamA, B: lp.ParamB)
            );
        
        var especieByCodigoMap = especiesCatalogo
            .Where(e => !string.IsNullOrEmpty(e.CodigoInidep))
            .GroupBy(e => NormalizeInidepCode(e.CodigoInidep))
            .ToDictionary(g => g.Key, g => g.First().ID);

        // Construir lista unificada de todos los candidatos de especies ordenados por frecuencia de manera determinista y estricta
        var candidatosEspecies = new List<EspecieMatchCandidate>();
        foreach (var e in especiesCatalogo)
        {
            candidatosEspecies.Add(new EspecieMatchCandidate
            {
                Id = e.ID,
                CodigoInidep = NormalizeInidepCode(e.CodigoInidep),
                NombreVulgar = e.NombreVulgar?.Trim().ToUpper().Normalize(NormalizationForm.FormC) ?? string.Empty,
                NombreCientifico = e.NombreCientifico?.Trim().ToUpper().Normalize(NormalizationForm.FormC) ?? string.Empty,
                Frecuente = e.Frecuente,
                EsVieja = false
            });
        }

        // Usamos la tabla especies_viejas como puente para encontrar el código que mapea a la tabla especies actual.
        var especiesViejasCatalogo = await dbContext.EspeciesViejas.ToListAsync();
        foreach (var ev in especiesViejasCatalogo)
        {
            var code = NormalizeInidepCode(ev.CodigoInidep);
            if (string.IsNullOrEmpty(code)) continue;

            // Si el código de la vieja existe en la nueva, mapeamos al ID de la nueva
            if (especieByCodigoMap.TryGetValue(code, out var newId))
            {
                var frecuenteActual = especiesCatalogo.FirstOrDefault(e => e.ID == newId)?.Frecuente ?? 0;
                candidatosEspecies.Add(new EspecieMatchCandidate
                {
                    Id = newId,
                    CodigoInidep = code,
                    NombreVulgar = ev.NombreVulgar?.Trim().ToUpper().Normalize(NormalizationForm.FormC) ?? string.Empty,
                    NombreCientifico = ev.NombreCientifico?.Trim().ToUpper().Normalize(NormalizationForm.FormC) ?? string.Empty,
                    Frecuente = frecuenteActual,
                    EsVieja = true
                });
            }
        }

        // Ordenar candidatos determinísticamente
        candidatosEspecies = candidatosEspecies
            .OrderByDescending(c => c.Frecuente)
            .ThenBy(c => c.EsVieja ? 1 : 0) // Preferir actuales
            .ThenBy(c => c.CodigoInidep)
            .ToList();



        // Map Capturas -> Lances
        var lanceMap = new Dictionary<double, Lance>();
        foreach (var c in report.Capturas)
        {
            var lanceTime = GetLanceDateTime(c);
            var etapa = marea.Etapas.FirstOrDefault(e => lanceTime >= e.FechaZarpada && lanceTime < (e.FechaArribo?.Date.AddDays(1) ?? DateTime.MaxValue));
            
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
                DescarteTotalKg = c.Descarte,
                
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
                DistanciaPortonesM = c.DistEPor,
                Comentarios = c.Observac,

                // Campos de Integridad 1:1
                Mus = c.Mus,
                EstacionGral = c.EstacGral,
                Estrato = c.Estrato,
                EdadLuna = c.EdadLuna,
                Luz = c.Luz,
                TmpAHum = c.TmpAHum,
                TmpMarS = c.TmpMarS,
                ArteTipo = c.Tarte,
                ArteNro = c.Narte,
                AreaBarrida = c.AreaBarr,
                MallaSobre = c.MallSobre
            };

            // Items de Captura (Especies preservando el orden original de las columnas)
            int itemIndex = 1;
            foreach (var sCode in c.EspeciesOrder)
            {
                if (c.Especies.TryGetValue(sCode, out var val) && val > 0)
                {
                    especieByCodigoMap.TryGetValue(sCode, out var especieId);
                    
                    lance.ItemsCaptura.Add(new ItemCaptura
                    {
                        EspecieID = especieId,
                        EspecieOriginal = sCode,
                        DatoCaptura = val,
                        DatoDescarte = c.DescartesPorEspecie.TryGetValue(sCode, out var d) ? d : 0,
                        TipoDatoDescarte = report.UnidadDescarte,
                        NumeroOrden = itemIndex++
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
                // Resolución de especie: Priorizar código, luego nombre (con puente incluido)
                string? especieId = null;
                if (!string.IsNullOrEmpty(rm.CodEspec)) especieByCodigoMap.TryGetValue(rm.CodEspec.Trim(), out especieId);
                
                if (especieId == null && !string.IsNullOrEmpty(rm.Especie))
                {
                    var normName = rm.Especie.Trim().ToUpper().Normalize(NormalizationForm.FormC);
                    var exactMatch = candidatosEspecies.FirstOrDefault(c => 
                        c.NombreVulgar == normName || 
                        c.NombreCientifico == normName);
                    
                    if (exactMatch != null)
                    {
                        especieId = exactMatch.Id;
                    }
                    else
                    {
                        var normNameNoAccents = RemoveAccents(normName);
                        var accentMatch = candidatosEspecies.FirstOrDefault(c => 
                            RemoveAccents(c.NombreVulgar) == normNameNoAccents || 
                            RemoveAccents(c.NombreCientifico) == normNameNoAccents);
                        
                        if (accentMatch != null)
                        {
                            especieId = accentMatch.Id;
                        }
                    }
                }


                if (especieId != null)
                {
                    var muestra = new Muestra
                    {
                        Lance = lance,
                        EspecieID = especieId,
                        Intervalo = rm.Intervalo,
                        // Inferir Flags
                        UnidadMedidaTalla = 1, // CM por defecto en archivos M*
                        ModoMedicionTalla = 1, // LT por defecto
                        Origen = 1, // Muestreo de Captura
                        DiscriminaSexo = rm.Tallies.Any(t => t.Males > 0 || t.Females > 0) ? 1 : 0,
                        HayIndeterminados = rm.Tallies.Any(t => t.Indeterminate > 0) ? 1 : 0,
                        TipoMuestra = rm.TipoMuestra,
                        NumeroOrden = rm.NumeroOrden,
                        EspecieOriginal = rm.Especie,
                        Fuente = rm.Fuente,
                        Tarte = rm.Tarte,
                        FactPond = rm.FactPond,
                        PrimTalla = rm.PrimTalla,
                        UltTalla = rm.UltTalla,
                        Area = (rm.Area == null || rm.Area <= 0) && lance.LatitudInicioDecimal.HasValue && lance.LongitudInicioDecimal.HasValue
                            ? LegacyDecoder.CalculateGridArea(lance.LatitudInicioDecimal.Value, lance.LongitudInicioDecimal.Value)
                            : rm.Area,
                        PesoMuestra_PesoGramos = rm.PesoMues * 1000.0
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
                    
                    // Priorizar muestras estándar para el vínculo con submuestras. 
                    // Si ya existe una (estándar), no la sobrescribimos con una de descarte.
                    if (rm.TipoMuestra == 1 || !muestraMap.ContainsKey(key))
                    {
                        muestraMap[key] = muestra;
                    }
                    
                    // Mapa adicional para búsqueda por ID de especie (usado por archivos L*)
                    muestraByIdMap[$"{rm.Lance}_{especieId}"] = muestra;
                }
            }
        }

        // Reconstrucción automática de muestras huérfanas a partir de submuestras si la opción está habilitada
        if (report.ProcesarSubmuestrasSinMuestraTalla)
        {
            var submuestrasHuerfanas = report.Submuestras
                .Where(rs => !muestraMap.ContainsKey($"{rs.Lance}_{rs.Especie?.Trim().ToUpper().Normalize(NormalizationForm.FormC)}"))
                .ToList();

            if (submuestrasHuerfanas.Any())
            {
                var gruposHuerfanos = submuestrasHuerfanas
                    .GroupBy(s => new { s.Lance, EspecieKey = s.Especie?.Trim().ToUpper().Normalize(NormalizationForm.FormC) });

                foreach (var grupo in gruposHuerfanos)
                {
                    if (lanceMap.TryGetValue(grupo.Key.Lance, out var lance))
                    {
                        string? especieId = null;
                        var muestraEjemplo = grupo.First();

                        if (grupo.Key.EspecieKey != null)
                        {
                            var exactMatch = candidatosEspecies.FirstOrDefault(c => 
                                c.NombreVulgar == grupo.Key.EspecieKey || 
                                c.NombreCientifico == grupo.Key.EspecieKey);

                            if (exactMatch != null)
                            {
                                especieId = exactMatch.Id;
                            }
                            else
                            {
                                var normNameNoAccents = RemoveAccents(grupo.Key.EspecieKey);
                                var accentMatch = candidatosEspecies.FirstOrDefault(c => 
                                    RemoveAccents(c.NombreVulgar) == normNameNoAccents || 
                                    RemoveAccents(c.NombreCientifico) == normNameNoAccents);

                                if (accentMatch != null)
                                {
                                    especieId = accentMatch.Id;
                                }
                            }
                        }

                        if (especieId != null)
                        {
                            var ejemplaresConTalla = grupo.Where(x => x.LargoTot > 0).ToList();
                            int? primTalla = ejemplaresConTalla.Any() ? ejemplaresConTalla.Min(x => x.LargoTot) : null;
                            int? ultTalla = ejemplaresConTalla.Any() ? ejemplaresConTalla.Max(x => x.LargoTot) : null;

                            double sumaPesoKg = grupo.Sum(s => s.PesoTot);
                            double pesoGramos = sumaPesoKg * 1000.0;

                            var muestraReconstruida = new Muestra
                            {
                                Lance = lance,
                                EspecieID = especieId,
                                Intervalo = 1.0, // por defecto 1 cm
                                UnidadMedidaTalla = 1, // CM
                                ModoMedicionTalla = 1, // LT
                                Origen = 1, // Muestreo de Captura
                                TipoMuestra = 1, // Estándar
                                NumeroOrden = 999, // Identificador para muestras reconstruidas
                                EspecieOriginal = muestraEjemplo.Especie,
                                Fuente = muestraEjemplo.Fuente,
                                Tarte = muestraEjemplo.Tarte,
                                FactPond = 1.0,
                                PrimTalla = primTalla,
                                UltTalla = ultTalla,
                                Area = (muestraEjemplo.Area == null || muestraEjemplo.Area <= 0) && lance.LatitudInicioDecimal.HasValue && lance.LongitudInicioDecimal.HasValue
                                    ? LegacyDecoder.CalculateGridArea(lance.LatitudInicioDecimal.Value, lance.LongitudInicioDecimal.Value)
                                    : muestraEjemplo.Area,
                                PesoMuestra_PesoGramos = pesoGramos,
                                Comentarios = "Muestra reconstruida automáticamente a partir de submuestra.",
                                Automatica = true
                            };

                            var agrupacionesTalla = grupo
                                .GroupBy(s => s.LargoTot)
                                .OrderBy(g => g.Key);

                            foreach (var gt in agrupacionesTalla)
                            {
                                int machos = gt.Count(x => x.Sexo == 1);
                                int hembras = gt.Count(x => x.Sexo == 2);
                                int indeterminados = gt.Count(x => x.Sexo != 1 && x.Sexo != 2);
                                int total = gt.Count();

                                muestraReconstruida.FrecuenciasTallas.Add(new FrecuenciaTalla
                                {
                                    Talla = gt.Key, // en centímetros
                                    NroMachos = machos,
                                    NroHembras = hembras,
                                    NroIndeterminados = indeterminados,
                                    NroTotal = total
                                });
                            }

                            muestraReconstruida.DiscriminaSexo = grupo.Any(t => t.Sexo == 1 || t.Sexo == 2) ? 1 : 0;
                            muestraReconstruida.HayIndeterminados = grupo.Any(t => t.Sexo != 1 && t.Sexo != 2) ? 1 : 0;

                            int totalEjemplares = grupo.Count();
                            if (sumaPesoKg > 0)
                            {
                                muestraReconstruida.EjemplaresPorKg = (int)Math.Round(totalEjemplares / sumaPesoKg);
                            }

                            // Calcular el peso de la muestra automática reconstruida usando la relación largo-peso de sus ejemplares
                            var especieEntidad = especiesCatalogo.FirstOrDefault(e => e.ID == especieId);
                            string? especieCodInidep = especieEntidad?.CodigoInidep != null ? NormalizeInidepCode(especieEntidad.CodigoInidep) : null;

                            if (!string.IsNullOrEmpty(especieCodInidep))
                            {
                                string espId = especieCodInidep.Trim();
                                double totalWeightGramos = 0;
                                bool foundAnyParams = false;

                                // Fallback en el archivo LG (L*) si existe
                                double fallbackA = 0, fallbackB = 0;
                                bool hasFallback = false;
                                var lg = report.Lgs.FirstOrDefault(l => NormalizeInidepCode(l.CodEspecIE) == espId);
                                if (lg != null && lg.ParamA > 0)
                                {
                                    fallbackA = lg.ParamA;
                                    fallbackB = lg.ParamB;
                                    hasFallback = true;
                                }

                                if (hasFallback || largoPesoCatalogo.Any(k => k.Key.EspecieId.Trim() == espId))
                                {
                                    // Función local para obtener parámetros alométricos inteligentes con fallback
                                    (double A, double B) GetSmartParams(int targetSex, out bool isSpecific)
                                    {
                                        (double A, double B) res = (0, 0);
                                        isSpecific = false;

                                        // 1. Intentar búsqueda exacta
                                        if (largoPesoCatalogo.TryGetValue((espId, targetSex), out res))
                                        {
                                            isSpecific = (targetSex == 1 || targetSex == 2);
                                        }

                                        // 2. Fallbacks de sexo
                                        if (res.A <= 0)
                                        {
                                            if (targetSex == 0 && largoPesoCatalogo.TryGetValue((espId, 3), out res)) { }
                                            else if (targetSex != 0 && largoPesoCatalogo.TryGetValue((espId, 0), out res)) { }
                                        }

                                        // 3. Cualquier sexo (último recurso)
                                        if (res.A <= 0)
                                        {
                                            var anyEntry = largoPesoCatalogo.FirstOrDefault(k => k.Key.EspecieId == espId);
                                            if (anyEntry.Key.EspecieId != null) { res = anyEntry.Value; }
                                        }

                                        // 4. Promedios
                                        if (res.A <= 0)
                                        {
                                            bool hasM = largoPesoCatalogo.TryGetValue((espId, 1), out var pM);
                                            bool hasF = largoPesoCatalogo.TryGetValue((espId, 2), out var pF);
                                            if (hasM && hasF) { res = ((pM.A + pF.A) / 2.0, (pM.B + pF.B) / 2.0); }
                                            else if (hasM) { res = pM; }
                                            else if (hasF) { res = pF; }
                                        }

                                        if (res.A <= 0 && hasFallback) { res = (fallbackA, fallbackB); }
                                        return res;
                                    }

                                    foreach (var tally in muestraReconstruida.FrecuenciasTallas)
                                    {
                                        double tallaCm = tally.Talla;

                                        var pM = GetSmartParams(1, out bool specificM);
                                        var pH = GetSmartParams(2, out bool specificH);
                                        var pI = GetSmartParams(0, out _);

                                        // Si hay discriminación y fórmulas específicas para ambos sexos
                                        if ((tally.NroMachos > 0 || tally.NroHembras > 0) && specificM && specificH)
                                        {
                                            if (tally.NroMachos > 0 && pM.A > 0) totalWeightGramos += (tally.NroMachos * (pM.A * Math.Pow(tallaCm, pM.B)));
                                            if (tally.NroHembras > 0 && pH.A > 0) totalWeightGramos += (tally.NroHembras * (pH.A * Math.Pow(tallaCm, pH.B)));
                                            if (tally.NroIndeterminados > 0 && pI.A > 0) totalWeightGramos += (tally.NroIndeterminados * (pI.A * Math.Pow(tallaCm, pI.B)));
                                            foundAnyParams = true;
                                        }
                                        // De lo contrario, si hay Indeterminados o conteo parcial, usamos la fórmula general
                                        else if (tally.NroMachos > 0 || tally.NroHembras > 0 || tally.NroIndeterminados > 0)
                                        {
                                            if (pI.A > 0)
                                            {
                                                int suma = tally.NroMachos + tally.NroHembras + tally.NroIndeterminados;
                                                totalWeightGramos += (suma * (pI.A * Math.Pow(tallaCm, pI.B)));
                                                foundAnyParams = true;
                                            }
                                        }
                                        // Si todo es cero pero hay Total (muestra sin discriminar)
                                        else if (tally.NroTotal > 0)
                                        {
                                            if (pI.A > 0)
                                            {
                                                totalWeightGramos += (tally.NroTotal * (pI.A * Math.Pow(tallaCm, pI.B)));
                                                foundAnyParams = true;
                                            }
                                        }
                                    }
                                }

                                if (foundAnyParams && totalWeightGramos > 0)
                                {
                                    double totalWeightKg = Math.Round(totalWeightGramos / 1000.0, 2);
                                    muestraReconstruida.PesoMuestra_PesoGramos = totalWeightKg * 1000.0;
                                    if (totalWeightKg > 0)
                                    {
                                        muestraReconstruida.EjemplaresPorKg = (int)Math.Round(totalEjemplares / totalWeightKg);
                                    }
                                }
                            }

                            dbContext.Muestras.Add(muestraReconstruida);

                            string key = $"{grupo.Key.Lance}_{grupo.Key.EspecieKey}";
                            muestraMap[key] = muestraReconstruida;
                            muestraByIdMap[$"{grupo.Key.Lance}_{especieId}"] = muestraReconstruida;
                        }
                    }
                }
            }
        }

        // Map Submuestras
        foreach (var rs in report.Submuestras)
        {
            string speciesKey = rs.Especie.Trim().ToUpper().Normalize(NormalizationForm.FormC);
            if (muestraMap.TryGetValue($"{rs.Lance}_{speciesKey}", out var muestra))
            {
                var itemSub = new ItemSubmuestra
                {
                    MuestraID = muestra.ID,
                    NroEjemplar = rs.NEjemplar,
                    Sexo = rs.Sexo,
                    Estadio = rs.Estadio,
                    ReplecionGastrica = rs.Replecion,
                    Edad = rs.Edad,
                    LargoTotalMm = rs.LargoTot,
                    LargoEstandarMm = rs.LargoSta,
                    PesoTotalGramos = rs.PesoTot, // Guardar en gramos directamente
                    Comentarios = rs.Comentario,
                    
                    // Campos de Integridad 1:1
                    NumeroOrden = rs.NumeroOrden,
                    Tarte = rs.Tarte,
                    Fuente = rs.Fuente,
                    Area = rs.Area,
                    PesoVac = rs.PesoVac,
                    PesoGon = rs.PesoGon,
                    PesoHig = rs.PesoHig,
                    RTotal = rs.RTotal,
                    EspecieOriginal = rs.Especie
                };
                dbContext.ItemsSubmuestras.Add(itemSub);
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

        // Map Tracking (Se asume hora local desde el origen)
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
                        else
                        {
                            // Intentar resolución determinista priorizada
                            // 1. Coincidencia exacta de NombreVulgar o NombreCientifico
                            var exactMatch = candidatosEspecies.FirstOrDefault(c => 
                                c.NombreVulgar == searchName || 
                                c.NombreCientifico == searchName);
                            
                            if (exactMatch != null)
                            {
                                speciesId = exactMatch.Id;
                            }
                            else
                            {
                                // 2. Coincidencia sin acentos
                                var searchNameNoAccents = RemoveAccents(searchName);
                                var accentMatch = candidatosEspecies.FirstOrDefault(c => 
                                    RemoveAccents(c.NombreVulgar) == searchNameNoAccents || 
                                    RemoveAccents(c.NombreCientifico) == searchNameNoAccents);
                                
                                if (accentMatch != null)
                                {
                                    speciesId = accentMatch.Id;
                                }
                                else
                                {
                                    // 3. Fallback: Probar si el campo Especie trae el código INIDEP directamente (normalizado)
                                    var searchCode = NormalizeInidepCode(rp.Especie);
                                    especieByCodigoMap.TryGetValue(searchCode, out speciesId);
                                }
                            }
                        }
                    }


                    dbContext.RegistrosProduccion.Add(new RegistroProduccion
                    {
                        MareaEtapaId = etapa.ID,
                        Fecha = rp.Fecha.ToString("yyyy-MM-dd"),
                        IdProducto = productGuid,
                        Categoria = rp.Categoria,
                        EspecieId = speciesId,
                        EspecieOriginal = rp.Especie,
                        Factor = rp.Factor,
                        Operarios = rp.Operarios,
                        Kg = rp.Kilos,
                        NumeroOrden = rp.NumeroOrden,
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

    public async Task ClearTrackingDataAsync(string mareaId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        
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

    private string RemoveAccents(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    private class EspecieMatchCandidate
    {
        public string Id { get; set; } = string.Empty;
        public string CodigoInidep { get; set; } = string.Empty;
        public string NombreVulgar { get; set; } = string.Empty;
        public string NombreCientifico { get; set; } = string.Empty;
        public int Frecuente { get; set; }
        public bool EsVieja { get; set; }
    }
}

