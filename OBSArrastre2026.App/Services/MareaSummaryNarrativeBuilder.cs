using System.Text;
using OBSArrastre2026.App.Models.Reports;

namespace OBSArrastre2026.App.Services;

/// <summary>
/// Genera el resumen narrativo textual del resumen general de marea.
/// El texto es adaptativo: contempla una o varias etapas, una o varias especies objetivo,
/// porcentajes de descarte parcial o total, y presencia de especies secundarias.
/// </summary>
public static class MareaSummaryNarrativeBuilder
{
    // -------------------------------------------------------------------------
    // Punto de entrada principal
    // -------------------------------------------------------------------------

    /// <summary>
    /// Construye el texto narrativo completo del resumen de marea.
    /// Retorna una lista de párrafos listos para renderizar en el PDF.
    /// Cada elemento de la lista es un párrafo independiente.
    /// </summary>
    public static List<NarrativaParagraph> Build(MareaSummaryReport report)
    {
        var parrafos = new List<NarrativaParagraph>();

        if (!report.NarrativaEtapas.Any())
            return parrafos;

        parrafos.Add(BuildParrafoGeneral(report));

        foreach (var etapa in report.NarrativaEtapas)
        {
            parrafos.Add(BuildParrafoEtapa(etapa, report.NarrativaEtapas.Count));
        }

        if (report.NarrativaMuestras.Any())
        {
            parrafos.Add(BuildParrafoMuestras(report));
        }

        return parrafos;
    }

    // -------------------------------------------------------------------------
    // Párrafo 1: Resumen general de la marea
    // -------------------------------------------------------------------------

    private static NarrativaParagraph BuildParrafoGeneral(MareaSummaryReport report)
    {
        var sb = new NarrativaSpanBuilder();
        int nEtapas = report.NarrativaEtapas.Count;

        // "El buque realizó N viaje(s)/etapa(s)..."
        sb.Normal("El buque realizó ");
        sb.Normal(Pluralizar(nEtapas, "un viaje", $"{nEtapas} viajes"));
        sb.Normal(", ");

        // Fechas de cada etapa
        for (int i = 0; i < report.NarrativaEtapas.Count; i++)
        {
            var e = report.NarrativaEtapas[i];
            if (i > 0 && i == report.NarrativaEtapas.Count - 1)
                sb.Normal(" y ");
            else if (i > 0)
                sb.Normal(", ");

            sb.Normal($"desde el {e.FechaInicio:dd/MM/yyyy} al {e.FechaFin:dd/MM/yyyy}");
        }
        sb.Normal(". ");

        // Captura total, descarte, lances, días
        sb.Normal($"Captura total de marea: {report.NarrativaCapturaTotal:N3} kg, ");
        sb.Normal(FormatDescartePct(report.NarrativaDescartePct));
        sb.Normal($", {report.NarrativaTotalLances} {Pluralizar(report.NarrativaTotalLances, "lance", "lances")}");
        sb.Normal($", {report.NarrativaTotalDiasPesca} {Pluralizar(report.NarrativaTotalDiasPesca, "día pesca", "días pesca")}. ");

        // Especie(s) objetivo
        if (report.NarrativaEspeciesObjetivo.Any())
        {
            sb.Normal(Pluralizar(report.NarrativaEspeciesObjetivo.Count, "Especie objetivo: ", "Especies objetivo: "));
            for (int i = 0; i < report.NarrativaEspeciesObjetivo.Count; i++)
            {
                var eo = report.NarrativaEspeciesObjetivo[i];
                if (i > 0) sb.Normal(i == report.NarrativaEspeciesObjetivo.Count - 1 ? " y " : ", ");
                sb.Bold($"{eo.NombreVulgar} ");
                sb.Italic($"({eo.NombreCientifico})");
                sb.Normal($", {eo.CapturaKg:N3} kg de captura, ");
                sb.Normal(FormatDescartePctBreve(eo.DescartePct));
                sb.Normal(" de descarte");
            }
            sb.Normal(".");
        }

        return new NarrativaParagraph(sb.ToSpans());
    }

    // -------------------------------------------------------------------------
    // Párrafo por etapa
    // -------------------------------------------------------------------------

