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
            var nombre = item.GetProperty("Nombre").GetString();
            var matricula = item.GetProperty("Matricula").GetRawText(); // Handle as raw to avoid parsing issues if it's float in JSON
            var idRadial = item.GetProperty("IdRadial").GetRawText();

            if (string.IsNullOrEmpty(nombre)) continue;

            int.TryParse(matricula, out int nMatricula);
            int.TryParse(idRadial, out int nIdRadial);

            // Búsqueda jerárquica para Upsert
            var existing = await context.Buques
                .FirstOrDefaultAsync(b => b.Nombre == nombre 
                                          || (nMatricula != 0 && b.Matricula == nMatricula) 
                                          || (nIdRadial != 0 && b.IdRadial == nIdRadial));

            if (existing != null)
            {
                // Update
                existing.Nombre = nombre;
                if (nMatricula != 0) existing.Matricula = nMatricula;
                if (nIdRadial != 0) existing.IdRadial = nIdRadial;
                if (item.TryGetProperty("IMO", out var imoProp) && imoProp.ValueKind == JsonValueKind.Number) existing.IMO = imoProp.GetInt32();
                if (item.TryGetProperty("MMSI", out var mmsiProp) && mmsiProp.ValueKind == JsonValueKind.Number) existing.MMSI = mmsiProp.GetInt32();
            }
            else
            {
                // Insert
                context.Buques.Add(new Buque
                {
                    Id = Guid.NewGuid(),
                    Nombre = nombre,
                    Matricula = nMatricula,
                    IdRadial = nIdRadial,
                    IMO = item.TryGetProperty("IMO", out var i) && i.ValueKind == JsonValueKind.Number ? i.GetInt32() : null,
                    MMSI = item.TryGetProperty("MMSI", out var m) && m.ValueKind == JsonValueKind.Number ? m.GetInt32() : null
                });
            }
        }

        await context.SaveChangesAsync();
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
            if (!item.TryGetProperty("CodigoInidep", out var codProp)) continue;
            var codigo = codProp.GetString();
            if (string.IsNullOrEmpty(codigo)) continue;

            var existing = await context.Especies.FirstOrDefaultAsync(e => e.CodigoInidep == codigo);

            if (existing != null)
            {
                // Update
                UpdateEspecie(existing, item);
            }
            else
            {
                // Insert
                var nuevo = new Especie { CodigoInidep = codigo };
                UpdateEspecie(nuevo, item);
                context.Especies.Add(nuevo);
            }
        }

        await context.SaveChangesAsync();
    }

    private void UpdateEspecie(Especie target, JsonElement source)
    {
        if (source.TryGetProperty("NombreVulgar", out var nv)) target.NombreVulgar = nv.GetString();
        if (source.TryGetProperty("NombreCientifico", out var nc)) target.NombreCientifico = nc.GetString();
        if (source.TryGetProperty("Familia", out var f)) target.Familia = f.GetString();
        if (source.TryGetProperty("Genero", out var g)) target.Genero = g.GetString();
        if (source.TryGetProperty("Especifico", out var e)) target.Especifico = e.GetString();
        if (source.TryGetProperty("Orden", out var o)) target.Orden = o.GetString();
        if (source.TryGetProperty("DocumentoInformativo", out var d)) target.DocumentoInformativo = d.GetString();
        if (source.TryGetProperty("Frecuente", out var fr)) target.Frecuente = fr.GetBoolean();
    }
}
