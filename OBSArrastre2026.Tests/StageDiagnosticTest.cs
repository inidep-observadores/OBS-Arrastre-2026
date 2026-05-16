using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OBSArrastre2026.App.Data;
using OBSArrastre2026.App.Data.Entities;
using Xunit;
using Xunit.Abstractions;

namespace OBSArrastre2026.Tests;

public class StageDiagnosticTest
{
    private readonly ITestOutputHelper _output;

    public StageDiagnosticTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task DiagnoseMuestraStages()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dbPath = Path.Combine(localAppData, "OBSArrastre2026", "obs-arrastre-2026.db");
        var logPath = Path.Combine(localAppData, "OBSArrastre2026", "diagnostic_log.txt");
        
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        using var db = new AppDbContext(options);

        var lances = await db.Lances.ToListAsync();
        using var sw = new StreamWriter(logPath, false);
        sw.WriteLine($"Total lances: {lances.Count}");

        sw.WriteLine("\n--- Simulando carga por lance (como en ViewModel) ---");
        var simulatedSamples = new List<Muestra>();
        foreach (var lance in lances.Take(20))
        {
            using var dbIter = new AppDbContext(options);
            var lanceSamples = await dbIter.Muestras
                .Include(m => m.Lance)
                    .ThenInclude(l => l.MareaEtapa)
                        .ThenInclude(e => e.Marea)
                            .ThenInclude(m => m.Etapas)
                .Where(m => m.LanceID == lance.Id)
                .ToListAsync();
            
            simulatedSamples.AddRange(lanceSamples);
        }

        foreach (var s in simulatedSamples)
        {
            var num = s.Lance?.MareaEtapa?.NumeroEtapa;
            var etapasCount = s.Lance?.MareaEtapa?.Marea?.Etapas?.Count ?? 0;
            sw.WriteLine($"Muestra ID: {s.ID} | Lance: {s.Lance?.NroLance} | Etapa Num: {num} (Total Etapas en Marea: {etapasCount})");
        }
    }
}
