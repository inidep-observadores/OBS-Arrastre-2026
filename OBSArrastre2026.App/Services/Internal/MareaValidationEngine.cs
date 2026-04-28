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
        Dictionary<(string EspecieId, int Sexo), (double A, double B)> largoPesoCatalogo)
    {
        var report = new MareaValidationReport
        {
            Barco = barcoMareaActual,
            Marea = nroMareaActual.ToString(),
            Año = anioMareaActual,
            TotalLances = capturas.Count
        };

        ValidateBaseConsistency(report, barcoMareaActual, nroMareaActual, capturas, muestras, submuestras, lgs, tracking, produccion);
        ValidateLances(report, capturas, tracking);
        ValidateSamples(report, muestras, capturas, lgs, largoPesoCatalogo, especiesDict);
        ValidateSubSamples(report, submuestras, muestras);
        ValidateProduction(report, produccion, especiesDict, capturas);
        ValidateTemporalConsistency(report, etapasFechas, capturas, muestras, produccion);
        
        report.Capturas = capturas;
        report.Muestras = muestras;
        report.Submuestras = submuestras;
        report.Lgs = lgs;
        report.Tracking = tracking;
        report.Produccion = produccion;

        return report;
    }

    private void ValidateProduction(MareaValidationReport report, List<LegacyProduccion> produccion, Dictionary<string, long> especiesDict, List<LegacyCaptura> capturas)
    {
        // 1. Validar existencia de especie por nombre
        var setNombresVulgares = new HashSet<string>(especiesDict.Keys, StringComparer.OrdinalIgnoreCase);
        var especiesDesconocidas = produccion
            .Select(p => p.Especie?.Trim().ToUpper())
            .Where(name => !string.IsNullOrEmpty(name) && !setNombresVulgares.Contains(name))
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
                $"Existen {group.Count()} registros para el producto '{group.Key.Producto}' ({group.Key.Categoria}) el {group.Key.Fecha:yyyy-MM-dd}. Se importarán todos pero se recomienda verificar posibles duplicados.", 
                $"Fecha {group.Key.Fecha:yyyy-MM-dd}");
        }

        // 3. Reglas avanzadas de producción (P*)
        foreach (var p in produccion)
        {
            string ctx = $"Fecha {p.Fecha:yyyy-MM-dd} Especie {p.Especie}";
            // Factor vs Producto
            if (p.Producto?.IndexOf("ENTERO", StringComparison.OrdinalIgnoreCase) >= 0 && p.Factor != 1)
            {
                report.AddIssue(ValidationLevel.Error, "Producción", $"Producto indica 'ENTERO' pero el factor de conversión es {p.Factor} (debería ser 1).", ctx);
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

        // 4. Balance de Masa
        var prodGroups = produccion
            .Where(p => !string.IsNullOrEmpty(p.Especie) && especiesDict.ContainsKey(p.Especie.Trim().ToUpper()))
            .GroupBy(p => especiesDict[p.Especie.Trim().ToUpper()]);

        foreach (var prodGroup in prodGroups)
        {
            long codEspecie = prodGroup.Key;
            double pesoVivoProduccion = prodGroup.Sum(p => p.Factor > 0 ? (p.Kilos / p.Factor) : p.Kilos);
            double capturaTotalEspecie = capturas.Sum(c => c.Especies.ContainsKey(codEspecie) ? c.Especies[codEspecie] : 0);

            if (capturaTotalEspecie > 0)
            {
                double diferencia = (pesoVivoProduccion - capturaTotalEspecie) / capturaTotalEspecie;
                if (diferencia > 0.10)
                {
                    report.AddIssue(ValidationLevel.Error, "Producción", $"Balance de masa: Producción ({pesoVivoProduccion:F0} kg) excede la captura ({capturaTotalEspecie:F0} kg) en más del 10%.", $"Cod Especie {codEspecie}");
                }
                else if (diferencia > 0.02)
                {
                    report.AddIssue(ValidationLevel.Warning, "Producción", $"Balance de masa: Producción ({pesoVivoProduccion:F0} kg) excede la captura ({capturaTotalEspecie:F0} kg) en más del 2%.", $"Cod Especie {codEspecie}");
                }
            }
        }
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
            report.AddIssue(ValidationLevel.Error, "Consistencia Temporal", $"Lance {c.Lance} con fecha {c.Fecha:yyyy-MM-dd} fuera del rango de etapas de marea ({minDate:yyyy-MM-dd} al {maxDate:yyyy-MM-dd}).");
        }
        foreach (var m in muestras.Where(m => m.Fecha.Date < minDate || m.Fecha.Date > maxDate))
        {
            report.AddIssue(ValidationLevel.Error, "Consistencia Temporal", $"Muestra (Lance {m.Lance}) con fecha {m.Fecha:yyyy-MM-dd} fuera del rango de etapas de marea.");
        }
        foreach (var p in produccion.Where(p => p.Fecha.Date < minDate || p.Fecha.Date > maxDate))
        {
            report.AddIssue(ValidationLevel.Error, "Consistencia Temporal", $"Producción con fecha {p.Fecha:yyyy-MM-dd} fuera del rango de etapas de marea.");
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
                report.AddIssue(ValidationLevel.Error, "Consistencia", $"Nro Marea en captura ({c.Marea}) no coincide con marea activa ({mareaActual})", $"Lance {c.Lance}");
        }

        // 2. MUESTRAS
        foreach (var m in muestras)
        {
            if (m.Barco.Trim().ToUpper() != bActual)
                report.AddIssue(ValidationLevel.Fatal, "Consistencia", $"Barco en muestra ({m.Barco}) no coincide con marea activa ({barcoActual})", $"Lance {m.Lance}");
        }

        // 3. SUBMUES
        foreach (var s in submuestras)
        {
            if (s.Barco.Trim().ToUpper() != bActual)
                report.AddIssue(ValidationLevel.Fatal, "Consistencia", $"Barco en submuestra ({s.Barco}) no coincide con marea activa ({barcoActual})", $"Lance {s.Lance} Ej {s.NEjemplar}");
        }

        // 4. LG
        foreach (var l in lgs)
        {
            if (l.Barco.Trim().ToUpper() != bActual)
                report.AddIssue(ValidationLevel.Fatal, "Consistencia", $"Barco en archivo LG ({l.Barco}) no coincide con marea activa ({barcoActual})", $"Lance {l.Lance}");
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
                report.AddIssue(ValidationLevel.Fatal, "Consistencia", $"Barco en producción ({p.Barco}) no coincide con marea activa ({barcoActual})", $"Fecha {p.Fecha:yyyy-MM-dd}");
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

            // REQ-3.3.1: Cálculo y validación de Área
            double calculatedArea = CalculateArea(latDec, lonDec);
            if (calculatedArea < 3500)
                report.AddIssue(ValidationLevel.Warning, "Geografía", $"Área calculada ({calculatedArea}) es inusualmente baja", ctx);

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

        // Resolución de porcentaje de descarte
        if (countPorcentaje > 0 && countKilos > 0)
        {
            report.AddIssue(ValidationLevel.Fatal, "Descarte", $"Datos mixtos de descarte: {countPorcentaje} lances en porcentaje, {countKilos} en kilos. Bloqueando importación.", "Toda la marea");
        }
        else if (countPorcentaje > 0)
        {
            report.AddIssue(ValidationLevel.AutoFixed, "Descarte", "Se detectó que TODOS los descartes están en porcentaje. Convertidos a kilos automáticamente.", "Toda la marea");
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
        Dictionary<string, long> especiesDict)
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

            // Peso Alométrico
            if (m.PesoMues <= 0)
            {
                double totalWeight = 0;
                bool foundAnyParams = false;

                // Intentar obtener parámetros fallback (general o del LG legado)
                double fallbackA = 0, fallbackB = 0;
                bool hasFallback = false;

                if (!string.IsNullOrEmpty(m.Especie) && especiesDict.TryGetValue(m.Especie.Trim().ToUpper(), out long especieIdNum))
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

                long speciesIdForLookup = m.CodEspec; // Usar código numérico directo si existe
                if (speciesIdForLookup == 0 && !string.IsNullOrEmpty(m.Especie) && especiesDict != null)
                {
                    especiesDict.TryGetValue(m.Especie.Trim().ToUpper(), out speciesIdForLookup);
                }

                string espIdLookupStr = speciesIdForLookup.ToString();

                if (hasFallback || (largoPesoCatalogo != null && largoPesoCatalogo.Any(k => k.Key.EspecieId == espIdLookupStr)))
                {
                    string espId = espIdLookupStr;
                    
                    // Función local para obtener parámetros con fallback y promedios
                    (double A, double B) GetSmartParams(int targetSex)
                    {
                        // 1. Intentar búsqueda exacta
                        if (largoPesoCatalogo.TryGetValue((espId, targetSex), out var p)) return p;
                        
                        // 2. Si no es 0, intentar buscar el general (0)
                        if (targetSex != 0 && largoPesoCatalogo.TryGetValue((espId, 0), out p)) return p;
                        
                        // 3. Si no hay general (o es el que buscamos), intentar promediar Macho (1) y Hembra (2)
                        bool hasM = largoPesoCatalogo.TryGetValue((espId, 1), out var pM);
                        bool hasF = largoPesoCatalogo.TryGetValue((espId, 2), out var pF);
                        
                        if (hasM && hasF) return ((pM.A + pF.A) / 2.0, (pM.B + pF.B) / 2.0);
                        if (hasM) return pM;
                        if (hasF) return pF;
                        
                        // 4. Fallback final a LG o 0
                        return (fallbackA, fallbackB);
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
                        if (tally.Indeterminate > 0)
                        {
                            var p = GetSmartParams(0);
                            if (p.A > 0) { wIndet = tally.Indeterminate * (p.A * Math.Pow(tallaCm, p.B)); foundAnyParams = true; }
                        }
                        
                        totalWeight += (wMales + wFemales + wIndet) / 1000.0; 
                    }
                }

                if (foundAnyParams && totalWeight > 0)
                {
                    string oldVal = m.PesoMues.ToString();
                    m.PesoMues = totalWeight;
                    report.AddIssue(ValidationLevel.AutoFixed, "Biometría", "Peso de muestra era 0. Recalculado mediante relación Largo-Peso diferenciada por sexo.", ctx, oldVal, m.PesoMues.ToString("F2"));
                }
                else
                {
                    string debugInfo = $"[ID Resuelto: {espIdLookupStr}, Nombre: '{m.Especie}', CodEspec Original: {m.CodEspec}]";
                    report.AddIssue(ValidationLevel.Error, "Biometría", $"Peso de muestra es 0 y no se encontraron parámetros Largo-Peso. Detalles técnicos: {debugInfo}", ctx);
                }
            }
        }
    }

    private void ValidateSubSamples(MareaValidationReport report, List<LegacySubmuestra> submuestras, List<LegacyMuestra> muestras)
    {
        foreach (var s in submuestras)
        {
            string ctx = $"Lance {s.Lance} - Ejemplar {s.NEjemplar}";

            // Integridad: Verificar existencia de muestra padre
            bool hasParent = muestras.Any(m => m.Lance == s.Lance && m.Especie?.Trim().ToUpper() == s.Especie?.Trim().ToUpper());
            if (!hasParent)
            {
                report.AddIssue(ValidationLevel.Error, "Integridad", $"Submuestra huerfana: No existe muestra padre para la especie {s.Especie} en el lance {s.Lance}.", ctx);
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
            var trackTimeUtc3 = t.GetUtcDateTime().AddHours(-3);
            
            // Solo evaluamos puntos dentro de la ventana de búsqueda (evita que radios enormes anulen la validación)
            if (trackTimeUtc3 >= windowStart && trackTimeUtc3 <= windowEnd)
            {
                double actualDist = CalculateDistanceNauticalMiles(lanceLat, lanceLon, t.Latitud, t.Longitud);
                double timeDiffHours = Math.Abs((lanceTime - trackTimeUtc3).TotalHours);
                
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
