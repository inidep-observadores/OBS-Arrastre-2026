using System.IO;
using System.Text.Json;
using DbfDataReader;

namespace OBSArrastre2026.App.Services;

public sealed class DbfExtractorService : IDbfExtractorService
{
    public async Task ExtractBuquesAsync(string dbfPath, string jsonOutputPath)
    {
        var records = new List<Dictionary<string, object?>>();

        using (var dbfReader = new DbfDataReader.DbfDataReader(dbfPath))
        {
            var columns = dbfReader.DbfTable.Columns;

            while (dbfReader.Read())
            {
                var record = new Dictionary<string, object?>();
                for (int i = 0; i < columns.Count; i++)
                {
                    var column = columns[i];
                    var value = dbfReader.GetValue(i);
                    var mappedName = MapBuqueColumn(column.ColumnName);
                    if (mappedName != null)
                    {
                        record[mappedName] = CleanValue(value);
                    }
                }
                records.Add(record);
            }
        }

        var json = JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(jsonOutputPath, json);
    }

    public async Task ExtractEspeciesAsync(string dbfPath, string jsonOutputPath)
    {
        var records = new List<Dictionary<string, object?>>();

        using (var dbfReader = new DbfDataReader.DbfDataReader(dbfPath))
        {
            var columns = dbfReader.DbfTable.Columns;

            while (dbfReader.Read())
            {
                var record = new Dictionary<string, object?>();
                for (int i = 0; i < columns.Count; i++)
                {
                    var column = columns[i];
                    var value = dbfReader.GetValue(i);
                    var mappedName = MapEspecieColumn(column.ColumnName);
                    if (mappedName != null)
                    {
                        var cleanedValue = CleanValue(value);
                        
                        // Conversión especial para Frecuente (bool)
                        if (mappedName == "Frecuente")
                        {
                            record[mappedName] = IsTrueValue(cleanedValue);
                        }
                        else
                        {
                            record[mappedName] = cleanedValue;
                        }
                    }
                }
                records.Add(record);
            }
        }

        var json = JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(jsonOutputPath, json);
    }

    private string? MapBuqueColumn(string dbfName)
    {
        return dbfName.ToUpper() switch
        {
            "ID" => "ID",
            "NOMBRE" => "Nombre",
            "MATRICULA" => "Matricula",
            "MATRIC" => "Matricula",
            "RADIAL" => "IdRadial",
            "ID_RADIAL" => "IdRadial",
            "IMO" => "IMO",
            "MMSI" => "MMSI",
            _ => null
        };
    }

    private string? MapEspecieColumn(string dbfName)
    {
        return dbfName.ToUpper() switch
        {
            "ID" => "ID",
            "COD_INIDEP" => "CodigoInidep",
            "COD" => "CodigoInidep",
            "DOC_INFO" => "DocumentoInformativo",
            "DOC" => "DocumentoInformativo",
            "ESPECIFICO" => "Especifico",
            "FAMILIA" => "Familia",
            "FRECUENTE" => "Frecuente",
            "GENERO" => "Genero",
            "NOM_CIENT" => "NombreCientifico",
            "NOM_VULGAR" => "NombreVulgar",
            "ORDEN" => "Orden",
            _ => null
        };
    }

    private object? CleanValue(object? value)
    {
        if (value is string s) return s.Trim();
        return value;
    }

    private bool IsTrueValue(object? value)
    {
        if (value == null) return false;
        if (value is bool b) return b;
        if (value is int i) return i != 0;
        if (value is short s) return s != 0;
        if (value is string str)
        {
            var normalized = str.Trim().ToUpper();
            return normalized == "T" || normalized == "S" || normalized == "1" || normalized == "TRUE";
        }
        return false;
    }
}
