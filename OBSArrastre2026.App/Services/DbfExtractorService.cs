using System.IO;
using System.Text;
using System.Text.Json;
using DbfDataReader;
using OBSArrastre2026.App.Models.Import;
using OBSArrastre2026.App.Services.Internal;

namespace OBSArrastre2026.App.Services;

public sealed class DbfExtractorService : IDbfExtractorService
{
    private readonly DbfDataReaderOptions _dbfOptions;

    public DbfExtractorService()
    {
        // El catálogo original de Clipper no tiene marca de CodePage en el header.
        // Forzamos Windows-1252 (ANSI) ya que es el que resuelve los acentos correctamente según FoxPro.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _dbfOptions = new DbfDataReaderOptions { Encoding = Encoding.GetEncoding(1252) };
    }
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

    public async Task<List<LegacyCaptura>> ReadCapturasAsync(string dbfPath)
    {
        var list = new List<LegacyCaptura>();
        if (!File.Exists(dbfPath)) return list;

        using var reader = new DbfDataReader.DbfDataReader(dbfPath, _dbfOptions);
        var colMap = GetColumnMap(reader);

        while (reader.Read())
        {
            var c = new LegacyCaptura
            {
                Barco = GetString(reader, colMap, "BARCO"),
                Marea = GetDouble(reader, colMap, "MAREA"),
                Lance = GetDouble(reader, colMap, "LANCE"),
                Fecha = GetDateTime(reader, colMap, "FECHA") ?? DateTime.MinValue,
                HoraInic = GetDouble(reader, colMap, "HORA_INIC"),
                HoraFinal = GetDouble(reader, colMap, "HORA_FINAL"),
                LatInic = GetDouble(reader, colMap, "LAT_INIC"),
                LongInic = GetDouble(reader, colMap, "LONG_INIC"),
                LatFinal = GetDouble(reader, colMap, "LAT_FINAL"),
                LongFinal = GetDouble(reader, colMap, "LONG_FINAL"),
                ProfInic = GetDouble(reader, colMap, "PROF_INIC"),
                ProfFinal = GetDouble(reader, colMap, "PROF_FINAL"),
                CaptTotal = GetDouble(reader, colMap, "CAPT_TOTAL"),
                Descarte = GetDouble(reader, colMap, "DESCARTE")
            };

            for (int i = 1; i <= 25; i++)
            {
                var espCode = GetDouble(reader, colMap, $"ESPECIE_{i}");
                if (espCode > 0)
                {
                    c.Especies[(long)espCode] = GetDouble(reader, colMap, $"KG_{i}");
                    c.DescartesPorEspecie[(long)espCode] = GetDouble(reader, colMap, $"DESCAR_{i}");
                }
            }
            list.Add(c);
        }
        return list;
    }

    public async Task<List<LegacyMuestra>> ReadMuestrasAsync(string dbfPath)
    {
        var list = new List<LegacyMuestra>();
        if (!File.Exists(dbfPath)) return list;

        using var reader = new DbfDataReader.DbfDataReader(dbfPath, _dbfOptions);
        var colMap = GetColumnMap(reader);

        while (reader.Read())
        {
            var m = new LegacyMuestra
            {
                Barco = GetString(reader, colMap, "BARCO"),
                Marea = GetDouble(reader, colMap, "MAREA"),
                Lance = GetDouble(reader, colMap, "LANCE"),
                Fecha = GetDateTime(reader, colMap, "FECHA") ?? DateTime.MinValue,
                Especie = GetString(reader, colMap, "ESPECIE"),
                CodEspec = (long)GetDouble(reader, colMap, "COD_ESPEC"),
                Area = GetDouble(reader, colMap, "AREA"),
                PrimTalla = (int)GetDouble(reader, colMap, "PRIM_TALLA"),
                UltTalla = (int)GetDouble(reader, colMap, "ULT_TALLA"),
                Intervalo = (int)GetDouble(reader, colMap, "INTERVALO"),
                PesoMues = GetDouble(reader, colMap, "PESO_MUES"),
                FactPond = GetDouble(reader, colMap, "FACT_POND")
            };

            for (int i = 1; i <= 90; i++)
            {
                var val = GetValue(reader, colMap, $"TALLA_{i}");
                if (val != null && val.ToString() != "0")
                {
                    m.Tallies.Add(LegacyDecoder.DecodeTally(val));
                }
            }
            list.Add(m);
        }
        return list;
    }

