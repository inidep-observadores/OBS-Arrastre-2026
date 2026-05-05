using System.Text;
using System.Globalization;
using OBSArrastre2026.App.Models.Import;

namespace OBSArrastre2026.App.Services.Internal;

public sealed class MareaValidationEngine
{
    /// <summary>
    /// Ejecuta el conjunto completo de validaciones sobre un set de datos de marea.
    /// </summary>
    public MareaValidationReport ValidateMarea(
        string barcoMareaActual, 
        int anioMareaActual, 
        int nroMareaActual,
        List<(DateTime Inicio, DateTime Fin)> etapasFechas,
        List<LegacyCaptura> capturas,
        List<LegacyMuestra> muestras,
        List<LegacySubmuestra> submuestras,
        List<LegacyLg> lgs,
        List<LegacyTracking> tracking,
        List<LegacyProduccion> produccion,
        Dictionary<string, long> especiesDict,
        Dictionary<string, long> especiesViejasDict,
        HashSet<long> especiesCodigosValidos,
        Dictionary<(string EspecieId, int Sexo), (double A, double B)> largoPesoCatalogo)
    {
        var report = new MareaValidationReport
        {
            Barco = barcoMareaActual,
            Marea = nroMareaActual.ToString(),
            Año = anioMareaActual,
            TotalLances = capturas.Count,
            FechaInicioMarea = etapasFechas.Any() ? etapasFechas.Min(e => e.Inicio) : null,
            FechaFinMarea = etapasFechas.Any() ? etapasFechas.Max(e => e.Fin) : null,
            Etapas = etapasFechas.Select((e, i) => new EtapaValidationInfo(i + 1, e.Inicio, e.Fin)).ToList()
        };

        ValidateBaseConsistency(report, barcoMareaActual, nroMareaActual, capturas, muestras, submuestras, lgs, tracking, produccion);
        ValidateLances(report, capturas, tracking);
        ValidateSamples(report, muestras, capturas, lgs, largoPesoCatalogo, especiesDict, especiesViejasDict, especiesCodigosValidos);
        ValidateSubSamples(report, submuestras, muestras);
        ValidateProduction(report, produccion, especiesDict, especiesViejasDict, especiesCodigosValidos, capturas);
        ValidateTemporalConsistency(report, etapasFechas, capturas, muestras, produccion);
        
        report.Capturas = capturas;
        report.Muestras = muestras;
        report.Submuestras = submuestras;
        report.Lgs = lgs;
        report.Tracking = tracking;
        report.Produccion = produccion;

        return report;
    }

    private void ValidateProduction(MareaValidationReport report, List<LegacyProduccion> produccion, Dictionary<string, long> especiesDict, Dictionary<string, long> especiesViejasDict, HashSet<long> especiesCodigosValidos, List<LegacyCaptura> capturas)
    {
        // 1. Validar existencia de especie por nombre
        var setNombresVulgares = new HashSet<string>(especiesDict.Keys, StringComparer.OrdinalIgnoreCase);
        var setNombresViejos = new HashSet<string>(especiesViejasDict.Keys, StringComparer.OrdinalIgnoreCase);
        
        var especiesDesconocidas = produccion
            .Select(p => p.Especie?.Trim().ToUpper().Normalize(NormalizationForm.FormC))
            .Where(name => {
                if (string.IsNullOrEmpty(name)) return false;
                // 1. Buscar en especies actuales
                if (setNombresVulgares.Contains(name)) return false;
                // 2. Buscar en especies viejas (puente)
                if (especiesViejasDict.TryGetValue(name, out long oldCode))
                {
                    // 3. Verificar si el código de la vieja existe en las actuales
                    if (especiesCodigosValidos.Contains(oldCode)) return false;
                }
                
                // 4. Fallback final: ¿Es un código INIDEP?
                // (Para validación, los códigos válidos están en especiesCodigosValidos)
                if (double.TryParse(name, out double d))
                {
                    if (especiesCodigosValidos.Contains((long)d)) return false;
                }
                
                return true;
            })
            .Distinct();

        foreach (var esp in especiesDesconocidas)
        {
            report.AddIssue(ValidationLevel.Warning, "Catálogo Especies", 
                $"La especie de producción '{esp}' no fue encontrada por Nombre Vulgar en el catálogo local. El registro se importará pero sin vínculo a la especie.", "Archivo P*");
        }

        // 2. Regla de negocio: Unicidad de Fecha-Producto-Categoría-Especie (Warning)
        var duplicates = produccion
            .GroupBy(p => new { p.Fecha, p.Producto, p.Categoria, p.Especie })
            .Where(g => g.Count() > 1);

        foreach (var group in duplicates)
        {
            report.AddIssue(ValidationLevel.Warning, "Producción", 
                $"Existen {group.Count()} registros para el producto '{group.Key.Producto}' ({group.Key.Categoria}) el {group.Key.Fecha:dd/MM/yyyy}. Se importarán todos pero se recomienda verificar posibles duplicados.", 
                $"Fecha {group.Key.Fecha:dd/MM/yyyy}");
        }

        // 3. Reglas avanzadas de producción (P*)
        foreach (var p in produccion)
        {
            string ctx = $"Fecha {p.Fecha:dd/MM/yyyy} Especie {p.Especie}";
            // Factor vs Producto
            if (p.Producto?.IndexOf("ENTERO", StringComparison.OrdinalIgnoreCase) >= 0 && p.Factor != 1)
            {
                string oldFactor = p.Factor.ToString();
                p.Factor = 1.0;
                report.AddIssue(ValidationLevel.AutoFixed, "Producción", $"Producto indica 'ENTERO' pero el factor era {oldFactor}. Se corrige automáticamente a 1.0.", ctx, oldFactor, "1.0");
            }
            // Límites
            if (p.Factor > 10)
            {
                report.AddIssue(ValidationLevel.Warning, "Producción", $"Factor de conversión inusualmente alto: {p.Factor}.", ctx);
            }
            if (p.Kilos > 100000)
            {
                report.AddIssue(ValidationLevel.Warning, "Producción", $"Kilos producidos inusualmente altos: {p.Kilos}.", ctx);
            }
        }

        // 4. Balance de Masa Diario por Especie (REQ: Producción vs Captura Neta)
        var fechasProduccion = produccion.Select(p => p.Fecha.Date);
        var fechasCaptura = capturas.Select(c => c.Fecha.Date);
        var todasLasFechas = fechasProduccion.Union(fechasCaptura).Distinct().OrderBy(d => d);

        var nombresEspeciesProduccion = produccion
            .Where(p => !string.IsNullOrEmpty(p.Especie))
            .Select(p => p.Especie.Trim().ToUpper())
            .Distinct();

        var culture = new CultureInfo("es-AR");

        foreach (var fecha in todasLasFechas)
        {
            var lancesDelDia = capturas
                .Where(c => c.Fecha.Date == fecha)
                .Select(c => (int)c.Lance)
                .OrderBy(n => n)
                .ToList();
            
            string lancesStr = lancesDelDia.Any() ? $", {FormatLanceList(lancesDelDia)}" : "";

            foreach (var nombreEspecie in nombresEspeciesProduccion)
            {
                var searchName = nombreEspecie.Normalize(NormalizationForm.FormC);
                if (!especiesDict.TryGetValue(searchName, out long codEspecie))
                {
                    if (!especiesViejasDict.TryGetValue(searchName, out codEspecie))
                    {
                        // Fallback: ¿Es un código?
                        if (double.TryParse(searchName, out double d)) codEspecie = (long)d;
                        else continue;
                    }
                }

                // A. Reconstruir captura desde producción: Suma(Kilos * Factor)
                double capturaReconstruida = produccion
                    .Where(p => p.Fecha.Date == fecha && p.Especie.Trim().ToUpper() == nombreEspecie)
                    .Sum(p => p.Kilos * p.Factor);

                if (capturaReconstruida <= 0) continue;

                // B. Obtener captura neta real de los lances: Suma(Captura - Descarte)
                double capturaNetaReal = capturas
                    .Where(c => c.Fecha.Date == fecha)
                    .Sum(c => {
                        double cap = c.Especies.ContainsKey(codEspecie) ? c.Especies[codEspecie] : 0;
                        double des = c.DescartesPorEspecie.ContainsKey(codEspecie) ? c.DescartesPorEspecie[codEspecie] : 0;
                        return cap - des;
                    });

                // C. Comparar y reportar diferencias > 1%
                double diferencia = capturaNetaReal - capturaReconstruida;
                double diffAbs = Math.Abs(diferencia);
                double margenTolerancia = capturaNetaReal * 0.01;

                string ctx = $"Fecha: {fecha:dd/MM/yyyy} | Especie: {nombreEspecie}";
                string capReconStr = capturaReconstruida.ToString("N1", culture);
                string capRealStr = capturaNetaReal.ToString("N1", culture);

                if (capturaReconstruida > capturaNetaReal + margenTolerancia)
                {
                    // Error: Se produjo más de lo que se capturó físicamente
                    report.AddIssue(ValidationLevel.Error, "Balance de Captura Diaria", 
                        $"Inconsistencia: La captura reconstruida ({capReconStr} kg) excede la captura neta real disponible ({capRealStr} kg){lancesStr}.", 
                        ctx);
                }
                else if (diffAbs > margenTolerancia)
                {
                    // Advertencia: Diferencia superior al 1%
                    string tipoDiff = diferencia > 0 ? "sobrante" : "faltante";
                    string diffStr = diffAbs.ToString("N1", culture);
                    report.AddIssue(ValidationLevel.Warning, "Balance de Captura Diaria", 
                        $"Diferencia de masa significativa ({tipoDiff}): Real {capRealStr} kg vs Reconstruida {capReconStr} kg (Dif: {diffStr} kg){lancesStr}.", 
                        ctx);
                }
            }
        }
    }

