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
        List<LegacyCaptura> capturas,
        List<LegacyMuestra> muestras,
        List<LegacySubmuestra> submuestras,
        List<LegacyProduccion> produccion,
        HashSet<string> nombresVulgaresExistentes)
    {
        var report = new MareaValidationReport
        {
            Barco = barcoMareaActual,
            Marea = nroMareaActual.ToString(),
            Año = anioMareaActual,
            TotalLances = capturas.Count
        };

        ValidateBaseConsistency(report, barcoMareaActual, nroMareaActual, capturas, muestras, submuestras);
        ValidateLances(report, capturas);
        ValidateSamples(report, muestras, capturas);
        ValidateSubSamples(report, submuestras, muestras);
        ValidateProduction(report, produccion, nombresVulgaresExistentes);

        return report;
    }

    private void ValidateProduction(MareaValidationReport report, List<LegacyProduccion> produccion, HashSet<string> nombresVulgaresExistentes)
    {
        // 1. Validar existencia de especie por nombre
        var especiesDesconocidas = produccion
            .Select(p => p.Especie?.Trim().ToUpper())
            .Where(name => !string.IsNullOrEmpty(name) && !nombresVulgaresExistentes.Contains(name))
            .Distinct();

        foreach (var esp in especiesDesconocidas)
        {
            report.AddIssue(ValidationLevel.Warning, "Catálogo Especies", 
                $"La especie de producción '{esp}' no fue encontrada por Nombre Vulgar en el catálogo local. El registro se importará pero sin vínculo a la especie.", "Archivo P*");
        }

        // 2. Regla de negocio: Unicidad de Fecha-Producto-Categoría (Warning)
        var duplicates = produccion
            .GroupBy(p => new { p.Fecha, p.Producto, p.Categoria })
            .Where(g => g.Count() > 1);

        foreach (var group in duplicates)
        {
            report.AddIssue(ValidationLevel.Warning, "Producción", 
                $"Existen {group.Count()} registros para el producto '{group.Key.Producto}' ({group.Key.Categoria}) el {group.Key.Fecha:yyyy-MM-dd}. Se importarán todos pero se recomienda verificar posibles duplicados.", 
                $"Fecha {group.Key.Fecha:yyyy-MM-dd}");
        }
    }

    private void ValidateBaseConsistency(
        MareaValidationReport report, 
        string barcoActual, 
        int mareaActual, 
        List<LegacyCaptura> capturas, 
        List<LegacyMuestra> muestras, 
        List<LegacySubmuestra> submuestras)
    {
        // REQ-3.1.1: Consistencia de Barco y Marea
        foreach (var c in capturas)
        {
            if (c.Barco.Trim() != barcoActual.Trim())
                report.AddIssue(ValidationLevel.Error, "Consistencia", $"Barco en captura ({c.Barco}) no coincide con marea activa ({barcoActual})", $"Lance {c.Lance}");
            
            if ((int)c.Marea != mareaActual)
                report.AddIssue(ValidationLevel.Error, "Consistencia", $"Nro Marea en captura ({c.Marea}) no coincide con marea activa ({mareaActual})", $"Lance {c.Lance}");
        }
    }

    private void ValidateLances(MareaValidationReport report, List<LegacyCaptura> capturas)
    {
        foreach (var c in capturas)
        {
            string ctx = $"Lance {c.Lance}";

            // REQ-2.2.3: Rangos de coordenadas
            if (c.LatInic < -60 || c.LatInic > 0)
                report.AddIssue(ValidationLevel.Warning, "Geografía", $"Latitud inicial ({c.LatInic}) fuera de rango operativo", ctx);

            ValidateLanceDetails(report, c);

            // REQ-3.3.1: Cálculo y validación de Área (base = INT(lat)*100 + INT(lon))
            double calculatedArea = CalculateArea(c.LatInic, c.LongInic);
            if (calculatedArea < 3500)
                report.AddIssue(ValidationLevel.Warning, "Geografía", $"Área calculada ({calculatedArea}) es inusualmente baja (< 3500)", ctx);

            // REQ-3.5.1: Recalcular CAPT_TOTAL
            double sumEspecies = c.Especies.Values.Sum();
            if (Math.Abs(sumEspecies - c.CaptTotal) > 0.1)
            {
                report.AddIssue(ValidationLevel.AutoFixed, "Captura", "Captura total inconsistente con suma de especies. Se recalcula.", ctx, c.CaptTotal.ToString(), sumEspecies.ToString());
                c.CaptTotal = sumEspecies;
            }

            // REQ-5.3: Coherencia Temporal
            if (c.HoraInic > c.HoraFinal)
                report.AddIssue(ValidationLevel.Warning, "Tiempo", "Hora de inicio es posterior a hora de fin", ctx);
        }

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

    private void ValidateSamples(MareaValidationReport report, List<LegacyMuestra> muestras, List<LegacyCaptura> capturas)
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
        }
    }

    private void ValidateSubSamples(MareaValidationReport report, List<LegacySubmuestra> submuestras, List<LegacyMuestra> muestras)
    {
        foreach (var s in submuestras)
        {
            string ctx = $"Lance {s.Lance} - Ejemplar {s.NEjemplar}";

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
}
