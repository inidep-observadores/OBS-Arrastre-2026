using FluentAssertions;
using ControlMareas.App.Models.Import;
using ControlMareas.App.Services.Internal;
using Xunit;

namespace ControlMareas.Tests.Services;

public class MareaValidationEngineTests
{
    private readonly MareaValidationEngine _engine = new();

    [Fact]
    public void ValidateMarea_ShouldDetectBarcoMismatch()
    {
        // Arrange
        var capturas = new List<LegacyCaptura>
        {
            new() { Lance = 1, Barco = "BUQUE ERRONEO", Marea = 100 }
        };

        // Act
        var report = _engine.ValidateMarea("BUQUE CORRECTO", 2026, 100, null, null, null, null, new(), capturas, new(), new(), new(), new(), new(), new(), new(), new(), new(), new());

        // Assert
        report.Issues.Should().Contain(i => i.Level == ValidationLevel.Fatal && i.Category == "Consistencia");
    }

    [Fact]
    public void ValidateMarea_ShouldAutoFixCaptTotal()
    {
        // Arrange
        var c = new LegacyCaptura
        {
            Lance = 1,
            Barco = "B",
            Marea = 100,
            CaptTotal = 500
        };
        c.Especies["1"] = 100;
        c.Especies["2"] = 100;

        var capturas = new List<LegacyCaptura> { c };

        // Act
        var report = _engine.ValidateMarea("B", 2026, 100, null, null, null, null, new(), capturas, new(), new(), new(), new(), new(), new(), new(), new(), new(), new());

        // Assert
        report.Issues.Should().Contain(i => i.Level == ValidationLevel.AutoFixed && i.Category == "Captura");
        capturas[0].CaptTotal.Should().Be(200);
    }

    [Fact]
    public void CalculateArea_ShouldMatchSpecExample()
    {
        var result = _engine.CalculateArea(-35.25, -55.40);
        result.Should().Be(3555.2);
    }

    [Fact]
    public void ValidateMarea_ShouldWarnOnLargeLargoTot()
    {
        // Arrange
        var c = new LegacyCaptura { Lance = 1, Barco = "B", Marea = 100 };
        c.Especies["1"] = 10;
        
        var submuestras = new List<LegacySubmuestra>
        {
            new() { Lance = 1, NEjemplar = 1, LargoTot = 260 } // > 250
        };

        // Act
        var report = _engine.ValidateMarea("B", 2026, 100, null, null, null, null, new(), new() { c }, new(), submuestras, new(), new(), new(), new(), new(), new(), new(), new());

        // Assert
        report.Issues.Should().Contain(i => i.Level == ValidationLevel.Warning && i.Category == "Biometría");
    }

    [Fact]
    public void ValidateMarea_ShouldDetectSequenceGap()
    {
        // Arrange
        var c1 = new LegacyCaptura { Lance = 1, Barco = "B", Marea = 100 };
        c1.Especies["1"] = 10;
        var c2 = new LegacyCaptura { Lance = 3, Barco = "B", Marea = 100 };
        c2.Especies["1"] = 10;

        var capturas = new List<LegacyCaptura> { c1, c2 };

        // Act
        var report = _engine.ValidateMarea("B", 2026, 100, null, null, null, null, new(), capturas, new(), new(), new(), new(), new(), new(), new(), new(), new(), new());

        // Assert
        report.Issues.Should().Contain(i => i.Category == "Estructura" && i.Message.Contains("Salto"));
    }

    [Fact]
    public void ValidateMarea_ShouldDetectInvalidDepths()
    {
        // Arrange
        var c = new LegacyCaptura { Lance = 1, Barco = "B", Marea = 100, ProfInic = -5, ProfFinal = 2500 };
        c.Especies["1"] = 10;

        // Act
        var report = _engine.ValidateMarea("B", 2026, 100, null, null, null, null, new(), new() { c }, new(), new(), new(), new(), new(), new(), new(), new(), new(), new());

        // Assert
        report.Issues.Should().Contain(i => i.Category == "Geografía" && i.Message.Contains("inicial (-5m)"));
        report.Issues.Should().Contain(i => i.Category == "Geografía" && i.Message.Contains("final (2500m)"));
    }

