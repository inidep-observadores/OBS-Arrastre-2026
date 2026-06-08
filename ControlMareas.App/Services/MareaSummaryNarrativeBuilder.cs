using System.Text;
using ControlMareas.App.Models.Reports;

namespace ControlMareas.App.Services;

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

        if (!report.NarrativaViajes.Any())
            return parrafos;

        // Párrafo 1: Viajes y fechas
        parrafos.Add(BuildParrafoViajes(report));

        // Párrafo 2: Captura total de marea
        parrafos.Add(BuildParrafoResumenCaptura(report));

        // Párrafo 3: Especies objetivo (Punto y aparte antes)
        if (report.NarrativaEspeciesObjetivo.Any())
        {
            foreach (var p in BuildParrafosEspeciesObjetivo(report))
            {
                parrafos.Add(p);
            }
            // Agregamos un párrafo vacío para forzar el salto de línea antes de las etapas
            parrafos.Add(new NarrativaParagraph(new List<NarrativaSpan>()));
        }

        foreach (var viaje in report.NarrativaViajes)
        {
            if (viaje.Etapas.Count == 1)
            {
                parrafos.Add(BuildParrafoEtapa(viaje.Etapas[0], report.NarrativaViajes.Count, viaje.NumeroViaje));
            }
            else
            {
                parrafos.Add(BuildParrafoViajeJerarquico(viaje, report.NarrativaViajes.Count));
                foreach (var etapa in viaje.Etapas)
                {
                    parrafos.Add(BuildParrafoSubEtapa(etapa));
                }
            }
        }

        if (report.NarrativaMuestras.Any())
        {
            parrafos.Add(BuildParrafoMuestras(report));
        }

        return parrafos;
    }

    private static NarrativaParagraph BuildParrafoViajes(MareaSummaryReport report)
    {
        var sb = new NarrativaSpanBuilder();
        int nViajes = report.NarrativaViajes.Count;

        sb.Normal("El buque realizó ");
        sb.Normal(Pluralizar(nViajes, "un viaje", $"{nViajes} viajes"));
        sb.Normal(", ");

        for (int i = 0; i < report.NarrativaViajes.Count; i++)
        {
            var v = report.NarrativaViajes[i];
            if (i > 0 && i == report.NarrativaViajes.Count - 1)
                sb.Normal(" y ");
            else if (i > 0)
                sb.Normal(", ");

            sb.Normal($"desde el {v.FechaInicio:dd/MM/yyyy} al {v.FechaFin:dd/MM/yyyy}");
        }
        sb.Normal(".");

        return new NarrativaParagraph(sb.ToSpans());
    }

    private static NarrativaParagraph BuildParrafoResumenCaptura(MareaSummaryReport report)
    {
        var sb = new NarrativaSpanBuilder();

        // Captura total, descarte, lances, días
        sb.Normal($"Captura total de marea: {report.NarrativaCapturaTotal:N0} kg, ");
        sb.Normal(FormatDescartePct(report.NarrativaDescartePct));
        sb.Normal($", {report.NarrativaTotalLances} {Pluralizar(report.NarrativaTotalLances, "lance", "lances")}");
        sb.Normal($", {report.NarrativaTotalDiasPesca} {Pluralizar(report.NarrativaTotalDiasPesca, "día pesca", "días pesca")}.");

        return new NarrativaParagraph(sb.ToSpans());
    }

    private static List<NarrativaParagraph> BuildParrafosEspeciesObjetivo(MareaSummaryReport report)
    {
        var result = new List<NarrativaParagraph>();
        int count = report.NarrativaEspeciesObjetivo.Count;

        if (count == 1)
        {
            // Caso 1 sola especie: En la misma línea
            var sb = new NarrativaSpanBuilder();
            var eo = report.NarrativaEspeciesObjetivo[0];
            sb.Normal("Especie objetivo: ");
            sb.Bold($"{eo.NombreVulgar} ");
            sb.Italic($"({eo.NombreCientifico})");
            sb.Normal($", {eo.CapturaKg:N0} kg de captura, {eo.DescartePct:N2}% de descarte.");
            result.Add(new NarrativaParagraph(sb.ToSpans()));
        }
        else
        {
            // Caso múltiples: Título y luego una por línea
            var sbTitle = new NarrativaSpanBuilder();
            sbTitle.Normal("Especies objetivo:");
            result.Add(new NarrativaParagraph(sbTitle.ToSpans()));

            foreach (var eo in report.NarrativaEspeciesObjetivo)
            {
                var sbEo = new NarrativaSpanBuilder();
                sbEo.Normal("    "); // Sangría
                sbEo.Bold($"{eo.NombreVulgar} ");
                sbEo.Italic($"({eo.NombreCientifico})");
                sbEo.Normal($", {eo.CapturaKg:N0} kg de captura, {eo.DescartePct:N2}% de descarte.");
                result.Add(new NarrativaParagraph(sbEo.ToSpans()));
            }
        }

        return result;
    }

    // -------------------------------------------------------------------------
    // Párrafo por etapa
    // -------------------------------------------------------------------------

    private static NarrativaParagraph BuildParrafoEtapa(NarrativaEtapa etapa, int totalViajes, int numeroViaje)
    {
        var sb = new NarrativaSpanBuilder();

        // Encabezado ordinal
        string ordinal;
        if (totalViajes > 1)
        {
            string prospeccionSuffix = etapa.TipoEtapa == "EP" ? " (Prospección)" : "";
            ordinal = $"{OrdinalMasculino(numeroViaje)} viaje{prospeccionSuffix}: ";
        }
        else
        {
            ordinal = etapa.TipoEtapa == "EP" ? "En modalidad de prospección, el buque " : "El buque ";
        }

        sb.Normal(ordinal);

        // Cuadrados estadísticos donde operó
        if (etapa.Cuadrados.Any())
        {
            string inicioFrase = totalViajes > 1
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
            if (etapa.Cuadrados.Count > 1)
            {
                if (etapa.CuadradoMasLances == etapa.CuadradoMayorCaptura)
                {
                    if (etapa.CuadradoMasLances != null)
                    {
                        sb.Normal($", siendo el de mayor cantidad de operaciones de pesca y captura el {etapa.CuadradoMasLances} con ");
                        sb.Normal($"{etapa.CuadradoMayorCapturaKg:N0} kg en {etapa.CuadradoMasLancesNro} {Pluralizar(etapa.CuadradoMasLancesNro, "lance", "lances")}");
                    }
                }
                else
                {
                    if (etapa.CuadradoMasLances != null)
                    {
                        sb.Normal($", siendo el de mayor cantidad de operaciones de pesca el {etapa.CuadradoMasLances} ({etapa.CuadradoMasLancesNro} {Pluralizar(etapa.CuadradoMasLancesNro, "lance", "lances")})");
                    }
                    if (etapa.CuadradoMayorCaptura != null)
                    {
                        sb.Normal($" y el de mayor captura el {etapa.CuadradoMayorCaptura} ({etapa.CuadradoMayorCapturaKg:N0} kg)");
                    }
                }
            }
            sb.Normal(". ");
        }

        // Captura y descarte de la etapa
        sb.Normal($"La captura del {Pluralizar(totalViajes > 1 ? 2 : 1, "viaje", "viaje")} fue de ");
        sb.Normal($"{etapa.CapturaKg:N0} kg ");

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
            sb.Normal($"{eo.CapturaKg:N0} kg ");

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
                sb.Normal($" de {esp.CapturaKg:N0} kg ");

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

    private static NarrativaParagraph BuildParrafoViajeJerarquico(NarrativaViaje viaje, int totalViajes)
    {
        var sb = new NarrativaSpanBuilder();
        string ordinal = totalViajes > 1 ? $"{OrdinalMasculino(viaje.NumeroViaje)} viaje: " : "El viaje: ";
        
        sb.Bold(ordinal);
        sb.Normal($"La captura total del viaje fue de {viaje.CapturaTotalKg:N0} kg ");
        
        if (viaje.DescartePct >= 99.9)
            sb.Normal("descarándose en su totalidad ");
        else if (viaje.DescartePct > 0)
            sb.Normal($"con un {viaje.DescartePct:N2}% de descarte ");
        else
            sb.Normal("sin descarte ");
            
        sb.Normal($"en {viaje.TotalLances} {Pluralizar(viaje.TotalLances, "lance", "lances")}. Desglose de operaciones:");
        
        return new NarrativaParagraph(sb.ToSpans());
    }

    private static NarrativaParagraph BuildParrafoSubEtapa(NarrativaEtapa etapa)
    {
        var sb = new NarrativaSpanBuilder();
        string tipoStr = etapa.TipoEtapa == "EP" ? "Prospección" : "Comercial";
        
        sb.Normal("    • "); // viñeta con indentación
        sb.Bold($"Etapa {etapa.Numero} ({tipoStr} - {etapa.FechaInicio:dd/MM} al {etapa.FechaFin:dd/MM}): ");
        
        // Cuadrados estadísticos donde operó
        if (etapa.Cuadrados.Any())
        {
            sb.Normal("operó en ");
            if (etapa.Cuadrados.Count == 1)
            {
                sb.Normal($"el cuadrado estadístico {etapa.Cuadrados[0]}. ");
            }
            else
            {
                sb.Normal($"los cuadrados estadísticos: {ListarCuadrados(etapa.Cuadrados)}");

                if (etapa.CuadradoMasLances == etapa.CuadradoMayorCaptura)
                {
                    if (etapa.CuadradoMasLances != null)
                    {
                        sb.Normal($", siendo el de mayor cantidad de operaciones de pesca y captura el {etapa.CuadradoMasLances} con ");
                        sb.Normal($"{etapa.CuadradoMayorCapturaKg:N0} kg en {etapa.CuadradoMasLancesNro} {Pluralizar(etapa.CuadradoMasLancesNro, "lance", "lances")}");
                    }
                }
                else
                {
                    if (etapa.CuadradoMasLances != null)
                    {
                        sb.Normal($", siendo el de mayor cantidad de operaciones de pesca el {etapa.CuadradoMasLances} ({etapa.CuadradoMasLancesNro} {Pluralizar(etapa.CuadradoMasLancesNro, "lance", "lances")})");
                    }
                    if (etapa.CuadradoMayorCaptura != null)
                    {
                        sb.Normal($" y el de mayor captura el {etapa.CuadradoMayorCaptura} ({etapa.CuadradoMayorCapturaKg:N0} kg)");
                    }
                }
                
                sb.Normal(". ");
            }
        }
        
        sb.Normal($"Captura: {etapa.CapturaKg:N0} kg ");
        
        if (etapa.DescartePct >= 99.9)
            sb.Normal("descarándose en su totalidad ");
        else if (etapa.DescartePct > 0)
            sb.Normal($"con un {etapa.DescartePct:N2}% de descarte ");
        else
            sb.Normal("sin descarte ");
            
        sb.Normal($"en {etapa.TotalLances} {Pluralizar(etapa.TotalLances, "lance", "lances")} durante {etapa.DiasPesca} {Pluralizar(etapa.DiasPesca, "día", "días")}.");
        
        return new NarrativaParagraph(sb.ToSpans());
    }

    // -------------------------------------------------------------------------
    // Párrafo final: cantidad de muestras realizadas
    // -------------------------------------------------------------------------

    private static NarrativaParagraph BuildParrafoMuestras(MareaSummaryReport report)
    {
        var sb = new NarrativaSpanBuilder();
        int total = report.NarrativaMuestras.Sum(m => m.TotalMuestras + m.TotalMuestrasDescarte);

        sb.Normal($"Cantidad de muestras realizadas: {total} {Pluralizar(total, "muestra total", "muestras totales")}. ");

        for (int i = 0; i < report.NarrativaMuestras.Count; i++)
        {
            var m = report.NarrativaMuestras[i];
            if (i > 0) sb.Normal(" – ");
            
            sb.Normal($"{m.NombreVulgar} ");
            sb.Italic($"({m.NombreCientifico})");
            sb.Normal(": ");
            
            if (m.TotalMuestras > 0 && m.TotalMuestrasDescarte > 0)
            {
                sb.Normal($"{m.TotalMuestras} de captura y {m.TotalMuestrasDescarte} de descarte");
            }
            else if (m.TotalMuestras > 0)
            {
                sb.Normal($"{m.TotalMuestras} de captura");
            }
            else if (m.TotalMuestrasDescarte > 0)
            {
                sb.Normal($"{m.TotalMuestrasDescarte} de descarte");
            }
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