    private string FormatLanceList(List<int> lances)
    {
        if (lances == null || lances.Count == 0) return "";
        if (lances.Count == 1) return $"lance {lances[0]}";
        
        var distinctSorted = lances.Distinct().OrderBy(n => n).ToList();
        if (distinctSorted.Count == 1) return $"lance {distinctSorted[0]}";

        var firstPart = string.Join(", ", distinctSorted.Take(distinctSorted.Count - 1));
        return $"lances {firstPart} y {distinctSorted.Last()}";
    }

    private void ValidateTemporalConsistency(
        MareaValidationReport report, 
        List<(DateTime Inicio, DateTime Fin)> etapasFechas,
        List<LegacyCaptura> capturas,
        List<LegacyMuestra> muestras,
        List<LegacyProduccion> produccion)
    {
        if (etapasFechas == null || !etapasFechas.Any()) return;
        var minDate = etapasFechas.Min(e => e.Inicio.Date);
        var maxDate = etapasFechas.Max(e => e.Fin.Date);

        foreach (var c in capturas.Where(c => c.Fecha.Date < minDate || c.Fecha.Date > maxDate))
        {
            report.AddIssue(ValidationLevel.Fatal, "Consistencia Temporal", $"Lance {c.Lance} con fecha {c.Fecha:dd/MM/yyyy} fuera del rango de etapas de marea ({minDate:dd/MM/yyyy} al {maxDate:dd/MM/yyyy}).");
        }
        foreach (var m in muestras.Where(m => m.Fecha.Date < minDate || m.Fecha.Date > maxDate))
        {
            report.AddIssue(ValidationLevel.Fatal, "Consistencia Temporal", $"Muestra (Lance {m.Lance}) con fecha {m.Fecha:dd/MM/yyyy} fuera del rango de etapas de marea.");
        }
        foreach (var p in produccion.Where(p => p.Fecha.Date < minDate || p.Fecha.Date > maxDate))
        {
            report.AddIssue(ValidationLevel.Error, "Consistencia Temporal", $"Producción con fecha {p.Fecha:dd/MM/yyyy} fuera del rango de etapas de marea.");
        }
    }