    private static NarrativaParagraph BuildParrafoEtapa(NarrativaEtapa etapa, int totalEtapas)
    {
        var sb = new NarrativaSpanBuilder();

        // Encabezado ordinal
        string ordinal = totalEtapas > 1
            ? $"{OrdinalMasculino(etapa.Numero)} viaje: "
            : "El buque ";

        sb.Normal(ordinal);

        // Cuadrados estadísticos donde operó
        if (etapa.Cuadrados.Any())
        {
            string inicioFrase = totalEtapas > 1
                ? "el buque operó en "
                : "operó en ";
            sb.Normal(inicioFrase);

            if (etapa.Cuadrados.Count == 1)
            {
                sb.Normal($"el cuadrado estadístico {etapa.Cuadrados[0]}");
            }
            else
            {
                sb.Normal($"los siguientes cuadrados estadísticos: ");
                sb.Normal(ListarCuadrados(etapa.Cuadrados));
            }

            // Cuadrado dominante (mayor cantidad de operaciones de pesca y captura)
            if (etapa.CuadradoDominante != null && etapa.Cuadrados.Count > 1)
            {
                sb.Normal($", siendo el de mayor cantidad de operaciones de pesca y captura el {etapa.CuadradoDominante} con ");
                sb.Normal($"{etapa.CuadradoDominanteCapturaKg:N3} kg en {etapa.CuadradoDominanteLances} {Pluralizar(etapa.CuadradoDominanteLances, "lance", "lances")} ");
                sb.Normal($"durante {etapa.CuadradoDominanteDias} {Pluralizar(etapa.CuadradoDominanteDias, "día", "días")}");
            }
            sb.Normal(". ");
        }

        // Captura y descarte de la etapa
        sb.Normal($"La captura del {Pluralizar(totalEtapas > 1 ? 2 : 1, "viaje", "viaje")} fue de ");
        sb.Normal($"{etapa.CapturaKg:N3} kg ");

        if (etapa.DescartePct >= 99.9)
        {
            sb.Normal("descarándose en su totalidad. ");
        }
        else if (etapa.DescartePct > 0)
        {
            sb.Normal($"con un {etapa.DescartePct:N2}% de descarte en ");
            sb.Normal($"{etapa.TotalLances} {Pluralizar(etapa.TotalLances, "lance", "lances")} ");
            sb.Normal($"durante {etapa.DiasPesca} {Pluralizar(etapa.DiasPesca, "día", "días")}. ");
        }
        else
        {
            sb.Normal($"sin descarte en ");
            sb.Normal($"{etapa.TotalLances} {Pluralizar(etapa.TotalLances, "lance", "lances")} ");
            sb.Normal($"durante {etapa.DiasPesca} {Pluralizar(etapa.DiasPesca, "día", "días")}. ");
        }

        // Especie objetivo de la etapa
        if (etapa.EspecieObjetivo != null)
        {
            var eo = etapa.EspecieObjetivo;
            sb.Normal($"La captura de {eo.NombreVulgar} fue de ");
            sb.Normal($"{eo.CapturaKg:N3} kg ");

            if (eo.DescarteTotal)
            {
                sb.Normal("descarándose en su totalidad. ");
            }
            else if (eo.DescartePct > 0)
            {
                sb.Normal($"con un {eo.DescartePct:N2}% de descarte. ");
            }
            else
            {
                sb.Normal("sin descarte. ");
            }
        }

        // Especies secundarias
        var espConCaptura = etapa.EspeciesSecundarias.Where(e => e.CapturaKg > 0).ToList();
        if (espConCaptura.Any())
        {
            foreach (var esp in espConCaptura)
            {
                sb.Normal($"Se observó una captura de {esp.NombreVulgar} ");
                sb.Italic($"({esp.NombreCientifico})");
                sb.Normal($" de {esp.CapturaKg:N3} kg ");

                if (esp.DescarteTotal)
                {
                    sb.Normal("descartándose en su totalidad. ");
                }
                else if (esp.DescartePct >= 99.9)
                {
                    sb.Normal("descartándose en su totalidad. ");
                }
                else if (esp.DescartePct > 0)
                {
                    sb.Normal($"con un {esp.DescartePct:N2}% de descarte. ");
                }
                else
                {
                    sb.Normal("sin descarte. ");
                }
            }
        }

        return new NarrativaParagraph(sb.ToSpans());
    }

