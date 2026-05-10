using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using OBSArrastre2026.App.Models.Import;
using OBSArrastre2026.App.Models.Reports;
using OBSArrastre2026.App.Data.Entities;
using Xceed.Words.NET;
using Xceed.Document.NET;
using XColor = Xceed.Drawing.Color;
using SkiaSharp;

namespace OBSArrastre2026.App.Services;

public interface IMareaReportService
{
    Task<byte[]> GenerateValidationPdfAsync(MareaValidationReport report);
    Task<byte[]> GenerateControlProduccionPdfAsync(ControlProduccionReport report);
    Task<byte[]> GenerateMareaSummaryPdfAsync(MareaSummaryReport report);
    Task<byte[]> GenerateMareaSummaryWordAsync(MareaSummaryReport report);
    Task<byte[]> GenerateFullMareaReportWordAsync(Marea marea, List<Lance> lances, List<RegistroProduccion> produccion, MareaSummaryReport summary);
    Task<byte[]> GenerateMareaReportTemplateAsync(Marea marea, List<Lance> lances, List<RegistroProduccion> produccion, MareaSummaryReport summary);
}

public class MareaReportService : IMareaReportService
{
    private readonly IMapRenderingService _mapRenderingService;
    private readonly IUserSettingsService _userSettingsService;

    static MareaReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public MareaReportService(IMapRenderingService mapRenderingService, IUserSettingsService userSettingsService)
    {
        _mapRenderingService = mapRenderingService;
        _userSettingsService = userSettingsService;
    }

