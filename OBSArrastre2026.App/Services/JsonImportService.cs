using System.IO;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OBSArrastre2026.App.Data;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.Services;

public sealed class JsonImportService : IJsonImportService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public JsonImportService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task ImportBuquesAsync(string jsonPath)
    {
        if (!File.Exists(jsonPath)) return;

        var json = await File.ReadAllTextAsync(jsonPath);
        var sourceItems = JsonSerializer.Deserialize<List<JsonElement>>(json);
        if (sourceItems == null) return;

        using var context = await _dbContextFactory.CreateDbContextAsync();

        foreach (var item in sourceItems)
        {
            var nombre = GetStringValue(item, "Nombre");
            var matriculaStr = GetStringValue(item, "Matricula");
            var idRadialStr = GetStringValue(item, "IdRadial");

            if (string.IsNullOrWhiteSpace(nombre)) continue;

            int.TryParse(matriculaStr, out int nMatricula);
            int.TryParse(idRadialStr, out int nIdRadial);

            // Búsqueda secuencial (Priority: Nombre > Matricula > IdRadial)
            // Primero buscamos en la base de datos
            Buque? existing = await context.Buques.FirstOrDefaultAsync(b => b.Nombre == nombre);
            
            if (existing == null && nMatricula != 0)
                existing = await context.Buques.FirstOrDefaultAsync(b => b.Matricula == nMatricula);

            if (existing == null && nIdRadial != 0)
                existing = await context.Buques.FirstOrDefaultAsync(b => b.IdRadial == nIdRadial);

            // Si no está en DB, buscamos en el ChangeTracker por si ya añadimos uno con el mismo nombre en este ciclo
            if (existing == null)
            {
                existing = context.Buques.Local.FirstOrDefault(b => b.Nombre == nombre);
            }

            if (existing != null)
            {
                // Update
                existing.Nombre = nombre;
                if (nMatricula != 0) existing.Matricula = nMatricula;
                if (nIdRadial != 0) existing.IdRadial = nIdRadial;
                
                if (item.TryGetProperty("IMO", out var imoProp) && imoProp.ValueKind == JsonValueKind.Number) 
                    existing.IMO = imoProp.GetInt32();
                
                if (item.TryGetProperty("MMSI", out var mmsiProp) && mmsiProp.ValueKind == JsonValueKind.Number) 
                    existing.MMSI = mmsiProp.GetInt32();
            }
            else
            {
                // Insert
                context.Buques.Add(new Buque
                {
                    Id = Guid.NewGuid().ToString(),
                    Nombre = nombre,
                    Matricula = nMatricula,
                    IdRadial = nIdRadial,
                    IMO = item.TryGetProperty("IMO", out var i) && i.ValueKind == JsonValueKind.Number ? i.GetInt32() : null,
                    MMSI = item.TryGetProperty("MMSI", out var ms) && ms.ValueKind == JsonValueKind.Number ? ms.GetInt32() : null
                });
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task<bool> IsBuquesEmptyAsync()
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        return !await context.Buques.AnyAsync();
    }

    public async Task<bool> IsEspeciesEmptyAsync()
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        return !await context.Especies.AnyAsync();
    }

    public async Task ImportEspeciesAsync(string jsonPath)
    {
        if (!File.Exists(jsonPath)) return;

        var json = await File.ReadAllTextAsync(jsonPath);
        var sourceItems = JsonSerializer.Deserialize<List<JsonElement>>(json);
        if (sourceItems == null) return;

        using var context = await _dbContextFactory.CreateDbContextAsync();

        foreach (var item in sourceItems)
        {
            var codigo = GetStringValue(item, "CodigoInidep");
            if (string.IsNullOrEmpty(codigo)) continue;

            // Buscamos en DB
            var existing = await context.Especies.FirstOrDefaultAsync(e => e.CodigoInidep == codigo);
            
            // Si no está en DB, buscamos en la memoria local del contexto (ChangeTracker) 
            // por si el DBF tiene duplicados
            if (existing == null)
            {
                existing = context.Especies.Local.FirstOrDefault(e => e.CodigoInidep == codigo);
            }

            if (existing != null)
            {
                UpdateEspecie(existing, item);
            }
            else
            {
                var nuevo = new Especie { CodigoInidep = codigo };
                UpdateEspecie(nuevo, item);
                context.Especies.Add(nuevo);
            }
        }

        await context.SaveChangesAsync();
    }

    private void UpdateEspecie(Especie target, JsonElement source)
    {
        target.NombreVulgar = GetStringValue(source, "NombreVulgar");
        target.NombreCientifico = GetStringValue(source, "NombreCientifico");
        target.Familia = GetStringValue(source, "Familia");
        target.Genero = GetStringValue(source, "Genero");
        target.Especifico = GetStringValue(source, "Especifico");
        target.Orden = GetStringValue(source, "Orden");
        target.DocumentoInformativo = GetStringValue(source, "DocumentoInformativo");
        
        if (source.TryGetProperty("Frecuente", out var fr) && fr.ValueKind != JsonValueKind.Null) 
            target.Frecuente = fr.GetBoolean();
    }

    private string? GetStringValue(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var prop) || prop.ValueKind == JsonValueKind.Null)
            return null;

        if (prop.ValueKind == JsonValueKind.String)
            return prop.GetString();

        if (prop.ValueKind == JsonValueKind.Number)
            return prop.GetRawText();

        return null;
    }
}
