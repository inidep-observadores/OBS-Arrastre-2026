using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.Services
{
    public class ExcelReportService : IExcelReportService
    {
        public async Task GenerateTablasExcelAsync(Marea marea, IEnumerable<Lance> lances, IEnumerable<RegistroProduccion> produccion, string outputPath)
        {
            await Task.Run(() =>
            {
                using var workbook = new XLWorkbook();
                
                var lancesList = lances.ToList();
                var produccionList = produccion.ToList();
                
                // --- HOJA: ESPECIES ---
                GenerateSpeciesSheet(workbook, lancesList);
                AddMetadataHeader(workbook.Worksheets.Last(), marea, 7);

                // --- HOJA: DISTRIBUCIÓN ---
                GenerateDistribucionSheet(workbook, lancesList);
                AddMetadataHeader(workbook.Worksheets.Last(), marea, 9);

                // --- HOJA: ÁREAS ---
                GenerateAreasSheet(workbook, lancesList);
                AddMetadataHeader(workbook.Worksheets.Last(), marea, 7);

                // --- HOJA: PRODUCCIÓN ---
                GenerateProduccionSheet(workbook, produccionList);
                if (workbook.Worksheets.Any(w => w.Name == "Producción"))
                {
                    var ws = workbook.Worksheet("Producción");
                    AddMetadataHeader(ws, marea, 5);
                }

                // --- HOJA: GIS ---
                GenerateGisSheet(workbook, lancesList);
                AddMetadataHeader(workbook.Worksheets.Last(), marea, 6);

                // --- HOJAS: FRECUENCIAS POR ESPECIE ---
                int countBefore = workbook.Worksheets.Count;
                GenerateFrequencySheets(workbook, lancesList);
                int countAfter = workbook.Worksheets.Count;
                
                for (int i = countBefore + 1; i <= countAfter; i++)
                {
                    AddMetadataHeader(workbook.Worksheet(i), marea, 8);
                }

                workbook.SaveAs(outputPath);
            });
        }

        private void AddMetadataHeader(IXLWorksheet worksheet, Marea? marea, int lastColumn)
        {
            if (marea == null) return;

            worksheet.Row(1).InsertRowsAbove(3);
            
            var buqueInfo = marea.BuqueCodigo.HasValue ? $"{marea.Buque?.Nombre} ({marea.BuqueCodigo})" : marea.Buque?.Nombre;
            var mareaInfo = $"{buqueInfo} - Marea {marea.NumeroInidep} ({marea.AnioInidep})";
            
            var cellMarea = worksheet.Cell(1, 1);
            cellMarea.Value = mareaInfo;
            cellMarea.Style.Font.Bold = true;
            cellMarea.Style.Font.FontSize = 14;
            worksheet.Range(1, 1, 1, lastColumn).Merge();

            var obsInfo = "Observador: ";
            if (!string.IsNullOrEmpty(marea.ObservadorApellido)) obsInfo += marea.ObservadorApellido;
            if (!string.IsNullOrEmpty(marea.ObservadorNombre)) obsInfo += (string.IsNullOrEmpty(marea.ObservadorApellido) ? "" : ", ") + marea.ObservadorNombre;
            if (marea.ObservadorCodigo.HasValue) obsInfo += $" ({marea.ObservadorCodigo})";
            
            var cellObs = worksheet.Cell(2, 1);
            cellObs.Value = obsInfo;
            cellObs.Style.Font.Italic = true;
            cellObs.Style.Font.FontSize = 11;
            worksheet.Range(2, 1, 2, lastColumn).Merge();

            worksheet.Row(3).Height = 10; // Espaciador
        }

        private void GenerateFrequencySheets(XLWorkbook workbook, List<Lance> lancesList)
        {
            var todasMuestras = lancesList.SelectMany(l => l.Muestras).ToList();
            var muestrasPorEspecie = todasMuestras
                .GroupBy(m => m.EspecieID)
                .Where(g => g.Count() > 2)
                .ToList();

            foreach (var grupo in muestrasPorEspecie)
            {
                var especie = grupo.First().Especie;
                if (especie == null) continue;

                string scientificName = especie.NombreCientifico ?? "Sin Nombre";
                // Límite de 31 caracteres para nombres de hoja en Excel
                string sheetName = scientificName.Length > 31 ? scientificName.Substring(0, 31) : scientificName;
                
                // Si la hoja ya existe (por truncamiento colisionado), buscamos un nombre único
                int suffix = 1;
                string baseName = sheetName;
                while (workbook.Worksheets.Any(w => w.Name == sheetName))
                {
                    string suffixStr = $"({suffix++})";
                    sheetName = baseName.Length + suffixStr.Length > 31 
                        ? baseName.Substring(0, 31 - suffixStr.Length) + suffixStr 
                        : baseName + suffixStr;
                }

                var worksheet = workbook.Worksheets.Add(sheetName);
                worksheet.Style.Font.FontName = "Times New Roman";
                worksheet.Style.Font.FontSize = 12;

                // Nombre científico en A1
                var cellA1 = worksheet.Cell(1, 1);
                cellA1.Value = scientificName;
                cellA1.Style.Font.Italic = true;
                cellA1.Style.Font.Bold = true;

                bool esLangostino = especie.CodigoInidep == "5139030101";

                // Límite de talla comercial (si aplica)
                int cutoff = GetSpeciesCutoff(especie.CodigoInidep);
                if (cutoff > 0)
                {
                    // Dejar una columna en blanco (B1) y poner en C1
                    var cellLimit = worksheet.Cell(1, 3);
                    cellLimit.Value = $"Talla comercial: {cutoff}";
                    cellLimit.Style.Font.Bold = true;
                }

                // Encabezados en Fila 3
                var headers = new List<string> { "Talla" };
                if (esLangostino) headers.Add("M.MAD");
                headers.Add("MACHOS");
                headers.Add("HEMBRAS");
                if (esLangostino)
                {
                    headers.Add("H.MAD");
                    headers.Add("H.IMP");
                }
                headers.Add("INDET");
                headers.Add("TOTAL");

                for (int i = 0; i < headers.Count; i++)
                {
                    var cell = worksheet.Cell(3, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                // Agrupar todas las frecuencias de esta especie por talla
                var frecuenciasAgrupadas = grupo
                    .SelectMany(m => m.FrecuenciasTallas)
                    .GroupBy(f => f.Talla)
                    .Select(g => new
                    {
                        Talla = g.Key,
                        Machos = g.Sum(f => f.NroMachos),
                        Hembras = g.Sum(f => f.NroHembras),
                        Indet = g.Sum(f => f.NroIndeterminados),
                        MMad = g.Sum(f => f.NroLangostinosMachoMaduros),
                        HMad = g.Sum(f => f.NroLangostinosHembraMaduras),
                        HImp = g.Sum(f => f.NroLangostinosHembraImpregnadas),
                        Total = g.Sum(f => f.NroTotal)
                    })
                    .OrderBy(f => f.Talla)
                    .ToList();

                int row = 4;
                foreach (var f in frecuenciasAgrupadas)
                {
                    int col = 1;
                    worksheet.Cell(row, col++).Value = f.Talla;
                    
                    if (esLangostino)
                    {
                        worksheet.Cell(row, col++).Value = f.MMad;
                    }
                    worksheet.Cell(row, col++).Value = f.Machos;
                    worksheet.Cell(row, col++).Value = f.Hembras;

                    if (esLangostino)
                    {
                        worksheet.Cell(row, col++).Value = f.HMad;
                        worksheet.Cell(row, col++).Value = f.HImp;
                    }
                    
                    worksheet.Cell(row, col++).Value = f.Indet;
                    worksheet.Cell(row, col++).Value = f.Total;
                    row++;
                }

                worksheet.Columns().AdjustToContents();
            }
        }


        private void GenerateProduccionSheet(XLWorkbook workbook, List<RegistroProduccion> produccionList)
        {
            if (!produccionList.Any()) return;

            var worksheet = workbook.Worksheets.Add("Producción");
            worksheet.Style.Font.FontName = "Times New Roman";
            worksheet.Style.Font.FontSize = 12;

            // Agrupar y sumarizar por Especie, Código de Producto y Categoría
            var groupedProduccion = produccionList
                .GroupBy(p => new 
                { 
                    EspecieId = p.EspecieId,
                    EspecieNombre = p.Especie?.NombreCientifico ?? "Sin Especie",
                    ProductoCodigo = p.Producto?.Codigo ?? "S/C",
                    Categoria = p.Categoria ?? ""
                })
                .Select(g => new
                {
                    EspecieNombre = g.Key.EspecieNombre,
                    ProductoCodigo = g.Key.ProductoCodigo,
                    Categoria = g.Key.Categoria,
                    TotalKg = g.Sum(x => x.Kg ?? 0),
                    Factor = g.FirstOrDefault()?.Factor ?? 1
                })
                .OrderBy(g => g.EspecieNombre)
                .ThenBy(g => g.ProductoCodigo)
                .ToList();

            // Determinar si incluir columna Categoría
            bool incluirCategoria = groupedProduccion.Any(p => !string.IsNullOrWhiteSpace(p.Categoria));

            // Encabezados
            var headerList = new List<string> { "Especie", "Producto" };
            if (incluirCategoria) headerList.Add("Categoría");
            headerList.Add("Kilos");
            headerList.Add("Factor");

            for (int i = 0; i < headerList.Count; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headerList[i];
                cell.Style.Font.Bold = true;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            int row = 2;
            foreach (var p in groupedProduccion)
            {
                int col = 1;
                
                // Especie (Nombre Científico)
                var cellEspecie = worksheet.Cell(row, col++);
                cellEspecie.Value = p.EspecieNombre;
                cellEspecie.Style.Font.Italic = true;

                // Producto (CÓDIGO)
                worksheet.Cell(row, col++).Value = p.ProductoCodigo;

                // Categoría (opcional)
                if (incluirCategoria)
                {
                    worksheet.Cell(row, col++).Value = p.Categoria;
                }

                // Kilos (Suma)
                var cellKg = worksheet.Cell(row, col++);
                cellKg.Value = p.TotalKg;
                cellKg.Style.NumberFormat.Format = "#,##0.00";

                // Factor (Primero)
                var cellFactor = worksheet.Cell(row, col++);
                cellFactor.Value = p.Factor;
                cellFactor.Style.NumberFormat.Format = "#,##0.00";

                // Bordes para la fila
                worksheet.Range(row, 1, row, headerList.Count).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                worksheet.Range(row, 1, row, headerList.Count).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                row++;
            }

            worksheet.Columns().AdjustToContents();
        }


        private void GenerateGisSheet(XLWorkbook workbook, List<Lance> lancesList)
        {
            var worksheet = workbook.Worksheets.Add("GIS");
            worksheet.Style.Font.FontName = "Times New Roman";
            worksheet.Style.Font.FontSize = 12;

            // Encabezados
            var headers = new[] { "Buque", "marea", "lan", "fecha_hora", "latitud", "longitud" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
            }

            int row = 2;
            foreach (var lance in lancesList)
            {
                var marea = lance.MareaEtapa?.Marea;
                var buque = marea?.Buque;

                worksheet.Cell(row, 1).Value = buque?.Nombre ?? "";
                worksheet.Cell(row, 2).Value = marea?.NumeroInidep ?? 0;
                worksheet.Cell(row, 3).Value = lance.NroLance;
                
                // Combinar fecha y hora
                string fullDateTimeStr = $"{lance.Fecha} {lance.HoraInicio ?? "00:00"}";
                if (DateTime.TryParse(fullDateTimeStr, out var fechaHora))
                {
                    worksheet.Cell(row, 4).Value = fechaHora;
                    worksheet.Cell(row, 4).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                }
                else
                {
                    worksheet.Cell(row, 4).Value = fullDateTimeStr;
                }

                worksheet.Cell(row, 5).Value = lance.LatitudInicioDecimal ?? 0;
                worksheet.Cell(row, 6).Value = lance.LongitudInicioDecimal ?? 0;

                // Formato numérico para coordenadas (4 decimales)
                worksheet.Cell(row, 5).Style.NumberFormat.Format = "0.0000";
                worksheet.Cell(row, 6).Style.NumberFormat.Format = "0.0000";

                row++;
            }

            worksheet.Columns().AdjustToContents();
        }


        private void GenerateDistribucionSheet(XLWorkbook workbook, List<Lance> lancesList)
        {
            var worksheet = workbook.Worksheets.Add("Distribución");
            worksheet.Style.Font.FontName = "Times New Roman";
            worksheet.Style.Font.FontSize = 12;

            // Agrupar todas las frecuencias de talla por especie en la etapa
            var allFrecuencias = lancesList
                .SelectMany(l => l.Muestras)
                .SelectMany(m => m.FrecuenciasTallas.Select(f => new { f, m.Especie }))
                .Where(x => x.Especie != null)
                .GroupBy(x => x.Especie!.ID)
                .ToList();

            int currentRow = 1;

            foreach (var group in allFrecuencias)
            {
                var especie = group.First().Especie!;
                var frecuencias = group.Select(x => x.f).ToList();

                // Título de la especie (Nombre Científico)
                worksheet.Cell(currentRow, 1).Value = especie.NombreCientifico;
                worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                worksheet.Cell(currentRow, 1).Style.Font.Italic = true;
                currentRow++;

                // Definir límite para % < X (basado en obsdist.PRG)
                int cutoff = GetSpeciesCutoff(especie.CodigoInidep);
                string cutoffHeader = cutoff > 0 ? $"%<{cutoff}" : "%<0";

                // Encabezados de columnas
                var colHeaders = new[] { "", "Media", "Desv.St.", "Porcent.", "Coef.V.", "Suma N", "Suma X", "Suma X2", cutoffHeader };
                for (int i = 0; i < colHeaders.Length; i++)
                {
                    worksheet.Cell(currentRow, i + 1).Value = colHeaders[i];
                    worksheet.Cell(currentRow, i + 1).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(currentRow, i + 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }
                currentRow++;

                // Calcular estadísticas por sexo
                var statsMachos = CalculateStats(frecuencias, f => f.NroMachos, cutoff);
                var statsHembras = CalculateStats(frecuencias, f => f.NroHembras, cutoff);
                var statsIndet = CalculateStats(frecuencias, f => f.NroIndeterminados, cutoff);
                
                // Calcular Total como la suma de todos los individuos medidos
                var statsTotal = CalculateStats(frecuencias, f => f.NroMachos + f.NroHembras + f.NroIndeterminados, cutoff);

                // Calcular Porcentajes (N_sexo / N_total * 100 * 100 para el formato x100)
                double totalN = statsTotal.SumN;
                statsMachos.Porcent = totalN > 0 ? (statsMachos.SumN / totalN) * 100 : 0;
                statsHembras.Porcent = totalN > 0 ? (statsHembras.SumN / totalN) * 100 : 0;
                statsIndet.Porcent = totalN > 0 ? (statsIndet.SumN / totalN) * 100 : 0;
                statsTotal.Porcent = totalN > 0 ? 100 : 0;

                // Escribir Filas
                WriteStatsRow(worksheet, ref currentRow, "Machos", statsMachos);
                WriteStatsRow(worksheet, ref currentRow, "Hembras", statsHembras);
                WriteStatsRow(worksheet, ref currentRow, "Indet.", statsIndet);
                WriteStatsRow(worksheet, ref currentRow, "Total", statsTotal, true);

                currentRow += 2; // Espacio entre especies
            }

            // Formateo general de la hoja
            worksheet.Columns().AdjustToContents();
        }

        private class StatsResult
        {
            public double Media { get; set; }
            public double DesvSt { get; set; }
            public double Porcent { get; set; }
            public double CoefV { get; set; }
            public double SumN { get; set; }
            public double SumX { get; set; }
            public double SumX2 { get; set; } // Representa (SumX)^2 / (N-1) según PRG
            public double PorcentLimit { get; set; }
        }

        private StatsResult CalculateStats(List<FrecuenciaTalla> frecuencias, Func<FrecuenciaTalla, int> getCount, int cutoff)
        {
            var result = new StatsResult();
            
            double sumN = frecuencias.Sum(f => (double)getCount(f));
            if (sumN == 0) return result;

            double sumX = frecuencias.Sum(f => f.Talla * getCount(f));
            double media = sumX / sumN;

            double sumDevSq = frecuencias.Sum(f => Math.Pow(f.Talla - media, 2) * getCount(f));
            double desvSt = sumN > 1 ? Math.Sqrt(sumDevSq / (sumN - 1)) : 0;

            // Lógica FoxPro para SumX2 y CoefV
            double bm = sumN > 1 ? sumX / (sumN - 1) : 0;
            double sumX2_PRG = sumX * bm; // (SumX)^2 / (N-1)
            
            // CoefV según PRG: (DesvSt / bm) * 100
            double coefV = bm > 0 ? (desvSt / bm) * 100 : 0;

            // Porcentaje < Cutoff
            double sumNLimit = cutoff > 0 ? frecuencias.Where(f => f.Talla < cutoff).Sum(f => (double)getCount(f)) : 0;
            double porcentLimit = (sumNLimit / sumN) * 100;

            result.SumN = sumN;
            result.SumX = sumX;
            result.SumX2 = sumX2_PRG;
            result.Media = media;
            result.DesvSt = desvSt;
            result.CoefV = coefV;
            result.PorcentLimit = porcentLimit;

            return result;
        }

        private void WriteStatsRow(IXLWorksheet ws, ref int row, string label, StatsResult stats, bool isBold = false)
        {
            ws.Cell(row, 1).Value = label;
            
            // Aplicar factor x100 observado en la referencia para Media, DesvSt, Porcent, CoefV y PorcentLimit
            ws.Cell(row, 2).Value = stats.Media * 100;
            ws.Cell(row, 3).Value = stats.DesvSt * 100;
            ws.Cell(row, 4).Value = stats.Porcent * 100;
            ws.Cell(row, 5).Value = stats.CoefV * 100;
            
            ws.Cell(row, 6).Value = stats.SumN;
            ws.Cell(row, 7).Value = stats.SumX;
            ws.Cell(row, 8).Value = stats.SumX2;
            ws.Cell(row, 9).Value = stats.PorcentLimit * 100;

            if (isBold)
            {
                ws.Range(row, 1, row, 9).Style.Font.Bold = true;
            }

            // Bordes y formato numérico
            var range = ws.Range(row, 1, row, 9);
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            
            ws.Range(row, 2, row, 9).Style.NumberFormat.Format = "#,##0.00";

            row++;
        }

        private int GetSpeciesCutoff(string? codigoInidep)
        {
            if (string.IsNullOrEmpty(codigoInidep)) return 0;

            return codigoInidep switch
            {
                "7210040101" => 35, // Merluza Hubbsi
                "7210040201" => 59, // Merluza de Cola
                "7226030101" => 70, // Abadejo
                "7218280201" => 82, // Narval?
                "7210030201" => 32, // Polaca
                "7210040102" => 61, // Merluza Austral
                "7218390102" => 29, // S?
                "7204020101" => 93, // San Pedro?
                _ => 0
            };
        }


        private void GenerateSpeciesSheet(XLWorkbook workbook, List<Lance> lancesList)
        {
            var worksheet = workbook.Worksheets.Add("Especies");

            // Configuración de fuente base
            worksheet.Style.Font.FontName = "Times New Roman";
            worksheet.Style.Font.FontSize = 12;

            // Encabezados
            worksheet.Cell(1, 1).Value = "Especie";
            worksheet.Cell(1, 2).Value = "Kilos";
            worksheet.Cell(1, 3).Value = "Descarte";
            worksheet.Cell(1, 4).Value = "Desc.%";
            worksheet.Cell(1, 5).Value = "Lances";
            worksheet.Cell(1, 6).Value = "Días";
            worksheet.Cell(1, 7).Value = "Horas";

            var headerRange = worksheet.Range(1, 1, 1, 7);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Agrupamiento de datos por especie
            var itemsCaptura = lancesList.SelectMany(l => l.ItemsCaptura).ToList();

            var speciesSummary = itemsCaptura
                .GroupBy(i => i.EspecieID)
                .Select(g => {
                    var especie = g.First().Especie;
                    var lancesDeEspecie = g.Select(i => i.Lance).Where(l => l != null).Distinct().ToList();
                    
                    double kilos = g.Sum(i => i.CapturaTotalKgCalculado);
                    double descarte = g.Sum(i => i.PesoDescarteCalculado);
                    double horas = lancesDeEspecie.Sum(l => CalculateDurationHours(l!));
                    int nroLances = lancesDeEspecie.Count;
                    int nroDias = lancesDeEspecie.Select(l => l!.Fecha).Distinct().Count();

                    return new {
                        NombreCientifico = especie?.NombreCientifico ?? "Desconocida",
                        Kilos = kilos,
                        Descarte = descarte,
                        DescartePorc = kilos > 0 ? (descarte * 100.0 / kilos) : 0,
                        Lances = nroLances,
                        Dias = nroDias,
                        Horas = horas
                    };
                })
                .OrderByDescending(s => s.Kilos)
                .ToList();

            int currentRow = 2;
            foreach (var item in speciesSummary)
            {
                worksheet.Cell(currentRow, 1).Value = item.NombreCientifico;
                worksheet.Cell(currentRow, 1).Style.Font.Italic = true;
                
                worksheet.Cell(currentRow, 2).Value = item.Kilos;
                worksheet.Cell(currentRow, 3).Value = item.Descarte;
                worksheet.Cell(currentRow, 4).Value = item.DescartePorc;
                worksheet.Cell(currentRow, 5).Value = item.Lances;
                worksheet.Cell(currentRow, 6).Value = item.Dias;
                worksheet.Cell(currentRow, 7).Value = item.Horas;

                currentRow++;
            }

            // Totales
            worksheet.Cell(currentRow, 1).Value = "TOTAL";
            
            double totalKilos = speciesSummary.Sum(s => s.Kilos);
            double totalDescarte = speciesSummary.Sum(s => s.Descarte);
            
            worksheet.Cell(currentRow, 2).Value = totalKilos;
            worksheet.Cell(currentRow, 3).Value = totalDescarte;
            
            worksheet.Cell(currentRow, 4).Value = speciesSummary.Any() ? speciesSummary.Average(s => s.DescartePorc) : 0;
            worksheet.Cell(currentRow, 5).Value = lancesList.Count;
            worksheet.Cell(currentRow, 6).Value = lancesList.Select(l => l.Fecha).Distinct().Count();
            worksheet.Cell(currentRow, 7).Value = Blank.Value;
            
            var totalRange = worksheet.Range(currentRow, 1, currentRow, 7);
            totalRange.Style.Font.Bold = true;

            ApplyFormatting(worksheet, currentRow);
        }

        private void GenerateAreasSheet(XLWorkbook workbook, List<Lance> lancesList)
        {
            var worksheet = workbook.Worksheets.Add("Áreas");

            // Configuración de fuente base
            worksheet.Style.Font.FontName = "Times New Roman";
            worksheet.Style.Font.FontSize = 12;

            // Encabezados
            worksheet.Cell(1, 1).Value = "Área";
            worksheet.Cell(1, 2).Value = "Kilos";
            worksheet.Cell(1, 3).Value = "Descarte";
            worksheet.Cell(1, 4).Value = "Desc.%";
            worksheet.Cell(1, 5).Value = "Lances";
            worksheet.Cell(1, 6).Value = "Días";
            worksheet.Cell(1, 7).Value = "Horas";

            var headerRange = worksheet.Range(1, 1, 1, 7);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Agrupamiento de datos por Área (Cuadrícula)
            var areaSummary = lancesList
                .GroupBy(l => GetCuadricula(l))
                .Select(g => {
                    var lancesDeArea = g.ToList();
                    double kilos = lancesDeArea.Sum(l => l.ItemsCaptura.Sum(i => i.CapturaTotalKgCalculado));
                    double descarte = lancesDeArea.Sum(l => l.ItemsCaptura.Sum(i => i.PesoDescarteCalculado));
                    double horas = lancesDeArea.Sum(l => CalculateDurationHours(l));
                    int nroLances = lancesDeArea.Count;
                    int nroDias = lancesDeArea.Select(l => l.Fecha).Distinct().Count();

                    return new {
                        Area = g.Key,
                        Kilos = kilos,
                        Descarte = descarte,
                        DescartePorc = kilos > 0 ? (descarte * 100.0 / kilos) : 0,
                        Lances = nroLances,
                        Dias = nroDias,
                        Horas = horas
                    };
                })
                .OrderBy(s => s.Area)
                .ToList();

            int currentRow = 2;
            foreach (var item in areaSummary)
            {
                worksheet.Cell(currentRow, 1).Value = item.Area;
                worksheet.Cell(currentRow, 2).Value = item.Kilos;
                worksheet.Cell(currentRow, 3).Value = item.Descarte;
                worksheet.Cell(currentRow, 4).Value = item.DescartePorc;
                worksheet.Cell(currentRow, 5).Value = item.Lances;
                worksheet.Cell(currentRow, 6).Value = item.Dias;
                worksheet.Cell(currentRow, 7).Value = item.Horas;

                currentRow++;
            }

            // Totales (opcional, pero consistente con especies)
            worksheet.Cell(currentRow, 1).Value = "TOTAL";
            double totalKilos = areaSummary.Sum(s => s.Kilos);
            double totalDescarte = areaSummary.Sum(s => s.Descarte);
            worksheet.Cell(currentRow, 2).Value = totalKilos;
            worksheet.Cell(currentRow, 3).Value = totalDescarte;
            worksheet.Cell(currentRow, 4).Value = areaSummary.Any() ? areaSummary.Average(s => s.DescartePorc) : 0;
            worksheet.Cell(currentRow, 5).Value = lancesList.Count;
            worksheet.Cell(currentRow, 6).Value = lancesList.Select(l => l.Fecha).Distinct().Count();
            worksheet.Cell(currentRow, 7).Value = Blank.Value;

            var totalRange = worksheet.Range(currentRow, 1, currentRow, 7);
            totalRange.Style.Font.Bold = true;

            ApplyFormatting(worksheet, currentRow);
        }

        private void ApplyFormatting(IXLWorksheet worksheet, int lastRow)
        {
            // Formateo numérico
            // Kilos, Descarte y Horas con 2 decimales y separador de miles
            var decimalRange = worksheet.Range(2, 2, lastRow, 4); // Kilos, Descarte, %
            decimalRange.Style.NumberFormat.Format = "#,##0.00";
            
            var horasRange = worksheet.Range(2, 7, lastRow - 1, 7); // Horas (filas de datos)
            horasRange.Style.NumberFormat.Format = "#,##0.00";

            // Lances y Días como enteros con separador de miles
            var integerRange = worksheet.Range(2, 5, lastRow, 6);
            integerRange.Style.NumberFormat.Format = "#,##0";

            // Bordes
            var fullRange = worksheet.Range(1, 1, lastRow, 7);
            fullRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            fullRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Ajustar columnas
            worksheet.Columns().AdjustToContents();
        }

        private string GetCuadricula(Lance lance)
        {
            if (!lance.LatitudInicioDecimal.HasValue || !lance.LongitudInicioDecimal.HasValue) 
                return "S/D";

            double lat = Math.Abs(lance.LatitudInicioDecimal.Value);
            double lon = Math.Abs(lance.LongitudInicioDecimal.Value);

            int cuad = ((int)Math.Truncate(lat) * 100) + (int)Math.Truncate(lon);
            return cuad.ToString();
        }

        private double CalculateDurationHours(Lance lance)
        {
            if (string.IsNullOrEmpty(lance.HoraInicio) || string.IsNullOrEmpty(lance.HoraFinal)) return 0;
            
            try 
            {
                // Formato esperado HH:mm o similar
                if (TimeSpan.TryParse(lance.HoraInicio, out var inicio) && 
                    TimeSpan.TryParse(lance.HoraFinal, out var fin))
                {
                    var diff = fin - inicio;
                    if (diff.TotalHours < 0) diff = diff.Add(TimeSpan.FromDays(1)); // Cruce de medianoche
                    return Math.Round(diff.TotalHours, 2);
                }
                return 0;
            }
            catch 
            {
                return 0;
            }
        }
    }
}
