using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using SkiaSharp;
using OBSArrastre2026.App.Data.Entities;
using OBSArrastre2026.App.Services.Internal;

namespace OBSArrastre2026.App.Services
{
    public class ExcelReportService : IExcelReportService
    {
        public async Task GenerateTablasExcelAsync(Marea marea, IEnumerable<Lance> lances, IEnumerable<RegistroProduccion> produccion, string outputPath, byte[]? mapImage = null)
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
                GenerateGisSheet(workbook, lancesList, mapImage);
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
            
            var meta = MareaMetadataHelper.GetMetadata(marea);
            
            var buqueInfo = meta.BuqueCodigo.HasValue ? $"{marea.Buque?.Nombre} ({meta.BuqueCodigo})" : marea.Buque?.Nombre;
            var mareaInfo = $"{buqueInfo} - Marea {marea.NumeroInidep}/{marea.AnioInidep}";
            
            var cellMarea = worksheet.Cell(1, 1);
            cellMarea.Value = mareaInfo;
            cellMarea.Style.Font.Bold = true;
            cellMarea.Style.Font.FontSize = 14;
            worksheet.Range(1, 1, 1, lastColumn).Merge();

            var obsInfo = "Observador: ";
            if (!string.IsNullOrEmpty(meta.ObservadorApellido)) obsInfo += meta.ObservadorApellido;
            if (!string.IsNullOrEmpty(meta.ObservadorNombre)) obsInfo += (string.IsNullOrEmpty(meta.ObservadorApellido) ? "" : ", ") + meta.ObservadorNombre;
            if (meta.ObservadorCodigo.HasValue) obsInfo += $" ({meta.ObservadorCodigo})";
            
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
            var muestrasAgrupadas = todasMuestras
                .GroupBy(m => new { m.EspecieID, m.TipoMuestra })
                .Where(g => g.Count() > 2)
                .OrderBy(g => g.First().Especie?.NombreCientifico)
                .ThenBy(g => g.Key.TipoMuestra)
                .ToList();

            foreach (var grupo in muestrasAgrupadas)
            {
                var especie = grupo.First().Especie;
                if (especie == null) continue;

                string scientificName = especie.NombreCientifico ?? "Sin Nombre";
                string suffixTipo = grupo.Key.TipoMuestra == 2 ? " (D)" : " (C)";
                
                // Límite de 31 caracteres para nombres de hoja en Excel
                string baseName = scientificName;
                if (baseName.Length + suffixTipo.Length > 31)
                {
                    baseName = baseName.Substring(0, 31 - suffixTipo.Length);
                }
                string sheetName = baseName + suffixTipo;
                
                // Si la hoja ya existe (por truncamiento colisionado), buscamos un nombre único
                int suffixNum = 1;
                string finalBaseName = sheetName;
                while (workbook.Worksheets.Any(w => w.Name == sheetName))
                {
                    string uniqueSuffix = $"_{suffixNum++}";
                    sheetName = finalBaseName.Length + uniqueSuffix.Length > 31 
                        ? finalBaseName.Substring(0, 31 - uniqueSuffix.Length) + uniqueSuffix 
                        : finalBaseName + uniqueSuffix;
                }

                var worksheet = workbook.Worksheets.Add(sheetName);
                worksheet.Style.Font.FontName = "Times New Roman";
                worksheet.Style.Font.FontSize = 12;

                // Nombre científico en A1
                var cellA1 = worksheet.Cell(1, 1);
                cellA1.Value = $"{scientificName}{suffixTipo}";
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
                headers.Add("% MACHOS");
                headers.Add("% HEMBRAS");
                headers.Add("% INDET");
                headers.Add("% TOTAL");

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

                double totalMachos = frecuenciasAgrupadas.Sum(f => (double)f.Machos);
                double totalHembras = frecuenciasAgrupadas.Sum(f => (double)f.Hembras);
                double totalIndet = frecuenciasAgrupadas.Sum(f => (double)f.Indet);
                double totalGeneral = frecuenciasAgrupadas.Sum(f => (double)f.Total);

                var chartPoints = new List<(double Talla, double Machos, double Hembras, double Indet, double Total)>();

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

                    // Columnas de porcentaje
                    double pMachos = totalMachos > 0 ? (f.Machos * 100.0) / totalMachos : 0;
                    double pHembras = totalHembras > 0 ? (f.Hembras * 100.0) / totalHembras : 0;
                    double pIndet = totalIndet > 0 ? (f.Indet * 100.0) / totalIndet : 0;
                    double pTotal = totalGeneral > 0 ? (f.Total * 100.0) / totalGeneral : 0;

                    worksheet.Cell(row, col).Value = pMachos;
                    worksheet.Cell(row, col++).Style.NumberFormat.Format = "0.00";

                    worksheet.Cell(row, col).Value = pHembras;
                    worksheet.Cell(row, col++).Style.NumberFormat.Format = "0.00";

                    worksheet.Cell(row, col).Value = pIndet;
                    worksheet.Cell(row, col++).Style.NumberFormat.Format = "0.00";

                    worksheet.Cell(row, col).Value = pTotal;
                    worksheet.Cell(row, col++).Style.NumberFormat.Format = "0.00";

                    chartPoints.Add((f.Talla, pMachos, pHembras, pIndet, pTotal));
                    row++;
                }

