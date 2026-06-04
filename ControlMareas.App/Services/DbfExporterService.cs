using System.IO;
using System.Text;
using System.Diagnostics;
using DotNetDBF;
using Microsoft.EntityFrameworkCore;
using ControlMareas.App.Data;
using ControlMareas.App.Data.Entities;
using ControlMareas.App.Models;
using ControlMareas.App.Services.Internal;

namespace ControlMareas.App.Services;

public sealed class DbfExporterService : IDbfExporterService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public DbfExporterService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
        // Asegurar soporte para codificaciones legacy (CP850)
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public async Task<DbfExportSummary> ExportMareaToDbfAsync(Marea marea, string outputPath, bool useOriginalFilenames = false, IProgress<double>? progress = null)
    {
        var totalSw = Stopwatch.StartNew();
        var summary = new DbfExportSummary();

        progress?.Report(5);
        if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);

        using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        // Recargar marea con todos los datos necesarios (Auditamos el tiempo de DB)
        var swDb = Stopwatch.StartNew();
        var fullMarea = await dbContext.Mareas
            .AsSplitQuery()
            .Include(m => m.Buque)
            .Include(m => m.Etapas)
                .ThenInclude(e => e.Lances)
                    .ThenInclude(l => l.ItemsCaptura)
                        .ThenInclude(i => i.Especie)
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
                    .ThenInclude(p => p.Producto)
            .Include(m => m.Etapas)
                .ThenInclude(e => e.RegistrosProduccion)
                    .ThenInclude(p => p.Especie)
            .FirstOrDefaultAsync(m => m.ID == marea.ID);

        summary.StageTimings["Carga Base de Datos"] = swDb.Elapsed;

        if (fullMarea == null) return summary;


        string mareaSuffix = $"{fullMarea.NumeroInidep}{fullMarea.AnioInidep % 100:D2}";
        string barcoNombre = fullMarea.Buque?.Nombre ?? "S/D";

        // Determinar codificación de exportación basada en los metadatos (original detectado)
        var metadata = MareaMetadataHelper.GetMetadata(fullMarea.Metadata);
        var exportEncoding = metadata.EncodingCodePage.HasValue 
            ? Encoding.GetEncoding(metadata.EncodingCodePage.Value) 
            : Encoding.GetEncoding(850); // Fallback recomendado para INIDEP (antes era 437)

        var originalFilenames = useOriginalFilenames ? metadata.OriginalFilenames : null;

        // Exportar cada archivo
        progress?.Report(10);
        var sw = Stopwatch.StartNew();
        await ExportCapturasAsync(fullMarea, barcoNombre, mareaSuffix, outputPath, exportEncoding, originalFilenames);
        summary.StageTimings["Capturas (C)"] = sw.Elapsed;

        progress?.Report(30);
        sw.Restart();
        await ExportProduccionAsync(fullMarea, barcoNombre, mareaSuffix, outputPath, exportEncoding, originalFilenames);
        summary.StageTimings["Producción (P)"] = sw.Elapsed;


        progress?.Report(70);
        sw.Restart();
        await ExportMuestrasYSasyn(fullMarea, barcoNombre, mareaSuffix, outputPath, exportEncoding, originalFilenames, progress);
        summary.StageTimings["Muestras y Submuestras (M,S,L,X)"] = sw.Elapsed;

        progress?.Report(100);
        
        return summary with { TotalTime = totalSw.Elapsed };
    }

    private async Task ExportCapturasAsync(Marea marea, string barco, string suffix, string path, Encoding encoding, Dictionary<string, string>? originalFilenames)
    {
        if (!marea.Etapas.Any(e => e.Lances.Any())) return;

        string name = (originalFilenames != null && originalFilenames.TryGetValue("C", out var originalName) && !string.IsNullOrWhiteSpace(originalName))
            ? originalName
            : $"C{suffix}.DBF";
        string fileName = Path.Combine(path, name);

        using var stream = File.Open(fileName, FileMode.Create, FileAccess.Write);
        var writer = new DBFWriter(stream) { CharEncoding = encoding };

        var fields = new List<DBFField>
        {
            new DBFField("BARCO", NativeDbType.Char, 20),
            new DBFField("MAREA", NativeDbType.Numeric, 3, 0),
            new DBFField("LANCE", NativeDbType.Numeric, 3, 0),
            new DBFField("MUS", NativeDbType.Numeric, 1, 0),
            new DBFField("ESTAC_GRAL", NativeDbType.Numeric, 4, 0),
            new DBFField("FECHA", NativeDbType.Date),
            new DBFField("HORA_INIC", NativeDbType.Numeric, 5, 2),
            new DBFField("HORA_FINAL", NativeDbType.Numeric, 5, 2),
            new DBFField("LAT_INIC", NativeDbType.Numeric, 6, 3),
            new DBFField("LAT_FINAL", NativeDbType.Numeric, 6, 3),
            new DBFField("LONG_INIC", NativeDbType.Numeric, 6, 3),
            new DBFField("LONG_FINAL", NativeDbType.Numeric, 6, 3),
            new DBFField("ESTRATO", NativeDbType.Numeric, 4, 0),
            new DBFField("RUMBO", NativeDbType.Numeric, 3, 0),
            new DBFField("TIEMPO", NativeDbType.Numeric, 2, 0),
            new DBFField("MAR", NativeDbType.Numeric, 2, 0),
            new DBFField("EDAD_LUNA", NativeDbType.Numeric, 2, 0),
            new DBFField("LUZ", NativeDbType.Numeric, 2, 0),
            new DBFField("DIR_VIENTO", NativeDbType.Numeric, 3, 0),
            new DBFField("VEL_VIENTO", NativeDbType.Numeric, 2, 0),
            new DBFField("PROF_INIC", NativeDbType.Numeric, 4, 0),
            new DBFField("PROF_FINAL", NativeDbType.Numeric, 4, 0),
            new DBFField("TMP_A_SECO", NativeDbType.Numeric, 5, 2),
            new DBFField("TMP_A_HUM", NativeDbType.Numeric, 5, 2),
            new DBFField("TMP_MAR_S", NativeDbType.Numeric, 5, 2),
            new DBFField("TMP_MAR_F", NativeDbType.Numeric, 5, 2),
            new DBFField("PRESION_B", NativeDbType.Numeric, 6, 1),
            new DBFField("CAPT_TOTAL", NativeDbType.Numeric, 10, 1),
            new DBFField("DESCARTE", NativeDbType.Numeric, 10, 1),
            new DBFField("OBSERVAC", NativeDbType.Char, 50),
            new DBFField("TARTE", NativeDbType.Numeric, 2, 0),
            new DBFField("NARTE", NativeDbType.Numeric, 2, 0),
            new DBFField("VEL_ARRAS", NativeDbType.Numeric, 4, 2),
            new DBFField("AREA_BARR", NativeDbType.Numeric, 8, 5),
            new DBFField("CAB_FILAD", NativeDbType.Numeric, 4, 0),
            new DBFField("DIST_ALAS", NativeDbType.Numeric, 5, 1),
            new DBFField("ABER_VERT", NativeDbType.Numeric, 4, 1),
            new DBFField("MALL_ALAS", NativeDbType.Numeric, 4, 0),
            new DBFField("MALL_COPO", NativeDbType.Numeric, 3, 0),
            new DBFField("MALL_SOBRE", NativeDbType.Numeric, 3, 0),
            new DBFField("DIST_E_POR", NativeDbType.Numeric, 5, 1)
        };

        for (int i = 1; i <= 25; i++)
        {
            fields.Add(new DBFField($"ESPECIE_{i}", NativeDbType.Numeric, 10, 0));
            fields.Add(new DBFField($"KG_{i}", NativeDbType.Numeric, 9, 2));
            fields.Add(new DBFField($"DESCAR_{i}", NativeDbType.Numeric, 9, 2));
        }

        writer.Fields = fields.ToArray();

        foreach (var etapa in marea.Etapas)
        {
            foreach (var lance in etapa.Lances.OrderBy(l => l.NroLance))
            {
                var row = new object[fields.Count];
                int idx = 0;
                row[idx++] = barco;
                row[idx++] = (double)marea.NumeroInidep;
                row[idx++] = (double)lance.NroLance;
                row[idx++] = (double?)lance.Mus;
                row[idx++] = (double?)lance.EstacionGral;
                row[idx++] = DateTime.Parse(lance.Fecha);
                row[idx++] = LegacyDecoder.EncodeTime(ParseTime(lance.HoraInicio));
                row[idx++] = LegacyDecoder.EncodeTime(ParseTime(lance.HoraFinal));
                row[idx++] = LegacyDecoder.EncodeCoordinate(lance.LatitudInicioDecimal ?? 0);
                row[idx++] = LegacyDecoder.EncodeCoordinate(lance.LatitudFinalDecimal ?? 0);
                row[idx++] = LegacyDecoder.EncodeCoordinate(lance.LongitudInicioDecimal ?? 0);
                row[idx++] = LegacyDecoder.EncodeCoordinate(lance.LongitudFinalDecimal ?? 0);
                row[idx++] = (double?)lance.Estrato;
                row[idx++] = (double?)lance.RumboGrados;
                row[idx++] = (double?)lance.EstadoTiempoCodigo;
                row[idx++] = (double?)lance.EstadoMarCodigo;
                row[idx++] = (double?)lance.EdadLuna;
                row[idx++] = (double?)lance.Luz;
                row[idx++] = (double?)lance.VientoDireccionGrados;
                row[idx++] = (double?)lance.VientoFuerzaBeaufort;
                row[idx++] = (double?)lance.ProfundidadInicioM;
                row[idx++] = (double?)lance.ProfundidadFinalM;
                row[idx++] = (double?)lance.TemperaturaAireC;
                row[idx++] = (double?)lance.TmpAHum;
                row[idx++] = (double?)lance.TmpMarS;
                row[idx++] = (double?)lance.TemperaturaRedC;
                row[idx++] = (double?)lance.PresionHpa;
                row[idx++] = (double?)lance.CapturaTotalKg;
                row[idx++] = (double?)lance.DescarteTotalKg;
                row[idx++] = lance.Comentarios ?? "";
                row[idx++] = (double?)lance.ArteTipo;
                row[idx++] = (double?)lance.ArteNro;
                row[idx++] = (double?)lance.VelocidadArrastreNudos;
                row[idx++] = (double?)lance.AreaBarrida;
                row[idx++] = (double?)lance.CableFiladoM;
                row[idx++] = (double?)lance.DistanciaAlasM;
                row[idx++] = (double?)lance.AberturaVerticalM;
                row[idx++] = (double?)lance.MallaAlasMm;
                row[idx++] = (double?)lance.MallaCopoMm;
                row[idx++] = (double?)lance.MallaSobre;
                row[idx++] = (double?)lance.DistanciaPortonesM;

                var items = lance.ItemsCaptura.OrderBy(i => i.NumeroOrden).Take(25).ToList();
                for (int i = 0; i < 25; i++)
                {
                    if (i < items.Count)
                    {
                        var item = items[i];
                        item.Lance = lance; // Asegurar back-reference para el correcto cálculo de kilogramos
                        row[idx++] = double.TryParse(item.Especie?.CodigoInidep ?? item.EspecieOriginal, out double spCode) ? spCode : null;
                        row[idx++] = item.CapturaTotalKgCalculado;
                        row[idx++] = item.PesoDescarteCalculado;
                    }
                    else
                    {
                        row[idx++] = 0.0;
                        row[idx++] = 0.0;
                        row[idx++] = 0.0;
                    }
                }

                writer.WriteRecord(row);
            }
        }

        writer.Close();
    }

    private async Task ExportProduccionAsync(Marea marea, string barco, string suffix, string path, Encoding encoding, Dictionary<string, string>? originalFilenames)
    {
        if (!marea.Etapas.Any(e => e.RegistrosProduccion.Any())) return;

        string name = (originalFilenames != null && originalFilenames.TryGetValue("P", out var originalName) && !string.IsNullOrWhiteSpace(originalName))
            ? originalName
            : $"P{suffix}.DBF";
        string fileName = Path.Combine(path, name);

        using var stream = File.Open(fileName, FileMode.Create, FileAccess.Write);
        var writer = new DBFWriter(stream) { CharEncoding = encoding };

        var fields = new List<DBFField>
        {
            new DBFField("BARCO", NativeDbType.Char, 20),
            new DBFField("MAREA", NativeDbType.Numeric, 3, 0),
            new DBFField("FECHA", NativeDbType.Date),
            new DBFField("ESPECIE", NativeDbType.Char, 25),
            new DBFField("PRODUCTO", NativeDbType.Char, 25),
            new DBFField("CATEGORIA", NativeDbType.Char, 3),
            new DBFField("OPERARIOS", NativeDbType.Numeric, 3, 0),
            new DBFField("FACTOR", NativeDbType.Numeric, 5, 2),
            new DBFField("KILOS", NativeDbType.Numeric, 9, 2)
        };

        writer.Fields = fields.ToArray();

        foreach (var etapa in marea.Etapas)
        {
            foreach (var p in etapa.RegistrosProduccion.OrderBy(r => r.NumeroOrden))
            {
                var row = new object[fields.Count];
                int idx = 0;
                row[idx++] = barco;
                row[idx++] = (double)marea.NumeroInidep;
                row[idx++] = DateTime.Parse(p.Fecha);
                row[idx++] = p.EspecieOriginal ?? p.Especie?.NombreVulgar ?? "";
                row[idx++] = p.Producto?.Codigo ?? "";
                row[idx++] = p.Categoria ?? "";
                row[idx++] = p.Operarios != null ? (double)p.Operarios : DBNull.Value;
                row[idx++] = p.Factor != null ? (double)p.Factor : DBNull.Value;
                row[idx++] = p.Kg != null ? (double)p.Kg : DBNull.Value;

                writer.WriteRecord(row);
            }
        }

        writer.Close();
    }

    private async Task ExportMuestrasYSasyn(Marea marea, string barco, string suffix, string path, Encoding encoding, Dictionary<string, string>? originalFilenames, IProgress<double>? progress = null)
    {
        string mName = (originalFilenames != null && originalFilenames.TryGetValue("M", out var oM) && !string.IsNullOrWhiteSpace(oM)) ? oM : $"M{suffix}.DBF";
        string mdName = (originalFilenames != null && originalFilenames.TryGetValue("MD", out var oMd) && !string.IsNullOrWhiteSpace(oMd)) ? oMd : $"MD{suffix}.DBF";
        string sName = (originalFilenames != null && originalFilenames.TryGetValue("S", out var oS) && !string.IsNullOrWhiteSpace(oS)) ? oS : $"S{suffix}.DBF";
        string lName = (originalFilenames != null && originalFilenames.TryGetValue("L", out var oL) && !string.IsNullOrWhiteSpace(oL)) ? oL : $"L{suffix}.DBF";
        string xName = (originalFilenames != null && originalFilenames.TryGetValue("X", out var oX) && !string.IsNullOrWhiteSpace(oX)) ? oX : $"X{suffix}.DBF";

        string mPath = Path.Combine(path, mName);
        string mdPath = Path.Combine(path, mdName);
        string sPath = Path.Combine(path, sName);
        string lPath = Path.Combine(path, lName);
        string xPath = Path.Combine(path, xName);

        FileStream? mStream = null;
        DBFWriter? mWriter = null;
        FileStream? mdStream = null;
        DBFWriter? mdWriter = null;
        FileStream? sStream = null;
        DBFWriter? sWriter = null;
        DBFWriter? lWriter = null;
        FileStream? lStream = null;
        DBFWriter? xWriter = null;
        FileStream? xStream = null;

        var mFields = new List<DBFField>
        {
            new DBFField("BARCO", NativeDbType.Char, 20),
            new DBFField("FECHA", NativeDbType.Date),
            new DBFField("MAREA", NativeDbType.Numeric, 3, 0),
            new DBFField("LANCE", NativeDbType.Numeric, 3, 0),
            new DBFField("ESPECIE", NativeDbType.Char, 34),
            new DBFField("COD_ESPEC", NativeDbType.Numeric, 10, 0),
            new DBFField("FUENTE", NativeDbType.Numeric, 1, 0),
            new DBFField("TARTE", NativeDbType.Numeric, 2, 0),
            new DBFField("AREA", NativeDbType.Numeric, 6, 1),
            new DBFField("PRIM_TALLA", NativeDbType.Numeric, 3, 0),
            new DBFField("ULT_TALLA", NativeDbType.Numeric, 3, 0),
            new DBFField("INTERVALO", NativeDbType.Numeric, 2, 0),
            new DBFField("PESO_MUES", NativeDbType.Numeric, 7, 2),
            new DBFField("FACT_POND", NativeDbType.Numeric, 17, 4)
        };
        for (int i = 1; i <= 90; i++) mFields.Add(new DBFField($"TALLA_{i}", NativeDbType.Numeric, 15, 0));

        var xFields = new List<DBFField>(mFields.GetRange(0, 14));
        for (int i = 91; i <= 150; i++) xFields.Add(new DBFField($"TALLA_{i}", NativeDbType.Numeric, 15, 0));

        var sFields = new List<DBFField>
        {
            new DBFField("BARCO", NativeDbType.Char, 20),
            new DBFField("MAREA", NativeDbType.Numeric, 3, 0),
            new DBFField("LANCE", NativeDbType.Numeric, 3, 0),
            new DBFField("FECHA", NativeDbType.Date),
            new DBFField("TARTE", NativeDbType.Numeric, 2, 0),
            new DBFField("FUENTE", NativeDbType.Numeric, 1, 0),
            new DBFField("AREA", NativeDbType.Numeric, 6, 1),
            new DBFField("ESPECIE", NativeDbType.Char, 24),
            new DBFField("NEJEMPLAR", NativeDbType.Numeric, 3, 0),
            new DBFField("LARGO_TOT", NativeDbType.Numeric, 3, 0),
            new DBFField("LARGO_STA", NativeDbType.Numeric, 3, 0),
            new DBFField("PESO_TOT", NativeDbType.Numeric, 7, 1),
            new DBFField("PESO_VAC", NativeDbType.Numeric, 6, 1),
            new DBFField("SEXO", NativeDbType.Numeric, 1, 0),
            new DBFField("ESTADIO", NativeDbType.Numeric, 1, 0),
            new DBFField("PESO_GON", NativeDbType.Numeric, 6, 2),
            new DBFField("PESO_HIG", NativeDbType.Numeric, 6, 2),
            new DBFField("REPLESION", NativeDbType.Numeric, 1, 0),
            new DBFField("CONTENIDO", NativeDbType.Char, 30),
            new DBFField("EDAD", NativeDbType.Numeric, 2, 0),
            new DBFField("R_TOTAL", NativeDbType.Numeric, 4, 2)
        };

        var todasMuestras = marea.Etapas
            .SelectMany(e => e.Lances)
            .SelectMany(l => l.Muestras)
            .OrderBy(m => m.NumeroOrden)
            .ToList();

        foreach (var m in todasMuestras)
        {
            var lance = m.Lance;
            if (lance == null) continue;

            int baseTalla = m.PrimTalla ?? (m.FrecuenciasTallas.Any() ? (int)m.FrecuenciasTallas.Min(f => f.Talla) : 0);
            int interval = (int)m.Intervalo;
            if (interval <= 0) interval = 1;
            bool requiereExtension = m.FrecuenciasTallas.Any(f => f.Talla > baseTalla + 89 * interval);

            var mRow = new object[mFields.Count];
            int mIdx = 0;
            mRow[mIdx++] = barco;
            mRow[mIdx++] = DateTime.Parse(lance.Fecha);
            mRow[mIdx++] = (double)marea.NumeroInidep;
            mRow[mIdx++] = (double)lance.NroLance;
            mRow[mIdx++] = m.EspecieOriginal ?? m.Especie?.NombreVulgar ?? "";
            mRow[mIdx++] = double.TryParse(m.Especie?.CodigoInidep ?? m.EspecieOriginal, out var c) ? c : null;
            mRow[mIdx++] = (object)m.Fuente ?? DBNull.Value;
            mRow[mIdx++] = (object)m.Tarte ?? DBNull.Value;
            mRow[mIdx++] = (object)m.Area ?? DBNull.Value;
            mRow[mIdx++] = m.PrimTalla != null ? (double)m.PrimTalla : DBNull.Value;

            double? ultTallaExportar = m.UltTalla;
            if (requiereExtension && ultTallaExportar != null)
            {
                ultTallaExportar = baseTalla + 89 * interval; // En archivo M, ULT_TALLA corresponde a TALLA_90
            }
            mRow[mIdx++] = ultTallaExportar != null ? (double)ultTallaExportar : DBNull.Value;

            mRow[mIdx++] = (object)m.Intervalo ?? DBNull.Value;
            mRow[mIdx++] = (m.PesoMuestra_PesoGramos ?? 0) / 1000.0; // Kg
            mRow[mIdx++] = (object)m.FactPond ?? DBNull.Value;

            var freqMap = m.FrecuenciasTallas.ToDictionary(f => (int)f.Talla, f => f);
            int prim = m.PrimTalla ?? baseTalla;
            int ult = m.UltTalla ?? (m.FrecuenciasTallas.Any() ? (int)m.FrecuenciasTallas.Max(f => f.Talla) : prim);

            for (int i = 0; i < 90; i++)
            {
                int currentTalla = baseTalla + (i * interval);
                if (freqMap.TryGetValue(currentTalla, out var ft))
                {
                    mRow[mIdx++] = long.Parse(LegacyDecoder.EncodeTally((int)ft.Talla, ft.NroMachos, ft.NroHembras, ft.NroIndeterminados, ft.NroTotal));
                }
                else if (currentTalla >= prim && currentTalla <= ult)
                {
                    // Rellenar con ceros empaquetados si está en el rango original
                    mRow[mIdx++] = long.Parse(LegacyDecoder.EncodeTally(currentTalla, 0, 0, 0, 0));
                }
                else
                {
                    mRow[mIdx++] = null;
                }
            }
            if (m.TipoMuestra == 2)
            {
                if (mdWriter == null)
                {
                    mdStream = File.Open(mdPath, FileMode.Create, FileAccess.Write);
                    mdWriter = new DBFWriter(mdStream) { CharEncoding = encoding };
                    mdWriter.Fields = mFields.ToArray();
                }
                mdWriter.WriteRecord(mRow);
            }
            else
            {
                if (mWriter == null)
                {
                    mStream = File.Open(mPath, FileMode.Create, FileAccess.Write);
                    mWriter = new DBFWriter(mStream) { CharEncoding = encoding };
                    mWriter.Fields = mFields.ToArray();
                }
                mWriter.WriteRecord(mRow);
            }

            var freqs = m.FrecuenciasTallas.OrderBy(f => f.Talla).ToList();

            if (requiereExtension)
            {
                if (xWriter == null)
                {
                    xStream = File.Open(xPath, FileMode.Create, FileAccess.Write);
                    xWriter = new DBFWriter(xStream) { CharEncoding = encoding };
                    xWriter.Fields = xFields.ToArray();
                }
                var xRow = new object[xFields.Count];
                Array.Copy(mRow, xRow, 14);
                xRow[9] = (double)(baseTalla + 90 * interval); // PRIM_TALLA en el archivo X corresponde a TALLA_91
                xRow[10] = m.UltTalla != null ? (double)m.UltTalla : DBNull.Value; // ULT_TALLA en el archivo X conserva el valor real total
                int xIdx = 14;
                for (int i = 90; i < 150; i++)
                {
                    int currentTalla = baseTalla + (i * interval);
                    if (freqMap.TryGetValue(currentTalla, out var ft))
                    {
                        xRow[xIdx++] = long.Parse(LegacyDecoder.EncodeTally((int)ft.Talla, ft.NroMachos, ft.NroHembras, ft.NroIndeterminados, ft.NroTotal));
                    }
                    else if (currentTalla >= prim && currentTalla <= ult)
                    {
                        // Rellenar con ceros si cae dentro del rango original observado de la muestra
                        xRow[xIdx++] = long.Parse(LegacyDecoder.EncodeTally(currentTalla, 0, 0, 0, 0));
                    }
                    else
                    {
                        xRow[xIdx++] = null;
                    }
                }
                xWriter.WriteRecord(xRow);
            }

            foreach (var s in m.ItemsSubmuestras.OrderBy(x => x.NumeroOrden))
            {
                if (sWriter == null)
                {
                    sStream = File.Open(sPath, FileMode.Create, FileAccess.Write);
                    sWriter = new DBFWriter(sStream) { CharEncoding = encoding };
                    sWriter.Fields = sFields.ToArray();
                }
                var sRow = new object[sFields.Count];
                int sIdx = 0;
                sRow[sIdx++] = barco;
                sRow[sIdx++] = (double)marea.NumeroInidep;
                sRow[sIdx++] = (double)lance.NroLance;
                sRow[sIdx++] = DateTime.Parse(lance.Fecha);
                sRow[sIdx++] = (object)s.Tarte ?? DBNull.Value;
                sRow[sIdx++] = (object)s.Fuente ?? DBNull.Value;
                sRow[sIdx++] = (object)s.Area ?? DBNull.Value;
                sRow[sIdx++] = s.EspecieOriginal ?? m.Especie?.NombreVulgar ?? "";
                sRow[sIdx++] = (double)s.NroEjemplar;
                sRow[sIdx++] = s.LargoTotalMm != null ? (double)s.LargoTotalMm : null;
                sRow[sIdx++] = s.LargoEstandarMm != null ? (double)s.LargoEstandarMm : null;
                sRow[sIdx++] = s.PesoTotalGramos != null ? Math.Round(s.PesoTotalGramos.Value, 1) : null;
                sRow[sIdx++] = s.PesoVac != null ? (double)s.PesoVac : DBNull.Value;
                sRow[sIdx++] = s.Sexo != null ? (double)s.Sexo : DBNull.Value;
                sRow[sIdx++] = s.Estadio != null ? (double)s.Estadio : DBNull.Value;
                sRow[sIdx++] = s.PesoGon != null ? (double)s.PesoGon : DBNull.Value;
                sRow[sIdx++] = s.PesoHig != null ? (double)s.PesoHig : DBNull.Value;
                sRow[sIdx++] = s.ReplecionGastrica != null ? (double)s.ReplecionGastrica : DBNull.Value;
                sRow[sIdx++] = s.Comentarios ?? "";
                sRow[sIdx++] = s.Edad;
                sRow[sIdx++] = s.RTotal;
                sWriter.WriteRecord(sRow);
            }

            if (m.Especie?.CodigoInidep == "5139030101") // Langostino
            {
                if (lWriter == null)
                {
                    lStream = File.Open(lPath, FileMode.Create, FileAccess.Write);
                    lWriter = new DBFWriter(lStream) { CharEncoding = encoding };
                    var lFields = new List<DBFField>
                    {
                        new DBFField("BARCO", NativeDbType.Char, 20),
                        new DBFField("MAREA", NativeDbType.Numeric, 3, 0),
                        new DBFField("LANCE", NativeDbType.Numeric, 3, 0),
                        new DBFField("FECHA", NativeDbType.Date)
                    };
                    for (int i = 1; i <= 70; i++) lFields.Add(new DBFField($"TALLA_{i}", NativeDbType.Numeric, 9, 0));
                    lWriter.Fields = lFields.ToArray();
                }
                var lRow = new object[lWriter.Fields.Length];
                int lIdx = 0;
                lRow[lIdx++] = barco;
                lRow[lIdx++] = (double)marea.NumeroInidep;
                lRow[lIdx++] = (double)lance.NroLance;
                lRow[lIdx++] = DateTime.Parse(lance.Fecha);
                for (int talla = 1; talla <= 70; talla++)
                {
                    if (freqMap.TryGetValue(talla, out var f))
                    {
                        lRow[lIdx++] = LegacyDecoder.EncodeMatureTally(f.NroLangostinosMachoMaduros, f.NroLangostinosHembraMaduras, f.NroLangostinosHembraImpregnadas);
                    }
                    else 
                    {
                        lRow[lIdx++] = null;
                    }
                }
                lWriter.WriteRecord(lRow);
            }
        }

        mWriter?.Close();
        mStream?.Dispose();
        sWriter?.Close();
        sStream?.Dispose();
        lWriter?.Close();
        lStream?.Dispose();
        xWriter?.Close();
        xStream?.Dispose();
        mdWriter?.Close();
        mdStream?.Dispose();
    }

    private TimeSpan ParseTime(string? time)
    {
        if (string.IsNullOrEmpty(time)) return TimeSpan.Zero;
        if (TimeSpan.TryParse(time, out var ts)) return ts;
        return TimeSpan.Zero;
    }
}
