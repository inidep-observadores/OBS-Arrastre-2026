using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ControlMareas.App.Data;
using ControlMareas.App.Data.Entities;

// Ruta dinámica basada en LocalApplicationData
var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
var dbPath = Path.Combine(localAppData, "ControlMareas", "control-mareas.db");
var outputDir = @"c:\Users\danieldt\Documents\Desarrollo\.net\WPF\ControlMareas\Export";

Console.WriteLine($"Buscando base de datos en: {dbPath}");

if (!File.Exists(dbPath))
{
    Console.WriteLine($"Error: No se encontró la base de datos en {dbPath}");
    return;
}

if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
optionsBuilder.UseSqlite($"Data Source={dbPath}");

using var db = new AppDbContext(optionsBuilder.Options);

var mareasRaw = await db.Mareas
    .Include(m => m.Buque)
    .Include(m => m.Etapas)
    .ToListAsync();

int count = 0;
foreach (var m in mareasRaw)
{
    var portableMarea = new {
        BuqueNombre = m.Buque?.Nombre ?? "Sin Nombre",
        Anio = m.AnioInidep,
        Numero = m.NumeroInidep,
        Comentarios = m.Comentarios,
        FechaInicio = m.FechaInicio,
        FechaFin = m.FechaFin,
        Etapas = m.Etapas.Select(e => new {
            e.FechaZarpada,
            e.FechaArribo,
            e.NombreCapitan,
            e.AnioMareaBuque,
            e.NumeroMareaBuque
        }).ToList()
    };

    string fileName = $"M{m.NumeroInidep}{m.AnioInidep % 100:D2}.json";
    string fullPath = Path.Combine(outputDir, fileName);
    
    var json = JsonSerializer.Serialize(portableMarea, new JsonSerializerOptions { WriteIndented = true });
    await File.WriteAllTextAsync(fullPath, json);
    count++;
}

Console.WriteLine($"Exportación exitosa: {count} archivos generados en {outputDir}");
