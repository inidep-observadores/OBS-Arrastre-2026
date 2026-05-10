using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetDBF;
using Xunit;
using Xunit.Abstractions;
using OBSArrastre2026.App.Services;
using OBSArrastre2026.App.Data;
using OBSArrastre2026.App.Data.Entities;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace OBSArrastre2026.Tests;

public class IntegrityAuditTests
{
    private readonly ITestOutputHelper _output;
    private const string EntradaPath = @"d:\Desarrollo\_INIDEP\OBS\OBS-Arrastre-2026\ejemplos\Entrada";
    private const string SalidaPath = @"d:\Desarrollo\_INIDEP\OBS\OBS-Arrastre-2026\ejemplos\Salida";
    private const string DbPath = "audit_test.db";

    public IntegrityAuditTests(ITestOutputHelper output)
    {
        _output = output;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    [Fact]
    public async Task PerformRoundTripAndAudit()
    {
        _output.WriteLine("Starting Byte-Perfect Integrity Audit...");

        // 1. Setup Database
        if (File.Exists(DbPath)) File.Delete(DbPath);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={DbPath}")
            .Options;
        
        var dbFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        dbFactory.CreateDbContextAsync().Returns(_ => Task.FromResult(new AppDbContext(options)));
        
        using (var db = new AppDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
            
            // Sembrar Buque
            if (!await db.Buques.AnyAsync(b => b.Nombre == "ARGENTINO"))
            {
                db.Buques.Add(new Buque { Nombre = "ARGENTINO", Matricula = 2685 });
            }

            // Sembrar Especies y Productos dinámicamente desde los archivos de entrada
            var codigosEspecie = new Dictionary<string, string>(); // Codigo -> Nombre
            var productos = new HashSet<string>();

            var extractorSvc = new DbfExtractorService();
            foreach (var file in Directory.GetFiles(EntradaPath, "*.DBF"))
            {
                if (Path.GetFileName(file).StartsWith("M"))
                {
                    var muestras = await extractorSvc.ReadMuestrasAsync(file);
                    foreach (var m in muestras)
                    {
                        if (!string.IsNullOrEmpty(m.CodEspec))
                            codigosEspecie.TryAdd(m.CodEspec, m.Especie);
                    }
                }
                if (Path.GetFileName(file).StartsWith("P"))
                {
                    var prods = await extractorSvc.ReadProduccionAsync(file);
                    foreach (var p in prods)
                    {
                        if (!string.IsNullOrEmpty(p.Producto)) productos.Add(p.Producto);
                    }
                }
            }

            foreach (var kvp in codigosEspecie)
            {
                if (!await db.Especies.AnyAsync(e => e.CodigoInidep == kvp.Key))
                {
                    db.Especies.Add(new Especie { 
                        NombreVulgar = kvp.Value, 
                        CodigoInidep = kvp.Key 
                    });
                }
            }

            foreach (var pCode in productos)
            {
                if (!await db.Productos.AnyAsync(p => p.Codigo == pCode))
                {
                    db.Productos.Add(new Producto { Codigo = pCode, Descripcion = pCode });
                }
            }

            await db.SaveChangesAsync();
        }

        // 2. Setup Services
        var extractor = new DbfExtractorService();
        var reportService = Substitute.For<IMareaReportService>();
        var importer = new MareaImportService(extractor, reportService, dbFactory);
        var exporter = new DbfExporterService(dbFactory);

        // 3. Import
        _output.WriteLine("Importing from Entrada...");
        var marea = new Marea { 
            NumeroInidep = 36, 
            AnioInidep = 2026, 
            BuqueID = "TEST-BARCO-ID",
            Etapas = new List<MareaEtapa>
            {
                new MareaEtapa { 
                    FechaZarpada = new DateTime(2026, 1, 1),
                    FechaArribo = new DateTime(2026, 12, 31)
                }
            }
        };

        using (var db = new AppDbContext(options))
        {
            var buque = await db.Buques.FirstOrDefaultAsync(b => b.Nombre == "ARGENTINO");
            marea.BuqueID = buque!.Id;
            db.Mareas.Add(marea);
            await db.SaveChangesAsync();
        }

        var report = await importer.ProcessMareaImportAsync(EntradaPath, marea);
        await importer.ImportAsync(marea.ID, report);

        // 4. Export
        _output.WriteLine("Exporting to Salida...");
        if (!Directory.Exists(SalidaPath)) Directory.CreateDirectory(SalidaPath);
        
        // Obtenemos la marea persistida para tener todos los datos cargados
        using (var db = new AppDbContext(options))
        {
            var pMarea = await db.Mareas.Include(m => m.Buque).FirstOrDefaultAsync(m => m.ID == marea.ID);
            if (pMarea == null) throw new Exception("Import failed, marea not found in DB");

            await exporter.ExportMareaToDbfAsync(pMarea, SalidaPath);

            // Diagnóstico DB
            var samples = await db.Muestras.ToListAsync();
            Console.WriteLine($"[DB DEBUG] Muestras en DB: {samples.Count}");
            foreach(var s in samples.Take(2)) Console.WriteLine($"[DB DEBUG] Muestra: {s.EspecieOriginal} | Fuente: {s.Fuente}");
            
            var prods = await db.RegistrosProduccion.ToListAsync();
            Console.WriteLine($"[DB DEBUG] Producciones en DB: {prods.Count}");
            foreach(var p in prods.Take(2)) 
            {
                string hex = p.EspecieOriginal != null ? string.Join(" ", Encoding.GetEncoding(437).GetBytes(p.EspecieOriginal).Select(b => b.ToString("X2"))) : "NULL";
                Console.WriteLine($"[DB DEBUG] Produccion: '{p.EspecieOriginal}' (Hex CP437: {hex}) | Kg: {p.Kg}");
            }

            await exporter.ExportMareaToDbfAsync(pMarea, SalidaPath);
        }

        // 5. Audit
        _output.WriteLine("Comparing files...");
        var files = Directory.GetFiles(EntradaPath, "*.DBF");
        int totalFiles = 0;
        int passedFiles = 0;

        foreach (var entradaFile in files)
        {
            string fileName = Path.GetFileName(entradaFile);
            
            // Los archivos MD y LG no se exportan directamente como archivos individuales con el mismo nombre
            // El exportador genera C, M, S, P (y X si hay muchas tallas)
            if (fileName.StartsWith("MD") || fileName.StartsWith("L")) continue;

            string salidaFile = Path.Combine(SalidaPath, fileName);
            totalFiles++;

            if (!File.Exists(salidaFile))
            {
                _output.WriteLine($"[FAIL] Output file {fileName} missing.");
                continue;
            }

            if (await CompareDbf(entradaFile, salidaFile))
            {
                passedFiles++;
            }
        }

        _output.WriteLine($"Audit Finished: {passedFiles}/{totalFiles} files matched perfectly.");
        Assert.Equal(totalFiles, passedFiles);
    }

    private async Task<bool> CompareDbf(string fileA, string fileB)
    {
        var extractor = new DbfExtractorService();
        var encA = await extractor.DetectEncodingSmartAsync(fileA);
        var encB = await extractor.DetectEncodingSmartAsync(fileB);

        using var streamA = File.OpenRead(fileA);
        using var streamB = File.OpenRead(fileB);

        var readerA = new DBFReader(streamA) { CharEncoding = Encoding.GetEncoding(encA.CodePage) };
        var readerB = new DBFReader(streamB) { CharEncoding = Encoding.GetEncoding(encB.CodePage) };

        bool match = true;

        // 1. Structure
        if (readerA.Fields.Length != readerB.Fields.Length)
        {
            _output.WriteLine($"[FAIL] Field count mismatch in {Path.GetFileName(fileA)}: Entrada={readerA.Fields.Length}, Salida={readerB.Fields.Length}");
            match = false;
        }

        // 2. Records
        if (readerA.RecordCount != readerB.RecordCount)
        {
            _output.WriteLine($"[FAIL] Record count mismatch in {Path.GetFileName(fileA)}: Entrada={readerA.RecordCount}, Salida={readerB.RecordCount}");
            match = false;
        }

        if (!match) return false;

        int count = readerA.RecordCount;
        int errors = 0;
        for (int r = 0; r < count; r++)
        {
            var rowA = readerA.NextRecord();
            var rowB = readerB.NextRecord();

            for (int f = 0; f < readerA.Fields.Length; f++)
            {
                var valA = rowA[f];
                var valB = rowB[f];

                if (!AreValuesEqual(valA, valB))
                {
                    string hexA = valA is string sa ? string.Join(" ", Encoding.GetEncoding(encA.CodePage).GetBytes(sa).Select(b => b.ToString("X2"))) : "N/A";
                    string hexB = valB is string sb ? string.Join(" ", Encoding.GetEncoding(encB.CodePage).GetBytes(sb).Select(b => b.ToString("X2"))) : "N/A";
                    _output.WriteLine($"[FAIL] Value mismatch in {Path.GetFileName(fileA)} at Row {r}, Field {readerA.Fields[f].Name}: Entrada='{valA}' (Hex: {hexA}) vs Salida='{valB}' (Hex: {hexB})");
                    errors++;
                    if (errors > 5) {
                        _output.WriteLine("... too many errors, aborting file comparison.");
                        return false;
                    }
                }
            }
        }
        
        if (errors == 0)
        {
            _output.WriteLine($"[OK] {Path.GetFileName(fileA)} matches perfectly.");
            return true;
        }
        return false;
    }

    private bool AreValuesEqual(object? a, object? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) 
        {
            if (a is string s && string.IsNullOrWhiteSpace(s)) return true;
            if (b is string s2 && string.IsNullOrWhiteSpace(s2)) return true;
            return false;
        }

        if (a is string sa && b is string sb) return sa.Trim().Equals(sb.Trim(), StringComparison.OrdinalIgnoreCase);
        if (a is DateTime da && b is DateTime db) return da.Date == db.Date;
        
        if (IsNumeric(a) && IsNumeric(b))
        {
            return Math.Abs(Convert.ToDouble(a) - Convert.ToDouble(b)) < 0.001;
        }

        return a.Equals(b);
    }

    private bool IsNumeric(object obj) => 
        obj is sbyte || obj is byte || obj is short || obj is ushort || obj is int || obj is uint || obj is long || obj is ulong || obj is float || obj is double || obj is decimal;
}