    public async Task<byte[]> GenerateMareaSummaryPdfAsync(MareaSummaryReport report)
    {
        return await Task.Run(() => QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily(Fonts.Verdana));

                ComposeHeader(page.Header(), report.Barco, report.Marea, report.Anio, report.FechaInicioMarea, report.FechaFinMarea, "RESUMEN DE DATOS DE MAREA",
                    report.BuqueCodigo, report.ObservadorNombre, report.ObservadorApellido, report.ObservadorCodigo);
                
                page.Content().PaddingVertical(10).Column(col => 
                {
                    ComposeMareaSummarySection(col, report.ResumenGeneral, report.ResumenEtapas, report);

                    if (report.ResumenEtapas.Any())

                    {
                        foreach (var etapa in report.ResumenEtapas)
                        {
                            col.Item().PageBreak();
                            ComposeMareaSummarySection(col, etapa);
                        }
                    }
                });

                ComposeFooter(page.Footer());
            });
        }).GeneratePdf());
    }

    public async Task<byte[]> GenerateMareaSummaryWordAsync(MareaSummaryReport report)
    {
        return await Task.Run(() =>
        {
            using var ms = new MemoryStream();
            using var doc = DocX.Create(ms);

            // Título
            var title = doc.InsertParagraph("RESUMEN DE DATOS DE MAREA")
                .Font("Times New Roman")
                .FontSize(12)
                .Bold();
            title.Alignment = Alignment.center;
            doc.InsertParagraph().SpacingAfter(10);

            // Datos de cabecera
            var pCab = doc.InsertParagraph()
                .Font("Times New Roman")
                .FontSize(12);
            pCab.Append($"Buque: ").Font("Times New Roman");
            pCab.Append(report.Barco).Font("Times New Roman");
            pCab.Append($" (Código: {report.BuqueCodigo})").Font("Times New Roman");
            pCab.SpacingAfter(2);
            
            doc.InsertParagraph($"Marea: {report.Marea}/{report.Anio}")
                .Font("Times New Roman")
                .FontSize(12)
                .SpacingAfter(2);
            doc.InsertParagraph($"Período: {report.FechaInicioMarea:dd/MM/yyyy} - {report.FechaFinMarea:dd/MM/yyyy}")
                .Font("Times New Roman")
                .FontSize(12)
                .SpacingAfter(2);
            doc.InsertParagraph($"Observador: {report.ObservadorApellido}, {report.ObservadorNombre} ({report.ObservadorCodigo})")
                .Font("Times New Roman")
                .FontSize(12)
                .SpacingAfter(15);

            // Resumen General (Narrativa)
            doc.InsertParagraph("RESUMEN GENERAL")
                .Font("Times New Roman")
                .FontSize(12)
                .Bold()
                .Color(XColor.DarkBlue)
                .SpacingAfter(10);
            
            var parrafos = MareaSummaryNarrativeBuilder.Build(report);
            foreach (var p in parrafos)
            {
                var para = doc.InsertParagraph();
                para.Alignment = Alignment.both;
                para.Font("Times New Roman").FontSize(10); // Tamaño 10 para el resumen textual

                foreach (var span in p.Spans)
                {
                    var formatted = para.Append(span.Text);
                    formatted.Font("Times New Roman").FontSize(10);

                    switch (span.Style)
                    {
                        case NarrativaSpanStyle.Bold:
                            formatted.Bold();
                            break;
                        case NarrativaSpanStyle.Italic:
                            formatted.Italic();
                            break;
                        case NarrativaSpanStyle.BoldItalic:
                            formatted.Bold().Italic();
                            break;
                        case NarrativaSpanStyle.Underline:
                            formatted.UnderlineStyle(UnderlineStyle.singleLine);
                            break;
                    }
                }
                para.SpacingAfter(8);
            }

            doc.Save();
            return ms.ToArray();
        });
    }

    private void ComposeMareaSummarySection(ColumnDescriptor col, MareaSummarySection section, List<MareaSummarySection>? allEtapas = null, MareaSummaryReport? fullReport = null)
    {

        col.Item().PaddingTop(10).Background(Colors.Grey.Lighten4).Padding(8).Text(section.Titulo).FontSize(12).SemiBold().FontColor(Colors.Blue.Darken3);


        col.Item().PaddingVertical(10).Row(row =>
        {
            row.RelativeItem().Column(c =>
            {
                c.Item().Text("Días Navegados").FontSize(7).FontColor(Colors.Grey.Medium);
                c.Item().Text(section.DiasNavegados.ToString()).FontSize(10).SemiBold();
            });
            row.RelativeItem().Column(c =>
            {
                c.Item().Text("Días de Pesca").FontSize(7).FontColor(Colors.Grey.Medium);
                c.Item().Text(section.DiasPesca.ToString()).FontSize(10).SemiBold();
            });
            row.RelativeItem().Column(c =>
            {
                c.Item().Text("Total Lances").FontSize(7).FontColor(Colors.Grey.Medium);
                c.Item().Text(section.CantidadLances.ToString()).FontSize(10).SemiBold();
            });
            row.RelativeItem().Column(c =>
            {
                c.Item().Text("Muestras Captura").FontSize(7).FontColor(Colors.Grey.Medium);
                c.Item().Text(section.CantidadMuestrasCaptura.ToString()).FontSize(10).SemiBold();
            });
            row.RelativeItem().Column(c =>
            {
                c.Item().Text("Muestras Descarte").FontSize(7).FontColor(Colors.Grey.Medium);
                c.Item().Text(section.CantidadMuestrasDescarte.ToString()).FontSize(10).SemiBold();
            });
            row.RelativeItem().Column(c =>
            {
                c.Item().Text("Submuestras").FontSize(7).FontColor(Colors.Grey.Medium);
                c.Item().Text(section.CantidadSubmuestras.ToString()).FontSize(10).SemiBold();
            });
        });

        // Detalle de Etapas (solo en resumen general si hay más de una)
        if (!section.EsEtapa && allEtapas != null && allEtapas.Count > 1)
        {
            col.Item().PaddingTop(15).Text("DETALLE DE ETAPAS REALIZADAS").FontSize(10).SemiBold().FontColor(Colors.Blue.Darken3);
            col.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(60); // # Etapa
                    columns.RelativeColumn(2);  // Fecha Inicio - Fin
                    columns.RelativeColumn(1);  // Días Navegados
                    columns.RelativeColumn(1);  // Días Pesca
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderStyle).Text("# Etapa");
                    header.Cell().Element(HeaderStyle).Text("Fecha Inicio - Fin");
                    header.Cell().Element(HeaderStyle).AlignRight().Text("Días Nav.");
                    header.Cell().Element(HeaderStyle).AlignRight().Text("Días Pesca");
                });

                foreach (var etapa in allEtapas)
                {
                    table.Cell().Element(CellStyle).Text(etapa.NumeroEtapa?.ToString() ?? "-");
                    table.Cell().Element(CellStyle).Text($"{etapa.FechaInicio:dd/MM/yyyy} — {etapa.FechaFin:dd/MM/yyyy}");
                    table.Cell().Element(CellStyle).AlignRight().Text(etapa.DiasNavegados.ToString());
                    table.Cell().Element(CellStyle).AlignRight().Text(etapa.DiasPesca.ToString());
                }

                IContainer CellStyle(IContainer container) => container.PaddingVertical(2).BorderBottom(1).BorderColor(Colors.Grey.Lighten4);
            });
        }


        // Especies Muestreadas
        if (section.EspeciesMuestreadas.Any())
        {
            col.Item().PaddingTop(15).Text("ESPECIES MUESTREADAS").FontSize(10).SemiBold().FontColor(Colors.Blue.Darken3);
            col.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3); // Especie
                    columns.RelativeColumn(1); // M. Captura
                    columns.RelativeColumn(1); // M. Descarte
                    columns.RelativeColumn(1); // M. c/Sub.
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderStyle).Text("Especie");
                    header.Cell().Element(HeaderStyle).AlignRight().Text("M. Captura");
                    header.Cell().Element(HeaderStyle).AlignRight().Text("M. Descarte");
                    header.Cell().Element(HeaderStyle).AlignRight().Text("M. c/Sub.");
                });

                foreach (var item in section.EspeciesMuestreadas)
                {
                    table.Cell().Element(CellStyle).Text(item.NombreCientifico).Italic();
                    table.Cell().Element(CellStyle).AlignRight().Text(item.MuestrasCaptura.ToString());
                    table.Cell().Element(CellStyle).AlignRight().Text(item.MuestrasDescarte.ToString());
                    table.Cell().Element(CellStyle).AlignRight().Text(item.MuestrasConSubmuestra.ToString());
                }
                
                IContainer CellStyle(IContainer container) => container.PaddingVertical(2).BorderBottom(1).BorderColor(Colors.Grey.Lighten4);
            });
        }

        // Especies Objetivo
        col.Item().PaddingTop(10).Text("PRINCIPALES ESPECIES OBJETIVO").FontSize(10).SemiBold().FontColor(Colors.Blue.Darken3);
        col.Item().PaddingTop(5).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3); // Especie
                columns.RelativeColumn(1.5f); // Captura
                columns.RelativeColumn(1.5f); // Descarte
                columns.RelativeColumn(1.2f); // Desc %
                columns.RelativeColumn(1.5f); // Prod
                columns.RelativeColumn(1); // Lan
                columns.RelativeColumn(1); // Días
                columns.RelativeColumn(1.2f); // Juv %
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderStyle).Text("Especie");
                header.Cell().Element(HeaderStyle).AlignRight().Text("Captura (kg)");
                header.Cell().Element(HeaderStyle).AlignRight().Text("Descarte (kg)");
                header.Cell().Element(HeaderStyle).AlignRight().Text("Desc %");
                header.Cell().Element(HeaderStyle).AlignRight().Text("Prod (kg)");
                header.Cell().Element(HeaderStyle).AlignRight().Text("Lan");
                header.Cell().Element(HeaderStyle).AlignRight().Text("Días");
                header.Cell().Element(HeaderStyle).AlignRight().Text("Juv %");
            });

            foreach (var item in section.EspeciesObjetivo)
            {
                var style = item.EsObjetivo ? (Func<IContainer, IContainer>)(c => c.PaddingVertical(2).BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Background(Colors.Blue.Lighten4)) 
                                           : (Func<IContainer, IContainer>)(c => c.PaddingVertical(2).BorderBottom(1).BorderColor(Colors.Grey.Lighten4));

                table.Cell().Element(style).Text(item.NombreCientifico).Italic();
                table.Cell().Element(style).AlignRight().Text(item.CapturaTotal.ToString("N1"));
                table.Cell().Element(style).AlignRight().Text(item.DescarteKg.ToString("N1"));
                table.Cell().Element(style).AlignRight().Text(item.DescartePorcentaje.ToString("N2") + "%");
                table.Cell().Element(style).AlignRight().Text(item.ProduccionTotal.ToString("N1"));
                table.Cell().Element(style).AlignRight().Text(item.NroLances.ToString());
                table.Cell().Element(style).AlignRight().Text(item.NroDias.ToString());
                table.Cell().Element(style).AlignRight().Text(item.PorcentajeJuveniles?.ToString("N2") + "%" ?? "-");
            }
        });

        // Áreas
        col.Item().PaddingTop(20).Row(row =>
        {
            row.RelativeItem().Column(c =>
            {
                c.Item().Text("ÁREAS DE TRABAJO").FontSize(10).SemiBold().FontColor(Colors.Blue.Darken3);
                c.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(); // Área
                        columns.RelativeColumn(); // Lances
                        columns.RelativeColumn(); // Captura
                        columns.RelativeColumn(); // Días
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderStyle).Text("Área");
                        header.Cell().Element(HeaderStyle).AlignRight().Text("Lances");
                        header.Cell().Element(HeaderStyle).AlignRight().Text("Captura");
                        header.Cell().Element(HeaderStyle).AlignRight().Text("Días");
                    });

                    foreach (var area in section.Areas)
                    {
                        table.Cell().Element(CellStyle).Text(area.Area);
                        table.Cell().Element(CellStyle).AlignRight().Text(area.CantidadLances.ToString());
                        table.Cell().Element(CellStyle).AlignRight().Text(area.CapturaKg.ToString("N1"));
                        table.Cell().Element(CellStyle).AlignRight().Text(area.DiasPesca.ToString());
                    }

                    IContainer CellStyle(IContainer container) => container.PaddingVertical(2).BorderBottom(1).BorderColor(Colors.Grey.Lighten4);
                });
            });

            row.ConstantItem(20);

            row.ConstantItem(150).Column(c =>
            {
                c.Item().PaddingTop(30).Border(1).BorderColor(Colors.Blue.Darken3).Padding(10).Column(inner =>
                {
                    inner.Item().Text("DESTACADOS").FontSize(9).SemiBold().FontColor(Colors.Blue.Darken3).AlignCenter();
                    
                    if (section.AreaMasLances == section.AreaMayorCaptura)
                    {
                        inner.Item().PaddingTop(15).Text("Área de mayor captura y Nº de lances").FontSize(7).FontColor(Colors.Grey.Medium).AlignCenter();
                        inner.Item().Text(section.AreaMasLances).FontSize(14).SemiBold().AlignCenter();
                    }
                    else
                    {
                        inner.Item().PaddingTop(10).Text("Área con más lances").FontSize(7).FontColor(Colors.Grey.Medium).AlignCenter();
                        inner.Item().Text(section.AreaMasLances).FontSize(11).SemiBold().AlignCenter();
                        inner.Item().PaddingTop(10).Text("Área de mayor captura").FontSize(7).FontColor(Colors.Grey.Medium).AlignCenter();
                        inner.Item().Text(section.AreaMayorCaptura).FontSize(11).SemiBold().AlignCenter();
                    }
                });
            });
        });
    }

    public async Task<byte[]> GenerateValidationPdfAsync(MareaValidationReport report)
    {
        return await Task.Run(() => QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily(Fonts.Verdana));

                ComposeHeader(page.Header(), report.Barco, report.Marea, report.Año, report.FechaInicioMarea, report.FechaFinMarea, "REPORTE DE AUDITORÍA DE MAREA",
                    report.BuqueCodigo, report.ObservadorNombre, report.ObservadorApellido, report.ObservadorCodigo);
                ComposeValidationContent(page.Content(), report);
                ComposeFooter(page.Footer());
            });
        }).GeneratePdf());
    }

    public async Task<byte[]> GenerateControlProduccionPdfAsync(ControlProduccionReport report)
    {

        return await Task.Run(() => QuestPDF.Fluent.Document.Create(container =>
        {
            // Primera parte: Balance de masa por Etapa (Landscape)
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily(Fonts.Verdana));

                var tituloProduccion = report.Etapas.Count > 1 ? "CONTROL DE CAPTURA Y PRODUCCIÓN POR ETAPA" : "CONTROL DE CAPTURA Y PRODUCCIÓN";
                ComposeHeader(page.Header(), report.Barco, report.Marea, report.Anio, report.FechaInicioMarea, report.FechaFinMarea, tituloProduccion,
                    report.BuqueCodigo, report.ObservadorNombre, report.ObservadorApellido, report.ObservadorCodigo);
                
                page.Content().PaddingVertical(10).Column(col => 
                {
                    foreach(var etapa in report.Etapas)
                    {
                        if (report.Etapas.Count > 1)
                        {
                            ComposeEtapaSubHeader(col, etapa);
                        }

                        ComposeControlProduccionContent(col.Item(), etapa);
                        col.Item().PaddingBottom(20);
                    }
                });

                ComposeFooter(page.Footer());
            });

            // Segunda parte: Resumen por Área y Detalle por Etapa (Portrait)
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily(Fonts.Verdana));

                ComposeHeader(page.Header(), report.Barco, report.Marea, report.Anio, report.FechaInicioMarea, report.FechaFinMarea, "RESUMEN POR ÁREA Y DETALLE DE PRODUCCIÓN",
                    report.BuqueCodigo, report.ObservadorNombre, report.ObservadorApellido, report.ObservadorCodigo);
                
                page.Content().PaddingVertical(10).Column(col => 
                {
                    foreach(var etapa in report.Etapas)
                    {
                        if (report.Etapas.Count > 1)
                        {
                            ComposeEtapaSubHeader(col, etapa);
                        }
                        
                        ComposeAreaSummaryContent(col, etapa);
                        col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        ComposeProductionDetailContent(col, etapa);
                        col.Item().PaddingBottom(30);
                    }
                });

                ComposeFooter(page.Footer());
            });
        }).GeneratePdf());
    }

    private void ComposeEtapaSubHeader(ColumnDescriptor col, ControlProduccionEtapaReport etapa)
    {
        col.Item().PaddingTop(10).Background(Colors.Grey.Lighten4).Padding(5).Row(row => 
        {
            row.RelativeItem().Text($"ETAPA {etapa.NumeroEtapa}").FontSize(10).SemiBold();
            row.RelativeItem().AlignRight().Text($"{etapa.FechaInicio:dd/MM/yyyy} - {etapa.FechaFin:dd/MM/yyyy}").FontSize(9);
        });
    }

    private void ComposeHeader(IContainer container, string barco, string marea, int anio, DateTime? fechaInicio, DateTime? fechaFin, string titulo,
        int? buqueCodigo = null, string? obsNombre = null, string? obsApellido = null, int? obsCodigo = null)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(titulo).FontSize(14).SemiBold().FontColor(Colors.Blue.Darken3);
                    
                    var buqueInfo = buqueCodigo.HasValue ? $"{barco} ({buqueCodigo})" : barco;
                    var mareaInfo = $"{buqueInfo} - Marea {marea} ({anio})";
                    
                    if (fechaInicio.HasValue && fechaFin.HasValue)
                    {
                        mareaInfo += $" | {fechaInicio:dd/MM/yyyy} — {fechaFin:dd/MM/yyyy}";
                    }
                    c.Item().Text(mareaInfo).FontSize(9);

                    if (!string.IsNullOrEmpty(obsNombre) || !string.IsNullOrEmpty(obsApellido))
                    {
                        var obsInfo = "Observador: ";
                        if (!string.IsNullOrEmpty(obsApellido)) obsInfo += obsApellido;
                        if (!string.IsNullOrEmpty(obsNombre)) obsInfo += (string.IsNullOrEmpty(obsApellido) ? "" : ", ") + obsNombre;
                        if (obsCodigo.HasValue) obsInfo += $" ({obsCodigo})";
                        c.Item().Text(obsInfo).FontSize(8).Italic().FontColor(Colors.Grey.Darken2);
                    }
                });

                row.ConstantItem(100).Column(c =>
                {
                    c.Item().Text("Reporte").AlignRight().FontSize(9).FontColor(Colors.Grey.Medium);
                    c.Item().Text(DateTime.Now.ToString("dd/MM/yyyy")).AlignRight().FontSize(9);
                });
            });
        });
    }

    private void ComposeValidationContent(IContainer container, MareaValidationReport report)
    {
        container.PaddingVertical(10).Column(col =>
        {
            if (report.Etapas.Any())
            {
                col.Item().PaddingBottom(10).Row(row =>
                {
                    row.ConstantItem(40).Text("Etapas:").FontSize(7).SemiBold().FontColor(Colors.Grey.Medium);
                    
                    row.RelativeItem().Text(text =>
                    {
                        for (int i = 0; i < report.Etapas.Count; i++)
                        {
                            var e = report.Etapas[i];
                            text.Span($"E{e.Numero}: ").FontSize(7).SemiBold();
                            text.Span($"{e.FechaInicio:dd/MM} - {e.FechaFin:dd/MM}").FontSize(7);
                            
                            if (i < report.Etapas.Count - 1)
                            {
                                text.Span("  |  ").FontSize(7).FontColor(Colors.Grey.Lighten1);
                            }
                        }
                    });
                });
            }

            // Resumen ejecutivo
            col.Item().PaddingBottom(10).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Row(row =>
            {
                row.RelativeItem().Column(c => {
                    c.Item().Text("Total Lances").FontSize(7).FontColor(Colors.Grey.Medium);
                    c.Item().Text(report.TotalLances.ToString()).FontSize(9).SemiBold();
                });
                row.RelativeItem().Column(c => {
                    c.Item().Text("Errores").FontSize(7).FontColor(Colors.Grey.Medium);
                    c.Item().Text(report.TotalErrors.ToString()).FontSize(9).SemiBold().FontColor(Colors.Red.Medium);
                });
                row.RelativeItem().Column(c => {
                    c.Item().Text("Advertencias").FontSize(7).FontColor(Colors.Grey.Medium);
                    c.Item().Text(report.TotalWarnings.ToString()).FontSize(9).SemiBold().FontColor(Colors.Orange.Medium);
                });
                row.RelativeItem().Column(c => {
                    c.Item().Text("Correcciones Auto").FontSize(7).FontColor(Colors.Grey.Medium);
                    c.Item().Text(report.TotalAutoFixes.ToString()).FontSize(9).SemiBold().FontColor(Colors.Green.Medium);
                });
            });

            // Lista de Issues
            col.Item().PaddingTop(10).Text("Detalle de Observaciones").FontSize(11).SemiBold();

            if (!report.Issues.Any())
            {
                col.Item().PaddingTop(20).Text("No se encontraron inconsistencias en los datos.").Italic();
            }
            else
            {
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(80); // Categoría
                        columns.RelativeColumn();   // Mensaje
                        columns.ConstantColumn(100); // Contexto
                        columns.ConstantColumn(30);  // Checkbox
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderStyle).Text("Categoría");
                        header.Cell().Element(HeaderStyle).Text("Observación");
                        header.Cell().Element(HeaderStyle).Text("Contexto");
                        header.Cell().Element(HeaderStyle).AlignRight().Text("Ok");
                    });

                    var sortedIssues = report.Issues
                        .OrderByDescending(i => i.Level)
                        .ThenBy(i => i.Category)
                        .ThenBy(i => 
                        {
                            if (string.IsNullOrEmpty(i.Context)) return "";
                            // Intentar extraer fecha dd/MM/yyyy
                            var matchFecha = System.Text.RegularExpressions.Regex.Match(i.Context, @"(\d{2}/\d{2}/\d{4})");
                            if (matchFecha.Success) 
                            {
                                var parts = matchFecha.Groups[1].Value.Split('/');
                                return $"{parts[2]}-{parts[1]}-{parts[0]}"; // yyyy-MM-dd para sort
                            }
                            return "9999-99-99"; // Para que lances vayan después si hay mezcla
                        })
                        .ThenBy(i => 
                        {
                            if (string.IsNullOrEmpty(i.Context)) return 0;
                            // Intentar extraer número de lance
                            var matchLance = System.Text.RegularExpressions.Regex.Match(i.Context, @"Lance\s+(\d+)");
                            return matchLance.Success ? int.Parse(matchLance.Groups[1].Value) : 0;
                        })
                        .ThenBy(i => i.Message);

                    foreach (var issue in sortedIssues)
                    {
                        table.Cell().Element(ContentStyle).Text(issue.Category).FontSize(7);
                        table.Cell().Element(ContentStyle).Column(c => {
                            c.Item().Text(issue.Message).FontSize(8);
                            if (!string.IsNullOrEmpty(issue.CorrectedValue))
                            {
                                var correctionMsg = string.IsNullOrEmpty(issue.OriginalValue) 
                                    ? $"=> Corregido a: {issue.CorrectedValue}" 
                                    : $"{issue.OriginalValue} => Corregido a: {issue.CorrectedValue}";
                                c.Item().Text(correctionMsg).FontSize(7).FontColor(Colors.Green.Medium);
                            }
                        });
                        table.Cell().Element(ContentStyle).Text(issue.Context ?? "").FontSize(7);
                        table.Cell().Element(ContentStyle).Border(1).BorderColor(Colors.Grey.Medium).Height(12).Width(12).AlignCenter();

                        IContainer ContentStyle(IContainer container) 
                        {
                            var c = container.PaddingVertical(5)
                                .PaddingRight(10) // Espacio entre columnas
                                .BorderBottom(1)
                                .BorderColor(Colors.Grey.Lighten3);
                            
                            if (issue.Level == ValidationLevel.Error) c = c.Background(Colors.Red.Lighten5);
                            return c;
                        }
                    }
                });
            }
        });
    }

    private void ComposeAreaSummaryContent(ColumnDescriptor col, ControlProduccionEtapaReport report)
    {
        if (!report.AreaSummaries.Any()) return;

        col.Item().PaddingTop(10).Text("RESUMEN POR ÁREA (Especies Predominantes)").FontSize(11).SemiBold();

        var groupedBySpecies = report.AreaSummaries.GroupBy(s => s.Especie);

        foreach (var speciesGroup in groupedBySpecies)
        {
            col.Item().PaddingTop(15).PaddingBottom(5).Text(speciesGroup.Key).FontSize(10).SemiBold().FontColor(Colors.Blue.Darken3);
            
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(); // Área
                    columns.RelativeColumn(); // Captura
                    columns.RelativeColumn(); // Descarte
                    columns.RelativeColumn(); // Días
                    columns.RelativeColumn(); // Lances
                    columns.RelativeColumn(); // Horas
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderStyle).Text("Área");
                    header.Cell().Element(HeaderStyle).AlignRight().Text("Captura");
                    header.Cell().Element(HeaderStyle).AlignRight().Text("Descarte");
                    header.Cell().Element(HeaderStyle).AlignRight().Text("Días");
                    header.Cell().Element(HeaderStyle).AlignRight().Text("Lances");
                    header.Cell().Element(HeaderStyle).AlignRight().Text("Horas");
                });

                foreach (var item in speciesGroup)
                {
                    table.Cell().Element(ContentStyle).Text(item.Area);
                    table.Cell().Element(ContentStyle).AlignRight().Text(item.CapturaKg.ToString("N1"));
                    table.Cell().Element(ContentStyle).AlignRight().Text(item.DescarteKg.ToString("N1"));
                    table.Cell().Element(ContentStyle).AlignRight().Text(item.DiasPesca.ToString("N0"));
                    table.Cell().Element(ContentStyle).AlignRight().Text(item.CantidadLances.ToString("N0"));
                    table.Cell().Element(ContentStyle).AlignRight().Text(item.TotalHoras.ToString("N2"));
                }

                // Total de la especie
                table.Cell().Element(FooterStyle).Text("Total");
                table.Cell().Element(FooterStyle).AlignRight().Text(speciesGroup.Sum(s => s.CapturaKg).ToString("N1"));
                table.Cell().Element(FooterStyle).AlignRight().Text(speciesGroup.Sum(s => s.DescarteKg).ToString("N1"));
                table.Cell().Element(FooterStyle).Text("");
                table.Cell().Element(FooterStyle).Text("");
                table.Cell().Element(FooterStyle).AlignRight().Text(speciesGroup.Sum(s => s.TotalHoras).ToString("N2"));

                IContainer ContentStyle(IContainer container) => container.PaddingVertical(2).BorderBottom(1).BorderColor(Colors.Grey.Lighten4);
                IContainer FooterStyle(IContainer container) => container.PaddingVertical(5).BorderTop(1).BorderColor(Colors.Black).DefaultTextStyle(x => x.SemiBold());
            });
        }
    }

    private void ComposeProductionDetailContent(ColumnDescriptor col, ControlProduccionEtapaReport report)
    {
        col.Item().PaddingTop(10).PaddingBottom(5).Text("DETALLE DE PRODUCCIÓN").FontSize(11).SemiBold();
        
        col.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3); // Especie
                columns.RelativeColumn(2); // Producto
                columns.RelativeColumn(2); // Categoría
                columns.RelativeColumn(2); // Kilos
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderStyle).Text("Especie");
                header.Cell().Element(HeaderStyle).Text("Producto");
                header.Cell().Element(HeaderStyle).Text("Categoría");
                header.Cell().Element(HeaderStyle).AlignRight().Text("Kilos");
            });

            foreach (var item in report.ProduccionDetalle.OrderByDescending(p => p.Kilos))
            {
                table.Cell().Element(ContentStyle).Text(item.Especie);
                table.Cell().Element(ContentStyle).Text(item.Producto);
                table.Cell().Element(ContentStyle).Text(item.Categoria);
                table.Cell().Element(ContentStyle).AlignRight().Text(item.Kilos.ToString("N1"));
            }

            table.Cell().Element(FooterStyle).Text("Total General");
            table.Cell().Element(FooterStyle).Text("");
            table.Cell().Element(FooterStyle).Text("");
            table.Cell().Element(FooterStyle).AlignRight().Text(report.ProduccionDetalle.Sum(p => p.Kilos).ToString("N1"));

            IContainer ContentStyle(IContainer container) => container.PaddingVertical(2).BorderBottom(1).BorderColor(Colors.Grey.Lighten4);
            IContainer FooterStyle(IContainer container) => container.PaddingVertical(5).BorderTop(1).BorderColor(Colors.Black).DefaultTextStyle(x => x.SemiBold());
        });
    }

    private void ComposeControlProduccionContent(IContainer container, ControlProduccionEtapaReport report)
    {
        container.PaddingVertical(5).Column(col =>
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3); // Especie
                    columns.RelativeColumn(2); // Prod. Total
                    columns.RelativeColumn(2); // Capt. Recon.
                    columns.RelativeColumn(2); // Captura
                    columns.RelativeColumn(2); // Descarte
                    columns.RelativeColumn(2); // Capt. Retenida
                    columns.RelativeColumn(2); // Dif. Kg
                    columns.RelativeColumn(1.5f); // Dif. %
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderStyle).Text("Especie");
                    header.Cell().Element(HeaderStyle).AlignRight().Text("Prod. Total");
                    header.Cell().Element(HeaderStyle).AlignRight().Text("Capt. Recon.");
                    header.Cell().Element(HeaderStyle).AlignRight().Text("Captura");
                    header.Cell().Element(HeaderStyle).AlignRight().Text("Descarte");
                    header.Cell().Element(HeaderStyle).AlignRight().Text("Capt. Retenida");
                    header.Cell().Element(HeaderStyle).AlignRight().Text("Dif. Kg");
                    header.Cell().Element(HeaderStyle).AlignRight().Text("Dif. %");
                });

                foreach (var item in report.Items)
                {
                    table.Cell().Element(ContentStyle).Text(item.Especie);
                    table.Cell().Element(ContentStyle).AlignRight().Text(item.ProduccionTotal.ToString("N1"));
                    table.Cell().Element(ContentStyle).AlignRight().Text(item.CapturaReconstruida.ToString("N1"));
                    table.Cell().Element(ContentStyle).AlignRight().Text(item.CapturaBruta.ToString("N1"));
                    table.Cell().Element(ContentStyle).AlignRight().Text(item.DescarteKg.ToString("N1"));
                    table.Cell().Element(ContentStyle).AlignRight().Text(item.CapturaRetenida.ToString("N1"));
                    table.Cell().Element(ContentStyle).AlignRight().Text(item.DiferenciaKg.ToString("N1"));
                    
                    var diffCell = table.Cell().Element(ContentStyle).AlignRight();
                    if (item.HasDiferenciaSignificativa)
                    {
                        diffCell.Text(item.DiferenciaPorcentaje).SemiBold().FontColor(Colors.Red.Medium);
                    }
                    else
                    {
                        diffCell.Text(item.DiferenciaPorcentaje);
                    }

                    IContainer ContentStyle(IContainer container) => container.PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);
                }
            });
        });
    }

    public async Task<byte[]> GenerateFullMareaReportWordAsync(Marea marea, List<Lance> lances, List<RegistroProduccion> produccion, MareaSummaryReport summary)
    {
        return await Task.Run(async () =>
        {
            using var ms = new MemoryStream();
            using (var doc = DocX.Create(ms))
            {
                InsertTechnicalHeader(doc, marea);

                await PopulateMareaReportContentAsync(doc, marea, lances, produccion, summary);

                doc.Save();
                return ms.ToArray();
            }
        });
    }

    public async Task<byte[]> GenerateMareaReportTemplateAsync(Marea marea, List<Lance> lances, List<RegistroProduccion> produccion, MareaSummaryReport summary)
    {
        return await Task.Run(async () =>
        {
            var templatePath = Path.Combine(AppContext.BaseDirectory, "Resources", "Templates", "Portada_informe.docx");
            
            if (!File.Exists(templatePath))
            {
                return await GenerateFullMareaReportWordAsync(marea, lances, produccion, summary);
            }

            using var ms = new MemoryStream();
            using (var doc = DocX.Load(templatePath))
            {
                // 1. Reemplazo de marcadores en la Portada
                var settings = _userSettingsService.GetSettings();
                string revisor = $"{settings.RevisorApellido}, {settings.RevisorNombre}".Trim(' ', ',');
                if (string.IsNullOrWhiteSpace(revisor)) revisor = "{Revisor no configurado}";

                doc.ReplaceText("{ApellidoNombreRevisor}", revisor);
                doc.ReplaceText("{AñoMarea}", marea.AnioInidep.ToString());
                doc.ReplaceText("{NroMarea}", marea.NumeroInidep.ToString("00"));

                // 2. Reemplazo en Pies de Página (DocX requiere reemplazo explícito en cada sección de footer)
                doc.Footers.Odd?.ReplaceText("{AñoMarea}", marea.AnioInidep.ToString());
                doc.Footers.Odd?.ReplaceText("{NroMarea}", marea.NumeroInidep.ToString("00"));
                
                doc.Footers.Even?.ReplaceText("{AñoMarea}", marea.AnioInidep.ToString());
                doc.Footers.Even?.ReplaceText("{NroMarea}", marea.NumeroInidep.ToString("00"));
                
                doc.Footers.First?.ReplaceText("{AñoMarea}", marea.AnioInidep.ToString());
                doc.Footers.First?.ReplaceText("{NroMarea}", marea.NumeroInidep.ToString("00"));

                // 3. Volcado de contenido técnico (Incluyendo cabecera técnica en la segunda página)
                InsertTechnicalHeader(doc, marea);
                await PopulateMareaReportContentAsync(doc, marea, lances, produccion, summary);

                doc.SaveAs(ms);
                return ms.ToArray();
            }
        });
    }

    private void InsertTechnicalHeader(DocX doc, Marea marea)
    {
        var settings = _userSettingsService.GetSettings();
        string revisor = $"{settings.RevisorApellido}, {settings.RevisorNombre}".Trim(' ', ',');
        if (string.IsNullOrWhiteSpace(revisor)) revisor = "{Revisor no configurado}";

        // Título principal
        doc.InsertParagraph("INFORME FINAL DE MAREA")
            .Font("Times New Roman").FontSize(18).Bold().Alignment = Alignment.center;
        
        doc.InsertParagraph(revisor)
            .Font("Times New Roman").FontSize(16).Alignment = Alignment.center;
            
        doc.InsertParagraph("Programa Adquisición de Información Biológico-Pesquera y Ambiental")
            .Font("Times New Roman").FontSize(12).Alignment = Alignment.center;
        doc.InsertParagraph().SpacingAfter(20);

        // Cabecera manual
        doc.InsertParagraph($"Marea: {marea.NumeroInidep}/{marea.AnioInidep}")
            .Font("Times New Roman").FontSize(14).Bold().SpacingAfter(12);

        // Fechas de realización
        var pFechas = doc.InsertParagraph("Fechas de realización: ")
            .Font("Times New Roman").FontSize(14).Bold();

        var etapasOrdenadas = marea.Etapas.OrderBy(e => e.FechaZarpada).ToList();
        if (etapasOrdenadas.Any())
        {
            for (int i = 0; i < etapasOrdenadas.Count; i++)
            {
                var e = etapasOrdenadas[i];
                if (i > 0) 
                {
                    pFechas.Append(i == etapasOrdenadas.Count - 1 ? " y " : ", ")
                        .Font("Times New Roman").FontSize(14).Bold();
                }
                
                if (i > 0 && i == etapasOrdenadas.Count - 1) 
                {
                    pFechas.Append("desde el ").Font("Times New Roman").FontSize(14).Bold();
                }

                pFechas.Append($"{e.FechaZarpada:dd/MM/yyyy} al {e.FechaArribo:dd/MM/yyyy}")
                    .Font("Times New Roman").FontSize(14).Bold();
            }
        }
        else
        {
            pFechas.Append($"{marea.FechaInicio:dd/MM/yyyy} al {marea.FechaFin:dd/MM/yyyy}")
                .Font("Times New Roman").FontSize(14).Bold();
        }
        pFechas.SpacingAfter(12);

        var meta = MareaMetadataHelper.GetMetadata(marea);
        doc.InsertParagraph($"Asistente Investigación Pesquera: {meta.ObservadorCodigo:N0}")
            .Font("Times New Roman").FontSize(14).Bold().SpacingAfter(12);
        
        string eslora = meta.BuqueEslora.HasValue ? $"{meta.BuqueEslora:N2} m" : "— m";
        string potencia = meta.BuquePotencia.HasValue ? $"{meta.BuquePotencia:N0} HP" : "— HP";
        
        doc.InsertParagraph($"Nombre del Buque: {meta.BuqueCodigo:N0}. Eslora: {eslora}. Potencia: {potencia}.")
            .Font("Times New Roman").FontSize(14).Bold().SpacingAfter(12);
        
        string tipoBuque = !string.IsNullOrEmpty(meta.TipoBuque) ? meta.TipoBuque : "—";
        doc.InsertParagraph($"Tipo de buque: {tipoBuque}")
            .Font("Times New Roman").FontSize(14).Bold().SpacingAfter(15);
    }

    private async Task PopulateMareaReportContentAsync(DocX doc, Marea marea, List<Lance> lances, List<RegistroProduccion> produccion, MareaSummaryReport summary)
    {
        // Resumen
        doc.InsertParagraph("Resumen").Font("Times New Roman").FontSize(14).Bold().SpacingAfter(6);
        var parrafos = MareaSummaryNarrativeBuilder.Build(summary);
        foreach (var p in parrafos)
        {
            var para = doc.InsertParagraph();
            para.Alignment = Alignment.both;
            para.Font("Times New Roman").FontSize(10);
            foreach (var span in p.Spans)
            {
                var text = para.Append(span.Text);
                text.Font("Times New Roman").FontSize(10);
                switch (span.Style)
                {
                    case NarrativaSpanStyle.Bold: text.Bold(); break;
                    case NarrativaSpanStyle.Italic: text.Italic(); break;
                    case NarrativaSpanStyle.BoldItalic: text.Bold().Italic(); break;
                    case NarrativaSpanStyle.Underline: text.UnderlineStyle(UnderlineStyle.singleLine); break;
                }
            }
            para.SpacingAfter(4);
        }
        doc.InsertParagraph("No se realizó la tarea de registrar la captura incidental de aves y mamíferos marinos.")
            .Font("Times New Roman").FontSize(10);
        doc.InsertParagraph("Aleteo de tiburones: No realizó.")
            .Font("Times New Roman").FontSize(10);
        doc.InsertParagraph("Habitabilidad del buque: Buena")
            .Font("Times New Roman").FontSize(10).SpacingAfter(10);

        // Palabras clave
        doc.InsertParagraph("Palabras Clave").Font("Times New Roman").FontSize(12).Bold().SpacingAfter(4);
        doc.InsertParagraph("{PalabrasClave}").Font("Times New Roman").FontSize(12).SpacingAfter(15);

        // Descripción artes de pesca
        doc.InsertParagraph("Descripción artes de pesca").Font("Times New Roman").FontSize(14).Bold().SpacingAfter(6);
        doc.InsertParagraph("Denominación y tipo: Red de arrastre de fondo.").Font("Times New Roman").FontSize(12).Italic();
        doc.InsertParagraph("Características generales:").Font("Times New Roman").FontSize(12).Italic().SpacingAfter(40);

        // Metodología
        doc.InsertParagraph("Metodología de captura, estimación y producción").Font("Times New Roman").FontSize(14).Bold().SpacingAfter(6);
        doc.InsertParagraph().SpacingAfter(60);

        // Resultados
        doc.InsertParagraph("Resultados obtenidos").Font("Times New Roman").FontSize(14).Bold().SpacingAfter(10);

        var etapas = marea.Etapas.OrderBy(e => e.FechaZarpada).ToList();
        bool multipleEtapas = etapas.Count > 1;

        for (int i = 0; i < etapas.Count; i++)
        {
            var etapa = etapas[i];
            var etapaLances = lances.Where(l => l.MareaEtapaId == etapa.ID).ToList();
            var etapaProduccion = produccion.Where(p => p.MareaEtapaId == etapa.ID).ToList();

            if (multipleEtapas)
            {
                doc.InsertParagraph($"VIAJE {i + 1}").Font("Times New Roman").FontSize(16).Bold().Alignment = Alignment.center;
                doc.InsertParagraph().SpacingAfter(10);
            }

            // Tablas de datos
            doc.InsertParagraph("Datos de captura, esfuerzo y producción").Font("Times New Roman").FontSize(12).Bold().SpacingAfter(6);

            doc.InsertParagraph("Captura por especie").Font("Times New Roman").FontSize(12).Italic().SpacingBefore(6);
            InsertSpeciesTable(doc, etapaLances);

            doc.InsertParagraph("Captura por área").Font("Times New Roman").FontSize(12).Italic().SpacingBefore(10);
            InsertAreaTable(doc, etapaLances);

            doc.InsertParagraph("Producción").Font("Times New Roman").FontSize(12).Italic().SpacingBefore(10);
            InsertProduccionTable(doc, etapaProduccion);

                    // Mapa
                    var lancesConCoord = etapaLances.Where(l => l.LatitudInicioDecimal.HasValue && l.LongitudInicioDecimal.HasValue).ToList();
                    if (lancesConCoord.Any())
                    {
                        doc.InsertParagraph("Localización del área de pesca").Font("Times New Roman").FontSize(12).Bold().SpacingBefore(10).Alignment = Alignment.center;
                        try
                        {
                            var lats = lancesConCoord.Select(l => l.LatitudInicioDecimal!.Value);
                            var lons = lancesConCoord.Select(l => l.LongitudInicioDecimal!.Value);
                            var mapBytes = await _mapRenderingService.RenderMapToBytesAsync(lats, lons);
                            using var mapMs = new MemoryStream(mapBytes);
                            var img = doc.AddImage(mapMs);
                            var pic = img.CreatePicture();
                            using var codec = SKCodec.Create(new MemoryStream(mapBytes));
                            double ratio = (double)codec.Info.Height / codec.Info.Width;
                            pic.Width = 450;
                            pic.Height = (int)(450 * ratio);
                            var pMap = doc.InsertParagraph();
                            pMap.AppendPicture(pic);
                            pMap.Alignment = Alignment.center;
                        }
                        catch { }
                    }

                    // Frecuencias de tallas
                    doc.InsertParagraph("Distribución de frecuencias de longitudes").Font("Times New Roman").FontSize(14).Bold().SpacingBefore(10).SpacingAfter(6);
                    await InsertFrequenciesSectionAsync(doc, etapaLances);

        }
    }

    private void InsertSpeciesTable(DocX doc, List<Lance> lances)
    {
        var itemsCaptura = lances.SelectMany(l => l.ItemsCaptura).ToList();
        var summary = itemsCaptura
            .GroupBy(i => i.EspecieID)
            .Select(g => {
                var esp = g.First().Especie;
                var lancesEsp = g.Select(i => i.Lance).Where(l => l != null).Distinct().ToList();
                double kilos = g.Sum(i => i.CapturaTotalKgCalculado);
                double descarte = g.Sum(i => i.PesoDescarteCalculado);
                double horas = lancesEsp.Sum(l => {
                    if (DateTime.TryParse($"{l!.Fecha} {l.HoraInicio}", out var start) && 
                        DateTime.TryParse($"{l.Fecha} {l.HoraFinal}", out var end))
                    {
                        if (end < start) end = end.AddDays(1);
                        return (end - start).TotalHours;
                    }
                    return 0;
                });

                return new {
                    Nombre = esp?.NombreCientifico ?? "Desconocida",
                    Kilos = kilos,
                    Descarte = descarte,
                    DescartePct = kilos > 0 ? (descarte * 100.0 / kilos) : 0,
                    Lances = lancesEsp.Count,
                    Dias = lancesEsp.Select(l => l!.Fecha).Distinct().Count(),
                    Horas = horas
                };
            })
            .OrderByDescending(s => s.Kilos)
            .ToList();

        var table = doc.AddTable(summary.Count + 2, 7);
        table.Alignment = Alignment.center;
        table.Design = TableDesign.TableGrid;
        table.AutoFit = AutoFit.Window;
        table.SetWidthsPercentage(new float[] { 36, 11, 11, 10, 9, 9, 14 }, null);

        // Headers
        string[] headers = { "Especie", "Kilos", "Descarte", "Desc.%", "Lances", "Días", "Horas" };
        for (int i = 0; i < headers.Length; i++)
        {
            var hp = table.Rows[0].Cells[i].Paragraphs[0];
            hp.Append(headers[i]).Bold().Font("Times New Roman").FontSize(12);
            table.Rows[0].Cells[i].FillColor = XColor.LightGray;
            if (i > 0) hp.Alignment = Alignment.right;
        }

        int rowIdx = 1;
        foreach (var s in summary)
        {
            table.Rows[rowIdx].Cells[0].Paragraphs[0].Append(s.Nombre).Italic().Font("Times New Roman").FontSize(12);
            void SetR(int col, string val) { var p = table.Rows[rowIdx].Cells[col].Paragraphs[0]; p.Append(val).Font("Times New Roman").FontSize(12); p.Alignment = Alignment.right; }
            SetR(1, FormatVal(s.Kilos));
            SetR(2, FormatVal(s.Descarte));
            SetR(3, FormatVal(s.DescartePct));
            SetR(4, s.Lances.ToString());
            SetR(5, s.Dias.ToString());
            SetR(6, FormatVal(s.Horas));
            rowIdx++;
        }
 
        // Totales
        double totalK = summary.Sum(s => s.Kilos);
        double totalD = summary.Sum(s => s.Descarte);
        table.Rows[rowIdx].Cells[0].Paragraphs[0].Append("Totales").Bold().Font("Times New Roman").FontSize(12);
        void SetT(int col, string val) { var p = table.Rows[rowIdx].Cells[col].Paragraphs[0]; p.Append(val).Bold().Font("Times New Roman").FontSize(12); p.Alignment = Alignment.right; }
        SetT(1, FormatVal(totalK));
        SetT(2, FormatVal(totalD));
        SetT(3, totalK > 0 ? FormatVal(totalD * 100.0 / totalK) : "0");
        SetT(4, lances.Count.ToString());
        SetT(5, lances.Select(l => l.Fecha).Distinct().Count().ToString());

        SetT(5, lances.Select(l => l.Fecha).Distinct().Count().ToString());

        doc.InsertTable(table);
    }

    private void InsertAreaTable(DocX doc, List<Lance> lances)
    {
        var areaSummary = lances
            .GroupBy(l => GetCuadricula(l))
            .Select(g => {
                double kilos = g.SelectMany(l => l.ItemsCaptura).Sum(i => i.CapturaTotalKgCalculado);
                double descarte = g.SelectMany(l => l.ItemsCaptura).Sum(i => i.PesoDescarteCalculado);
                double horas = g.Sum(l => {
                    if (DateTime.TryParse($"{l!.Fecha} {l.HoraInicio}", out var start) && 
                        DateTime.TryParse($"{l.Fecha} {l.HoraFinal}", out var end))
                    {
                        if (end < start) end = end.AddDays(1);
                        return (end - start).TotalHours;
                    }
                    return 0;
                });
                return new {
                    Area = g.Key,
                    Kilos = kilos,
                    Descarte = descarte,
                    DescartePct = kilos > 0 ? (descarte * 100.0 / kilos) : 0,
                    Lances = g.Count(),
                    Dias = g.Select(l => l.Fecha).Distinct().Count(),
                    Horas = horas
                };
            })
            .OrderByDescending(a => a.Kilos)
            .ToList();

        var table = doc.AddTable(areaSummary.Count + 1, 7);
        table.Alignment = Alignment.center;
        table.Design = TableDesign.TableGrid;
        table.AutoFit = AutoFit.Window;
        table.SetWidthsPercentage(new float[] { 36, 11, 11, 10, 9, 9, 14 }, null);

        string[] headers = { "Área", "Kilos", "Descarte", "Desc.%", "Lances", "Días", "Horas" };
        for (int i = 0; i < headers.Length; i++)
        {
            var hp = table.Rows[0].Cells[i].Paragraphs[0];
            hp.Append(headers[i]).Bold().Font("Times New Roman").FontSize(12);
            table.Rows[0].Cells[i].FillColor = XColor.LightGray;
            if (i > 0) hp.Alignment = Alignment.right;
        }

        for (int i = 0; i < areaSummary.Count; i++)
        {
            var a = areaSummary[i];
            table.Rows[i+1].Cells[0].Paragraphs[0].Append(a.Area).Font("Times New Roman").FontSize(12);
            void SetR(int col, string val) { var p = table.Rows[i+1].Cells[col].Paragraphs[0]; p.Append(val).Font("Times New Roman").FontSize(12); p.Alignment = Alignment.right; }
            SetR(1, FormatVal(a.Kilos));
            SetR(2, FormatVal(a.Descarte));
            SetR(3, FormatVal(a.DescartePct));
            SetR(4, a.Lances.ToString());
            SetR(5, a.Dias.ToString());
            SetR(6, FormatVal(a.Horas));
        }
        doc.InsertTable(table);
    }

    private void InsertProduccionTable(DocX doc, List<RegistroProduccion> produccion)
    {
        if (!produccion.Any())
        {
            doc.InsertParagraph("No se registraron datos de producción para esta etapa.").Font("Times New Roman").FontSize(12);
            return;
        }

        var grouped = produccion
            .GroupBy(p => new { p.EspecieId, p.IdProducto, p.Categoria })
            .Select(g => new {
                Especie = g.First().Especie?.NombreCientifico ?? "Desconocida",
                Producto = g.First().Producto?.Codigo ?? g.First().IdProducto ?? "S/C",
                Categoria = g.Key.Categoria ?? "-",
                Kilos = g.Sum(p => p.Kg ?? 0),
                Factor = g.First().Factor ?? 1.0
            })
            .OrderByDescending(p => p.Kilos)
            .ToList();

        var table = doc.AddTable(grouped.Count + 1, 5);
        table.Alignment = Alignment.center;
        table.Design = TableDesign.TableGrid;
        table.AutoFit = AutoFit.Window;
        table.SetWidthsPercentage(new float[] { 38, 22, 20, 12, 8 }, null);

        string[] headers = { "Especie", "Producto", "Categoría", "Kilos", "Factor" };
        for (int i = 0; i < headers.Length; i++)
        {
            var hp = table.Rows[0].Cells[i].Paragraphs[0];
            hp.Append(headers[i]).Bold().Font("Times New Roman").FontSize(12);
            table.Rows[0].Cells[i].FillColor = XColor.LightGray;
            if (i >= 3) hp.Alignment = Alignment.right;
        }

        for (int i = 0; i < grouped.Count; i++)
        {
            var p = grouped[i];
            table.Rows[i+1].Cells[0].Paragraphs[0].Append(p.Especie).Italic().Font("Times New Roman").FontSize(12);
            table.Rows[i+1].Cells[1].Paragraphs[0].Append(p.Producto).Font("Times New Roman").FontSize(12);
            table.Rows[i+1].Cells[2].Paragraphs[0].Append(p.Categoria).Font("Times New Roman").FontSize(12);
            void SetR(int col, string val) { var para = table.Rows[i+1].Cells[col].Paragraphs[0]; para.Append(val).Font("Times New Roman").FontSize(12); para.Alignment = Alignment.right; }
            SetR(3, FormatVal(p.Kilos));
            SetR(4, FormatVal(p.Factor));
        }
        doc.InsertTable(table);
    }

    private async Task InsertFrequenciesSectionAsync(DocX doc, List<Lance> lances)
    {
        var muestras = lances.SelectMany(l => l.Muestras).ToList();
        var grupos = muestras
            .GroupBy(m => new { m.EspecieID, m.TipoMuestra })
            .Where(g => g.Count() >= 3)
            .OrderBy(g => g.First().Especie?.NombreCientifico)
            .ThenBy(g => g.Key.TipoMuestra)
            .ToList();

        for (int gi = 0; gi < grupos.Count; gi++)
        {
            var g = grupos[gi];
            var especie = g.First().Especie;
            if (especie == null) continue;

            string tipoStr = g.Key.TipoMuestra == 2 ? "Descarte" : "Captura";
            doc.InsertParagraph($"Especie: {especie.NombreCientifico} ({tipoStr})")
                .Font("Times New Roman")
                .FontSize(12)
                .Bold()
                .Italic()
                .SpacingAfter(5);

            int cutoff = GetSpeciesCutoff(especie.CodigoInidep);
            var frecuencias = g.SelectMany(m => m.FrecuenciasTallas).ToList();

            // Tabla de estadísticas
            InsertStatsTable(doc, frecuencias, cutoff);
            doc.InsertParagraph().SpacingAfter(10);

            // Gráfico (si hay puntos)
            try
            {
                var statsPoints = frecuencias
                    .GroupBy(f => f.Talla)
                    .Select(pts => new {
                        Talla = pts.Key,
                        Machos = pts.Sum(f => f.NroMachos),
                        Hembras = pts.Sum(f => f.NroHembras),
                        Indet = pts.Sum(f => f.NroIndeterminados),
                        Total = pts.Sum(f => f.NroTotal)
                    })
                    .OrderBy(pts => pts.Talla)
                    .ToList();

                double totalN = statsPoints.Sum(p => (double)p.Total);
                if (totalN > 0)
                {
                    var chartData = statsPoints.Select(p => (
                        Talla: p.Talla,
                        Machos: (p.Machos * 100.0 / totalN),
                        Hembras: (p.Hembras * 100.0 / totalN),
                        Indet: (p.Indet * 100.0 / totalN),
                        Total: (p.Total * 100.0 / totalN)
                    )).ToList();

                    var chartBytes = RenderFrequencyChart(chartData, cutoff, especie.NombreCientifico, especie.CodigoInidep == "5139030101");
                    if (chartBytes.Length > 0)
                    {
                        using var chartMs = new MemoryStream(chartBytes);
                        var chartImg = doc.AddImage(chartMs);
                        var chartPic = chartImg.CreatePicture();

                        using var cCodec = SKCodec.Create(new MemoryStream(chartBytes));
                        double cRatio = (double)cCodec.Info.Height / cCodec.Info.Width;

                        chartPic.Width = 500;
                        chartPic.Height = (int)(500 * cRatio);

                        var pChart = doc.InsertParagraph();
                        pChart.AppendPicture(chartPic);
                        pChart.Alignment = Alignment.center;
                    }
                }
            }
            catch { }

            // Salto de página entre especies, pero no después de la última
            if (gi < grupos.Count - 1)
                doc.InsertParagraph().InsertPageBreakAfterSelf();
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
            double val = Math.Max(p.Machos, Math.Max(p.Hembras, p.Indet));
            if (plotTotal) val = Math.Max(val, p.Total);
            return val;
        });
        if (maxYValue <= 0) maxYValue = 10;
        else maxYValue *= 1.15;

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
        var gridPaint = new SKPaint { Color = SKColors.Gray, StrokeWidth = 0.5f, IsAntialias = true };
        var textPaint = new SKPaint { Color = SKColors.Black, TextSize = 14, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Times New Roman") };
        var labelCenterPaint = new SKPaint { Color = SKColors.Black, TextSize = 16, IsAntialias = true, FakeBoldText = true, Typeface = SKTypeface.FromFamilyName("Times New Roman"), TextAlign = SKTextAlign.Center };

        for (double yVal = 0; yVal <= maxY + (step/10.0); yVal += step)
        {
            float yPos = height - margin - 40 - (float)((yVal / maxY) * chartHeight);
            canvas.DrawLine(margin, yPos, width - margin, yPos, gridPaint);
            string format = (step % 1 == 0) ? "0" : "0.0";
            canvas.DrawText(yVal.ToString(format), margin - 10, yPos + 5, new SKPaint { Color = SKColors.Black, TextSize = 12, TextAlign = SKTextAlign.Right, IsAntialias = true });
        }

        double xInterval = 5;
        for (double xVal = minX; xVal <= maxX; xVal += xInterval)
        {
            float xPos = margin + (float)(((xVal - minX) / (maxX - minX)) * chartWidth);
            canvas.DrawText(xVal.ToString("0"), xPos, height - margin - 20, new SKPaint { Color = SKColors.Black, TextSize = 12, TextAlign = SKTextAlign.Center, IsAntialias = true });
            canvas.DrawLine(xPos, height - margin - 40, xPos, height - margin - 35, axisPaint);
        }

        canvas.DrawLine(margin, height - margin - 40, width - margin, height - margin - 40, axisPaint);
        canvas.DrawLine(margin, margin, margin, height - margin - 40, axisPaint);
        string xLabel = isLangostino ? "Talla (mm)" : "Talla (cm)";
        canvas.DrawText(xLabel, width / 2, height - margin + 15, labelCenterPaint);
        
        canvas.Save();
        canvas.RotateDegrees(-90, 25, height / 2);
        canvas.DrawText("Frecuencia relativa (%)", 25, height / 2, labelCenterPaint);
        canvas.Restore();

        void DrawSeries(Func<(double Talla, double Machos, double Hembras, double Indet, double Total), double> selector, SKColor color, float[] dashPattern = null, float strokeWidth = 2.5f)
        {
            var rawPoints = dataPoints.Select(p => new SKPoint(
                margin + (float)(((p.Talla - minX) / (maxX - minX)) * chartWidth),
                height - margin - 40 - (float)((selector(p) / maxY) * chartHeight)
            )).ToList();

            if (rawPoints.Count < 2) return;

            using var path = new SKPath();
            path.MoveTo(rawPoints[0]);

            // Smoothing algorithm (Catmull-Rom approximation)
            for (int i = 0; i < rawPoints.Count - 1; i++)
            {
                var p0 = i == 0 ? rawPoints[i] : rawPoints[i - 1];
                var p1 = rawPoints[i];
                var p2 = rawPoints[i + 1];
                var p3 = i == rawPoints.Count - 2 ? rawPoints[i + 1] : rawPoints[i + 2];

                // Control points
                var cp1 = new SKPoint(p1.X + (p2.X - p0.X) / 6, p1.Y + (p2.Y - p0.Y) / 6);
                var cp2 = new SKPoint(p2.X - (p3.X - p1.X) / 6, p2.Y - (p3.Y - p1.Y) / 6);

                path.CubicTo(cp1, cp2, p2);
            }

            var paint = new SKPaint { Color = color, Style = SKPaintStyle.Stroke, StrokeWidth = strokeWidth, IsAntialias = true, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round };
            if (dashPattern != null) paint.PathEffect = SKPathEffect.CreateDash(dashPattern, 0);
            canvas.DrawPath(path, paint);
        }

        // Draw Series
        if (plotTotal && hasTotal) DrawSeries(p => p.Total, SKColors.Black, null, 3.0f);
        if (hasHembras) DrawSeries(p => p.Hembras, SKColors.Black, new float[] { 10, 5, 2, 5 }, 1.5f);
        if (hasMachos) DrawSeries(p => p.Machos, SKColors.Black, null, 1.5f);
        if (hasIndet) DrawSeries(p => p.Indet, SKColors.Black, new float[] { 2, 5 }, 1.5f);

        // Draw Legend
        var legendItems = new List<(string Label, float[] Dash, float Width)>();
        if (hasMachos) legendItems.Add(("machos", null, 1.5f));
        if (hasHembras) legendItems.Add(("hembras", new float[] { 10, 5, 2, 5 }, 1.5f));
        if (plotTotal && hasTotal) legendItems.Add(("totales", null, 3.0f));
        if (hasIndet) legendItems.Add(("indet.", new float[] { 2, 5 }, 1.5f));

        if (legendItems.Any())
        {
            float legendY = height - 30;
            float itemWidth = 120;
            float totalLegendWidth = legendItems.Count * itemWidth;
            float startX = (width - totalLegendWidth) / 2;

            var legendTextPaint = new SKPaint { Color = SKColors.Black, TextSize = 14, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Times New Roman") };
            
            for (int i = 0; i < legendItems.Count; i++)
            {
                var item = legendItems[i];
                float itemX = startX + (i * itemWidth);
                
                // Sample line
                var lPaint = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Stroke, StrokeWidth = item.Width, IsAntialias = true };
                if (item.Dash != null) lPaint.PathEffect = SKPathEffect.CreateDash(item.Dash, 0);
                canvas.DrawLine(itemX, legendY - 5, itemX + 40, legendY - 5, lPaint);
                
                // Label
                canvas.DrawText(item.Label, itemX + 45, legendY, legendTextPaint);
            }
        }

        if (cutoff > 0 && cutoff >= minX && cutoff <= maxX)
        {
            float xPos = margin + (float)(((cutoff - minX) / (maxX - minX)) * chartWidth);
            canvas.DrawLine(xPos, margin, xPos, height - margin - 40, axisPaint);
        }

        using var image = surface.Snapshot();
        using var pngData = image.Encode(SKEncodedImageFormat.Png, 100);
        return pngData.ToArray();
    }

    private void InsertStatsTable(DocX doc, List<FrecuenciaTalla> frecuencias, int cutoff)
    {
        var statsTotal = CalculateStats(frecuencias, f => f.NroTotal, cutoff);
        var statsMachos = CalculateStats(frecuencias, f => f.NroMachos, cutoff);
        var statsHembras = CalculateStats(frecuencias, f => f.NroHembras, cutoff);
        var statsIndet = CalculateStats(frecuencias, f => f.NroIndeterminados, cutoff);

        // Ajustar porcentajes de sexo
        if (statsTotal.SumN > 0)
        {
            statsMachos.Porcent = (statsMachos.SumN * 100.0 / statsTotal.SumN);
            statsHembras.Porcent = (statsHembras.SumN * 100.0 / statsTotal.SumN);
            statsIndet.Porcent = (statsIndet.SumN * 100.0 / statsTotal.SumN);
            statsTotal.Porcent = 100;
        }

        bool sinSexo = frecuencias.Sum(f => f.NroMachos) == 0
                    && frecuencias.Sum(f => f.NroHembras) == 0
                    && frecuencias.Sum(f => f.NroIndeterminados) == 0;

        int numRows = sinSexo ? 3 : 5;
        var table = doc.AddTable(numRows, 8);
        table.Alignment = Alignment.center;
        table.Design = TableDesign.TableGrid;
        table.AutoFit = AutoFit.Window;
        table.SetWidthsPercentage(new float[] { 30, 10, 10, 10, 10, 10, 10, 10 }, null);

        string[] headers = { "Sexo", "Media", "Desv.St", "Porcent.", "Suma N", "Suma X", "Suma X2", cutoff > 0 ? $"%<{cutoff}" : "%<0" };
        for (int i = 0; i < headers.Length; i++)
        {
            var hp = table.Rows[0].Cells[i].Paragraphs[0];
            hp.Append(headers[i]).Bold().Font("Times New Roman").FontSize(12);
            table.Rows[0].Cells[i].FillColor = XColor.LightGray;
            if (i > 0) hp.Alignment = Alignment.right;
        }

        void FillRow(int r, string label, StatsResult s, bool bold = false)
        {
            table.Rows[r].Cells[0].Paragraphs[0].Append(label).Font("Times New Roman").FontSize(12);
            void SetR(int col, string val) { var p = table.Rows[r].Cells[col].Paragraphs[0]; p.Append(val).Font("Times New Roman").FontSize(12); p.Alignment = Alignment.right; }
            SetR(1, FormatVal(s.Media));
            SetR(2, FormatVal(s.DesvSt));
            SetR(3, FormatVal(s.Porcent));
            SetR(4, s.SumN.ToString("N0"));
            SetR(5, s.SumX.ToString("N0"));
            SetR(6, s.SumX2.ToString("N0"));
            SetR(7, FormatVal(s.PorcentLimit));
            if (bold) foreach(var cell in table.Rows[r].Cells) cell.Paragraphs[0].Bold();
        }

        if (sinSexo)
        {
            statsTotal.Porcent = 100;
            FillRow(1, "Sin determinar sexo", statsTotal);
            FillRow(2, "Total", statsTotal, true);
        }
        else
        {
            FillRow(1, "Machos", statsMachos);
            FillRow(2, "Hembras", statsHembras);
            FillRow(3, "Indet.", statsIndet);
            FillRow(4, "Total", statsTotal, true);
        }

        doc.InsertTable(table);
    }

    // --- Helpers de cálculo (Basados en ExcelReportService) ---
    private class StatsResult
    {
        public double Media { get; set; }
        public double DesvSt { get; set; }
        public double Porcent { get; set; }
        public double SumN { get; set; }
        public double SumX { get; set; }
        public double SumX2 { get; set; }
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
        double sumX2_PRG = sumX * (sumN > 1 ? sumX / (sumN - 1) : 0);
        double sumNLimit = cutoff > 0 ? frecuencias.Where(f => f.Talla < cutoff).Sum(f => (double)getCount(f)) : 0;

        result.SumN = sumN;
        result.SumX = sumX;
        result.SumX2 = sumX2_PRG;
        result.Media = media;
        result.DesvSt = desvSt;
        result.PorcentLimit = (sumNLimit / sumN) * 100;
        return result;
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

    private int GetSpeciesCutoff(string? codigoInidep)
    {
        if (string.IsNullOrEmpty(codigoInidep)) return 0;
        return codigoInidep switch {
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
            "5139440101" => 11,  // Centolla (110mm)
            _ => 0
        };
    }

    private static IContainer HeaderStyle(IContainer container) => container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);

    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text("Programa Observadores a Bordo - Control de datos de marea").FontSize(8).FontColor(Colors.Grey.Medium);
            row.RelativeItem().AlignRight().Text(x =>
            {
                x.Span("Página ");
                x.CurrentPageNumber();
            });
        });
    }
    private string FormatVal(double val)
    {
        return val % 1 == 0 ? val.ToString("N0") : val.ToString("N2");
    }
}