    private void ValidateBaseConsistency(
        MareaValidationReport report, 
        string barcoActual, 
        int mareaActual, 
        List<LegacyCaptura> capturas, 
        List<LegacyMuestra> muestras, 
        List<LegacySubmuestra> submuestras,
        List<LegacyLg> lgs,
        List<LegacyTracking> tracking,
        List<LegacyProduccion> produccion)
    {
        var bActual = barcoActual.Trim().ToUpper();

        // 1. CAPTURAS
        foreach (var c in capturas)
        {
            if (c.Barco.Trim().ToUpper() != bActual)
                report.AddIssue(ValidationLevel.Fatal, "Consistencia", $"Barco en captura ({c.Barco}) no coincide con marea activa ({barcoActual})", $"Lance {c.Lance}");
            
            if ((int)c.Marea != mareaActual)
            {
                if ((int)c.Marea == 0)
                {
                    string oldMarea = c.Marea.ToString();
                    c.Marea = mareaActual;
                    report.AddIssue(ValidationLevel.AutoFixed, "Consistencia", $"Nro Marea en captura era 0. Se corrige a {mareaActual}.", $"Lance {c.Lance}", oldMarea, mareaActual.ToString());
                }
                else
                {
                    report.AddIssue(ValidationLevel.Error, "Consistencia", $"Nro Marea en captura ({c.Marea}) no coincide con marea activa ({mareaActual})", $"Lance {c.Lance}");
                }
            }
        }

        // 2. MUESTRAS
        foreach (var m in muestras)
        {
            if (m.Barco.Trim().ToUpper() != bActual)
                report.AddIssue(ValidationLevel.Fatal, "Consistencia", $"Barco en muestra ({m.Barco}) no coincide con marea activa ({barcoActual})", $"Lance {m.Lance}");
            
            if ((int)m.Marea != mareaActual)
            {
                if ((int)m.Marea == 0)
                {
                    string oldMarea = m.Marea.ToString();
                    m.Marea = mareaActual;
                    report.AddIssue(ValidationLevel.AutoFixed, "Consistencia", $"Nro Marea en muestra era 0. Se corrige a {mareaActual}.", $"Lance {m.Lance} Especie {m.Especie}", oldMarea, mareaActual.ToString());
                }
                else
                {
                    report.AddIssue(ValidationLevel.Error, "Consistencia", $"Nro Marea en muestra ({m.Marea}) no coincide con marea activa ({mareaActual})", $"Lance {m.Lance} Especie {m.Especie}");
                }
            }
        }

        // 3. SUBMUES
        foreach (var s in submuestras)
        {
            if (s.Barco.Trim().ToUpper() != bActual)
                report.AddIssue(ValidationLevel.Fatal, "Consistencia", $"Barco en submuestra ({s.Barco}) no coincide con marea activa ({barcoActual})", $"Lance {s.Lance} Ej {s.NEjemplar}");

            if ((int)s.Marea != mareaActual)
            {
                if ((int)s.Marea == 0)
                {
                    string oldMarea = s.Marea.ToString();
                    s.Marea = mareaActual;
                    report.AddIssue(ValidationLevel.AutoFixed, "Consistencia", $"Nro Marea en submuestra era 0. Se corrige a {mareaActual}.", $"Lance {s.Lance} Ej {s.NEjemplar}", oldMarea, mareaActual.ToString());
                }
                else
                {
                    report.AddIssue(ValidationLevel.Error, "Consistencia", $"Nro Marea en submuestra ({s.Marea}) no coincide con marea activa ({mareaActual})", $"Lance {s.Lance} Ej {s.NEjemplar}");
                }
            }
        }

        // 4. LG
        foreach (var l in lgs)
        {
            if (l.Barco.Trim().ToUpper() != bActual)
                report.AddIssue(ValidationLevel.Fatal, "Consistencia", $"Barco en archivo LG ({l.Barco}) no coincide con marea activa ({barcoActual})", $"Lance {l.Lance}");

            if ((int)l.Marea != mareaActual)
            {
                if ((int)l.Marea == 0)
                {
                    string oldMarea = l.Marea.ToString();
                    l.Marea = mareaActual;
                    report.AddIssue(ValidationLevel.AutoFixed, "Consistencia", $"Nro Marea en archivo LG era 0. Se corrige a {mareaActual}.", $"Lance {l.Lance}", oldMarea, mareaActual.ToString());
                }
                else
                {
                    report.AddIssue(ValidationLevel.Error, "Consistencia", $"Nro Marea en archivo LG ({l.Marea}) no coincide con marea activa ({mareaActual})", $"Lance {l.Lance}");
                }
            }
        }

        // 5. SEGUIMIENTO (T*) - Aquí el campo es "Buque"
        foreach (var t in tracking)
        {
            if (t.Buque.Trim().ToUpper() != bActual)
                report.AddIssue(ValidationLevel.Fatal, "Consistencia", $"Barco en seguimiento satelital ({t.Buque}) no coincide con marea activa ({barcoActual})", "Seguimiento T*");
        }

        // 6. PRODUCCIÓN (P*)
        foreach (var p in produccion)
        {
            if (p.Barco.Trim().ToUpper() != bActual)
                report.AddIssue(ValidationLevel.Fatal, "Consistencia", $"Barco en producción ({p.Barco}) no coincide con marea activa ({barcoActual})", $"Fecha {p.Fecha:dd/MM/yyyy}");

            if ((int)p.Marea != mareaActual)
            {
                if ((int)p.Marea == 0)
                {
                    string oldMarea = p.Marea.ToString();
                    p.Marea = mareaActual;
                    report.AddIssue(ValidationLevel.AutoFixed, "Consistencia", $"Nro Marea en producción era 0. Se corrige a {mareaActual}.", $"Fecha {p.Fecha:dd/MM/yyyy}", oldMarea, mareaActual.ToString());
                }
                else
                {
                    report.AddIssue(ValidationLevel.Error, "Consistencia", $"Nro Marea en producción ({p.Marea}) no coincide con marea activa ({mareaActual})", $"Fecha {p.Fecha:dd/MM/yyyy}");
                }
            }
        }
    }

