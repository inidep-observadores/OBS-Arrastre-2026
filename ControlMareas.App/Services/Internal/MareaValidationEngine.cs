using System.Text;
using System.Globalization;
using ControlMareas.App.Models.Import;

namespace ControlMareas.App.Services.Internal;

public sealed class MareaValidationEngine
{
    /// <summary>
    /// Ejecuta el conjunto completo de validaciones sobre un set de datos de marea.
    /// </summary>
    public MareaValidationReport ValidateMarea(
        string barcoMareaActual, 
        int anioMareaActual, 
        int nroMareaActual,
        int? buqueCodigo,
        string? obsNombre,
        string? obsApellido,
        int? obsCodigo,
        List<(DateTime Inicio, DateTime Fin)> etapasFechas,
        List<LegacyCaptura> capturas,
        List<LegacyMuestra> muestras,
        List<LegacySubmuestra> submuestras,
        List<LegacyLg> lgs,
        List<LegacyTracking> tracking,
        List<LegacyProduccion> produccion,
        Dictionary<string, string> especiesDict,
        Dictionary<string, string> especiesViejasDict,
        Dictionary<string, string> especiesCientificasDict,
        HashSet<string> especiesCodigosValidos,
        Dictionary<(string EspecieId, int Sexo), (double A, double B)> largoPesoCatalogo,
        bool procesarSubmuestrasSinMuestraTalla = false,
        bool skipConsensusHeuristic = false,
        bool filtrarDiferenciasAuditoria = true,
        double toleranciaFiltroAuditoria = 2.0,
        bool omitirValidacionCapturaProduccion = false,
        bool omitirValidacionMuestraSubmuestra = false)
    {
        var report = new MareaValidationReport
        {
            Barco = barcoMareaActual,
            Marea = nroMareaActual.ToString(),
            Año = anioMareaActual,
            BuqueCodigo = buqueCodigo,
            ObservadorNombre = obsNombre,
            ObservadorApellido = obsApellido,
            ObservadorCodigo = obsCodigo,
            TotalLances = capturas.Count,
            FechaInicioMarea = etapasFechas.Any() ? etapasFechas.Min(e => e.Inicio) : null,
            FechaFinMarea = etapasFechas.Any() ? etapasFechas.Max(e => e.Fin) : null,
            Etapas = etapasFechas.Select((e, i) => new EtapaValidationInfo(i + 1, e.Inicio, e.Fin)).ToList(),
            ProcesarSubmuestrasSinMuestraTalla = procesarSubmuestrasSinMuestraTalla
        };

        // 1. Consistencia Temporal
        ValidateTemporalConsistency(report, etapasFechas, capturas, muestras, produccion);

        // 2. Consistencia Base (Barco/Marea en todos los registros)
        ValidateBaseConsistency(report, barcoMareaActual, nroMareaActual, capturas, muestras, submuestras, lgs, tracking, produccion);

        // 3. Validación de Lances (C*)
        ValidateLances(report, capturas, tracking, especiesDict, especiesViejasDict, especiesCodigosValidos, skipConsensusHeuristic);

        // 4. Validación de Muestras (M*) y Relación Largo-Peso
        ValidateSamples(report, muestras, capturas, lgs, largoPesoCatalogo, especiesDict, especiesViejasDict, especiesCodigosValidos, omitirValidacionCapturaProduccion);

        // 5. Validación de Submuestras (S*)
        ValidateSubSamples(report, submuestras, muestras, procesarSubmuestrasSinMuestraTalla);
        if (!omitirValidacionMuestraSubmuestra)
        {
            ValidateBiometricConsistency(report, muestras, submuestras);
        }

        // 6. Validación de Producción (P*)
        ValidateProduction(report, produccion, especiesDict, especiesViejasDict, especiesCientificasDict, especiesCodigosValidos, capturas, filtrarDiferenciasAuditoria, toleranciaFiltroAuditoria, omitirValidacionCapturaProduccion);

        report.Capturas = capturas;
        report.Muestras = muestras;
        report.Submuestras = submuestras;
        report.Lgs = lgs;
        report.Tracking = tracking;
        report.Produccion = produccion;

        return report;
    }

    private void ValidateProduction(MareaValidationReport report, List<LegacyProduccion> produccion, Dictionary<string, string> especiesDict, Dictionary<string, string> especiesViejasDict, Dictionary<string, string> especiesCientificasDict, HashSet<string> especiesCodigosValidos, List<LegacyCaptura> capturas, bool filtrarDiferenciasAuditoria, double toleranciaFiltroAuditoria, bool omitirValidacionCapturaProduccion)
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
                if (especiesViejasDict.TryGetValue(name, out string oldCode))
                {
                    // 3. Verificar si el código de la vieja existe en las actuales
                    if (especiesCodigosValidos.Contains(oldCode)) return false;
                }
                
                // 4. Fallback: ¿Es un código INIDEP?
                if (especiesCodigosValidos.Contains(name)) return false;

                // 5. NUEVO: Fallback tolerante a acentos
                var nameNoAccents = RemoveAccents(name);
                if (especiesDict.Keys.Any(k => RemoveAccents(k.ToUpper()) == nameNoAccents)) return false;
                if (especiesViejasDict.Keys.Any(k => RemoveAccents(k.ToUpper()) == nameNoAccents)) return false;
                
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

        if (omitirValidacionCapturaProduccion) return;

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
                var searchName = nombreEspecie.Normalize(NormalizationForm.FormC).Trim().ToUpper();
                string codEspecie = "";
                