    [Theory]
    [InlineData(100, 140, false)] // Diff 40m (< 50m minimum) -> false
    [InlineData(100, 160, true)]  // Diff 60m (>= 50) and 60m / 100 = 60% (>= 60%) -> true
    [InlineData(1000, 1500, false)] // Diff 500m (>= 50) and 500 / 1000 = 50% (< 60%) -> false
    [InlineData(1000, 1700, true)] // Diff 700m (>= 50) and 700 / 1000 = 70% (>= 60%) -> true
    [InlineData(17, 930, true)] // Diff 913m (>= 50) and 913 / 17 > 60% -> true
    public void ValidateMarea_ShouldDetectSignificantDepthDifferences(double inic, double final, bool shouldDetect)
    {
        // Arrange
        var c = new LegacyCaptura { Lance = 1, Barco = "B", Marea = 100, ProfInic = inic, ProfFinal = final };
        c.Especies["1"] = 10;

        // Act
        var report = _engine.ValidateMarea("B", 2026, 100, null, null, null, null, new(), new() { c }, new(), new(), new(), new(), new(), new(), new(), new(), new(), new());

        // Assert
        if (shouldDetect)
            report.Issues.Should().Contain(i => i.Category == "Geografía" && i.Message.Contains("Diferencia significativa de profundidad"));
        else
            report.Issues.Should().NotContain(i => i.Category == "Geografía" && i.Message.Contains("Diferencia significativa de profundidad"));
    }



    [Fact]
    public void ValidateMarea_ShouldErrorOnEmptyLance()
    {
        // Arrange
        var capturas = new List<LegacyCaptura>
        {
            new() { Lance = 1, Barco = "B", Marea = 100 } // Sin especies
        };

        // Act
        var report = _engine.ValidateMarea("B", 2026, 100, null, null, null, null, new(), capturas, new(), new(), new(), new(), new(), new(), new(), new(), new(), new());

        // Assert
        report.Issues.Should().Contain(i => i.Category == "Captura" && i.Message.Contains("sin registro de especies"));
    }

    [Fact]
    public void ValidateMarea_ShouldCalculateWeight_WhenWeightIsZero()
    {
        // Arrange
        var capturas = new List<LegacyCaptura> { new() { Lance = 1, Barco = "B", Marea = 100 } };
        capturas[0].Especies["7210040101"] = 100;

        var muestras = new List<LegacyMuestra>
        {
            new() { Lance = 1, Especie = "MERLUZA COMUN", CodEspec = "7210040101", PesoMues = 0 }
        };
        // Merluza Macho 40cm: a=0.01124, b=2.8340
        // P = 0.01124 * 40^2.8340 = 385.64g
        muestras[0].Tallies.Add(new DecodedTally(40, 10, 0, 0, 10)); // 10 machos de 40cm
        // Peso esperado: 10 * 385.64 / 1000 = 3.8564 kg

        var especiesDict = new Dictionary<string, string> { ["MERLUZA COMUN"] = "7210040101" };
        var largoPeso = new Dictionary<(string, int), (double, double)>
        {
            [("7210040101", 1)] = (0.01124, 2.8340)
        };

        // Act
        var report = _engine.ValidateMarea("B", 2026, 100, null, null, null, null, new(), capturas, muestras, new(), new(), new(), new(), especiesDict, new(), new(), new(), largoPeso);

        // Assert
        report.Issues.Should().Contain(i => i.Level == ValidationLevel.AutoFixed && i.Message.Contains("Recalculado mediante relación Largo-Peso"));
        muestras[0].PesoMues.Should().BeInRange(3.89, 3.91);
    }

    [Fact]
    public void ValidateMarea_ShouldIncludeSpeciesInContext_WhenDiscardIsGreaterLessThanTotalCapture()
    {
        // Arrange
        var capturas = new List<LegacyCaptura>();
        
        // 5 lances normales (consensúan que la marea se cargó en KILOS)
        for (int i = 1; i <= 5; i++)
        {
            var cap = new LegacyCaptura
            {
                Lance = i,
                Barco = "B",
                Marea = 100,
                CaptTotal = 1000,
                Descarte = 100
            };
            cap.Especies["7210040101"] = 1000;
            cap.DescartesPorEspecie["7210040101"] = 100;
            capturas.Add(cap);
        }

        // 1 lance con descarte excesivo (ratio > 1.0) que indica probable error de carga
        var lanceExcesivo = new LegacyCaptura
        {
            Lance = 6,
            Barco = "B",
            Marea = 100,
            CaptTotal = 1000,
            Descarte = 1500
        };
        lanceExcesivo.Especies["7210040101"] = 900;
        lanceExcesivo.DescartesPorEspecie["7210040101"] = 1500;
        capturas.Add(lanceExcesivo);

        var especiesDict = new Dictionary<string, string> { ["MERLUZA COMUN"] = "7210040101" };

        // Act
        var report = _engine.ValidateMarea("B", 2026, 100, null, null, null, null, new(), capturas, new(), new(), new(), new(), new(), especiesDict, new(), new(), new(), new());

        // Assert
        var issue = report.Issues.Should().ContainSingle(i => i.Category == "Captura" && i.Message.Contains("El descarte") && i.Message.Contains("superior a la captura total")).Subject;
        issue.Context.Should().Be("Lance 6 - Especie MERLUZA COMUN");
    }

