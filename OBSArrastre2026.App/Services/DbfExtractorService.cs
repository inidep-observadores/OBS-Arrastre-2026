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

    private async Task<DbfDataReaderOptions> GetOptionsAsync(string dbfPath)
    {
        var encoding = await DetectEncodingSmartAsync(dbfPath);
        return new DbfDataReaderOptions { Encoding = encoding };
    }

    public async Task<Encoding> DetectEncodingSmartAsync(string dbfPath)
    {
        try
        {
            if (!File.Exists(dbfPath)) return Encoding.GetEncoding(1252);

            // 1. Intentar detectar por contenido (Heurística)
            var heuristicEncoding = await FindCorrectEncodingByHeuristicAsync(dbfPath);
            if (heuristicEncoding != null) return heuristicEncoding;

            // 2. Si no hay evidencia clara en el contenido, usar el header
            return DetectEncoding(dbfPath);
        }
        catch
        {
            return Encoding.GetEncoding(1252);
        }
    }

    private Encoding DetectEncoding(string dbfPath)
    {
        try
        {
            if (!File.Exists(dbfPath)) return Encoding.GetEncoding(437);
            
            using (var stream = File.OpenRead(dbfPath))
            {
                if (stream.Length < 30) return Encoding.GetEncoding(437);

                stream.Position = 29;
                int cpByte = stream.ReadByte();

                // Mapeo de CodePage de DBF
                // Ref: https://www.dbf2002.com/dbf-file-format.html
                return cpByte switch
                {
                    0x00 => Encoding.GetEncoding(1252), // Windows ANSI por defecto (más común que 437 hoy)
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
                    _ => Encoding.GetEncoding(1252) // Default Windows ANSI
                };
            }
        }
        catch
        {
            return Encoding.GetEncoding(1252);
        }
    }

    private async Task<Encoding?> FindCorrectEncodingByHeuristicAsync(string dbfPath)
    {
        // Candidatos en orden de prioridad para el entorno INIDEP
        var candidateCPs = new List<int> { 1252, 850, 437, 65001 };
        
        foreach (var cp in candidateCPs)
        {
            try
            {
                var encoding = Encoding.GetEncoding(cp);
                var options = new DbfDataReaderOptions { Encoding = encoding };
                using var reader = new DbfDataReader.DbfDataReader(dbfPath, options);
                var colMap = GetColumnMap(reader);

                // Columnas donde es probable encontrar texto con acentos/eñes
                var columnsToTest = new[] { "ESPECIE", "NOMVULCAS", "NOM_VULGAR", "NOMVUL", "NOM_VUL", "NOMVULG", "BARCO", "COMENTARIO", "OBSERVAC", "PRODUCTO", "NOMBRE", "CATEGORIA" };
                var targetCols = colMap.Where(kv => columnsToTest.Contains(kv.Key.ToUpper())).Select(kv => kv.Value).ToList();

                // Si es el archivo de especies, tenemos un "Gold Standard" (Merluza común con su código)
                bool isSpeciesTable = colMap.ContainsKey("CODINIDEP") || colMap.ContainsKey("COD_INIDEP");

                while (reader.Read())
                {
                    if (isSpeciesTable)
                    {
                        var codVal = reader.GetValue(colMap.TryGetValue("CODINIDEP", out int i1) ? i1 : 
                                     colMap.TryGetValue("COD_INIDEP", out int i2) ? i2 : -1);
                        var codStr = codVal?.ToString()?.Trim();
                        if (!string.IsNullOrEmpty(codStr) && (codStr == "7210040101" || codStr.StartsWith("7210040101")))
                        {
                            var name = GetString(reader, colMap, "NOMVULCAS");
                            if (string.IsNullOrEmpty(name)) name = GetString(reader, colMap, "NOM_VULGAR");
                            if (string.IsNullOrEmpty(name)) name = GetString(reader, colMap, "NOMVUL");

                            if (name != null && name.Contains("com\u00FAn", StringComparison.OrdinalIgnoreCase))
                            {
                                return encoding;
                            }
                        }
                    }

                    // Heurística general para cualquier archivo: buscar palabras comunes con acentos
                    foreach (var colIdx in targetCols)
                    {
                        var val = reader.GetValue(colIdx)?.ToString();
                        if (string.IsNullOrEmpty(val)) continue;

                        // Patrones comunes: común, tiburón, español, bártola, marea, producción, categoría
                        if (val.Contains("com\u00FAn", StringComparison.OrdinalIgnoreCase) || 
                            val.Contains("tibur\u00F3n", StringComparison.OrdinalIgnoreCase) ||
                            val.Contains("espa\u00F1ol", StringComparison.OrdinalIgnoreCase) ||
                            val.Contains("producci\u00F3n", StringComparison.OrdinalIgnoreCase) ||
                            val.Contains("categor\u00EDa", StringComparison.OrdinalIgnoreCase))
                        {
                            return encoding;
                        }
                    }
                }
            }
            catch { }
        }

        return null; // No hay evidencia clara de acentos
    }

    public async Task ExtractBuquesAsync(string dbfPath, string jsonOutputPath)
    {
        var records = new List<Dictionary<string, object?>>();
        var encoding = await DetectEncodingSmartAsync(dbfPath);
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
        var encoding = await DetectEncodingForSpeciesCatalogAsync(dbfPath);
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

    private async Task<Encoding> DetectEncodingForSpeciesCatalogAsync(string dbfPath)
    {
        // Candidatos principales en INIDEP
        var candidates = new[] { 1252, 850, 437 };
        
        foreach (var cp in candidates)
        {
            var encoding = Encoding.GetEncoding(cp);
            try
            {
                using var reader = new DbfDataReader.DbfDataReader(dbfPath, new DbfDataReaderOptions { Encoding = encoding });
                var colMap = GetColumnMap(reader);
                
                // Buscar columnas clave
                string[] codCols = { "CODINIDEP", "COD_INIDEP", "COD", "CODIGO" };
                string[] nomCols = { "NOMVULCAS", "NOM_VULGAR", "NOMVUL", "NOMBRE" };
                
                var codCol = colMap.Keys.FirstOrDefault(k => codCols.Contains(k.ToUpper()));
                var nomCol = colMap.Keys.FirstOrDefault(k => nomCols.Contains(k.ToUpper()));
                
                if (codCol != null && nomCol != null)
                {
                    int codIdx = colMap[codCol];
                    int nomIdx = colMap[nomCol];
                    while (reader.Read()) // Escaneo total para catálogo
                    {
                        var codVal = reader.GetValue(codIdx)?.ToString()?.Trim();
                        // Buscamos Merluza común (7210040101)
                        if (codVal == "7210040101" || (codVal != null && codVal.StartsWith("7210040101")))
                        {
                            var name = reader.GetString(nomIdx);
                            // Si contiene "común" con tilde, esta es la codificación correcta
                            if (name != null && name.Contains("com\u00FAn", StringComparison.OrdinalIgnoreCase))
                            {
                                return encoding;
                            }
                        }
                    }
                }
            }
            catch { /* Continuar al siguiente candidato */ }
        }

        // Si falló el patrón específico de Merluza, usamos la detección inteligente genérica
        return await DetectEncodingSmartAsync(dbfPath);
    }

    public async Task<List<LegacyCaptura>> ReadCapturasAsync(string dbfPath)
    {
        var list = new List<LegacyCaptura>();
        if (!File.Exists(dbfPath)) return list;

        var options = await GetOptionsAsync(dbfPath);
        using var reader = new DbfDataReader.DbfDataReader(dbfPath, options);
        var colMap = GetColumnMap(reader);

        while (reader.Read())
        {
            var c = new LegacyCaptura
            {
                Barco = GetBarcoValue(reader, colMap),
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
                DistEPor = GetDoubleNullable(reader, colMap, "DIST_E_POR"),
                Observac = GetString(reader, colMap, "OBSERVAC"),

                // Campos de Integridad
                Mus = GetDoubleNullable(reader, colMap, "MUS"),
                EstacGral = GetDoubleNullable(reader, colMap, "ESTAC_GRAL"),
                Estrato = GetDoubleNullable(reader, colMap, "ESTRATO"),
                EdadLuna = GetDoubleNullable(reader, colMap, "EDAD_LUNA"),
                Luz = GetDoubleNullable(reader, colMap, "LUZ"),
                TmpAHum = GetDoubleNullable(reader, colMap, "TMP_A_HUM"),
                TmpMarS = GetDoubleNullable(reader, colMap, "TMP_MAR_S"),
                Tarte = GetDoubleNullable(reader, colMap, "TARTE"),
                Narte = GetDoubleNullable(reader, colMap, "NARTE"),
                AreaBarr = GetDoubleNullable(reader, colMap, "AREA_BARR"),
                MallSobre = GetDoubleNullable(reader, colMap, "MALL_SOBRE")
            };

            for (int i = 1; i <= 25; i++)
            {
                var espCode = GetDouble(reader, colMap, $"ESPECIE_{i}");
                if (espCode > 0)
                {
                    string sCode = ((long)espCode).ToString();
                    c.Especies[sCode] = GetDouble(reader, colMap, $"KG_{i}");
                    c.DescartesPorEspecie[sCode] = GetDouble(reader, colMap, $"DESCAR_{i}");
                    c.EspeciesOrder.Add(sCode);
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

        var options = await GetOptionsAsync(dbfPath);
        using var reader = new DbfDataReader.DbfDataReader(dbfPath, options);
        var colMap = GetColumnMap(reader);
        int order = 0;

        while (reader.Read())
        {
            var m = new LegacyMuestra
            {
                NumeroOrden = ++order,
                Barco = GetBarcoValue(reader, colMap),
                Marea = GetDouble(reader, colMap, "MAREA"),
                Lance = GetDouble(reader, colMap, "LANCE"),
                Fecha = GetDateTime(reader, colMap, "FECHA") ?? DateTime.MinValue,
                Especie = GetString(reader, colMap, "ESPECIE"),
                CodEspec = ((long)GetDouble(reader, colMap, "COD_ESPEC")).ToString(),
                Fuente = GetDoubleNullable(reader, colMap, "FUENTE"),
                Tarte = GetDoubleNullable(reader, colMap, "TARTE"),
                Area = GetDoubleNullable(reader, colMap, "AREA"),
                PrimTalla = (int?)GetDoubleNullable(reader, colMap, "PRIM_TALLA"),
                UltTalla = (int?)GetDoubleNullable(reader, colMap, "ULT_TALLA"),
                Intervalo = GetDoubleNullable(reader, colMap, "INTERVALO"),
                PesoMues = GetDouble(reader, colMap, "PESO_MUES"),
                FactPond = GetDoubleNullable(reader, colMap, "FACT_POND")
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

        var options = await GetOptionsAsync(dbfPath);
        using var reader = new DbfDataReader.DbfDataReader(dbfPath, options);
        var colMap = GetColumnMap(reader);
        int order = 0;

        while (reader.Read())
        {
            list.Add(new LegacySubmuestra
            {
                NumeroOrden = ++order,
                Barco = GetBarcoValue(reader, colMap),
                Marea = GetDouble(reader, colMap, "MAREA"),
                Lance = GetDouble(reader, colMap, "LANCE"),
                Fecha = GetDateTime(reader, colMap, "FECHA") ?? DateTime.MinValue,
                Tarte = GetDoubleNullable(reader, colMap, "TARTE"),
                Fuente = GetDoubleNullable(reader, colMap, "FUENTE"),
                Area = GetDoubleNullable(reader, colMap, "AREA"),
                Especie = GetString(reader, colMap, "ESPECIE"),
                NEjemplar = (int)GetDouble(reader, colMap, "NEJEMPLAR"),
                LargoTot = (int)GetDouble(reader, colMap, "LARGO_TOT"),
                LargoSta = (int)GetDouble(reader, colMap, "LARGO_STA"),
                PesoTot = GetDouble(reader, colMap, "PESO_TOT"),
                PesoVac = GetDouble(reader, colMap, "PESO_VAC"),
                Sexo = (int)GetDouble(reader, colMap, "SEXO"),
                Estadio = (int)GetDouble(reader, colMap, "ESTADIO"),
                PesoGon = GetDouble(reader, colMap, "PESO_GON"),
                PesoHig = GetDouble(reader, colMap, "PESO_HIG"),
                Replecion = (int)GetDouble(reader, colMap, "REPLESION"),
                Comentario = GetString(reader, colMap, "CONTENIDO"),
                Edad = GetDouble(reader, colMap, "EDAD"),
                RTotal = GetDouble(reader, colMap, "R_TOTAL")
            });
        }
        return list;
    }

    public async Task<List<LegacyLg>> ReadLgAsync(string dbfPath)
    {
        var list = new List<LegacyLg>();
        if (!File.Exists(dbfPath)) return list;

        var options = await GetOptionsAsync(dbfPath);
        using var reader = new DbfDataReader.DbfDataReader(dbfPath, options);
        var colMap = GetColumnMap(reader);

        while (reader.Read())
        {
            var lg = new LegacyLg
            {
                Barco = GetBarcoValue(reader, colMap),
                Marea = GetDouble(reader, colMap, "MAREA"),
                Lance = GetDouble(reader, colMap, "LANCE"),
                Fecha = GetDateTime(reader, colMap, "FECHA") ?? DateTime.MinValue,
                CodEspecIE = ((long)GetDouble(reader, colMap, "CODIGO")).ToString(),
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

        var options = await GetOptionsAsync(dbfPath);
        using var reader = new DbfDataReader.DbfDataReader(dbfPath, options);
        var colMap = GetColumnMap(reader);

        while (reader.Read())
        {
            var rawFecha = GetString(reader, colMap, "FECHA");
            string processedFecha = rawFecha;
            
            if (DateTime.TryParse(rawFecha, out var dt))
            {
                // Se mantiene la fecha tal como viene en el DBF (asumida como hora local)
                processedFecha = dt.ToString("yyyy-MM-dd HH:mm:ss");
            }

            list.Add(new LegacyTracking
            {
                Buque = GetBarcoValue(reader, colMap),
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

        var options = await GetOptionsAsync(dbfPath);
        using var reader = new DbfDataReader.DbfDataReader(dbfPath, options);
        var colMap = GetColumnMap(reader);
        int order = 0;

        while (reader.Read())
        {
            list.Add(new LegacyProduccion
            {
                NumeroOrden = ++order,
                Barco = GetBarcoValue(reader, colMap),
                Marea = GetDouble(reader, colMap, "MAREA"),
                Fecha = GetDateTime(reader, colMap, "FECHA") ?? DateTime.MinValue,
                Especie = GetString(reader, colMap, "ESPECIE"),
                Producto = GetString(reader, colMap, "PRODUCTO"),
                Categoria = GetString(reader, colMap, "CATEGORIA"),
                Operarios = (int?)GetDoubleNullable(reader, colMap, "OPERARIOS"),
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

    private string GetBarcoValue(DbfDataReader.DbfDataReader reader, Dictionary<string, int> map)
    {
        string[] candidates = { "BARCO", "BUQUE", "BUQ", "NOMB_BUQUE", "NOMB_BARCO", "NOMBRE" };
        foreach (var name in candidates)
        {
            if (map.TryGetValue(name, out int index))
            {
                var value = reader.GetValue(index);
                var strValue = value?.ToString()?.Trim() ?? "";
                if (!string.IsNullOrEmpty(strValue)) return strValue;
            }
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
            "NRO_EXP" => "MMSI",
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
