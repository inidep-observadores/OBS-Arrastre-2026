using System.IO;
using System.Text;
using System.Text.Json;
using DbfDataReader;
using OBSArrastre2026.App.Models.Import;
using OBSArrastre2026.App.Services.Internal;

namespace OBSArrastre2026.App.Services;

public sealed class DbfExtractorService : IDbfExtractorService
{
    public DbfExtractorService()
    {
        // Asegurar soporte para codificaciones legacy en cualquier contexto (incluyendo tests)
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    private DbfDataReaderOptions GetOptions(string dbfPath)
    {
        var encoding = DetectEncoding(dbfPath);
        return new DbfDataReaderOptions { Encoding = encoding };
    }

    private Encoding DetectEncoding(string dbfPath)
    {
        try
        {
            if (!File.Exists(dbfPath)) return Encoding.GetEncoding(1252);

            using (var stream = File.OpenRead(dbfPath))
            {
                if (stream.Length < 30) return Encoding.GetEncoding(1252);

                stream.Position = 29;
                int cpByte = stream.ReadByte();

                // Mapeo de CodePage de DBF
                // Ref: https://www.dbf2002.com/dbf-file-format.html
                return cpByte switch
                {
                    0x01 => Encoding.GetEncoding(437), // DOS USA
                    0x02 => Encoding.GetEncoding(850), // DOS Multilingual
                    0x03 => Encoding.GetEncoding(1252), // Windows ANSI
                    0x08 => Encoding.GetEncoding(865), // DOS Nordic
                    0x0A => Encoding.GetEncoding(850), // DOS Multilingual
                    0x0D => Encoding.GetEncoding(437), // DOS USA
                    0x14 => Encoding.GetEncoding(850), // DOS Multilingual (Clipper/dBase IV)
                    0x21 => Encoding.GetEncoding(1252), // Windows ANSI
                    0x57 => Encoding.GetEncoding(1252), // Windows ANSI (FoxPro)
                    0x58 => Encoding.GetEncoding(1252), // Windows ANSI (FoxPro)
                    0x59 => Encoding.GetEncoding(1252), // Windows ANSI (FoxPro)
                    0x64 => Encoding.GetEncoding(852), // DOS Eastern Europe
                    0x65 => Encoding.GetEncoding(866), // DOS Russian
                    0x66 => Encoding.GetEncoding(865), // DOS Nordic
                    0x67 => Encoding.GetEncoding(861), // DOS Icelandic
                    0x6A => Encoding.GetEncoding(737), // DOS Greek
                    0x6B => Encoding.GetEncoding(857), // DOS Turkish
                    0xC8 => Encoding.GetEncoding(1250), // Windows Eastern Europe
                    0xC9 => Encoding.GetEncoding(1251), // Windows Russian
                    0xCA => Encoding.GetEncoding(1254), // Windows Turkish
                    0xCB => Encoding.GetEncoding(1253), // Windows Greek
                    _ => Encoding.GetEncoding(1252) // Default conservador
                };
            }
        }
        catch
        {
            return Encoding.GetEncoding(1252);
        }
    }

    private async Task<Encoding> FindCorrectSpeciesEncoding(string dbfPath)
    {
        // Candidatos en orden de prioridad
        var candidateCPs = new List<int> { 1252, 850, 437 };
        
        // Intentar primero la detectada por el header si no está en la lista
        var headerEncoding = DetectEncoding(dbfPath);
        if (!candidateCPs.Contains(headerEncoding.CodePage))
        {
            candidateCPs.Insert(0, headerEncoding.CodePage);
        }

        foreach (var cp in candidateCPs)
        {
            try
            {
                var encoding = Encoding.GetEncoding(cp);
                var options = new DbfDataReaderOptions { Encoding = encoding };
                using var reader = new DbfDataReader.DbfDataReader(dbfPath, options);
                var colMap = GetColumnMap(reader);

                int count = 0;
                while (reader.Read() && count++ < 5000) // Ampliamos búsqueda a 5000 registros
                {
                    var val = reader.GetValue(colMap.TryGetValue("CODINIDEP", out int i1) ? i1 : 
                             colMap.TryGetValue("COD_INIDEP", out int i2) ? i2 :
                             colMap.TryGetValue("COD", out int i3) ? i3 : -1);

                    if (val == null) continue;
                    
                    // Comparación robusta (soporta decimal, double, string)
                    bool isMatch = false;
                    try 
                    {
                        if (val is double d) isMatch = Math.Abs(d - 7210040101.0) < 0.1;
                        else if (val is decimal dec) isMatch = dec == 7210040101m;
                        else isMatch = val.ToString()?.Trim() == "7210040101";
                    } catch { }

                    if (isMatch)
                    {
                        var name = GetString(reader, colMap, "NOMVULCAS");
                        if (string.IsNullOrEmpty(name)) name = GetString(reader, colMap, "NOM_VULGAR");

                        // Condición crítica: "común" con acento correctamente decodificado (\u00FA = ú)
                        if (name != null && name.Contains("com\u00FAn", StringComparison.OrdinalIgnoreCase))
                        {
                            System.Diagnostics.Debug.WriteLine($"Heurística DBF: Codificación {cp} seleccionada (Merluza com\u00FAn detectada)");
                            return encoding;
                        }
                        
                        // Si encontramos el código pero el nombre está mal, probamos con el siguiente CP
                        break; 
                    }
                }
            }
            catch 
            {
                // Ignorar errores de lectura y probar siguiente candidato
            }
        }

        return headerEncoding; // Fallback a la detección original si falla la heurística
    }

    public async Task ExtractBuquesAsync(string dbfPath, string jsonOutputPath)
    {
        var records = new List<Dictionary<string, object?>>();
        var options = GetOptions(dbfPath);

        using (var dbfReader = new DbfDataReader.DbfDataReader(dbfPath, options))
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
        var encoding = await FindCorrectSpeciesEncoding(dbfPath);
        var options = new DbfDataReaderOptions { Encoding = encoding };

        using (var dbfReader = new DbfDataReader.DbfDataReader(dbfPath, options))
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

        var options = GetOptions(dbfPath);
        using var reader = new DbfDataReader.DbfDataReader(dbfPath, options);
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
                Descarte = GetDouble(reader, colMap, "DESCARTE"),
                
                // Mapeo de campos adicionales
                Tiempo = GetDoubleNullable(reader, colMap, "TIEMPO"),
                Mar = GetDoubleNullable(reader, colMap, "MAR"),
                DirViento = GetDoubleNullable(reader, colMap, "DIR_VIENTO"),
                VelViento = GetDoubleNullable(reader, colMap, "VEL_VIENTO"),
                TmpASeco = GetDoubleNullable(reader, colMap, "TMP_A_SECO"),
                TmpMarF = GetDoubleNullable(reader, colMap, "TMP_MAR_F"),
                PresionB = GetDoubleNullable(reader, colMap, "PRESION_B"),
                VelArras = GetDoubleNullable(reader, colMap, "VEL_ARRAS"),
                Rumbo = GetDoubleNullable(reader, colMap, "RUMBO"),
                MallCopo = GetDoubleNullable(reader, colMap, "MALL_COPO"),
                MallAlas = GetDoubleNullable(reader, colMap, "MALL_ALAS"),
                CabFilad = GetDoubleNullable(reader, colMap, "CAB_FILAD"),
                AberVert = GetDoubleNullable(reader, colMap, "ABER_VERT"),
                DistAlas = GetDoubleNullable(reader, colMap, "DIST_ALAS"),
                DistEPor = GetDoubleNullable(reader, colMap, "DIST_E_POR")
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

        var options = GetOptions(dbfPath);
        using var reader = new DbfDataReader.DbfDataReader(dbfPath, options);
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
                    var decoded = LegacyDecoder.DecodeTally(val);
                    
                    if (decoded.Size == 0 && decoded.Total > 0)
                    {
                        int calculatedSize = (int)m.PrimTalla + ((i - 1) * (int)m.Intervalo);
                        decoded = decoded with { Size = calculatedSize };
                    }

                    if (decoded.Total > 0)
                    {
                        m.Tallies.Add(decoded);
                    }
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

        var options = GetOptions(dbfPath);
        using var reader = new DbfDataReader.DbfDataReader(dbfPath, options);
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

        var options = GetOptions(dbfPath);
        using var reader = new DbfDataReader.DbfDataReader(dbfPath, options);
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

        var options = GetOptions(dbfPath);
        using var reader = new DbfDataReader.DbfDataReader(dbfPath, options);
        var colMap = GetColumnMap(reader);

        while (reader.Read())
        {
            var rawFecha = GetString(reader, colMap, "FECHA");
            string processedFecha = rawFecha;
            
            if (DateTime.TryParse(rawFecha, out var dt))
            {
                // La fecha en el DBF de tracking está en UTC, la convertimos a UTC-3.
                processedFecha = dt.AddHours(-3).ToString("yyyy-MM-dd HH:mm:ss");
            }

            list.Add(new LegacyTracking
            {
                Buque = GetString(reader, colMap, "BUQUE"),
                Matricula = GetString(reader, colMap, "MATRICULA"),
                FechaStr = processedFecha,
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

        var options = GetOptions(dbfPath);
        using var reader = new DbfDataReader.DbfDataReader(dbfPath, options);
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
    
    private double? GetDoubleNullable(DbfDataReader.DbfDataReader reader, Dictionary<string, int> map, string name)
    {
        if (map.TryGetValue(name, out int index))
        {
            var value = reader.GetValue(index);
            if (value == null || value is DBNull) return null;
            
            try 
            {
                return Convert.ToDouble(value);
            }
            catch 
            {
                return null;
            }
        }
        return null;
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
