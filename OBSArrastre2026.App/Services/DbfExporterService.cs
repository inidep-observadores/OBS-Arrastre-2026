using System.IO;
using System.Text;
using System.Diagnostics;
using DotNetDBF;
using Microsoft.EntityFrameworkCore;
using OBSArrastre2026.App.Data;
using OBSArrastre2026.App.Data.Entities;
using OBSArrastre2026.App.Models;
using OBSArrastre2026.App.Services.Internal;

namespace OBSArrastre2026.App.Services;

public sealed class DbfExporterService : IDbfExporterService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public DbfExporterService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
        // Asegurar soporte para codificaciones legacy (CP850)
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public async Task<DbfExportSummary> ExportMareaToDbfAsync(Marea marea, string outputPath, IProgress<double>? progress = null)
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

        // Exportar cada archivo
        progress?.Report(10);
        var sw = Stopwatch.StartNew();
        await ExportCapturasAsync(fullMarea, barcoNombre, mareaSuffix, outputPath);
        summary.StageTimings["Capturas (C)"] = sw.Elapsed;

        progress?.Report(30);
        sw.Restart();
        await ExportProduccionAsync(fullMarea, barcoNombre, mareaSuffix, outputPath);
        summary.StageTimings["Producción (P)"] = sw.Elapsed;


        progress?.Report(70);
        sw.Restart();
        await ExportMuestrasYSasyn(fullMarea, barcoNombre, mareaSuffix, outputPath, progress);
        summary.StageTimings["Muestras y Submuestras (M,S,L,X)"] = sw.Elapsed;

        progress?.Report(100);
        
        return summary with { TotalTime = totalSw.Elapsed };
    }

    private async Task ExportCapturasAsync(Marea marea, string barco, string suffix, string path)
    {
        string fileName = Path.Combine(path, $"C{suffix}.DBF");
        var encoding = Encoding.GetEncoding(850);

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
                row[idx++] = 0.0; // MUS
                row[idx++] = 0.0; // ESTAC_GRAL
                row[idx++] = DateTime.Parse(lance.Fecha);
                row[idx++] = LegacyDecoder.EncodeTime(ParseTime(lance.HoraInicio));
                row[idx++] = LegacyDecoder.EncodeTime(ParseTime(lance.HoraFinal));
                row[idx++] = LegacyDecoder.EncodeCoordinate(lance.LatitudInicioDecimal ?? 0);
                row[idx++] = LegacyDecoder.EncodeCoordinate(lance.LatitudFinalDecimal ?? 0);
                row[idx++] = LegacyDecoder.EncodeCoordinate(lance.LongitudInicioDecimal ?? 0);
                row[idx++] = LegacyDecoder.EncodeCoordinate(lance.LongitudFinalDecimal ?? 0);
                row[idx++] = 0.0; // ESTRATO
                row[idx++] = (double)(lance.RumboGrados ?? 0);
                
                // TIEMPO (min) - Calculado a partir de horas si es posible
                double tiempoMin = 0;
                if (!string.IsNullOrEmpty(lance.HoraInicio) && !string.IsNullOrEmpty(lance.HoraFinal))
                {
                    var inicio = ParseTime(lance.HoraInicio);
                    var final = ParseTime(lance.HoraFinal);
                    var diff = final - inicio;
                    if (diff.TotalMinutes < 0) diff = diff.Add(TimeSpan.FromDays(1)); // Cruce de medianoche
                    tiempoMin = Math.Min(99, diff.TotalMinutes);
                }
                row[idx++] = tiempoMin;

                row[idx++] = (double)(lance.EstadoMarCodigo ?? 0);
                row[idx++] = 0.0; // EDAD_LUNA
                row[idx++] = 0.0; // LUZ
                row[idx++] = (double)(lance.VientoDireccionGrados ?? 0);
                row[idx++] = (double)(lance.VientoFuerzaBeaufort ?? 0);
                row[idx++] = (double)(lance.ProfundidadInicioM ?? 0);
                row[idx++] = (double)(lance.ProfundidadFinalM ?? 0);
                row[idx++] = (double)(lance.TemperaturaAireC ?? 0);
                row[idx++] = 0.0; // TMP_A_HUM
                row[idx++] = (double)(lance.TemperaturaRedC ?? 0); // TMP_MAR_S
                row[idx++] = 0.0; // TMP_MAR_F
                row[idx++] = (double)(lance.PresionHpa ?? 0);
                row[idx++] = (double)(lance.CapturaTotalKg ?? 0);
                row[idx++] = (double)(lance.DescarteTotalKg ?? 0);
                row[idx++] = lance.Comentarios ?? "";
                row[idx++] = 0.0; // TARTE
                row[idx++] = 0.0; // NARTE
                row[idx++] = (double)(lance.VelocidadArrastreNudos ?? 0);
                row[idx++] = 0.0; // AREA_BARR
                row[idx++] = (double)(lance.CableFiladoM ?? 0);
                row[idx++] = (double)(lance.DistanciaAlasM ?? 0);
                row[idx++] = (double)(lance.AberturaVerticalM ?? 0);
                row[idx++] = (double)(lance.MallaAlasMm ?? 0);
                row[idx++] = (double)(lance.MallaCopoMm ?? 0);
                row[idx++] = 0.0; // MALL_SOBRE
                row[idx++] = (double)(lance.DistanciaPortonesM ?? 0);

                var items = lance.ItemsCaptura.OrderByDescending(i => i.DatoCaptura).Take(25).ToList();
                for (int i = 0; i < 25; i++)
                {
                    if (i < items.Count)
                    {
                        var item = items[i];
                        row[idx++] = double.TryParse(item.Especie?.CodigoInidep, out double spCode) ? spCode : 0.0;
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

    private async Task ExportProduccionAsync(Marea marea, string barco, string suffix, string path)
    {
        string fileName = Path.Combine(path, $"P{suffix}.DBF");
        var encoding = Encoding.GetEncoding(850);

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
            foreach (var p in etapa.RegistrosProduccion.OrderBy(r => r.Fecha))
            {
                var row = new object[fields.Count];
                int idx = 0;
                row[idx++] = barco;
                row[idx++] = (double)marea.NumeroInidep;
                row[idx++] = DateTime.Parse(p.Fecha);
                row[idx++] = p.Especie?.NombreVulgar ?? "";
                row[idx++] = p.Producto?.Descripcion ?? "";
                row[idx++] = p.Categoria ?? "";
                row[idx++] = (double)(p.Operarios ?? 0);
                row[idx++] = p.Factor ?? 0.0;
                row[idx++] = p.Kg ?? 0.0;

                writer.WriteRecord(row);
            }
        }

        writer.Close();
    }

    private async Task ExportMuestrasYSasyn(Marea marea, string barco, string suffix, string path, IProgress<double>? progress = null)
    {
        var encoding = Encoding.GetEncoding(850);

        string mPath = Path.Combine(path, $"M{suffix}.DBF");
        string sPath = Path.Combine(path, $"S{suffix}.DBF");
        string lPath = Path.Combine(path, $"L{suffix}.DBF");
        string xPath = Path.Combine(path, $"X{suffix}.DBF");

        using var mStream = File.Open(mPath, FileMode.Create, FileAccess.Write);
        var mWriter = new DBFWriter(mStream) { CharEncoding = encoding };

        using var sStream = File.Open(sPath, FileMode.Create, FileAccess.Write);
        var sWriter = new DBFWriter(sStream) { CharEncoding = encoding };

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
        mWriter.Fields = mFields.ToArray();

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
        sWriter.Fields = sFields.ToArray();

        foreach (var etapa in marea.Etapas)
        {
            foreach (var lance in etapa.Lances.OrderBy(l => l.NroLance))
            {
                foreach (var m in lance.Muestras)
                {
                    var mRow = new object[mFields.Count];
                    int mIdx = 0;
                    mRow[mIdx++] = barco;
                    mRow[mIdx++] = DateTime.Parse(lance.Fecha);
                    mRow[mIdx++] = (double)marea.NumeroInidep;
                    mRow[mIdx++] = (double)lance.NroLance;
                    mRow[mIdx++] = m.Especie?.NombreVulgar ?? "";
                    mRow[mIdx++] = double.TryParse(m.Especie?.CodigoInidep, out var c) ? c : 0.0;
                    mRow[mIdx++] = 0.0; // FUENTE
                    mRow[mIdx++] = 0.0; // TARTE
                    mRow[mIdx++] = 0.0; // AREA
                    mRow[mIdx++] = (double)m.FrecuenciasTallas.Select(f => f.Talla).DefaultIfEmpty(0).Min();
                    mRow[mIdx++] = (double)m.FrecuenciasTallas.Select(f => f.Talla).DefaultIfEmpty(0).Max();
                    mRow[mIdx++] = (double)m.Intervalo;
                    mRow[mIdx++] = (m.PesoMuestra_PesoGramos ?? 0) / 1000.0;
                    mRow[mIdx++] = 1.0; // FACT_POND

                    var freqs = m.FrecuenciasTallas.OrderBy(f => f.Talla).ToList();
                    for (int i = 0; i < 90; i++)
                    {
                        if (i < freqs.Count)
                        {
                            var f = freqs[i];
                            mRow[mIdx++] = double.Parse(LegacyDecoder.EncodeTally((int)f.Talla, f.NroMachos, f.NroHembras, f.NroIndeterminados, f.NroTotal));
                        }
                        else mRow[mIdx++] = 0.0;
                    }
                    mWriter.WriteRecord(mRow);

                    if (freqs.Count > 90)
                    {
                        if (xWriter == null)
                        {
                            xStream = File.Open(xPath, FileMode.Create, FileAccess.Write);
                            xWriter = new DBFWriter(xStream) { CharEncoding = encoding };
                            xWriter.Fields = xFields.ToArray();
                        }
                        var xRow = new object[xFields.Count];
                        Array.Copy(mRow, xRow, 14);
                        int xIdx = 14;
                        for (int i = 90; i < 150; i++)
                        {
                            if (i < freqs.Count)
                            {
                                var f = freqs[i];
                                xRow[xIdx++] = double.Parse(LegacyDecoder.EncodeTally((int)f.Talla, f.NroMachos, f.NroHembras, f.NroIndeterminados, f.NroTotal));
                            }
                            else xRow[xIdx++] = 0.0;
                        }
                        xWriter.WriteRecord(xRow);
                    }

                    foreach (var s in m.ItemsSubmuestras)
                    {
                        var sRow = new object[sFields.Count];
                        int sIdx = 0;
                        sRow[sIdx++] = barco;
                        sRow[sIdx++] = (double)marea.NumeroInidep;
                        sRow[sIdx++] = (double)lance.NroLance;
                        sRow[sIdx++] = DateTime.Parse(lance.Fecha);
                        sRow[sIdx++] = 0.0; // TARTE
                        sRow[sIdx++] = 0.0; // FUENTE
                        sRow[sIdx++] = 0.0; // AREA
                        sRow[sIdx++] = m.Especie?.NombreVulgar ?? "";
                        sRow[sIdx++] = (double)s.NroEjemplar;
                        sRow[sIdx++] = (double)(s.LargoTotalMm ?? 0);
                        sRow[sIdx++] = (double)(s.LargoEstandarMm ?? 0);
                        sRow[sIdx++] = Math.Round((s.PesoTotalGramos ?? 0) / 10.0, 1);
                        sRow[sIdx++] = 0.0; // PESO_VAC
                        sRow[sIdx++] = (double)(s.Sexo ?? 0);
                        sRow[sIdx++] = (double)(s.Estadio ?? 0);
                        sRow[sIdx++] = 0.0; // PESO_GON
                        sRow[sIdx++] = 0.0; // PESO_HIG
                        sRow[sIdx++] = (double)(s.ReplecionGastrica ?? 0);
                        sRow[sIdx++] = s.Comentarios ?? "";
                        sRow[sIdx++] = s.Edad ?? 0.0;
                        sRow[sIdx++] = 0.0; // R_TOTAL
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
                        for (int i = 0; i < 70; i++)
                        {
                            if (i < freqs.Count)
                            {
                                var f = freqs[i];
                                lRow[lIdx++] = LegacyDecoder.EncodeMatureTally(f.NroLangostinosMachoMaduros, f.NroLangostinosHembraMaduras, f.NroLangostinosHembraImpregnadas);
                            }
                            else lRow[lIdx++] = 0.0;
                        }
                        lWriter.WriteRecord(lRow);
                    }
                }
            }
        }

        mWriter.Close();
        sWriter.Close();
        lWriter?.Close();
        lStream?.Dispose();
        xWriter?.Close();
        xStream?.Dispose();
    }

    private TimeSpan ParseTime(string? time)
    {
        if (string.IsNullOrEmpty(time)) return TimeSpan.Zero;
        if (TimeSpan.TryParse(time, out var ts)) return ts;
        return TimeSpan.Zero;
    }
}