    public async Task<List<LegacySubmuestra>> ReadSubmuestrasAsync(string dbfPath)
    {
        var list = new List<LegacySubmuestra>();
        if (!File.Exists(dbfPath)) return list;

        using var reader = new DbfDataReader.DbfDataReader(dbfPath, _dbfOptions);
        var colMap = GetColumnMap(reader);

        while (reader.Read())
        {
            list.Add(new LegacySubmuestra
            {
                Barco = GetString(reader, colMap, "BARCO"),
                Marea = GetDouble(reader, colMap, "MAREA"),
                Lance = GetDouble(reader, colMap, "LANCE"),
                Fecha = GetDateTime(reader, colMap, "FECHA") ?? DateTime.MinValue,
                Especie = GetString(reader, colMap, "ESPECIE"),
                NEjemplar = (int)GetDouble(reader, colMap, "NEJEMPLAR"),
                LargoTot = (int)GetDouble(reader, colMap, "LARGO_TOT"),
                LargoSta = (int)GetDouble(reader, colMap, "LARGO_STA"),
                PesoTot = GetDouble(reader, colMap, "PESO_TOT"),
                Sexo = (int)GetDouble(reader, colMap, "SEXO"),
                Estadio = (int)GetDouble(reader, colMap, "ESTADIO")
            });
        }
        return list;
    }

    public async Task<List<LegacyLg>> ReadLgAsync(string dbfPath)
    {
        var list = new List<LegacyLg>();
        if (!File.Exists(dbfPath)) return list;

        using var reader = new DbfDataReader.DbfDataReader(dbfPath, _dbfOptions);
        var colMap = GetColumnMap(reader);

        while (reader.Read())
        {
            var lg = new LegacyLg
            {
                Barco = GetString(reader, colMap, "BARCO"),
                Marea = GetDouble(reader, colMap, "MAREA"),
                Lance = GetDouble(reader, colMap, "LANCE"),
                Fecha = GetDateTime(reader, colMap, "FECHA") ?? DateTime.MinValue,
                CodEspecIE = (long)GetDouble(reader, colMap, "CODIGO"),
            };

            for (int i = 1; i <= 70; i++)
            {
                var val = GetDouble(reader, colMap, $"TALLA_{i}");
                if (val > 0)
                {
                    lg.Frecuencias[i] = val;
                }
            }
            list.Add(lg);
        }
        return list;
    }

    public async Task<List<LegacyTracking>> ReadTrackingAsync(string dbfPath)
    {
        var list = new List<LegacyTracking>();
        if (!File.Exists(dbfPath)) return list;

        using var reader = new DbfDataReader.DbfDataReader(dbfPath, _dbfOptions);
        var colMap = GetColumnMap(reader);

        while (reader.Read())
        {
            list.Add(new LegacyTracking
            {
                Buque = GetString(reader, colMap, "BUQUE"),
                Matricula = GetString(reader, colMap, "MATRICULA"),
                FechaStr = GetString(reader, colMap, "FECHA"),
                Latitud = GetDouble(reader, colMap, "LATITUD"),
                Longitud = GetDouble(reader, colMap, "LONGITUD"),
                Velocidad = GetDouble(reader, colMap, "VELOCIDAD"),
                Rumbo = GetDouble(reader, colMap, "RUMBO")
            });
        }
        return list;
    }

