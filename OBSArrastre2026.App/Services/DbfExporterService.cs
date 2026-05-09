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
            new DBFField("MAREA", NativeDbType.Numeric, 10, 0),
            new DBFField("FECHA", NativeDbType.Date),
            new DBFField("LANCE", NativeDbType.Numeric, 10, 0),
            new DBFField("HORA_INIC", NativeDbType.Numeric, 10, 2),
            new DBFField("HORA_FINAL", NativeDbType.Numeric, 10, 2),
            new DBFField("LAT_INIC", NativeDbType.Numeric, 10, 3),
            new DBFField("LONG_INIC", NativeDbType.Numeric, 10, 3),
            new DBFField("LAT_FINAL", NativeDbType.Numeric, 10, 3),
            new DBFField("LONG_FINAL", NativeDbType.Numeric, 10, 3),
            new DBFField("PROF_INIC", NativeDbType.Numeric, 10, 0),
            new DBFField("PROF_FINAL", NativeDbType.Numeric, 10, 0),
            new DBFField("TIEMPO", NativeDbType.Numeric, 2, 0),
            new DBFField("MAR", NativeDbType.Numeric, 2, 0),
            new DBFField("DIR_VIENTO", NativeDbType.Numeric, 5, 0),
            new DBFField("VEL_VIENTO", NativeDbType.Numeric, 5, 0),
            new DBFField("TMP_A_SECO", NativeDbType.Numeric, 5, 1),
            new DBFField("TMP_MAR_F", NativeDbType.Numeric, 5, 1),
            new DBFField("PRESION_B", NativeDbType.Numeric, 10, 0),
            new DBFField("CAPT_TOTAL", NativeDbType.Numeric, 10, 0),
            new DBFField("DESCARTE", NativeDbType.Numeric, 10, 0),
            new DBFField("VEL_ARRAS", NativeDbType.Numeric, 5, 1),
            new DBFField("RUMBO", NativeDbType.Numeric, 5, 0),
            new DBFField("MALL_COPO", NativeDbType.Numeric, 5, 0),
            new DBFField("MALL_ALAS", NativeDbType.Numeric, 5, 0),
            new DBFField("CAB_FILAD", NativeDbType.Numeric, 10, 0),
            new DBFField("ABER_VERT", NativeDbType.Numeric, 5, 1),
            new DBFField("DIST_ALAS", NativeDbType.Numeric, 5, 1),
            new DBFField("DIST_E_POR", NativeDbType.Numeric, 5, 1),
            new DBFField("OBSERVAC", NativeDbType.Char, 100)
        };

        // Agregar matriz de 25 especies
        for (int i = 1; i <= 25; i++)
        {
            fields.Add(new DBFField($"ESPECIE_{i}", NativeDbType.Numeric, 10, 0));
            fields.Add(new DBFField($"KG_{i}", NativeDbType.Numeric, 10, 0));
            fields.Add(new DBFField($"DESCAR_{i}", NativeDbType.Numeric, 10, 0));
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
                row[idx++] = DateTime.Parse(lance.Fecha);
                row[idx++] = (double)lance.NroLance;
                row[idx++] = LegacyDecoder.EncodeTime(ParseTime(lance.HoraInicio));
                row[idx++] = LegacyDecoder.EncodeTime(ParseTime(lance.HoraFinal));
                row[idx++] = LegacyDecoder.EncodeCoordinate(lance.LatitudInicioDecimal ?? 0);
                row[idx++] = LegacyDecoder.EncodeCoordinate(lance.LongitudInicioDecimal ?? 0);
                row[idx++] = LegacyDecoder.EncodeCoordinate(lance.LatitudFinalDecimal ?? 0);
                row[idx++] = LegacyDecoder.EncodeCoordinate(lance.LongitudFinalDecimal ?? 0);
                row[idx++] = (double)(lance.ProfundidadInicioM ?? 0);
                row[idx++] = (double)(lance.ProfundidadFinalM ?? 0);
                row[idx++] = (double)(lance.EstadoTiempoCodigo ?? 0);
                row[idx++] = (double)(lance.EstadoMarCodigo ?? 0);
                row[idx++] = (double)(lance.VientoDireccionGrados ?? 0);
                row[idx++] = (double)(lance.VientoFuerzaBeaufort ?? 0);
                row[idx++] = (double)(lance.TemperaturaAireC ?? 0);
                row[idx++] = (double)(lance.TemperaturaRedC ?? 0);
                row[idx++] = (double)(lance.PresionHpa ?? 0);
                row[idx++] = (double)(lance.CapturaTotalKg ?? 0);
                row[idx++] = (double)(lance.DescarteTotalKg ?? 0);
                row[idx++] = (double)(lance.VelocidadArrastreNudos ?? 0);
                row[idx++] = (double)(lance.RumboGrados ?? 0);
                row[idx++] = (double)(lance.MallaCopoMm ?? 0);
                row[idx++] = (double)(lance.MallaAlasMm ?? 0);
                row[idx++] = (double)(lance.CableFiladoM ?? 0);
                row[idx++] = (double)(lance.AberturaVerticalM ?? 0);
                row[idx++] = (double)(lance.DistanciaAlasM ?? 0);
                row[idx++] = (double)(lance.DistanciaPortonesM ?? 0);
                row[idx++] = lance.Comentarios ?? "";

                // Matriz de especies
                var items = lance.ItemsCaptura.OrderByDescending(i => i.DatoCaptura).Take(25).ToList();
                for (int i = 0; i < 25; i++)
                {
                    if (i < items.Count)
                    {
                        var item = items[i];
                        if (double.TryParse(item.Especie?.CodigoInidep, out double spCode))
                        {
                            row[idx++] = spCode;
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
            new DBFField("MAREA", NativeDbType.Numeric, 10, 0),
            new DBFField("FECHA", NativeDbType.Date),
            new DBFField("ESPECIE", NativeDbType.Char, 30),
            new DBFField("PRODUCTO", NativeDbType.Char, 20),
            new DBFField("CATEGORIA", NativeDbType.Char, 10),
            new DBFField("OPERARIOS", NativeDbType.Numeric, 5, 0),
            new DBFField("FACTOR", NativeDbType.Numeric, 10, 3),
            new DBFField("KILOS", NativeDbType.Numeric, 10, 1)
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
                row[idx++] = p.Producto?.Codigo ?? "";
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

        // 1. Preparar archivos
        string mPath = Path.Combine(path, $"M{suffix}.DBF");
        string sPath = Path.Combine(path, $"S{suffix}.DBF");
        string lPath = Path.Combine(path, $"L{suffix}.DBF");
        string xPath = Path.Combine(path, $"X{suffix}.DBF");

        using var mStream = File.Open(mPath, FileMode.Create, FileAccess.Write);
        var mWriter = new DBFWriter(mStream) { CharEncoding = encoding };

        using var sStream = File.Open(sPath, FileMode.Create, FileAccess.Write);
        var sWriter = new DBFWriter(sStream) { CharEncoding = encoding };

        // El archivo L solo se crea si hay datos de langostino
        DBFWriter? lWriter = null;
        FileStream? lStream = null;

        // El archivo X solo se crea si hay extensiones (muestras > 90 tallas)
        DBFWriter? xWriter = null;
        FileStream? xStream = null;

        // Definición de campos para M
        var mFields = new List<DBFField>
        {
            new DBFField("BARCO", NativeDbType.Char, 20),
            new DBFField("MAREA", NativeDbType.Numeric, 10, 0),
            new DBFField("LANCE", NativeDbType.Numeric, 10, 0),
            new DBFField("FECHA", NativeDbType.Date),
            new DBFField("ESPECIE", NativeDbType.Char, 30),
            new DBFField("COD_ESPEC", NativeDbType.Numeric, 10, 0),
            new DBFField("AREA", NativeDbType.Numeric, 5, 0),
            new DBFField("PRIM_TALLA", NativeDbType.Numeric, 5, 0),
            new DBFField("ULT_TALLA", NativeDbType.Numeric, 5, 0),
            new DBFField("INTERVALO", NativeDbType.Numeric, 5, 1),
            new DBFField("PESO_MUES", NativeDbType.Numeric, 10, 3),
            new DBFField("FACT_POND", NativeDbType.Numeric, 10, 3)
        };
        for (int i = 1; i <= 90; i++) mFields.Add(new DBFField($"TALLA_{i}", NativeDbType.Char, 14));
        mWriter.Fields = mFields.ToArray();

        // Definición de campos para S
        var sFields = new List<DBFField>
        {
            new DBFField("BARCO", NativeDbType.Char, 20),
            new DBFField("MAREA", NativeDbType.Numeric, 10, 0),
            new DBFField("LANCE", NativeDbType.Numeric, 10, 0),
            new DBFField("FECHA", NativeDbType.Date),
            new DBFField("ESPECIE", NativeDbType.Char, 30),
            new DBFField("NEJEMPLAR", NativeDbType.Numeric, 10, 0),
            new DBFField("LARGO_TOT", NativeDbType.Numeric, 5, 0),
            new DBFField("LARGO_STA", NativeDbType.Numeric, 5, 0),
            new DBFField("PESO_TOT", NativeDbType.Numeric, 10, 3),
            new DBFField("SEXO", NativeDbType.Numeric, 2, 0),
            new DBFField("ESTADIO", NativeDbType.Numeric, 2, 0)
        };
        sWriter.Fields = sFields.ToArray();

        foreach (var etapa in marea.Etapas)
        {
            foreach (var lance in etapa.Lances.OrderBy(l => l.NroLance))
            {
                foreach (var m in lance.Muestras)
                {
                    // Exportar registro M
                    var mRow = new object[mFields.Count];
                    int mIdx = 0;
                    mRow[mIdx++] = barco;
                    mRow[mIdx++] = (double)marea.NumeroInidep;
                    mRow[mIdx++] = (double)lance.NroLance;
                    mRow[mIdx++] = DateTime.Parse(lance.Fecha);
                    mRow[mIdx++] = m.Especie?.NombreVulgar ?? "";
                    mRow[mIdx++] = double.TryParse(m.Especie?.CodigoInidep, out var c) ? c : 0.0;
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
                            mRow[mIdx++] = LegacyDecoder.EncodeTally((int)f.Talla, f.NroMachos, f.NroHembras, f.NroIndeterminados, f.NroTotal);
                        }
                        else
                        {
                            mRow[mIdx++] = "0";
                        }
                    }
                    mWriter.WriteRecord(mRow);

                    // Exportar extensiones X si hay más de 90 tallas
                    if (freqs.Count > 90)
                    {
                        if (xWriter == null)
                        {
                            xStream = File.Open(xPath, FileMode.Create, FileAccess.Write);
                            xWriter = new DBFWriter(xStream) { CharEncoding = encoding };
                            xWriter.Fields = mFields.ToArray(); // Misma estructura que M
                        }
                        // Lógica simplificada: solo una extensión (hasta 180 tallas totales)
                        var xRow = new object[mFields.Count];
                        Array.Copy(mRow, xRow, 12); // Copiar encabezado
                        int xIdx = 12;
                        for (int i = 90; i < 180; i++)
                        {
                            if (i < freqs.Count)
                            {
                                var f = freqs[i];
                                xRow[xIdx++] = LegacyDecoder.EncodeTally((int)f.Talla, f.NroMachos, f.NroHembras, f.NroIndeterminados, f.NroTotal);
                            }
                            else
                            {
                                xRow[xIdx++] = "0";
                            }
                        }
                        xWriter.WriteRecord(xRow);
                    }

                    // Exportar Submuestras S
                    foreach (var s in m.ItemsSubmuestras)
                    {
                        var sRow = new object[sFields.Count];
                        int sIdx = 0;
                        sRow[sIdx++] = barco;
                        sRow[sIdx++] = (double)marea.NumeroInidep;
                        sRow[sIdx++] = (double)lance.NroLance;
                        sRow[sIdx++] = DateTime.Parse(lance.Fecha);
                        sRow[sIdx++] = m.Especie?.NombreVulgar ?? "";
                        sRow[sIdx++] = (double)s.NroEjemplar;
                        sRow[sIdx++] = (double)s.LargoTotalMm;
                        sRow[sIdx++] = (double)s.LargoEstandarMm;
                        sRow[sIdx++] = s.PesoTotalGramos / 1000.0;
                        sRow[sIdx++] = (double)(s.Sexo ?? 0);
                        sRow[sIdx++] = (double)(s.Estadio ?? 0);
                        sWriter.WriteRecord(sRow);
                    }

                    // Exportar Langostinos L
                    if (m.Especie?.CodigoInidep == "5139030101" && freqs.Any(f => f.NroLangostinosMachoMaduros > 0 || f.NroLangostinosHembraMaduras > 0))
                    {
                        if (lWriter == null)
                        {
                            lStream = File.Open(lPath, FileMode.Create, FileAccess.Write);
                            lWriter = new DBFWriter(lStream) { CharEncoding = encoding };
                            var lFields = new List<DBFField>
                            {
                                new DBFField("BARCO", NativeDbType.Char, 20),
                                new DBFField("MAREA", NativeDbType.Numeric, 10, 0),
                                new DBFField("LANCE", NativeDbType.Numeric, 10, 0),
                                new DBFField("FECHA", NativeDbType.Date),
                                new DBFField("CODIGO", NativeDbType.Numeric, 10, 0)
                            };
                            for (int i = 1; i <= 70; i++) lFields.Add(new DBFField($"TALLA_{i}", NativeDbType.Numeric, 10, 0));
                            lWriter.Fields = lFields.ToArray();
                        }

                        var lRow = new object[lWriter.Fields.Length];
                        int lIdx = 0;
                        lRow[lIdx++] = barco;
                        lRow[lIdx++] = (double)marea.NumeroInidep;
                        lRow[lIdx++] = (double)lance.NroLance;
                        lRow[lIdx++] = DateTime.Parse(lance.Fecha);
                        lRow[lIdx++] = 5139030101.0;

                        for (int i = 0; i < 70; i++)
                        {
                            if (i < freqs.Count)
                            {
                                var f = freqs[i];
                                lRow[lIdx++] = LegacyDecoder.EncodeMatureTally(f.NroLangostinosMachoMaduros, f.NroLangostinosHembraMaduras, f.NroLangostinosHembraImpregnadas);
                            }
                            else
                            {
                                lRow[lIdx++] = 0.0;
                            }
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