    // -------------------------------------------------------------------------
    // Párrafo final: cantidad de muestras realizadas
    // -------------------------------------------------------------------------

    private static NarrativaParagraph BuildParrafoMuestras(MareaSummaryReport report)
    {
        var sb = new NarrativaSpanBuilder();
        int total = report.NarrativaMuestras.Sum(m => m.TotalMuestras);

        sb.Normal($"Cantidad de muestras realizadas: {total} {Pluralizar(total, "muestra total", "muestras totales")}. ");

        for (int i = 0; i < report.NarrativaMuestras.Count; i++)
        {
            var m = report.NarrativaMuestras[i];
            if (i > 0) sb.Normal(" – ");
            sb.Normal($"{m.TotalMuestras} {m.NombreVulgar} ");
            sb.Italic($"({m.NombreCientifico})");
        }
        sb.Normal(".");

        return new NarrativaParagraph(sb.ToSpans());
    }

    // -------------------------------------------------------------------------
    // Helpers de formato y gramática
    // -------------------------------------------------------------------------

    private static string Pluralizar(int cantidad, string singular, string plural)
        => cantidad == 1 ? singular : plural;

    private static string OrdinalMasculino(int n) => n switch
    {
        1 => "1er",
        2 => "2do",
        3 => "3er",
        4 => "4to",
        5 => "5to",
        6 => "6to",
        7 => "7mo",
        8 => "8vo",
        9 => "9no",
        10 => "10mo",
        _ => $"{n}°"
    };

    private static string FormatDescartePct(double pct)
    {
        if (pct >= 99.9) return "100% descarte";
        if (pct <= 0) return "0% descarte";
        return $"{pct:N2}% descarte";
    }

    private static string FormatDescartePctBreve(double pct)
    {
        if (pct >= 99.9) return "100%";
        if (pct <= 0) return "0%";
        return $"{pct:N2}%";
    }

    private static string ListarCuadrados(List<string> cuadrados)
    {
        if (cuadrados.Count == 0) return string.Empty;
        if (cuadrados.Count == 1) return cuadrados[0];
        if (cuadrados.Count == 2) return $"{cuadrados[0]} y {cuadrados[1]}";
        var sb = new StringBuilder();
        for (int i = 0; i < cuadrados.Count - 1; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(cuadrados[i]);
        }
        sb.Append($" y {cuadrados[^1]}");
        return sb.ToString();
    }
}

// -------------------------------------------------------------------------
// Tipos auxiliares para la representación de texto enriquecido en el PDF
// -------------------------------------------------------------------------

/// <summary>Tipo de estilo de un span de texto narrativo.</summary>
public enum NarrativaSpanStyle
{
    Normal,
    Bold,
    Italic,
    BoldItalic,
    Underline
}

/// <summary>Fragmento de texto con un estilo aplicado.</summary>
public record NarrativaSpan(string Text, NarrativaSpanStyle Style);

/// <summary>Párrafo narrativo compuesto por uno o más spans de texto.</summary>
public record NarrativaParagraph(List<NarrativaSpan> Spans);

/// <summary>Builder interno para construir listas de spans de forma fluida.</summary>
internal class NarrativaSpanBuilder
{
    private readonly List<NarrativaSpan> _spans = new();

    public NarrativaSpanBuilder Normal(string text)     { _spans.Add(new(text, NarrativaSpanStyle.Normal));     return this; }
    public NarrativaSpanBuilder Bold(string text)       { _spans.Add(new(text, NarrativaSpanStyle.Bold));       return this; }
    public NarrativaSpanBuilder Italic(string text)     { _spans.Add(new(text, NarrativaSpanStyle.Italic));     return this; }
    public NarrativaSpanBuilder BoldItalic(string text) { _spans.Add(new(text, NarrativaSpanStyle.BoldItalic)); return this; }
    public NarrativaSpanBuilder Underline(string text)  { _spans.Add(new(text, NarrativaSpanStyle.Underline));  return this; }

    public List<NarrativaSpan> ToSpans() => _spans;
}
