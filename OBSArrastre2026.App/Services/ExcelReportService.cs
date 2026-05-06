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
        public async Task GenerateTablasExcelAsync(IEnumerable<Lance> lances, string outputPath)
        {
            await Task.Run(() =>
            {
                using var workbook = new XLWorkbook();
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
                var lancesList = lances.ToList();
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
                
                // Desc. % es el promedio de los datos de las filas (promedio simple)
                worksheet.Cell(currentRow, 4).Value = speciesSummary.Any() ? speciesSummary.Average(s => s.DescartePorc) : 0;
                
                worksheet.Cell(currentRow, 5).Value = lancesList.Count;
                worksheet.Cell(currentRow, 6).Value = lancesList.Select(l => l.Fecha).Distinct().Count();
                // La columna Horas del total se deja vacía explícitamente
                worksheet.Cell(currentRow, 7).Value = Blank.Value;
                
                var totalRange = worksheet.Range(currentRow, 1, currentRow, 7);
                totalRange.Style.Font.Bold = true;

                // Formateo numérico
                // Kilos, Descarte y Horas con 2 decimales y separador de miles
                var decimalRange = worksheet.Range(2, 2, currentRow, 4); // Kilos, Descarte, %
                decimalRange.Style.NumberFormat.Format = "#,##0.00";
                
                var horasRange = worksheet.Range(2, 7, currentRow - 1, 7); // Horas (filas de datos)
                horasRange.Style.NumberFormat.Format = "#,##0.00";

                // Lances y Días como enteros con separador de miles
                var integerRange = worksheet.Range(2, 5, currentRow, 6);
                integerRange.Style.NumberFormat.Format = "#,##0";

                // Bordes
                var fullRange = worksheet.Range(1, 1, currentRow, 7);
                fullRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                fullRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                // Ajustar columnas
                worksheet.Columns().AdjustToContents();

                workbook.SaveAs(outputPath);
            });
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