    private void ValidateLances(MareaValidationReport report, List<LegacyCaptura> capturas, List<LegacyTracking> tracking)
    {
        int countPorcentaje = 0;
        int countKilos = 0;
        foreach (var c in capturas)
        {
            string ctx = $"Lance {c.Lance}";

            // REQ-2.2.3: Rangos de coordenadas (Normalizar primero a decimal negativo)
            double latDec = LegacyDecoder.DecodeCoordinate(c.LatInic);
            double lonDec = LegacyDecoder.DecodeCoordinate(c.LongInic);

            if (latDec < -65 || latDec > -30)
                report.AddIssue(ValidationLevel.Warning, "Geografía", $"Latitud inicial ({c.LatInic} -> {latDec:F3}) fuera de rango operativo (30°S - 65°S)", ctx);

            ValidateLanceDetails(report, c);


            // REQ-3.5.1: Recalcular CAPT_TOTAL
            double sumEspecies = c.Especies.Values.Sum();
            if (Math.Abs(sumEspecies - c.CaptTotal) > 0.1)
            {
                report.AddIssue(ValidationLevel.AutoFixed, "Captura", "Captura total inconsistente con suma de especies. Se recalcula.", ctx, c.CaptTotal.ToString(), sumEspecies.ToString());
                c.CaptTotal = sumEspecies;
            }

            // Totales de descarte
            double sumDescartes = c.DescartesPorEspecie.Values.Sum();
            if (Math.Abs(sumDescartes - c.Descarte) > 0.1)
            {
                report.AddIssue(ValidationLevel.AutoFixed, "Captura", "Descarte total inconsistente con suma de descartes por especie. Se recalcula.", ctx, c.Descarte.ToString(), sumDescartes.ToString());
                c.Descarte = sumDescartes;
            }

            // Detección para conversión de descarte
            if (c.CaptTotal > 0 && c.Descarte > 0)
            {
                double ratio = c.Descarte / c.CaptTotal;
                if (ratio > 1.0) countPorcentaje++;
                else if (ratio <= 1.0 && ratio > 0) countKilos++;
            }

            // REQ-5.3: Coherencia Temporal
            if (c.HoraInic > c.HoraFinal)
                report.AddIssue(ValidationLevel.Warning, "Tiempo", "Hora de inicio es posterior a hora de fin", ctx);

            // Nueva Validación: Coherencia con Tracking (Posición y Velocidad)
            ValidateLanceTrackingConsistency(report, c, tracking);

            // Nueva Validación: Velocidad interna del lance (Inicio vs Fin)
            ValidateInternalLanceSpeed(report, c);
        }

        // Nueva Validación: Velocidad entre lances (Fin de N vs Inicio de N+1)
        ValidateSpeedBetweenLances(report, capturas);

        // REQ-5.1: Duplicados de Lance
        var duplicates = capturas.GroupBy(x => x.Lance).Where(g => g.Count() > 1).Select(g => g.Key);
        foreach (var d in duplicates)
        {
            report.AddIssue(ValidationLevel.Error, "Estructura", $"El lance {d} aparece duplicado en el archivo de captura");
        }

        // REQ-5.2: Secuencia de lances
        var sortedLances = capturas.OrderBy(x => x.Lance).Select(x => (int)x.Lance).ToList();
        for (int i = 0; i < sortedLances.Count - 1; i++)
        {
            if (sortedLances[i+1] - sortedLances[i] > 1)
                report.AddIssue(ValidationLevel.Warning, "Estructura", $"Salto en la secuencia de lances detectado entre {sortedLances[i]} y {sortedLances[i+1]}");
        }

        // Resolución de porcentaje de descarte (Heurística de consenso)
        if (countPorcentaje > 0 || countKilos > 0)
        {
            double totalConDatos = countPorcentaje + countKilos;
            bool asPercentage = (countKilos == 0) || (countPorcentaje / totalConDatos > 0.8);
            bool asKilos = (countPorcentaje == 0) || (countKilos / totalConDatos > 0.8);

            if (asPercentage && !asKilos)
            {
                report.AddIssue(ValidationLevel.AutoFixed, "Descarte", "Se detectó que los descartes están en porcentaje (consenso > 80%). Convertidos a kilos automáticamente.", "Toda la marea");
                foreach (var c in capturas)
                {
                    if (c.Descarte > 0)
                    {
                        double pctDescarteTotal = c.Descarte;
                        c.Descarte = (c.Descarte * c.CaptTotal) / 100.0;
                        foreach (var key in c.DescartesPorEspecie.Keys.ToList())
                        {
                            if (c.DescartesPorEspecie[key] > 0)
                            {
                                // DESCAR_i = (DESCAR_i * KG_i) / 100
                                double especieCaptura = c.Especies.ContainsKey(key) ? c.Especies[key] : 0;
                                c.DescartesPorEspecie[key] = (c.DescartesPorEspecie[key] * especieCaptura) / 100.0;
                            }
                        }
                    }
                }
            }
            else if (asKilos && !asPercentage)
            {
                // Caso de la consulta del usuario: 142 kilos vs 3 lances con ratio > 1 (probables errores de carga)
                if (countPorcentaje > 0)
                {
                    foreach (var c in capturas)
                    {
                        if (c.CaptTotal > 0 && c.Descarte > c.CaptTotal)
                        {
                            report.AddIssue(ValidationLevel.Error, "Captura", $"El descarte ({c.Descarte} kg) es superior a la captura total ({c.CaptTotal} kg). Se asume que son kilos erróneos (no porcentaje) por consenso de marea.", $"Lance {c.Lance}");
                        }
                    }
                }
            }
            else
            {
                // Ambigüedad real (ej. 50/50 o sin mayoría clara)
                report.AddIssue(ValidationLevel.Fatal, "Descarte", $"Datos mixtos de descarte: {countPorcentaje} lances parecen porcentaje (ratio > 1), {countKilos} parecen kilos. No se puede determinar la unidad automáticamente por falta de consenso (> 80%).", "Toda la marea");
            }
        }
    }

    private void ValidateLanceDetails(MareaValidationReport report, LegacyCaptura c)
    {
        string ctx = $"Lance {c.Lance}";

        // REQ-5.5: Profundidad
        if (c.ProfInic <= 0 || c.ProfInic > 2000)
            report.AddIssue(ValidationLevel.Error, "Geografía", $"Profundidad inicial ({c.ProfInic}m) fuera de rango (0-2000)", ctx);
        
        if (c.ProfFinal <= 0 || c.ProfFinal > 2000)
            report.AddIssue(ValidationLevel.Error, "Geografía", $"Profundidad final ({c.ProfFinal}m) fuera de rango (0-2000)", ctx);

        if (c.ProfFinal > c.ProfInic * 2 && c.ProfInic > 0)
            report.AddIssue(ValidationLevel.Warning, "Geografía", $"Cambio de profundidad inusual ({c.ProfInic}m -> {c.ProfFinal}m)", ctx);

        // REQ-5.8: Lances sin especies
        if (!c.Especies.Any() || c.Especies.Values.Sum() == 0)
            report.AddIssue(ValidationLevel.Error, "Captura", "Lance sin registro de especies o captura total cero", ctx);
    }