    [Fact]
    public void ValidateMarea_WithSkipConsensusHeuristic_ShouldReportErrorDirectlyWithoutAlteringValues()
    {
        // Arrange
        var capturas = new List<LegacyCaptura>
        {
            new()
            {
                Lance = 1,
                Barco = "B",
                Marea = 100,
                CaptTotal = 100,
                Descarte = 150
            }
        };
        capturas[0].Especies["7210040101"] = 100;
        capturas[0].DescartesPorEspecie["7210040101"] = 150;

        var especiesDict = new Dictionary<string, string> { ["MERLUZA COMUN"] = "7210040101" };

        // Act
        var report = _engine.ValidateMarea(
            barcoMareaActual: "B",
            anioMareaActual: 2026,
            nroMareaActual: 100,
            buqueCodigo: null,
            obsNombre: null,
            obsApellido: null,
            obsCodigo: null,
            etapasFechas: new(),
            capturas: capturas,
            muestras: new(),
            submuestras: new(),
            lgs: new(),
            tracking: new(),
            produccion: new(),
            especiesDict: especiesDict,
            especiesViejasDict: new(),
            especiesCientificasDict: new(),
            especiesCodigosValidos: new(),
            largoPesoCatalogo: new(),
            procesarSubmuestrasSinMuestraTalla: false,
            skipConsensusHeuristic: true
        );

        // Assert
        var issue = report.Issues.Should().ContainSingle(i => i.Category == "Captura" && i.Message.Contains("El descarte (150 kg) es superior a la captura total (100 kg).")).Subject;
        issue.Level.Should().Be(ValidationLevel.Error);
        issue.Context.Should().Be("Lance 1 - Especie MERLUZA COMUN");

        capturas[0].Descarte.Should().Be(150);
        capturas[0].DescartesPorEspecie["7210040101"].Should().Be(150);
    }

    [Fact]
    public void ValidateMarea_ShouldHandleMidnightCrossingInTrackingConsistency_WhenFechaFinIsNull()
    {
        // Arrange
        // Lance inicia el 2026-05-20 a las 23:30 y finaliza el 2026-05-21 a las 01:15 (cruce de medianoche)
        var c = new LegacyCaptura
        {
            Lance = 1,
            Barco = "BUQUE",
            Marea = 100,
            Fecha = new DateTime(2026, 5, 20),
            FechaFin = null, // Inicialmente nulo (como al importar de DBF)
            HoraInic = 23.30,
            HoraFinal = 01.15,
            LatInic = 40.000,   // -40 grados decimales
            LongInic = 60.000,  // -60 grados decimales
            LatFinal = 40.050,  // -40.0833 grados decimales
            LongFinal = 60.050, // -60.0833 grados decimales
            CaptTotal = 100
        };
        c.Especies["7210040101"] = 100;

        // Tracking satelital que coincide perfectamente con los momentos del lance
        var tracking = new List<LegacyTracking>
        {
            new()
            {
                Buque = "BUQUE",
                FechaStr = "2026-05-20 23:30:00",
                Latitud = -40.0,
                Longitud = -60.0
            },
            new()
            {
                Buque = "BUQUE",
                FechaStr = "2026-05-21 01:15:00", // Día siguiente
                Latitud = -40.0833,
                Longitud = -60.0833
            }
        };

        // Act
        var report = _engine.ValidateMarea(
            barcoMareaActual: "BUQUE",
            anioMareaActual: 2026,
            nroMareaActual: 100,
            buqueCodigo: null,
            obsNombre: null,
            obsApellido: null,
            obsCodigo: null,
            etapasFechas: new(),
            capturas: new() { c },
            muestras: new(),
            submuestras: new(),
            lgs: new(),
            tracking: tracking,
            produccion: new(),
            especiesDict: new() { ["MERLUZA"] = "7210040101" },
            especiesViejasDict: new(),
            especiesCientificasDict: new(),
            especiesCodigosValidos: new() { "7210040101" },
            largoPesoCatalogo: new()
        );

        // Assert
        // No debe haber problemas de inconsistencia geográfica de "Fin" ni diferencias importantes 
        // de distancia contra el track en el reporte.
        var geoIssues = report.Issues.Where(i => i.Category == "Geografía");
        geoIssues.Should().NotContain(i => i.Message.Contains("Fin") && i.Message.Contains("difiere"));
    }

