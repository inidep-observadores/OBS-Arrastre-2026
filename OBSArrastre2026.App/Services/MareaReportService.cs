using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using OBSArrastre2026.App.Models.Import;

namespace OBSArrastre2026.App.Services;

public interface IMareaReportService
{
    byte[] GenerateValidationPdf(MareaValidationReport report);
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

                ComposeHeader(page.Header(), report);
                ComposeContent(page.Content(), report);
                ComposeFooter(page.Footer());
            });
        }).GeneratePdf();
    }

    private void ComposeHeader(IContainer container, MareaValidationReport report)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("REPORTE DE AUDITORÍA DE MAREA").FontSize(14).SemiBold().FontColor(Colors.Blue.Medium);
                col.Item().Text($"{report.Barco} - Marea {report.Marea} ({report.Año})").FontSize(10);
            });

            row.ConstantItem(100).Column(col =>
            {
                col.Item().Text("Checklist").AlignRight().FontSize(10).FontColor(Colors.Grey.Medium);
                col.Item().Text(DateTime.Now.ToString("dd/MM/yyyy")).AlignRight();
            });
        });
    }

    private void ComposeContent(IContainer container, MareaValidationReport report)
    {
        container.PaddingVertical(10).Column(col =>
        {
            // Resumen ejecutivo
            col.Item().PaddingBottom(10).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Row(row =>
            {
                row.RelativeItem().Column(c => {
                    c.Item().Text("Total Lances").FontSize(7).FontColor(Colors.Grey.Medium);
                    c.Item().Text(report.TotalLances.ToString()).FontSize(9).SemiBold();
                });
                row.RelativeItem().Column(c => {
                    c.Item().Text("Errores").FontSize(7).FontColor(Colors.Grey.Medium);
                    c.Item().Text(report.Issues.Count(i => i.Level == ValidationLevel.Error).ToString()).FontSize(9).SemiBold().FontColor(Colors.Red.Medium);
                });
                row.RelativeItem().Column(c => {
                    c.Item().Text("Correcciones Auto").FontSize(7).FontColor(Colors.Grey.Medium);
                    c.Item().Text(report.Issues.Count(i => i.Level == ValidationLevel.AutoFixed).ToString()).FontSize(9).SemiBold().FontColor(Colors.Green.Medium);
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
                        header.Cell().Element(CellStyle).Text("Categoría");
                        header.Cell().Element(CellStyle).Text("Observación");
                        header.Cell().Element(CellStyle).Text("Contexto");
                        header.Cell().Element(CellStyle).AlignRight().Text("Ok");

                        static IContainer CellStyle(IContainer container) => container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                    });

                    foreach (var issue in report.Issues.OrderByDescending(i => i.Level))
                    {
                        table.Cell().Element(ContentStyle).Text(issue.Category).FontSize(7);
                        table.Cell().Element(ContentStyle).Column(c => {
                            c.Item().Text(issue.Message).FontSize(8);
                            if (!string.IsNullOrEmpty(issue.CorrectedValue))
                                c.Item().Text($"=> Corregido a: {issue.CorrectedValue}").FontSize(7).FontColor(Colors.Green.Medium);
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