    private void ValidateSamples(MareaValidationReport report, List<LegacyMuestra> muestras, List<LegacyCaptura> capturas, List<LegacyLg> lgs, 
        Dictionary<(string EspecieId, int Sexo), (double A, double B)> largoPesoCatalogo,
        Dictionary<string, long> especiesDict,
        Dictionary<string, long> especiesViejasDict,
        HashSet<long> especiesCodigosValidos)
    {
        foreach (var m in muestras)
        {
            string ctx = $"Lance {m.Lance} - Especie {m.Especie}";

            // REQ-4.1.1: Verificar existencia de lance en captura
            if (!capturas.Any(c => (int)c.Lance == (int)m.Lance))
                report.AddIssue(ValidationLevel.Error, "Integridad", $"Muestra de lance {m.Lance} no tiene lance correspondiente en CAPTURA", ctx);

            // REQ-4.3.1: Verificar Rangos de Talla
            if (m.UltTalla <= m.PrimTalla)
                report.AddIssue(ValidationLevel.Error, "Biometría", $"Última talla ({m.UltTalla}) no es mayor que primera talla ({m.PrimTalla})", ctx);

            if (m.Intervalo <= 0)
                report.AddIssue(ValidationLevel.Error, "Biometría", "Intervalo de tallas inválido (<= 0)", ctx);

            // --- NUEVAS VALIDACIONES DE INTEGRIDAD (Punto 2) ---

            var lanceCorrespondiente = capturas.FirstOrDefault(c => (int)c.Lance == (int)m.Lance);
            long codEspecieMuestra = m.CodEspec;

            if (lanceCorrespondiente != null)
            {
                // REQ-3.2.4: Consistencia de Fecha (Muestra vs Lance)
                if (m.Fecha.Date != lanceCorrespondiente.Fecha.Date)
                {
                    string oldFecha = m.Fecha.ToString("dd/MM/yyyy");
                    string newFecha = lanceCorrespondiente.Fecha.ToString("dd/MM/yyyy");
                    m.Fecha = lanceCorrespondiente.Fecha; // Auto-corrección como en pcorrecc.PRG
                    report.AddIssue(ValidationLevel.AutoFixed, "Integridad", $"Fecha de muestra ({oldFecha}) no coincide con fecha de lance ({newFecha}). Corregido.", ctx, oldFecha, newFecha);
                }


                // REQ-3.4.4: Consistencia de Especie (Muestra vs Captura)
                // Verificar si la especie muestreada existe en el registro de captura con kg > 0
                if (!especiesCodigosValidos.Contains(codEspecieMuestra))
                {
                    // El código no está en especies actuales. Probar puente.
                    if (string.IsNullOrEmpty(m.Especie))
                    {
                        // Si no tiene nombre, solo podemos probar si el código está en viejas
                        // Buscar si el código m.CodEspec existe en el catálogo viejo
                        // (Nota: especiesViejasDict mapea Nombre -> Código, necesitamos Código -> Código o similar)
                        // Para simplificar, si el código de muestra no está en actuales, intentaremos 
                        // resolverlo por nombre si existe.
                    }
                    else 
                    {
                        var searchName = m.Especie.Trim().ToUpper();
                        // 1. Buscar en actuales por nombre
                        if (especiesDict.TryGetValue(searchName, out long newCode))
                        {
                            codEspecieMuestra = newCode;
                        }
                        // 2. Buscar en viejas por nombre
                        else if (especiesViejasDict.TryGetValue(searchName, out long bridgeCode))
                        {
                            // 3. Verificar puente a actuales
                            if (especiesCodigosValidos.Contains(bridgeCode))
                            {
                                codEspecieMuestra = bridgeCode;
                            }
                        }
                    }
                }

                if (codEspecieMuestra > 0)
                {
                    bool capturada = lanceCorrespondiente.Especies.ContainsKey(codEspecieMuestra) && lanceCorrespondiente.Especies[codEspecieMuestra] > 0;
                    if (!capturada)
                    {
                        report.AddIssue(ValidationLevel.Warning, "Integridad", $"Se registró una muestra de '{m.Especie}' (Cod: {codEspecieMuestra}) pero esta especie no figura con kilos capturados en el lance {m.Lance}.", ctx);
                    }
                }
            }

            // --- FIN NUEVAS VALIDACIONES ---

            // Peso Alométrico
            if (m.PesoMues <= 0)
            {
                var lookupLogs = new List<string>();
                double totalWeight = 0;
                bool foundAnyParams = false;

                // Intentar obtener parámetros fallback (general o del LG legado)
                double fallbackA = 0, fallbackB = 0;
                bool hasFallback = false;
                long especieIdNum = 0;

                if (!string.IsNullOrEmpty(m.Especie))
                {
                    if (!especiesDict.TryGetValue(m.Especie.Trim().ToUpper(), out especieIdNum))
                    {
                        especiesViejasDict.TryGetValue(m.Especie.Trim().ToUpper(), out especieIdNum);
                    }
                }

                if (especieIdNum > 0)
                {
                    string espId = especieIdNum.ToString();
                    if (largoPesoCatalogo.TryGetValue((espId, 0), out var paramsGral))
                    {
                        fallbackA = paramsGral.A;
                        fallbackB = paramsGral.B;
                        hasFallback = true;
                    }
                }

                if (!hasFallback)
                {
                    var lg = lgs.FirstOrDefault(l => l.CodEspecIE == m.CodEspec);
                    if (lg != null && lg.ParamA > 0)
                    {
                        fallbackA = lg.ParamA;
                        fallbackB = lg.ParamB;
                        hasFallback = true;
                    }
                }

                // Usar el código ya resuelto y puenteado en la sección de integridad (REQ-3.4.4)
                long speciesIdForLookup = codEspecieMuestra > 0 ? codEspecieMuestra : m.CodEspec;
                
                if (speciesIdForLookup == 0 && !string.IsNullOrEmpty(m.Especie))
                {
                    if (especiesDict == null || !especiesDict.TryGetValue(m.Especie.Trim().ToUpper(), out speciesIdForLookup))
                    {
                        if (especiesViejasDict != null)
                        {
                            especiesViejasDict.TryGetValue(m.Especie.Trim().ToUpper(), out speciesIdForLookup);
                        }
                    }
                }

                string espIdLookupStr = speciesIdForLookup.ToString().Trim();

                if (hasFallback || (largoPesoCatalogo != null && largoPesoCatalogo.Any(k => k.Key.EspecieId.Trim() == espIdLookupStr)))
                {
                    string espId = espIdLookupStr;
                    
                    // Función local para obtener parámetros con fallback y promedios
                    (double A, double B) GetSmartParams(int targetSex)
                    {
                        (double A, double B) res = (0, 0);
                        string method = "";

                        // 1. Intentar búsqueda exacta
                        if (largoPesoCatalogo.TryGetValue((espId, targetSex), out res)) method = $"Exacta(Sexo:{targetSex})";
                        
                        // Fallback por si hay ceros a la izquierda o discrepancias de formato numérico en el string
                        if (res.A <= 0 && long.TryParse(espId, out long numericId))
                        {
                             string normalizedId = numericId.ToString();
                             if (largoPesoCatalogo.TryGetValue((normalizedId, targetSex), out res)) method = $"ExactaNorm(Sexo:{targetSex})";
                        }
                        
                        // 2. Fallbacks de sexo
                        if (res.A <= 0)
                        {
                            if (targetSex == 0 && largoPesoCatalogo.TryGetValue((espId, 3), out res)) method = "IndetLegacy(3)";
                            else if (targetSex != 0 && largoPesoCatalogo.TryGetValue((espId, 0), out res)) method = "General(0)";
                            else if (targetSex == 3 && largoPesoCatalogo.TryGetValue((espId, 0), out res)) method = "General(0) for 3";
                        }

                        // 3. Cualquier sexo
                        if (res.A <= 0)
                        {
                            var anyEntry = largoPesoCatalogo.FirstOrDefault(k => k.Key.EspecieId == espId);
                            if (anyEntry.Key.EspecieId != null) { res = anyEntry.Value; method = $"CualquierSexo({anyEntry.Key.Sexo})"; }
                        }

                        // 4. Promedios
                        if (res.A <= 0)
                        {
                            bool hasM = largoPesoCatalogo.TryGetValue((espId, 1), out var pM);
                            bool hasF = largoPesoCatalogo.TryGetValue((espId, 2), out var pF);
                            if (hasM && hasF) { res = ((pM.A + pF.A) / 2.0, (pM.B + pF.B) / 2.0); method = "Promedio(1+2)"; }
                            else if (hasM) { res = pM; method = "SoloMacho(1)"; }
                            else if (hasF) { res = pF; method = "SoloHembra(2)"; }
                        }

                        // 5. Fallback final
                        if (res.A <= 0 && hasFallback) { res = (fallbackA, fallbackB); method = "Fallback(LG/Gral)"; }

                        if (res.A > 0) lookupLogs.Add($"{method}: A={res.A}, B={res.B}");
                        return res;
                    }

                    foreach (var tally in m.Tallies)
                    {
                        double tallaCm = tally.Size; 
                        double wMales = 0, wFemales = 0, wIndet = 0;

                        if (tally.Males > 0)
                        {
                            var p = GetSmartParams(1);
                            if (p.A > 0) { wMales = tally.Males * (p.A * Math.Pow(tallaCm, p.B)); foundAnyParams = true; }
                        }
                        if (tally.Females > 0)
                        {
                            var p = GetSmartParams(2);
                            if (p.A > 0) { wFemales = tally.Females * (p.A * Math.Pow(tallaCm, p.B)); foundAnyParams = true; }
                        }
                        if (tally.Indeterminate > 0 || (tally.Males == 0 && tally.Females == 0 && tally.Total > 0))
                        {
                            var p = GetSmartParams(0);
                            int count = tally.Indeterminate > 0 ? tally.Indeterminate : tally.Total;
                            if (p.A > 0) { wIndet = count * (p.A * Math.Pow(tallaCm, p.B)); foundAnyParams = true; }
                        }
                        
                        totalWeight += (wMales + wFemales + wIndet) / 1000.0; 
                    }
                }

                if (foundAnyParams && totalWeight > 0)
                {
                    string oldVal = m.PesoMues.ToString();
                    m.PesoMues = Math.Round(totalWeight, 2);
                    report.AddIssue(ValidationLevel.AutoFixed, "Biometría", "Peso de muestra era 0. Recalculado mediante relación Largo-Peso diferenciada por sexo.", ctx, oldVal, m.PesoMues.ToString("F2"));
                }
                else
                {
                    int catalogCount = largoPesoCatalogo?.Count ?? 0;
                    string logText = lookupLogs.Any() ? $" [Logs: {string.Join(" | ", lookupLogs.Distinct())}]" : "";
                    string catalogPreview = catalogCount > 0 
                        ? $" Catálogo ({catalogCount} regs): [{string.Join(", ", largoPesoCatalogo.Keys.Take(3).Select(k => $"{k.EspecieId}:{k.Sexo}"))}...]" 
                        : " Catálogo vacío";
                        
                    string debugInfo = $"[ID Resuelto: {espIdLookupStr}, Nombre: '{m.Especie}', CodEspec Original: {m.CodEspec}]{catalogPreview}{logText}";
                    report.AddIssue(ValidationLevel.Error, "Biometría", $"Peso de muestra es 0 y no se pudieron encontrar parámetros de biometría para la especie '{m.Especie}'.", ctx);
                }
            }
            // --- NUEVA VALIDACIÓN: Peso Muestra vs Peso Captura ---
            if (lanceCorrespondiente != null && codEspecieMuestra > 0)
            {
                if (lanceCorrespondiente.Especies.TryGetValue(codEspecieMuestra, out double kilosCaptura))
                {
                    // Tolerancia de 10 gramos por redondeos en la conversión gramos/kilos
                    if (m.PesoMues > kilosCaptura + 0.01)
                    {
                        report.AddIssue(ValidationLevel.Error, "Integridad", 
                            $"Inconsistencia: El peso de la muestra ({m.PesoMues:F2} kg) es superior a la captura registrada de la especie ({kilosCaptura:F2} kg) en el lance {m.Lance}.", ctx);
                    }
                }
            }
        }

        // --- VALIDACIÓN DE INTEGRIDAD L* vs M* ---
        foreach (var lg in lgs)
        {
            string ctx = $"Archivo L* - Lance {lg.Lance}";
            
            // Buscar muestra correspondiente a Langostino (Código fijo 5139030101)
            var muestraM = muestras.FirstOrDefault(m => 
                (int)m.Lance == (int)lg.Lance && 
                m.CodEspec == 5139030101);

            if (muestraM == null)
            {
                if (lg.Frecuencias.Any(f => f.Value > 0))
                {
                    report.AddIssue(ValidationLevel.Fatal, "Integridad L/M", 
                        $"El archivo L* contiene datos de madurez para el lance {lg.Lance} pero no existe una muestra biológica (M*) de Langostino correspondiente.", ctx);
                }
                continue;
            }

            // Verificar que cada talla en L* exista en M*
            foreach (var kvp in lg.Frecuencias)
            {
                int tallaL = kvp.Key;
                if (kvp.Value > 0)
                {
                    bool existeEnM = muestraM.Tallies.Any(t => t.Size == tallaL);
                    if (!existeEnM)
                    {
                        report.AddIssue(ValidationLevel.Fatal, "Integridad L/M", 
                            $"El archivo L* registra datos de madurez para la talla {tallaL} mm, pero esa talla no figura como medida en el archivo de muestra (M*).", 
                            $"Lance {lg.Lance} - Especie {lg.CodEspecIE} - Talla {tallaL}");
                    }
                }
            }
        }
    }