    [Fact]
    public void ValidateMarea_Palangre_ShouldEnforceDepthRangeBetween50And5000()
    {
        // Arrange
        var cpInvalidDepth = new LegacyCapturaPalangre
        {
            Lance = 1,
            Barco = "B",
            Marea = 100,
            Fecha = new DateTime(2026, 5, 20),
            FechaVi = new DateTime(2026, 5, 20),
            HoraInic = 8.00,
            HoraFinF = 10.00,
            HoraIniV = 14.00,
            HoraFinV = 18.00,
            ProfIniF = 30, // Fuera de rango (< 50)
            ProfFinF = 6000, // Fuera de rango (> 5000)
            AnzNTotal = 2000
        };
        cpInvalidDepth.Especies["7218280201"] = 100;
        cpInvalidDepth.EspeciesOrder.Add("7218280201");

        // Act
        var report = _engine.ValidateMarea(
            "B", 2026, 100, null, null, null, null, new(),
            new() { cpInvalidDepth }, new(), new(), new(), new(), new(),
            new() { ["BACALAO"] = "7218280201" }, new(), new(),
            new() { "7218280201" }, new()
        );

        // Assert
        report.Issues.Should().Contain(i => i.Level == ValidationLevel.Error && i.Category == "Geografía" && i.Message.Contains("50-5000m"));
    }

    [Fact]
    public void ValidateMarea_Palangre_ShouldDetectOverlapStrictly()
    {
        // Arrange
        var cp1 = new LegacyCapturaPalangre
        {
            Lance = 1,
            Barco = "B",
            Marea = 100,
            Fecha = new DateTime(2026, 5, 20),
            FechaVi = new DateTime(2026, 5, 20),
            HoraInic = 6.00,
            HoraFinF = 8.00,
            HoraIniV = 12.00,
            HoraFinV = 16.00, // Termina virado a las 16:00
            ProfIniV = 1200,
            ProfFinV = 1200,
            AnzNTotal = 3000
        };
        cp1.Especies["7218280201"] = 200;
        cp1.EspeciesOrder.Add("7218280201");

        var cp2 = new LegacyCapturaPalangre
        {
            Lance = 2,
            Barco = "B",
            Marea = 100,
            Fecha = new DateTime(2026, 5, 20),
            FechaVi = new DateTime(2026, 5, 20),
            HoraInic = 15.00, // Inicia calado a las 15:00 (solapa con virado de lance 1 hasta 16:00)
            HoraFinF = 17.00,
            HoraIniV = 19.00,
            HoraFinV = 23.00,
            ProfIniV = 1200,
            ProfFinV = 1200,
            AnzNTotal = 3000
        };
        cp2.Especies["7218280201"] = 300;
        cp2.EspeciesOrder.Add("7218280201");

        // Act
        var report = _engine.ValidateMarea(
            "B", 2026, 100, null, null, null, null, new(),
            new() { cp1, cp2 }, new(), new(), new(), new(), new(),
            new() { ["BACALAO"] = "7218280201" }, new(), new(),
            new() { "7218280201" }, new()
        );

        // Assert
        report.Issues.Should().Contain(i => i.Level == ValidationLevel.Error && i.Category == "Tiempo" && i.Message.Contains("Solapamiento detectado"));
    }

    [Fact]
    public void ValidateMarea_Palangre_ShouldEnforceHooksCountAndPiecesCoherence()
    {
        // Arrange
        var cp = new LegacyCapturaPalangre
        {
            Lance = 1,
            Barco = "B",
            Marea = 100,
            Fecha = new DateTime(2026, 5, 20),
            FechaVi = new DateTime(2026, 5, 20),
            HoraInic = 6.00,
            HoraFinF = 8.00,
            HoraIniV = 12.00,
            HoraFinV = 16.00,
            ProfIniV = 1200,
            ProfFinV = 1200,
            AnzNTotal = 0 // Invalido
        };
        cp.Especies["7218280201"] = 0; // Sin kilos
        cp.PiezasPorEspecie["7218280201"] = 15; // 15 piezas pero 0 kg
        cp.EspeciesOrder.Add("7218280201");

        // Act
        var report = _engine.ValidateMarea(
            "B", 2026, 100, null, null, null, null, new(),
            new() { cp }, new(), new(), new(), new(), new(),
            new() { ["BACALAO"] = "7218280201" }, new(), new(),
            new() { "7218280201" }, new()
        );

        // Assert
        report.Issues.Should().Contain(i => i.Level == ValidationLevel.Error && i.Category == "Esfuerzo" && i.Message.Contains("anzuelos"));
        report.Issues.Should().Contain(i => i.Level == ValidationLevel.Error && i.Category == "Captura" && i.Message.Contains("piezas pero la captura en kilos es 0"));
    }