                // Insertar Gráfico
                if (chartPoints.Any())
                {
                    var chartBytes = RenderFrequencyChart(chartPoints, cutoff, scientificName, esLangostino);
                    if (chartBytes.Length > 0)
                    {
                        using var ms = new MemoryStream(chartBytes);
                        worksheet.AddPicture(ms)
                            .MoveTo(worksheet.Cell(6, 14)) // N6
                            .Scale(0.75);
                    }
                }

                worksheet.Columns().AdjustToContents();
            }
        }

        private byte[] RenderFrequencyChart(List<(double Talla, double Machos, double Hembras, double Indet, double Total)> dataPoints, int cutoff, string title, bool isLangostino)
        {
            int width = 900;
            int height = 550;
            float margin = 80;
            float chartWidth = width - (margin * 2);
            float chartHeight = height - (margin * 2) - 40;

            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            if (!dataPoints.Any()) return Array.Empty<byte>();

            double minX = Math.Floor(dataPoints.Min(p => p.Talla) / 5.0) * 5.0;
            double maxX = Math.Ceiling(dataPoints.Max(p => p.Talla) / 5.0) * 5.0;
            if (maxX - minX < 20) maxX = minX + 20;

            bool hasMachos = dataPoints.Any(p => p.Machos > 0.01);
            bool hasHembras = dataPoints.Any(p => p.Hembras > 0.01);
            bool hasIndet = dataPoints.Any(p => p.Indet > 0.01);
            bool hasTotal = dataPoints.Any(p => p.Total > 0.01);
            bool plotTotal = isLangostino || (!hasMachos && !hasHembras && !hasIndet);

            double maxYValue = dataPoints.Max(p => {
                double val = Math.Max(p.Machos, p.Hembras);
                if (plotTotal) val = Math.Max(val, p.Total);
                return val;
            });
            if (maxYValue <= 0) maxYValue = 10;
            else maxYValue *= 1.12; // Añadir 12% de margen superior

            // Lógica de "Nice Numbers" para el eje Y
            double targetStep = maxYValue / 5.0;
            double magnitude = Math.Pow(10, Math.Floor(Math.Log10(targetStep)));
            if (double.IsInfinity(magnitude) || magnitude == 0) magnitude = 1;
            double residual = targetStep / magnitude;
            double step;
            if (residual < 1.5) step = 1;
            else if (residual < 3.5) step = 2;
            else if (residual < 7.5) step = 5;
            else step = 10;
            step *= magnitude;

            double maxY = Math.Ceiling(maxYValue / step) * step;
            if (maxY == 0) maxY = step;

            var axisPaint = new SKPaint { Color = SKColors.Black, StrokeWidth = 1.5f, IsAntialias = true };
            var gridPaint = new SKPaint { Color = SKColors.Gray, StrokeWidth = 1.0f, IsAntialias = true };
            var textPaint = new SKPaint { Color = SKColors.Black, TextSize = 14, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Times New Roman") };
            var labelCenterPaint = new SKPaint { Color = SKColors.Black, TextSize = 16, IsAntialias = true, FakeBoldText = true, Typeface = SKTypeface.FromFamilyName("Times New Roman"), TextAlign = SKTextAlign.Center };

            for (double yVal = 0; yVal <= maxY + (step/10.0); yVal += step)
            {
                float yPos = height - margin - 40 - (float)((yVal / maxY) * chartHeight);
                canvas.DrawLine(margin, yPos, width - margin, yPos, gridPaint);
                // Si el paso tiene decimales, mostramos 1 decimal. Si no, ninguno.
                string format = (step % 1 == 0) ? "0" : "0.0";
                canvas.DrawText(yVal.ToString(format), margin - 10, yPos + 5, new SKPaint { Color = SKColors.Black, TextSize = 12, TextAlign = SKTextAlign.Right, IsAntialias = true });
            }

            double xInterval = 4;
            for (double xVal = minX; xVal <= maxX; xVal += xInterval)
            {
                float xPos = margin + (float)(((xVal - minX) / (maxX - minX)) * chartWidth);
                canvas.DrawText(xVal.ToString("0"), xPos, height - margin - 20, new SKPaint { Color = SKColors.Black, TextSize = 12, TextAlign = SKTextAlign.Center, IsAntialias = true });
                canvas.DrawLine(xPos, height - margin - 40, xPos, height - margin - 35, axisPaint);
            }

            canvas.DrawLine(margin, height - margin - 40, width - margin, height - margin - 40, axisPaint);
            canvas.DrawLine(margin, margin, margin, height - margin - 40, axisPaint);
            canvas.DrawText("Talla (cm)", width / 2, height - margin + 15, labelCenterPaint);
            
            canvas.Save();
            canvas.RotateDegrees(-90, 25, height / 2);
            canvas.DrawText("Frecuencia relativa (%)", 25, height / 2, labelCenterPaint);
            canvas.Restore();

            void DrawSeries(Func<(double Talla, double Machos, double Hembras, double Indet, double Total), double> selector, SKColor color, float[] dashPattern = null, float strokeWidth = 2.5f)
            {
                var points = dataPoints.Select(p => new SKPoint(
                    margin + (float)(((p.Talla - minX) / (maxX - minX)) * chartWidth),
                    height - margin - 40 - (float)((selector(p) / maxY) * chartHeight)
                )).ToArray();

                if (points.Length < 2) return;
                using var path = new SKPath();
                path.MoveTo(points[0]);

                // Smoothing algorithm (Catmull-Rom approximation)
                for (int i = 0; i < points.Length - 1; i++)
                {
                    var p0 = i == 0 ? points[i] : points[i - 1];
                    var p1 = points[i];
                    var p2 = points[i + 1];
                    var p3 = i == points.Length - 2 ? points[i + 1] : points[i + 2];

                    // Control points
                    var cp1 = new SKPoint(p1.X + (p2.X - p0.X) / 6, p1.Y + (p2.Y - p0.Y) / 6);
                    var cp2 = new SKPoint(p2.X - (p3.X - p1.X) / 6, p2.Y - (p3.Y - p1.Y) / 6);

                    path.CubicTo(cp1, cp2, p2);
                }

                var paint = new SKPaint { 
                    Color = color, 
                    Style = SKPaintStyle.Stroke, 
                    StrokeWidth = strokeWidth, 
                    IsAntialias = true,
                    StrokeJoin = SKStrokeJoin.Round,
                    StrokeCap = SKStrokeCap.Round
                };
                if (dashPattern != null) paint.PathEffect = SKPathEffect.CreateDash(dashPattern, 0);
                canvas.DrawPath(path, paint);
            }

            // Dibujar Total primero si es extra para que no tape las leyendas de los otros si se cruzan,
            // pero como es la suma siempre estará arriba. Usamos grosor para destacar.
            if (plotTotal && hasTotal)
            {
                float totalWidth = (hasMachos || hasHembras || hasIndet) ? 3.2f : 2.2f;
                DrawSeries(p => p.Total, SKColors.Black, null, totalWidth);
            }

            if (hasMachos) DrawSeries(p => p.Machos, SKColors.Black, null, 2.0f);
            if (hasHembras) DrawSeries(p => p.Hembras, SKColors.Black, new float[] { 10, 5, 2, 5 }, 2.0f);

            if (cutoff > 0 && cutoff >= minX && cutoff <= maxX)
            {
                float xPos = margin + (float)(((cutoff - minX) / (maxX - minX)) * chartWidth);
                canvas.DrawLine(xPos, margin, xPos, height - margin - 40, axisPaint);
            }

            float legendX = margin;
            float legendY = height - 15;
            if (hasMachos) { canvas.DrawLine(legendX, legendY - 5, legendX + 30, legendY - 5, new SKPaint { Color = SKColors.Black, StrokeWidth = 2.0f }); canvas.DrawText("Machos", legendX + 35, legendY, textPaint); legendX += 130; }
            if (hasHembras) { canvas.DrawLine(legendX, legendY - 5, legendX + 30, legendY - 5, new SKPaint { Color = SKColors.Black, StrokeWidth = 2.0f, PathEffect = SKPathEffect.CreateDash(new float[] { 10, 5, 2, 5 }, 0) }); canvas.DrawText("Hembras", legendX + 35, legendY, textPaint); legendX += 130; }
            
            if (plotTotal && hasTotal)
            {
                float tWidth = (hasMachos || hasHembras || hasIndet) ? 3.2f : 2.2f;
                canvas.DrawLine(legendX, legendY - 5, legendX + 30, legendY - 5, new SKPaint { Color = SKColors.Black, StrokeWidth = tWidth });
                canvas.DrawText("Total", legendX + 35, legendY, textPaint);
            }

            using var image = surface.Snapshot();
            using var pngData = image.Encode(SKEncodedImageFormat.Png, 100);
            return pngData.ToArray();
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
                .OrderByDescending(g => g.TotalKg)
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


        private void GenerateGisSheet(XLWorkbook workbook, List<Lance> lancesList, byte[]? mapImage)
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

            // Incrustar mapa si está disponible
            if (mapImage != null && mapImage.Length > 0)
            {
                try 
                {
                    using var ms = new MemoryStream(mapImage);
                    var picture = worksheet.AddPicture(ms)
                        .MoveTo(worksheet.Cell(1, 8)); // Columna H
                    
                    // Escalamos un poco para que no ocupe demasiado espacio inicial
                    picture.Scale(0.6); 
                }
                catch 
                {
                    // Si falla la inserción de imagen, el reporte sigue siendo válido sin ella
                }
            }
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

                // Calcular estadísticas
                bool tieneDatosSexo = frecuencias.Any(f => f.NroMachos > 0 || f.NroHembras > 0 || f.NroIndeterminados > 0);

                if (tieneDatosSexo)
                {
                    var statsMachos = CalculateStats(frecuencias, f => f.NroMachos, cutoff);
                    var statsHembras = CalculateStats(frecuencias, f => f.NroHembras, cutoff);
                    var statsIndet = CalculateStats(frecuencias, f => f.NroIndeterminados, cutoff);
                    var statsTotal = CalculateStats(frecuencias, f => f.NroMachos + f.NroHembras + f.NroIndeterminados, cutoff);

                    // Calcular Porcentajes
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
                }
                else
                {
                    // Caso sin determinación de sexo: se basa en NroTotal
                    var statsSinSexo = CalculateStats(frecuencias, f => f.NroTotal, cutoff);
                    statsSinSexo.Porcent = statsSinSexo.SumN > 0 ? 100 : 0;

                    WriteStatsRow(worksheet, ref currentRow, "Sin determinar sexo", statsSinSexo);
                    WriteStatsRow(worksheet, ref currentRow, "Total", statsSinSexo, true);
                }

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
            
            // Los valores ya vienen calculados correctamente (porcentajes ya tienen el x100)
            ws.Cell(row, 2).Value = stats.Media;
            ws.Cell(row, 3).Value = stats.DesvSt;
            ws.Cell(row, 4).Value = stats.Porcent;
            ws.Cell(row, 5).Value = stats.CoefV;
            
            ws.Cell(row, 6).Value = stats.SumN;
            ws.Cell(row, 7).Value = stats.SumX;
            ws.Cell(row, 8).Value = stats.SumX2;
            ws.Cell(row, 9).Value = stats.PorcentLimit;

            if (isBold)
            {
                ws.Range(row, 1, row, 9).Style.Font.Bold = true;
            }

            // Bordes y formato numérico
            var range = ws.Range(row, 1, row, 9);
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            
            // Decimales para Media, DesvSt, Porcent, CoefV y PorcentLimit
            ws.Range(row, 2, row, 5).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00";

            // Enteros para Suma N, Suma X y Suma X2
            ws.Range(row, 6, row, 8).Style.NumberFormat.Format = "#,##0";

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
                "7210030201" => 32, // Polaca
                "7210040102" => 61, // Merluza Austral
                "7210020101" => 40, // Salilota australis
                "7218320101" => 82, // Merluza Negra
                "7218350201" => 29, // Savorín
                "7218160501" => 30, // Pescadilla común
                "7204020101" => 9,  // Anchoíta
                "7105010101" => 56, // Gatuzo
                "7218250101" => 30, // Pez palo
                "7218360101" => 24, // Caballa
                "7109010105" => 78, // Raya
                "5139440101" => 7,  // Centolla (70mm)
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
            
            worksheet.Cell(currentRow, 4).Value = totalKilos > 0 ? (totalDescarte * 100.0 / totalKilos) : 0;
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
            worksheet.Cell(currentRow, 4).Value = totalKilos > 0 ? (totalDescarte * 100.0 / totalKilos) : 0;
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

            double area = LegacyDecoder.CalculateGridArea(lance.LatitudInicioDecimal.Value, lance.LongitudInicioDecimal.Value);
            return Math.Truncate(area).ToString("0");
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
