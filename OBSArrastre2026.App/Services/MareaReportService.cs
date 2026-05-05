using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using OBSArrastre2026.App.Models.Import;
using OBSArrastre2026.App.Models.Reports;

namespace OBSArrastre2026.App.Services;

public interface IMareaReportService
{
    byte[] GenerateValidationPdf(MareaValidationReport report);
    byte[] GenerateControlProduccionPdf(ControlProduccionReport report);
}

public class MareaReportService : IMareaReportService
{
    static MareaReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateValidationPdf(MareaValidationReport report)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily(Fonts.Verdana));

                ComposeHeader(page.Header(), report.Barco, report.Marea, report.Año, report.FechaInicioMarea, report.FechaFinMarea, "REPORTE DE AUDITORÍA DE MAREA");
                ComposeValidationContent(page.Content(), report);
                ComposeFooter(page.Footer());
            });
        }).GeneratePdf();
    }

    public byte[] GenerateControlProduccionPdf(ControlProduccionReport report)
    {
        return Document.Create(container =>
        {
            // Primera parte: Balance de masa por Etapa (Landscape)
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily(Fonts.Verdana));

                ComposeHeader(page.Header(), report.Barco, report.Marea, report.Anio, report.FechaInicioMarea, report.FechaFinMarea, "CONTROL DE CAPTURA Y PRODUCCIÓN POR ETAPA");
                
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

                ComposeHeader(page.Header(), report.Barco, report.Marea, report.Anio, report.FechaInicioMarea, report.FechaFinMarea, "RESUMEN POR ÁREA Y DETALLE POR ETAPA");
                
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
        }).GeneratePdf();
    }

    private void ComposeEtapaSubHeader(ColumnDescriptor col, ControlProduccionEtapaReport etapa)
    {
        col.Item().PaddingTop(10).Background(Colors.Grey.Lighten4).Padding(5).Row(row => 
        {
            row.RelativeItem().Text($"ETAPA {etapa.NumeroEtapa}").FontSize(10).SemiBold();
            row.RelativeItem().AlignRight().Text($"{etapa.FechaInicio:dd/MM/yyyy} - {etapa.FechaFin:dd/MM/yyyy}").FontSize(9);
        });
    }

    private void ComposeHeader(IContainer container, string barco, string marea, int anio, DateTime? fechaInicio, DateTime? fechaFin, string titulo)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(titulo).FontSize(14).SemiBold().FontColor(Colors.Blue.Medium);
                    
                    var mareaInfo = $"{barco} - Marea {marea} ({anio})";
                    if (fechaInicio.HasValue && fechaFin.HasValue)
                    {
                        mareaInfo += $" | {fechaInicio:dd/MM/yyyy} — {fechaFin:dd/MM/yyyy}";
                    }
                    c.Item().Text(mareaInfo).FontSize(9);
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
            col.Item().PaddingTop(15).PaddingBottom(5).Text(speciesGroup.Key).FontSize(10).SemiBold().FontColor(Colors.Blue.Medium);
            
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

            foreach (var item in report.ProduccionDetalle)
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

    private static IContainer HeaderStyle(IContainer container) => container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);

    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text("OBS Arrastre 2026 - Auditoría de Calidad").FontSize(8).FontColor(Colors.Grey.Medium);
            row.RelativeItem().AlignRight().Text(x =>
            {
                x.Span("Página ");
                x.CurrentPageNumber();
            });
        });
    }
}