    [Fact]
    public void ValidateMarea_ProductionBalance_Palangre_ShouldUseFechaVirado()
    {
        // Arrange: Lance de palangre calado el 10/05/2026 y virado el 11/05/2026
        var cp = new LegacyCapturaPalangre
        {
            Barco = "B",
            Marea = 100,
            Lance = 1,
            Fecha = new DateTime(2026, 5, 10), // Calado
            FechaVi = new DateTime(2026, 5, 11), // Virado
            HoraInic = 20.00,
            HoraFinF = 23.00,
            HoraIniV = 06.00,
            HoraFinV = 12.00,
            LatInic = 45.0,
            LongInic = 60.0,
            LatFinF = 45.1,
            LongFinF = 60.1,
            LatIniV = 45.1,
            LongIniV = 60.1,
            LatFinV = 45.0,
            LongFinV = 60.0,
            ProfIniF = 1200,
            ProfFinF = 1200,
            ProfIniV = 1200,
            ProfFinV = 1200,
            AnzNTotal = 5000,
            CaptTotal = 1000
        };
        cp.Especies["7210080101"] = 1000;
        cp.EspeciesOrder.Add("7210080101");

        // Producción el día de virado (11/05/2026)
        var prod = new LegacyProduccion
        {
            Marea = 100,
            Fecha = new DateTime(2026, 5, 11),
            Especie = "MERLUZA NEGRA",
            Kilos = 500,
            Factor = 2.0 // 500 * 2.0 = 1000 kg reconstruidos
        };

        var etapas = new List<(DateTime Inicio, DateTime Fin)>
        {
            (new DateTime(2026, 5, 1), new DateTime(2026, 5, 20))
        };

        // Act
        var report = _engine.ValidateMarea(
            "B", 2026, 100, null, null, null, null, etapas,
            new() { cp }, new(), new(), new(), new(), new() { prod },
            new() { ["MERLUZA NEGRA"] = "7210080101" }, new(), new(),
            new() { "7210080101" }, new()
        );

        // Assert: No debe haber errores ni advertencias de balance diario en producción
        report.Issues.Should().NotContain(i => i.Category == "Balance de Captura Diaria");
    }

    [Fact]
    public void ValidateMarea_ProductionBalance_Arrastre_ShouldUseFechaFin()
    {
        // Arrange: Lance de arrastre iniciado el 10/05/2026 y finalizado el 11/05/2026 (cruce de medianoche)
        var c = new LegacyCaptura
        {
            Barco = "B",
            Marea = 100,
            Lance = 2,
            Fecha = new DateTime(2026, 5, 10),
            FechaFin = new DateTime(2026, 5, 11),
            HoraInic = 23.00,
            HoraFinal = 02.00,
            LatInic = 45.0,
            LongInic = 60.0,
            LatFinal = 45.2,
            LongFinal = 60.2,
            ProfInic = 200,
            ProfFinal = 200,
            CaptTotal = 2000
        };
        c.Especies["7210080102"] = 2000;
        c.EspeciesOrder.Add("7210080102");

        // Producción el día de finalización del lance (11/05/2026)
        var prod = new LegacyProduccion
        {
            Marea = 100,
            Fecha = new DateTime(2026, 5, 11),
            Especie = "MERLUZA",
            Kilos = 2000,
            Factor = 1.0
        };

        var etapas = new List<(DateTime Inicio, DateTime Fin)>
        {
            (new DateTime(2026, 5, 1), new DateTime(2026, 5, 20))
        };

        // Act
        var report = _engine.ValidateMarea(
            "B", 2026, 100, null, null, null, null, etapas,
            new() { c }, new(), new(), new(), new(), new() { prod },
            new() { ["MERLUZA"] = "7210080102" }, new(), new(),
            new() { "7210080102" }, new()
        );

        // Assert: No debe haber problemas de balance diario
        report.Issues.Should().NotContain(i => i.Category == "Balance de Captura Diaria");
    }
}