    private void ValidateSubSamples(MareaValidationReport report, List<LegacySubmuestra> submuestras, List<LegacyMuestra> muestras)
    {
        // REQ-4.1.3: Verificar Números de Ejemplar Duplicados
        var subDuplicates = submuestras
            .GroupBy(s => new { s.Lance, s.Especie, s.NEjemplar })
            .Where(g => g.Count() > 1);
        
        foreach (var group in subDuplicates)
        {
            report.AddIssue(ValidationLevel.Error, "Estructura", 
                $"El ejemplar {group.Key.NEjemplar} de '{group.Key.Especie}' aparece duplicado {group.Count()} veces en el lance {group.Key.Lance}.");
        }

        foreach (var s in submuestras)
        {
            string ctx = $"Lance {s.Lance} - Ejemplar {s.NEjemplar}";

            // Integridad: Verificar existencia de muestra padre
            var parent = muestras.FirstOrDefault(m => m.Lance == s.Lance && m.Especie?.Trim().ToUpper() == s.Especie?.Trim().ToUpper());
            if (parent == null)
            {
                report.AddIssue(ValidationLevel.Error, "Integridad", $"Submuestra huerfana: No existe muestra padre para la especie {s.Especie} en el lance {s.Lance}.", ctx);
            }
            else
            {
                // REQ-3.2.4: Consistencia de Fecha (Submuestra vs Muestra)
                if (s.Fecha.Date != parent.Fecha.Date)
                {
                    string oldFecha = s.Fecha.ToString("dd/MM/yyyy");
                    string newFecha = parent.Fecha.ToString("dd/MM/yyyy");
                    s.Fecha = parent.Fecha;
                    report.AddIssue(ValidationLevel.AutoFixed, "Integridad", $"Fecha de submuestra ({oldFecha}) no coincide con muestra padre. Corregido.", ctx, oldFecha, newFecha);
                }

            }

            // REQ-4.2.1: Verificar Largo Total Atípico
            if (s.LargoTot > 250)
                report.AddIssue(ValidationLevel.Warning, "Biometría", $"Largo total atípico ({s.LargoTot}mm > 250mm). Requiere revisión.", ctx);

            // REQ-4.2.2: Largo estándar vs total
            if (s.LargoSta > s.LargoTot)
                report.AddIssue(ValidationLevel.Error, "Biometría", $"Largo estándar ({s.LargoSta}) mayor que largo total ({s.LargoTot})", ctx);
        }
    }

