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

    public async Task<bool> IsEspeciesViejasEmptyAsync()
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        return !await context.EspeciesViejas.AnyAsync();
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

    public async Task ImportEspeciesViejasAsync(string jsonPath)
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

            var existing = await context.EspeciesViejas.FirstOrDefaultAsync(e => e.CodigoInidep == codigo);
            
            if (existing == null)
            {
                existing = context.EspeciesViejas.Local.FirstOrDefault(e => e.CodigoInidep == codigo);
            }

            if (existing != null)
            {
                UpdateEspecieVieja(existing, item);
            }
            else
            {
                var nuevo = new EspecieVieja { CodigoInidep = codigo };
                UpdateEspecieVieja(nuevo, item);
                context.EspeciesViejas.Add(nuevo);
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

    private void UpdateEspecieVieja(EspecieVieja target, JsonElement source)
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

    public async Task<(int Imported, int Ignored)> ImportMareasAsync(string[] filePaths)
    {
        int imported = 0;
        int ignored = 0;

        using var context = await _dbContextFactory.CreateDbContextAsync();

        foreach (var path in filePaths)
        {
            if (!File.Exists(path)) { ignored++; continue; }

            try
            {
                var json = await File.ReadAllTextAsync(path);
                var mareaDto = JsonSerializer.Deserialize<PortableMareaDto>(json);
                if (mareaDto == null) { ignored++; continue; }

                // Buscar buque por nombre
                var buque = await context.Buques.FirstOrDefaultAsync(b => b.Nombre == mareaDto.BuqueNombre);
                if (buque == null) { ignored++; continue; }

                // Verificar duplicado
                bool exists = await context.Mareas.AnyAsync(m => 
                    m.BuqueID == buque.Id && 
                    m.AnioInidep == mareaDto.Anio && 
                    m.NumeroInidep == mareaDto.Numero);

                if (exists) { ignored++; continue; }

                // Crear nueva marea
                var marea = new Marea
                {
                    ID = Guid.NewGuid().ToString(),
                    BuqueID = buque.Id,
                    AnioInidep = mareaDto.Anio,
                    NumeroInidep = mareaDto.Numero,
                    Comentarios = mareaDto.Comentarios,
                    FechaInicio = mareaDto.FechaInicio,
                    FechaFin = mareaDto.FechaFin,
                    BuqueCodigo = mareaDto.BuqueCodigo,
                    ObservadorNombre = mareaDto.ObservadorNombre,
                    ObservadorApellido = mareaDto.ObservadorApellido,
                    ObservadorCodigo = mareaDto.ObservadorCodigo
                };

                foreach (var eDto in mareaDto.Etapas)
                {
                    marea.Etapas.Add(new MareaEtapa
                    {
                        ID = Guid.NewGuid().ToString(),
                        FechaZarpada = eDto.FechaZarpada,
                        FechaArribo = eDto.FechaArribo,
                        NombreCapitan = eDto.NombreCapitan,
                        AnioMareaBuque = eDto.AnioMareaBuque,
                        NumeroMareaBuque = eDto.NumeroMareaBuque
                    });
                }

                context.Mareas.Add(marea);
                imported++;
            }
            catch
            {
                ignored++;
            }
        }

        if (imported > 0)
        {
            await context.SaveChangesAsync();
        }

        return (imported, ignored);
    }

    public async Task UpdateMareaMetadataAsync(string mareaId, string jsonPath)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        var marea = await context.Mareas.Include(m => m.Etapas).FirstOrDefaultAsync(m => m.ID == mareaId);
        if (marea == null) return;

        var json = await File.ReadAllTextAsync(jsonPath);
        var dto = JsonSerializer.Deserialize<PortableMareaDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (dto == null) return;

        // Actualizar buque si coincide el nombre
        var buque = await context.Buques.FirstOrDefaultAsync(b => b.Nombre == dto.BuqueNombre);
        if (buque != null) marea.BuqueID = buque.Id;

        marea.AnioInidep = dto.Anio;
        marea.NumeroInidep = dto.Numero;
        marea.Comentarios = dto.Comentarios;
        marea.FechaInicio = dto.FechaInicio;
        marea.FechaFin = dto.FechaFin;
        marea.BuqueCodigo = dto.BuqueCodigo;
        marea.ObservadorNombre = dto.ObservadorNombre;
        marea.ObservadorApellido = dto.ObservadorApellido;
        marea.ObservadorCodigo = dto.ObservadorCodigo;

        // Reemplazar etapas (Borrado físico seguido de inserción)
        context.MareaEtapas.RemoveRange(marea.Etapas);
        marea.Etapas.Clear();

        foreach (var eDto in dto.Etapas)
        {
            marea.Etapas.Add(new MareaEtapa
            {
                ID = Guid.NewGuid().ToString(),
                MareaID = marea.ID,
                FechaZarpada = eDto.FechaZarpada,
                FechaArribo = eDto.FechaArribo,
                NombreCapitan = eDto.NombreCapitan,
                AnioMareaBuque = eDto.AnioMareaBuque,
                NumeroMareaBuque = eDto.NumeroMareaBuque
            });
        }

        await context.SaveChangesAsync();
    }

    private class PortableMareaDto
    {
        public string BuqueNombre { get; set; } = string.Empty;
        public int Anio { get; set; }
        public int Numero { get; set; }
        public string? Comentarios { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public int? BuqueCodigo { get; set; }
        public string? ObservadorNombre { get; set; }
        public string? ObservadorApellido { get; set; }
        public int? ObservadorCodigo { get; set; }
        public List<PortableEtapaDto> Etapas { get; set; } = new();
    }

    private class PortableEtapaDto
    {
        public DateTime FechaZarpada { get; set; }
        public DateTime? FechaArribo { get; set; }
        public string? NombreCapitan { get; set; }
        public int? AnioMareaBuque { get; set; }
        public int? NumeroMareaBuque { get; set; }
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