    public async Task<List<LegacyProduccion>> ReadProduccionAsync(string dbfPath)
    {
        var list = new List<LegacyProduccion>();
        if (!File.Exists(dbfPath)) return list;

        using var reader = new DbfDataReader.DbfDataReader(dbfPath, _dbfOptions);
        var colMap = GetColumnMap(reader);

        while (reader.Read())
        {
            list.Add(new LegacyProduccion
            {
                Barco = GetString(reader, colMap, "BARCO"),
                Marea = GetDouble(reader, colMap, "MAREA"),
                Fecha = GetDateTime(reader, colMap, "FECHA") ?? DateTime.MinValue,
                Especie = GetString(reader, colMap, "ESPECIE"),
                Producto = GetString(reader, colMap, "PRODUCTO"),
                Categoria = GetString(reader, colMap, "CATEGORIA"),
                Operarios = (int)GetDouble(reader, colMap, "OPERARIOS"),
                Factor = GetDouble(reader, colMap, "FACTOR"),
                Kilos = GetDouble(reader, colMap, "KILOS")
            });
        }
        return list;
    }

    private Dictionary<string, int> GetColumnMap(DbfDataReader.DbfDataReader reader)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var columns = reader.DbfTable.Columns;
        for (int i = 0; i < columns.Count; i++)
        {
            map[columns[i].ColumnName] = i;
        }
        return map;
    }

    private string GetString(DbfDataReader.DbfDataReader reader, Dictionary<string, int> map, string name)
    {
        if (map.TryGetValue(name, out int index))
        {
            var value = reader.GetValue(index);
            return value?.ToString()?.Trim() ?? "";
        }
        return "";
    }

    private double GetDouble(DbfDataReader.DbfDataReader reader, Dictionary<string, int> map, string name)
    {
        if (map.TryGetValue(name, out int index))
        {
            var value = reader.GetValue(index);
            if (value == null || value is DBNull) return 0;
            
            try 
            {
                return Convert.ToDouble(value);
            }
            catch 
            {
                return 0;
            }
        }
        return 0;
    }

    private DateTime? GetDateTime(DbfDataReader.DbfDataReader reader, Dictionary<string, int> map, string name)
    {
        if (map.TryGetValue(name, out int index))
        {
            var value = reader.GetValue(index);
            if (value == null || value is DBNull) return null;

            try 
            {
                return Convert.ToDateTime(value);
            }
            catch 
            {
                return null;
            }
        }
        return null;
    }

    private object? GetValue(DbfDataReader.DbfDataReader reader, Dictionary<string, int> map, string name)
    {
        if (map.TryGetValue(name, out int index)) return reader.GetValue(index);
        return null;
    }

    private string? MapBuqueColumn(string dbfName)
    {
        return dbfName.Trim().ToUpper() switch
        {
            "ID" => "ID",
            "BARCO" => "ID",
            "NOMBRE" => "Nombre",
            "NBRE_BQE" => "Nombre",
            "MATRICULA" => "Matricula",
            "MATRIC" => "Matricula",
            "MATR_BQE" => "Matricula",
            "RADIAL" => "IdRadial",
            "ID_RADIAL" => "IdRadial",
            "RIP" => "IdRadial",
            "IMO" => "IMO",
            "MMSI" => "MMSI",
            _ => null
        };
    }

    private string? MapEspecieColumn(string dbfName)
    {
        return dbfName.Trim().ToUpper() switch
        {
            "ID" => "ID",
            "COD_INIDEP" => "CodigoInidep",
            "COD" => "CodigoInidep",
            "CODINIDEP" => "CodigoInidep",
            "DOC_INFO" => "DocumentoInformativo",
            "DOC" => "DocumentoInformativo",
            "ESPECIFICO" => "Especifico",
            "FAMILIA" => "Familia",
            "FRECUENTE" => "Frecuente",
            "GENERO" => "Genero",
            "NOM_CIENT" => "NombreCientifico",
            "NOMCIENT" => "NombreCientifico",
            "NOM_VULGAR" => "NombreVulgar",
            "NOMVULCAS" => "NombreVulgar",
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