    public double CalculateArea(double lat, double lon)
    {
        // lat y lon vienen como decimales negativos (SW)
        double aLat = Math.Abs(lat);
        double aLon = Math.Abs(lon);

        int baseArea = (int)Math.Floor(aLat) * 100 + (int)Math.Floor(aLon);
        
        double latMin = (aLat - Math.Floor(aLat)) * 60; // Minutos reales (no los .30 del formato legacy)
        double lonMin = (aLon - Math.Floor(aLon)) * 60;

        // Lógica de cuadrantes definida en REQ-3.3.1
        if (latMin <= 30 && lonMin > 30) return baseArea + 0.1;
        if (latMin <= 30 && lonMin <= 30) return baseArea + 0.2;
        if (latMin > 30 && lonMin > 30) return baseArea + 0.3;
        if (latMin > 30 && lonMin <= 30) return baseArea + 0.4;

        return baseArea;
    }

    private void ValidateLanceTrackingConsistency(MareaValidationReport report, LegacyCaptura c, List<LegacyTracking> tracking)
    {
        if (tracking == null || !tracking.Any()) return;

        string ctx = $"Lance {c.Lance}";

        // Tiempos del lance (Ya vienen en UTC-3 según confirmación del usuario)
        var timeStart = c.Fecha.Date.Add(LegacyDecoder.DecodeTime(c.HoraInic));
        var timeEnd = c.Fecha.Date.Add(LegacyDecoder.DecodeTime(c.HoraFinal));

        // Validar punto de inicio
        ValidatePointWithTrack(report, ctx, "Inicio", timeStart, 
            LegacyDecoder.DecodeCoordinate(c.LatInic), LegacyDecoder.DecodeCoordinate(c.LongInic), tracking);

        // Validar punto de fin
        ValidatePointWithTrack(report, ctx, "Fin", timeEnd, 
            LegacyDecoder.DecodeCoordinate(c.LatFinal), LegacyDecoder.DecodeCoordinate(c.LongFinal), tracking);
    }

    private void ValidateInternalLanceSpeed(MareaValidationReport report, LegacyCaptura c)
    {
        string ctx = $"Lance {c.Lance}";
        
        var timeStart = c.Fecha.Date.Add(LegacyDecoder.DecodeTime(c.HoraInic));
        var timeEnd = c.Fecha.Date.Add(LegacyDecoder.DecodeTime(c.HoraFinal));
        
        // Si el lance cruzó la medianoche (fecha de fin es el día siguiente)
        if (timeEnd < timeStart) timeEnd = timeEnd.AddDays(1);

        double timeDiffHours = (timeEnd - timeStart).TotalHours;
        if (timeDiffHours <= 0) return; // Se valida consistencia horaria básica en otro lado

        double latStart = LegacyDecoder.DecodeCoordinate(c.LatInic);
        double lonStart = LegacyDecoder.DecodeCoordinate(c.LongInic);
        double latEnd = LegacyDecoder.DecodeCoordinate(c.LatFinal);
        double lonEnd = LegacyDecoder.DecodeCoordinate(c.LongFinal);

        double distNm = CalculateDistanceNauticalMiles(latStart, lonStart, latEnd, lonEnd);
        double speedKnots = distNm / timeDiffHours;

        if (speedKnots > 15)
        {
            report.AddIssue(ValidationLevel.Warning, "Geografía", 
                $"Velocidad de arrastre excesiva: {speedKnots:F1} nudos (calculada entre inicio y fin del lance). " +
                $"Distancia: {distNm:F2} mn en {timeDiffHours*60:F1} min.", ctx);
        }
    }