                // REQ: Caso especial Granadero (Ambigüedad en datos legado)
                if (searchName == "GRANADERO")
                {
                    codEspecie = "7210090401";
                }
                else if (!especiesDict.TryGetValue(searchName, out codEspecie))
                {
                    if (!especiesViejasDict.TryGetValue(searchName, out codEspecie))
                    {
                        // Fallback: ¿Es un código?
                        if (especiesCodigosValidos.Contains(searchName)) codEspecie = searchName;
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

                // C. Comparar y reportar diferencias
                double diferencia = capturaNetaReal - capturaReconstruida;
                double diffAbs = Math.Abs(diferencia);
                
                double toleranciaPorcentaje = filtrarDiferenciasAuditoria ? toleranciaFiltroAuditoria : 0.0;
                double margenTolerancia = capturaNetaReal * (toleranciaPorcentaje / 100.0);
                
                // Evitar errores de precisión flotante si tolerancia es 0
                if (margenTolerancia < 0.001) margenTolerancia = 0.001;

                // Obtener nombre para mostrar en el reporte
                string especieDisplay = especiesCientificasDict.TryGetValue(codEspecie, out var codStr) && !string.IsNullOrWhiteSpace(codStr) ? codStr : nombreEspecie;

                string ctx = $"Fecha: {fecha:dd/MM/yyyy} | Especie: {especieDisplay}";
                string capReconStr = capturaReconstruida.ToString("N1", culture);
                string capRealStr = capturaNetaReal.ToString("N1", culture);
                
                string diffStr = diffAbs.ToString("N1", culture);
                double diffPorcentaje = (diffAbs / capturaNetaReal) * 100.0;
                string diffPctStr = diffPorcentaje.ToString("N1", culture);

                if (capturaReconstruida > capturaNetaReal + margenTolerancia)
                {
                    // Error: Se produjo más de lo que se capturó físicamente
                    report.AddIssue(ValidationLevel.Error, "Balance de Captura Diaria", 
                        $"Inconsistencia: La captura reconstruida ({capReconStr} kg) excede la captura neta real disponible ({capRealStr} kg) (Dif: {diffStr} kg, {diffPctStr}%){lancesStr}.", 
                        ctx);
                }
                else if (diffAbs > margenTolerancia)
                {
                    // Advertencia: Diferencia superior a la tolerancia
                    string tipoDiff = diferencia > 0 ? "sobrante" : "faltante";
                    report.AddIssue(ValidationLevel.Warning, "Balance de Captura Diaria", 
                        $"Diferencia de masa significativa ({tipoDiff}): Real {capRealStr} kg vs Reconstruida {capReconStr} kg (Dif: {diffStr} kg, {diffPctStr}%){lancesStr}.", 
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

    private string RemoveAccents(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
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
            report.AddIssue(ValidationLevel.Fatal, "Consistencia Temporal", $"Producción con fecha {p.Fecha:dd/MM/yyyy} fuera del rango de etapas de marea ({minDate:dd/MM/yyyy} al {maxDate:dd/MM/yyyy}).");
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
        if (bActual.Length > 20) bActual = bActual.Substring(0, 20);

        // 1. CAPTURAS
        foreach (var c in capturas)
        {
            var bDbf = c.Barco.Trim().ToUpper();
            if (bDbf.Length > 20) bDbf = bDbf.Substring(0, 20);

            if (bDbf != bActual)
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
            var bDbf = m.Barco.Trim().ToUpper();
            if (bDbf.Length > 20) bDbf = bDbf.Substring(0, 20);

            if (bDbf != bActual)
                report.AddIssue(ValidationLevel.Fatal, "Consistencia", $"Barco en muestra ({m.Barco}) no coincide con marea activa ({barcoActual}) en lance {m.Lance} del {m.Fecha:dd/MM/yyyy}", $"Lance {m.Lance}");
            
            if ((int)m.Marea != mareaActual)
            {
                if ((int)m.Marea == 0)
                {
                    string oldMarea = m.Marea.ToString();
                    m.Marea = mareaActual;
                    report.AddIssue(ValidationLevel.AutoFixed, "Consistencia", $"Nro Marea en muestra era 0 (Lance {m.Lance} del {m.Fecha:dd/MM/yyyy}). Se corrige a {mareaActual}.", $"Lance {m.Lance} Especie {m.Especie}", oldMarea, mareaActual.ToString());
                }
                else
                {
                    report.AddIssue(ValidationLevel.Error, "Consistencia", $"Nro Marea en muestra ({m.Marea}) no coincide con marea activa ({mareaActual}) en lance {m.Lance} del {m.Fecha:dd/MM/yyyy}", $"Lance {m.Lance} Especie {m.Especie}");
                }
            }
        }

        // 3. SUBMUES
        foreach (var s in submuestras)
        {
            var bDbf = s.Barco.Trim().ToUpper();
            if (bDbf.Length > 20) bDbf = bDbf.Substring(0, 20);

            if (bDbf != bActual)
                report.AddIssue(ValidationLevel.Fatal, "Consistencia", $"Barco en submuestra ({s.Barco}) no coincide con marea activa ({barcoActual}) en lance {s.Lance} del {s.Fecha:dd/MM/yyyy}", $"Lance {s.Lance} Ej {s.NEjemplar}");

            if ((int)s.Marea != mareaActual)
            {
                if ((int)s.Marea == 0)
                {
                    string oldMarea = s.Marea.ToString();
                    s.Marea = mareaActual;
                    report.AddIssue(ValidationLevel.AutoFixed, "Consistencia", $"Nro Marea en submuestra era 0 (Lance {s.Lance} del {s.Fecha:dd/MM/yyyy}). Se corrige a {mareaActual}.", $"Lance {s.Lance} Ej {s.NEjemplar}", oldMarea, mareaActual.ToString());
                }
                else
                {
                    report.AddIssue(ValidationLevel.Error, "Consistencia", $"Nro Marea en submuestra ({s.Marea}) no coincide con marea activa ({mareaActual}) en lance {s.Lance} del {s.Fecha:dd/MM/yyyy}", $"Lance {s.Lance} Ej {s.NEjemplar}");
                }
            }
        }

        // 4. LG
        foreach (var l in lgs)
        {
            var bDbf = l.Barco.Trim().ToUpper();
            if (bDbf.Length > 20) bDbf = bDbf.Substring(0, 20);

            if (bDbf != bActual)
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

        var duplicateLgs = lgs.GroupBy(l => l.Lance).Where(g => g.Count() > 1);
        foreach (var dup in duplicateLgs)
        {
            report.AddIssue(ValidationLevel.Fatal, "Consistencia", $"El archivo L* contiene {dup.Count()} registros para el lance {dup.Key}. Sólo se admite un registro L* por lance.", $"Lance {dup.Key}");
        }

        // 5. SEGUIMIENTO (T*) - Aquí el campo es "Buque"
        foreach (var t in tracking)
        {
            var bDbf = t.Buque.Trim().ToUpper();
            if (bDbf.Length > 20) bDbf = bDbf.Substring(0, 20);

            if (bDbf != bActual)
                report.AddIssue(ValidationLevel.Fatal, "Consistencia", $"Barco en seguimiento satelital ({t.Buque}) no coincide con marea activa ({barcoActual})", "Seguimiento T*");
        }

        // 6. PRODUCCIÓN (P*)
        foreach (var p in produccion)
        {
            var bDbf = p.Barco.Trim().ToUpper();
            if (bDbf.Length > 20) bDbf = bDbf.Substring(0, 20);

            if (bDbf != bActual)
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

    private void ValidateLances(MareaValidationReport report, List<LegacyCaptura> capturas, List<LegacyTracking> tracking,
        Dictionary<string, string> especiesDict, Dictionary<string, string> especiesViejasDict, HashSet<string> especiesCodigosValidos, bool skipConsensusHeuristic)
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

            // Validación de especies en el catálogo
            foreach (var spCode in c.Especies.Keys)
            {
                int colIndex = c.EspecieColumnIndex.TryGetValue(spCode, out int idx) ? idx : 0;
                string colInfo = colIndex > 0 ? $" (Nº orden {colIndex})" : "";

                if (spCode.StartsWith("0_col"))
                {
                    report.AddIssue(ValidationLevel.Fatal, "Catálogo Especies", $"Se reportan pesos de captura o descarte pero el código de especie está en blanco o es 0{colInfo}.", $"Captura Lance {c.Lance}");
                }
                else if (!especiesCodigosValidos.Contains(spCode))
                {
                    report.AddIssue(ValidationLevel.Fatal, "Catálogo Especies", $"La especie importada con código '{spCode}' no existe ni en el catálogo actual ni en el histórico{colInfo}.", $"Captura Lance {c.Lance}");
                }
            }

            // Verificar si hay especies duplicadas en el detalle de captura del lance
            var duplicatedEspecies = c.EspeciesOrder.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            foreach (var dupCode in duplicatedEspecies)
            {
                var nombres = especiesDict.Where(kvp => kvp.Value == dupCode).Select(k => k.Key).ToList();
                if (nombres.Count == 0) nombres = especiesViejasDict.Where(kvp => kvp.Value == dupCode).Select(k => k.Key).ToList();
                
                string vulgar = nombres.Count > 0 ? nombres[0] : $"Cod: {dupCode}";
                string cientifico = nombres.Count > 1 ? nombres[1] : dupCode;

                var nivelError = skipConsensusHeuristic ? ValidationLevel.Error : ValidationLevel.Fatal;
                string accion = skipConsensusHeuristic ? "Debe unificarlas o corregir la especie." : "Esto impide la importación para evitar pérdida de datos.";
                report.AddIssue(nivelError, "Captura", $"Especie duplicada: '{vulgar}' ({cientifico}) figura más de una vez en el detalle de captura. {accion}", ctx);
            }


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
            var timeStart = LegacyDecoder.DecodeTime(c.HoraInic);
            var timeEnd = LegacyDecoder.DecodeTime(c.HoraFinal);
            
            // Si el fin es menor que el inicio, asumimos cruce de medianoche (lance nocturno)
            bool cruceMedianoche = timeEnd < timeStart;
            var duration = cruceMedianoche ? (timeEnd.Add(TimeSpan.FromDays(1)) - timeStart) : (timeEnd - timeStart);

            if (duration.TotalMinutes == 0)
                report.AddIssue(ValidationLevel.Warning, "Tiempo", "Hora de inicio y fin son idénticas", ctx);
            else if (duration.TotalHours > 12)
                report.AddIssue(ValidationLevel.Warning, "Tiempo", $"Duración de lance inusualmente larga: {duration.TotalHours:F1} horas", ctx);

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
        var sortedByNumber = capturas.OrderBy(x => x.Lance).ToList();
        for (int i = 0; i < sortedByNumber.Count - 1; i++)
        {
            var current = sortedByNumber[i];
            var next = sortedByNumber[i + 1];

            // Brecha en numeración
            if (next.Lance - current.Lance > 1)
                report.AddIssue(ValidationLevel.Warning, "Estructura", $"Salto en la secuencia de lances detectado entre {current.Lance} y {next.Lance}");

            // Coherencia cronológica vs numeración
            var timeCurrent = current.Fecha.Date.Add(LegacyDecoder.DecodeTime(current.HoraInic));
            var timeNext = next.Fecha.Date.Add(LegacyDecoder.DecodeTime(next.HoraInic));
            
            if (timeNext < timeCurrent)
            {
                report.AddIssue(ValidationLevel.Error, "Tiempo", 
                    $"Inconsistencia cronológica: El lance {next.Lance} ({timeNext:dd/MM/yyyy HH:mm}) es anterior al lance {current.Lance} ({timeCurrent:dd/MM/yyyy HH:mm}).", 
                    $"Lance {next.Lance}");
            }

            // Detección de solapamientos
            var fechaFinCurrent = current.FechaFin ?? current.Fecha;
            var timeCurrentEnd = fechaFinCurrent.Date.Add(LegacyDecoder.DecodeTime(current.HoraFinal));
            if (timeCurrentEnd < timeCurrent) timeCurrentEnd = timeCurrentEnd.AddDays(1);

            if (timeNext < timeCurrentEnd)
            {
                report.AddIssue(ValidationLevel.Error, "Tiempo", 
                    $"Solapamiento detectado: El lance {next.Lance} inicia ({timeNext:HH:mm}) antes de que finalice el lance {current.Lance} ({timeCurrentEnd:HH:mm}).", 
                    $"Lance {next.Lance}");
            }
        }

        if (skipConsensusHeuristic)
        {
            foreach (var c in capturas)
            {
                if (c.CaptTotal > 0 && c.Descarte > c.CaptTotal)
                {
                    var especiesConDescarteExcesivo = new List<string>();
                    foreach (var kvp in c.DescartesPorEspecie)
                    {
                        string codEspecie = kvp.Key;
                        double descarteEsp = kvp.Value;
                        if (descarteEsp > 0)
                        {
                            c.Especies.TryGetValue(codEspecie, out double captEsp);
                            if (descarteEsp > captEsp)
                            {
                                string nombre = GetEspecieNombre(codEspecie, especiesDict, especiesViejasDict);
                                especiesConDescarteExcesivo.Add(nombre);
                            }
                        }
                    }

                    if (especiesConDescarteExcesivo.Count == 0)
                    {
                        foreach (var kvp in c.DescartesPorEspecie)
                        {
                            if (kvp.Value > 0)
                            {
                                string nombre = GetEspecieNombre(kvp.Key, especiesDict, especiesViejasDict);
                                especiesConDescarteExcesivo.Add(nombre);
                            }
                        }
                    }

                    string ctxEspecies = especiesConDescarteExcesivo.Count > 0 
                        ? $"Lance {c.Lance} - Especie {string.Join(", ", especiesConDescarteExcesivo)}"
                        : $"Lance {c.Lance}";

                    report.AddIssue(ValidationLevel.Error, "Captura", $"El descarte ({c.Descarte} kg) es superior a la captura total ({c.CaptTotal} kg).", ctxEspecies);
                }
            }
        }
        else
        {
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
                                var especiesConDescarteExcesivo = new List<string>();
                                foreach (var kvp in c.DescartesPorEspecie)
                                {
                                    string codEspecie = kvp.Key;
                                    double descarteEsp = kvp.Value;
                                    if (descarteEsp > 0)
                                    {
                                        c.Especies.TryGetValue(codEspecie, out double captEsp);
                                        if (descarteEsp > captEsp)
                                        {
                                            string nombre = GetEspecieNombre(codEspecie, especiesDict, especiesViejasDict);
                                            especiesConDescarteExcesivo.Add(nombre);
                                        }
                                    }
                                }

                                // Fallback por si la suma acumulada de varias supera al total pero ninguna supera individualmente su propia captura
                                if (especiesConDescarteExcesivo.Count == 0)
                                {
                                    foreach (var kvp in c.DescartesPorEspecie)
                                    {
                                        if (kvp.Value > 0)
                                        {
                                            string nombre = GetEspecieNombre(kvp.Key, especiesDict, especiesViejasDict);
                                            especiesConDescarteExcesivo.Add(nombre);
                                        }
                                    }
                                }

                                string ctxEspecies = especiesConDescarteExcesivo.Count > 0 
                                    ? $"Lance {c.Lance} - Especie {string.Join(", ", especiesConDescarteExcesivo)}"
                                    : $"Lance {c.Lance}";

                                report.AddIssue(ValidationLevel.Error, "Captura", $"El descarte ({c.Descarte} kg) es superior a la captura total ({c.CaptTotal} kg). Se asume que son kilos erróneos (no porcentaje) por consenso de marea.", ctxEspecies);
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
    }

    private void ValidateLanceDetails(MareaValidationReport report, LegacyCaptura c)
    {
        string ctx = $"Lance {c.Lance}";

        // REQ-5.5: Profundidad
        if (c.ProfInic <= 0 || c.ProfInic > 2000)
            report.AddIssue(ValidationLevel.Error, "Geografía", $"Profundidad inicial ({c.ProfInic}m) fuera de rango (0-2000)", ctx);
        
        if (c.ProfFinal <= 0 || c.ProfFinal > 2000)
            report.AddIssue(ValidationLevel.Error, "Geografía", $"Profundidad final ({c.ProfFinal}m) fuera de rango (0-2000)", ctx);

        if (c.ProfInic > 0 && c.ProfFinal > 0)
        {
            double diff = Math.Abs(c.ProfFinal - c.ProfInic);
            double baseProf = c.ProfInic;
            
            if (diff >= 50 && diff >= baseProf * 0.60)
            {
                report.AddIssue(ValidationLevel.Warning, "Geografía", $"Diferencia significativa de profundidad ({(diff/baseProf*100):N1}%). Verifique posible error de tipeo ({c.ProfInic}m -> {c.ProfFinal}m)", ctx);
            }
        }

        // REQ-5.8: Lances sin especies
        if (!c.Especies.Any() || c.Especies.Values.Sum() == 0)
            report.AddIssue(ValidationLevel.Error, "Captura", "Lance sin registro de especies o captura total cero", ctx);
    }

    private void ValidateSamples(MareaValidationReport report, List<LegacyMuestra> muestras, List<LegacyCaptura> capturas, List<LegacyLg> lgs, 
        Dictionary<(string EspecieId, int Sexo), (double A, double B)> largoPesoCatalogo,
        Dictionary<string, string> especiesDict,
        Dictionary<string, string> especiesViejasDict,
        HashSet<string> especiesCodigosValidos,
        bool omitirValidacionCapturaProduccion)
    {
        foreach (var m in muestras)
        {
            string ctx = $"Lance {m.Lance} - Especie {m.Especie}";

            // REQ-4.1.1: Verificar existencia de lance en captura
            if (!capturas.Any(c => (int)c.Lance == (int)m.Lance))
                report.AddIssue(ValidationLevel.Error, "Integridad", $"Muestra de lance {m.Lance} ({m.Fecha:dd/MM/yyyy}) no tiene lance correspondiente en CAPTURA", ctx);

            // REQ-4.3.1: Verificar Rangos de Talla
            if (m.UltTalla < m.PrimTalla)
                report.AddIssue(ValidationLevel.Error, "Biometría", $"Última talla ({m.UltTalla}) no puede ser menor que la primera talla ({m.PrimTalla}) en lance {m.Lance} ({m.Fecha:dd/MM/yyyy})", ctx);

            if (m.Intervalo <= 0)
                report.AddIssue(ValidationLevel.Error, "Biometría", $"Intervalo de tallas inválido (<= 0) en lance {m.Lance} ({m.Fecha:dd/MM/yyyy})", ctx);

            // --- NUEVAS VALIDACIONES DE INTEGRIDAD (Punto 2) ---

            var lanceCorrespondiente = capturas.FirstOrDefault(c => (int)c.Lance == (int)m.Lance);
            string codEspecieMuestra = m.CodEspec;

            if (lanceCorrespondiente != null)
            {
                // REQ-3.2.4: Consistencia de Fecha (Muestra vs Lance)
                if (m.Fecha.Date != lanceCorrespondiente.Fecha.Date)
                {
                    string oldFecha = m.Fecha.ToString("dd/MM/yyyy");
                    string newFecha = lanceCorrespondiente.Fecha.ToString("dd/MM/yyyy");
                    m.Fecha = lanceCorrespondiente.Fecha; // Auto-corrección como en pcorrecc.PRG
                    report.AddIssue(ValidationLevel.AutoFixed, "Integridad", $"Fecha de muestra ({oldFecha}) no coincide con fecha de lance {m.Lance} ({newFecha}). Corregido.", ctx, oldFecha, newFecha);
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
                        var searchName = m.Especie.Trim().ToUpper().Normalize(NormalizationForm.FormC);
                        
                        // 1. Buscar en actuales por nombre
                        if (especiesDict.TryGetValue(searchName, out string newCode))
                        {
                            codEspecieMuestra = newCode;
                        }
                        // 2. Buscar en viejas por nombre
                        else if (especiesViejasDict.TryGetValue(searchName, out string bridgeCode))
                        {
                            // 3. Verificar puente a actuales
                            if (especiesCodigosValidos.Contains(bridgeCode))
                            {
                                codEspecieMuestra = bridgeCode;
                            }
                        }
                        // 4. NUEVO: Búsqueda tolerante a acentos
                        else
                        {
                            var searchNoAccents = RemoveAccents(searchName);
                            var match = especiesDict.FirstOrDefault(kvp => RemoveAccents(kvp.Key.ToUpper()) == searchNoAccents);
                            if (match.Value != null)
                            {
                                codEspecieMuestra = match.Value;
                            }
                            else
                            {
                                var oldMatch = especiesViejasDict.FirstOrDefault(kvp => RemoveAccents(kvp.Key.ToUpper()) == searchNoAccents);
                                if (oldMatch.Value != null && especiesCodigosValidos.Contains(oldMatch.Value))
                                {
                                    codEspecieMuestra = oldMatch.Value;
                                }
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(codEspecieMuestra))
                {
                    bool capturada = lanceCorrespondiente.Especies.ContainsKey(codEspecieMuestra) && lanceCorrespondiente.Especies[codEspecieMuestra] > 0;
                    if (!capturada)
                    {
                        report.AddIssue(ValidationLevel.Warning, "Integridad", $"Se registró una muestra de '{m.Especie}' (Cod: {codEspecieMuestra}) pero esta especie no figura con kilos capturados en el lance {m.Lance} del {m.Fecha:dd/MM/yyyy}.", ctx);
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
                string especieIdNum = "";

                if (!string.IsNullOrEmpty(m.Especie))
                {
                    if (!especiesDict.TryGetValue(m.Especie.Trim().ToUpper(), out especieIdNum))
                    {
                        especiesViejasDict.TryGetValue(m.Especie.Trim().ToUpper(), out especieIdNum);
                    }
                }

                if (!string.IsNullOrEmpty(especieIdNum))
                {
                    if (largoPesoCatalogo.TryGetValue((especieIdNum, 0), out var paramsGral))
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
                string speciesIdForLookup = !string.IsNullOrEmpty(codEspecieMuestra) ? codEspecieMuestra : m.CodEspec;
                
                if (string.IsNullOrEmpty(speciesIdForLookup) && !string.IsNullOrEmpty(m.Especie))
                {
                    if (especiesDict == null || !especiesDict.TryGetValue(m.Especie.Trim().ToUpper(), out speciesIdForLookup))
                    {
                        if (especiesViejasDict != null)
                        {
                            especiesViejasDict.TryGetValue(m.Especie.Trim().ToUpper(), out speciesIdForLookup);
                        }
                    }
                }

                string espId = (speciesIdForLookup ?? "").Trim();

                if (hasFallback || (largoPesoCatalogo != null && largoPesoCatalogo.Any(k => k.Key.EspecieId.Trim() == espId)))
                {
                    // Función local para obtener parámetros con fallback
                    (double A, double B) GetSmartParams(int targetSex, out bool isSpecific)
                    {
                        (double A, double B) res = (0, 0);
                        isSpecific = false;

                        // 1. Intentar búsqueda exacta
                        if (largoPesoCatalogo.TryGetValue((espId, targetSex), out res)) 
                        {
                            isSpecific = (targetSex == 1 || targetSex == 2);
                        }
                        
                        // 2. Fallbacks de sexo
                        if (res.A <= 0)
                        {
                            if (targetSex == 0 && largoPesoCatalogo.TryGetValue((espId, 3), out res)) { }
                            else if (targetSex != 0 && largoPesoCatalogo.TryGetValue((espId, 0), out res)) { }
                        }

                        // 3. Cualquier sexo (último recurso)
                        if (res.A <= 0)
                        {
                            var anyEntry = largoPesoCatalogo.FirstOrDefault(k => k.Key.EspecieId == espId);
                            if (anyEntry.Key.EspecieId != null) { res = anyEntry.Value; }
                        }

                        // 4. Promedios
                        if (res.A <= 0)
                        {
                            bool hasM = largoPesoCatalogo.TryGetValue((espId, 1), out var pM);
                            bool hasF = largoPesoCatalogo.TryGetValue((espId, 2), out var pF);
                            if (hasM && hasF) { res = ((pM.A + pF.A) / 2.0, (pM.B + pF.B) / 2.0); }
                            else if (hasM) { res = pM; }
                            else if (hasF) { res = pF; }
                        }

                        if (res.A <= 0 && hasFallback) { res = (fallbackA, fallbackB); }
                        return res;
                    }

                    // Validar consistencia de NroTotal vs suma de sexos
                    for (int idx = 0; idx < m.Tallies.Count; idx++)
                    {
                        var tally = m.Tallies[idx];
                        int sumaSexos = tally.Males + tally.Females + tally.Indeterminate;
                        
                        if (sumaSexos > 0 && tally.Total != sumaSexos)
                        {
                            int oldTotal = tally.Total;
                            m.Tallies[idx] = tally with { Total = sumaSexos };
                            report.AddIssue(ValidationLevel.AutoFixed, "Integridad", 
                                $"Total en talla {tally.Size} ({oldTotal}) no coincide con suma de sexos ({sumaSexos}). Corregido.", ctx);
                        }
                    }

                    foreach (var tally in m.Tallies)
                    {
                        double tallaCm = tally.Size; 
                        
                        var pM = GetSmartParams(1, out bool specificM);
                        var pH = GetSmartParams(2, out bool specificH);
                        var pI = GetSmartParams(0, out _);

                        // Si hay discriminación y fórmulas específicas para ambos sexos
                        if ((tally.Males > 0 || tally.Females > 0) && specificM && specificH)
                        {
                            if (tally.Males > 0 && pM.A > 0) totalWeight += (tally.Males * (pM.A * Math.Pow(tallaCm, pM.B)));
                            if (tally.Females > 0 && pH.A > 0) totalWeight += (tally.Females * (pH.A * Math.Pow(tallaCm, pH.B)));
                            if (tally.Indeterminate > 0 && pI.A > 0) totalWeight += (tally.Indeterminate * (pI.A * Math.Pow(tallaCm, pI.B)));
                            foundAnyParams = true;
                        }
                        // De lo contrario, si hay Indeterminados o conteo parcial, usamos fórmula general
                        else if (tally.Males > 0 || tally.Females > 0 || tally.Indeterminate > 0)
                        {
                            if (pI.A > 0) 
                            { 
                                int suma = tally.Males + tally.Females + tally.Indeterminate;
                                totalWeight += (suma * (pI.A * Math.Pow(tallaCm, pI.B))); 
                                foundAnyParams = true; 
                            }
                        }
                        // Si todo es cero pero hay Total (muestra sin discriminar)
                        else if (tally.Total > 0)
                        {
                            if (pI.A > 0) 
                            { 
                                totalWeight += (tally.Total * (pI.A * Math.Pow(tallaCm, pI.B))); 
                                foundAnyParams = true; 
                            }
                        }
                    }
                    totalWeight /= 1000.0; // Pasar de gramos a kg
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
                        
                    string debugInfo = $"[ID Resuelto: {espId}, Nombre: '{m.Especie}', CodEspec Original: {m.CodEspec}]{catalogPreview}{logText}";
                    report.AddIssue(ValidationLevel.Error, "Biometría", $"Peso de muestra es 0 y no se pudieron encontrar parámetros de biometría para la especie '{m.Especie}'.", ctx);
                }
            }
            // --- NUEVA VALIDACIÓN: Peso Muestra vs Peso Captura ---
            if (!omitirValidacionCapturaProduccion && lanceCorrespondiente != null && !string.IsNullOrEmpty(codEspecieMuestra))
            {
                if (lanceCorrespondiente.Especies.TryGetValue(codEspecieMuestra, out double kilosCaptura))
                {
                    // Tolerancia de 10 gramos por redondeos en la conversión gramos/kilos
                    if (m.PesoMues > kilosCaptura + 0.01)
                    {
                        double diffAbs = m.PesoMues - kilosCaptura;
                        string pctStr = kilosCaptura > 0 ? $", {(diffAbs / kilosCaptura * 100.0):N1}%" : "";
                        report.AddIssue(ValidationLevel.Error, "Integridad", 
                            $"Inconsistencia: El peso de la muestra ({m.PesoMues:N2} kg) es superior a la captura registrada de la especie ({kilosCaptura:N2} kg) en el lance {m.Lance} (Dif: {diffAbs:N2} kg{pctStr}).", ctx);
                    }
                }
            }
        }

        // --- VALIDACIÓN DE INTEGRIDAD L* vs M* ---
        foreach (var lg in lgs)
        {
            string ctx = $"Archivo L* - Lance {lg.Lance}";
            
            // Buscar muestra correspondiente a Langostino (Prioriza Código Inidep, y fallback por Nombre). Debe ser muestra Estándar (Tipo 1).
            var muestraM = muestras.FirstOrDefault(m => 
                (int)m.Lance == (int)lg.Lance && 
                m.TipoMuestra == 1 &&
                (m.CodEspec == "5139030101" || m.Especie?.Trim().ToUpper().Normalize(System.Text.NormalizationForm.FormC) == "LANGOSTINO"));

            if (muestraM == null)
            {
                if (lg.Frecuencias.Any(f => f.Value > 0))
                {
                    report.AddIssue(ValidationLevel.Fatal, "Integridad L/M", 
                        $"El archivo L* contiene datos de madurez para el lance {lg.Lance} pero no existe una muestra biológica (M*) de Langostino correspondiente.", ctx);
                }
                continue;
            }

            // Verificar que cada talla en L* exista en M* y aplicar validaciones biológicas
            foreach (var kvp in lg.Frecuencias)
            {
                int tallaL = kvp.Key;
                if (kvp.Value > 0)
                {
                    var tallyM = muestraM.Tallies.FirstOrDefault(t => t.Size == tallaL);
                    if (tallyM == null)
                    {
                        report.AddIssue(ValidationLevel.Fatal, "Integridad L/M", 
                            $"El archivo L* registra datos de madurez para la talla {tallaL} mm, pero esa talla no figura como medida en el archivo de muestra (M*).", 
                            $"Lance {lg.Lance} - Especie LANGOSTINO - Talla {tallaL}");
                    }
                    else
                    {
                        // Validaciones biológicas L* vs M*
                        var decoded = LegacyDecoder.DecodeMatureTally(kvp.Value);
                        if (decoded.MatureMales > tallyM.Males)
                        {
                            report.AddIssue(ValidationLevel.Error, "Integridad L/M",
                                $"La talla {tallaL} tiene {decoded.MatureMales} machos maduros (archivo L*), superando el total de {tallyM.Males} machos registrados (archivo M*).",
                                $"Lance {lg.Lance} - Especie LANGOSTINO - Talla {tallaL}");
                        }
                        
                        if ((decoded.MatureFemales + decoded.ImpregnatedFemales) > tallyM.Females)
                        {
                            report.AddIssue(ValidationLevel.Error, "Integridad L/M",
                                $"La talla {tallaL} tiene {decoded.MatureFemales + decoded.ImpregnatedFemales} hembras maduras e impregnadas (archivo L*), superando el total de {tallyM.Females} hembras registradas (archivo M*).",
                                $"Lance {lg.Lance} - Especie LANGOSTINO - Talla {tallaL}");
                        }
                    }
                }
            }
        }
    }

    private void ValidateSubSamples(MareaValidationReport report, List<LegacySubmuestra> submuestras, List<LegacyMuestra> muestras, bool procesarSubmuestrasSinMuestraTalla)
    {
        // NUEVA VALIDACIÓN: Suma de pesos de ejemplares (S*) vs Peso total de muestra (M*)
        var subPorMuestra = submuestras
            .GroupBy(s => new { s.Lance, Especie = s.Especie?.Trim().ToUpper() });

        foreach (var group in subPorMuestra)
        {
            var parent = muestras.FirstOrDefault(m => 
                (int)m.Lance == (int)group.Key.Lance && 
                m.Especie?.Trim().ToUpper() == group.Key.Especie &&
                m.TipoMuestra == 1);

            if (parent != null && parent.PesoMues > 0)
            {
                double sumSubWeights = group.Sum(s => s.PesoTot);
                // Tolerancia de 50 gramos para acumulado de redondeos en balanza/carga
                if (sumSubWeights > parent.PesoMues + 0.05)
                {
                    report.AddIssue(ValidationLevel.Error, "Integridad", 
                        $"Inconsistencia en Lance {group.Key.Lance} ({group.First().Fecha:dd/MM/yyyy}): La suma de pesos de los ejemplares ({sumSubWeights:F2} kg) excede el peso total de la muestra ({parent.PesoMues:F2} kg).", 
                        $"Lance {group.Key.Lance} - Especie {group.Key.Especie}");
                }
            }
        }
        // REQ-4.1.3: Verificar Números de Ejemplar Duplicados
        var subDuplicates = submuestras
            .GroupBy(s => new { s.Lance, s.Especie, s.NEjemplar })
            .Where(g => g.Count() > 1);
        
        foreach (var group in subDuplicates)
        {
            report.AddIssue(ValidationLevel.Error, "Estructura", 
                $"El ejemplar {group.Key.NEjemplar} de '{group.Key.Especie}' aparece duplicado {group.Count()} veces en el lance {group.Key.Lance} del {group.First().Fecha:dd/MM/yyyy}.");
        }

        foreach (var s in submuestras)
        {
            string ctx = $"Lance {s.Lance} - Ejemplar {s.NEjemplar}";

            // Integridad: Verificar existencia de muestra padre estándar
            var allParents = muestras.Where(m => (int)m.Lance == (int)s.Lance && m.Especie?.Trim().ToUpper() == s.Especie?.Trim().ToUpper()).ToList();
            var parent = allParents.FirstOrDefault(m => m.TipoMuestra == 1); // Preferencia Estándar
            
            if (parent == null)
            {
                var discardParent = allParents.FirstOrDefault(m => m.TipoMuestra == 2);
                if (discardParent != null)
                {
                    report.AddIssue(ValidationLevel.Fatal, "Integridad", $"La submuestra de la especie {s.Especie} en el lance {s.Lance} está vinculada a una muestra de descarte, lo cual no es permitido. Debe existir una muestra estándar para procesar biometría individual.", ctx);
                }
                else
                {
                    if (procesarSubmuestrasSinMuestraTalla)
                    {
                        report.AddIssue(ValidationLevel.Warning, "Integridad", $"Submuestra sin muestra de talla asociada: Se reconstruirá automáticamente la muestra de talla estándar correspondiente a la especie {s.Especie} en el lance {s.Lance} a partir de los datos biológicos individuales de la submuestra.", ctx);
                    }
                    else
                    {
                        report.AddIssue(ValidationLevel.Fatal, "Integridad", $"Submuestra huérfana: No existe una muestra estándar para la especie {s.Especie} en el lance {s.Lance} del {s.Fecha:dd/MM/yyyy}.", ctx);
                    }
                }
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
                report.AddIssue(ValidationLevel.Warning, "Biometría", $"Largo total atípico ({s.LargoTot}mm > 250mm) en lance {s.Lance} ({s.Fecha:dd/MM/yyyy}). Requiere revisión.", ctx);

            // REQ-4.2.2: Largo estándar vs total
            if (s.LargoSta > s.LargoTot)
                report.AddIssue(ValidationLevel.Error, "Biometría", $"Largo estándar ({s.LargoSta}) mayor que largo total ({s.LargoTot}) en lance {s.Lance} ({s.Fecha:dd/MM/yyyy})", ctx);
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
        var fechaFin = c.FechaFin ?? c.Fecha;
        var timeEnd = fechaFin.Date.Add(LegacyDecoder.DecodeTime(c.HoraFinal));

        // Si el lance cruzó la medianoche (hora final < hora inicio) y no tiene fecha fin explícita, se suma 1 día
        if (c.FechaFin == null && timeEnd < timeStart)
        {
            timeEnd = timeEnd.AddDays(1);
        }

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
        var fechaFin = c.FechaFin ?? c.Fecha;
        var timeEnd = fechaFin.Date.Add(LegacyDecoder.DecodeTime(c.HoraFinal));
        
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
                End = (c.FechaFin ?? c.Fecha).Date.Add(LegacyDecoder.DecodeTime(c.HoraFinal))
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

    private string GetEspecieNombre(string codigo, Dictionary<string, string> especiesDict, Dictionary<string, string> especiesViejasDict)
    {
        if (string.IsNullOrEmpty(codigo)) return "Especie Desconocida";

        var match = especiesDict.FirstOrDefault(kvp => kvp.Value == codigo);
        if (match.Key != null) return match.Key;
        
        var matchOld = especiesViejasDict.FirstOrDefault(kvp => kvp.Value == codigo);
        if (matchOld.Key != null) return matchOld.Key;
        
        return $"Código {codigo}";
    }

    private void ValidateBiometricConsistency(MareaValidationReport report, List<LegacyMuestra> muestras, List<LegacySubmuestra> submuestras)
    {
        // Agrupar submuestras por (Lance, Especie, Talla, Sexo)
        // Sexo en submuestra: 1=Macho, 2=Hembra, 3=Indeterminado
        var subGroupByTuple = submuestras
            .GroupBy(s => new { 
                Lance = s.Lance, 
                Especie = s.Especie.Trim().ToUpper(), 
                Talla = s.LargoTot, 
                Sexo = s.Sexo 
            })
            .ToDictionary(g => g.Key, g => g.Count());

        var muestrasByLanceEspecie = muestras
            .GroupBy(m => new { Lance = m.Lance, Especie = m.Especie.Trim().ToUpper() });

        foreach (var mGroup in muestrasByLanceEspecie)
        {
            double lance = mGroup.Key.Lance;
            string especie = mGroup.Key.Especie;

            // Ignorar validación si el lance no tiene submuestras asociadas a esta muestra
            bool hasSubmuestras = subGroupByTuple.Keys.Any(k => k.Lance == lance && k.Especie == especie);
            if (!hasSubmuestras)
            {
                continue;
            }

            int casosTotales = 0;
            int casosCorrectos = 0;
            List<string> erroresDetalle = new List<string>();

            foreach (var muestra in mGroup)
            {
                // Ignorar validación en muestras automáticas
                if (muestra.Automatica == 1) continue;

                foreach (var tally in muestra.Tallies)
                {
                    int talla = tally.Size;

                    // Ignorar tallas menores a 19 por regla de negocio
                    if (talla < 19) continue;

                    // Función local para procesar cada sexo
                    void Evaluar(int cantidadMuestra, int sexoCode)
                    {
                        if (cantidadMuestra == 0) return;

                        int esperado = (int)Math.Ceiling(cantidadMuestra / 5.0);
                        
                        var tupleKey = new { Lance = lance, Especie = especie, Talla = talla, Sexo = sexoCode };
                        int reales = subGroupByTuple.TryGetValue(tupleKey, out int count) ? count : 0;

                        casosTotales++;
                        if (reales == esperado)
                        {
                            casosCorrectos++;
                        }
                        else
                        {
                            string sexoNombre = sexoCode == 1 ? "Macho" : (sexoCode == 2 ? "Hembra" : "Indet.");
                            erroresDetalle.Add($"Talla {talla} {sexoNombre}: esp {esperado}, real {reales}");
                        }
                    }

                    if (tally.Males > 0) Evaluar(tally.Males, 1);
                    if (tally.Females > 0) Evaluar(tally.Females, 2);
                    if (tally.Indeterminate > 0) Evaluar(tally.Indeterminate, 3);
                }
            }

            // Evaluar los excesos (submuestras que existan y no tengan contraparte en muestra)
            var submuestrasExtras = subGroupByTuple
                .Where(kvp => kvp.Key.Lance == lance && kvp.Key.Especie == especie)
                .ToList();
            
            foreach (var extra in submuestrasExtras)
            {
                // Ignorar tallas menores a 19 por regla de negocio
                if (extra.Key.Talla < 19) continue;

                bool fueEvaluado = mGroup.Any(m => m.Tallies.Any(t => 
                    t.Size == extra.Key.Talla && 
                    ((extra.Key.Sexo == 1 && t.Males > 0) || 
                     (extra.Key.Sexo == 2 && t.Females > 0) || 
                     (extra.Key.Sexo == 3 && t.Indeterminate > 0))
                ));

                if (!fueEvaluado)
                {
                    casosTotales++;
                    string sexoNombre = extra.Key.Sexo == 1 ? "Macho" : (extra.Key.Sexo == 2 ? "Hembra" : "Indet.");
                    erroresDetalle.Add($"Talla {extra.Key.Talla} {sexoNombre}: esp 0, real {extra.Value}");
                }
            }

            if (casosTotales > 0)
            {
                double porcentaje = (double)casosCorrectos / casosTotales * 100.0;
                
                if (porcentaje < 100.0)
                {
                    string detallesStr = string.Join("; ", erroresDetalle);
                    report.AddIssue(ValidationLevel.Warning, "Consistencia Biológica", 
                        $"La consistencia biométrica (Submuestra vs Muestra) es del {porcentaje:F1}%. Fallos: {detallesStr}.", 
                        $"Lance {lance} {especie}");
                }
            }
        }
    }
}