    private void ValidateSpeedBetweenLances(MareaValidationReport report, List<LegacyCaptura> capturas)
    {
        if (capturas.Count < 2) return;

        // Ordenar lances cronológicamente por inicio
        var sorted = capturas
            .Select(c => new { 
                Captura = c, 
                Start = c.Fecha.Date.Add(LegacyDecoder.DecodeTime(c.HoraInic)),
                End = c.Fecha.Date.Add(LegacyDecoder.DecodeTime(c.HoraFinal)) 
            })
            .OrderBy(x => x.Start)
            .ToList();

        // Asegurar que manejamos cruce de medianoche en los 'End' individuales si es necesario
        // (Aunque para el ordenamiento 'Start' suele ser suficiente si los lances no duran +24h)
        
        for (int i = 0; i < sorted.Count - 1; i++)
        {
            var current = sorted[i];
            var next = sorted[i + 1];

            var currentEnd = current.End;
            if (currentEnd < current.Start) currentEnd = currentEnd.AddDays(1);

            // Tiempo entre Fin(i) e Inicio(i+1)
            double timeDiffHours = (next.Start - currentEnd).TotalHours;
            
            // Si hay solapamiento o están muy pegados, la validación de velocidad puede ser ruidosa.
            // Solo evaluamos si hay un gap positivo de tiempo.
            if (timeDiffHours <= 0) continue; 

            double latEnd = LegacyDecoder.DecodeCoordinate(current.Captura.LatFinal);
            double lonEnd = LegacyDecoder.DecodeCoordinate(current.Captura.LongFinal);
            double latStartNext = LegacyDecoder.DecodeCoordinate(next.Captura.LatInic);
            double lonStartNext = LegacyDecoder.DecodeCoordinate(next.Captura.LongInic);

            double distNm = CalculateDistanceNauticalMiles(latEnd, lonEnd, latStartNext, lonStartNext);
            double speedKnots = distNm / timeDiffHours;

            if (speedKnots > 15)
            {
                report.AddIssue(ValidationLevel.Warning, "Geografía", 
                    $"Velocidad excesiva para navegar entre lances ({current.Captura.Lance} -> {next.Captura.Lance}): {speedKnots:F1} nudos. " +
                    $"Distancia: {distNm:F2} mn en {timeDiffHours*60:F1} min entre el fin de uno y el inicio del siguiente.", 
                    $"Lance {current.Captura.Lance}");
            }
        }
    }

    private void ValidatePointWithTrack(MareaValidationReport report, string lanceCtx, string pointType, DateTime lanceTime, double lanceLat, double lanceLon, List<LegacyTracking> tracking)
    {
        if (tracking == null || !tracking.Any()) return;

        // PARÁMETROS DE LA NUEVA ESTRATEGIA (Radio de Alcanzabilidad)
        const double MaxCruisingSpeedKnots = 11.0; // Velocidad máxima supuesta para traslado
        const double MaxErrorToleranceNm = 10.0;    // Solo alertar si la discrepancia supera las 10 millas
        const int SearchWindowHours = 2;           // Buscar en una ventana de +/- 2 horas

        double minDiscrepancyFound = double.MaxValue;
        LegacyTracking bestPoint = null;
        double bestPointDist = 0;
        double bestPointTimeDiffMin = 0;

        DateTime windowStart = lanceTime.AddHours(-SearchWindowHours);
        DateTime windowEnd = lanceTime.AddHours(SearchWindowHours);

        // 1. Encontrar el punto del track que más se "acerque" a la posición del lance 
        // considerando lo que el barco pudo haber navegado en ese tiempo.
        foreach (var t in tracking)
        {
            var trackTimeLocal = t.GetDateTime();
            
            // Solo evaluamos puntos dentro de la ventana de búsqueda (evita que radios enormes anulen la validación)
            if (trackTimeLocal >= windowStart && trackTimeLocal <= windowEnd)
            {
                double actualDist = CalculateDistanceNauticalMiles(lanceLat, lanceLon, t.Latitud, t.Longitud);
                double timeDiffHours = Math.Abs((lanceTime - trackTimeLocal).TotalHours);
                
                // Distancia que el buque PODRÍA haber recorrido a velocidad crucero
                double maxReachDist = MaxCruisingSpeedKnots * timeDiffHours;
                
                // Discrepancia: Lo que le "falta" al buque para llegar incluso yendo a 11 nudos
                double discrepancy = actualDist - maxReachDist;

                if (discrepancy < minDiscrepancyFound)
                {
                    minDiscrepancyFound = discrepancy;
                    bestPoint = t;
                    bestPointDist = actualDist;
                    bestPointTimeDiffMin = timeDiffHours * 60;
                }
            }
        }

        // 2. Si no se encontró ningún punto en la ventana de 4 horas, es una alerta de falta de datos
        if (bestPoint == null)
        {
            // Opcionalmente reportar falta de cobertura de track
            return;
        }

        // 3. VALIDACIÓN FINAL
        // Si la discrepancia mínima encontrada es mayor a 10 millas, es un error geográfico claro
        if (minDiscrepancyFound > MaxErrorToleranceNm)
        {
            report.AddIssue(ValidationLevel.Warning, "Geografía", 
                $"Inconsistencia en {pointType}: El buque se encuentra a {bestPointDist:F1} mn del track (Dif: {bestPointTimeDiffMin:F0} min). " +
                $"Incluso a velocidad máxima, existe una discrepancia física de {minDiscrepancyFound:F1} mn.", 
                lanceCtx);
        }
    }

    private double CalculateDistanceNauticalMiles(double lat1, double lon1, double lat2, double lon2)
    {
        const double EarthRadiusNauticalMiles = 3440.065;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusNauticalMiles * c;
    }

    private double ToRadians(double degrees) => degrees * Math.PI / 180.0;

    private string FormatCoordShort(double decimalDegrees, bool isLat)
    {
        double absVal = Math.Abs(decimalDegrees);
        int degrees = (int)Math.Truncate(absVal);
        double minutes = (absVal - degrees) * 60;
        
        char quadrant = isLat 
            ? (decimalDegrees >= 0 ? 'N' : 'S') 
            : (decimalDegrees >= 0 ? 'E' : 'W');

        return $"{degrees:D2}º {minutes:F1}' {quadrant}".Replace('.', ',');
    }
}
