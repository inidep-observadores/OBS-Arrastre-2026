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
        var report = _engine.ValidateMarea("BUQUE CORRECTO", 2026, 100, null, null, null, null, new(), capturas, new(), new(), new(), new(), new(), new(), new(), new(), new());

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
        var report = _engine.ValidateMarea("B", 2026, 100, null, null, null, null, new(), capturas, new(), new(), new(), new(), new(), new(), new(), new(), new());

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
        var report = _engine.ValidateMarea("B", 2026, 100, null, null, null, null, new(), new() { c }, new(), submuestras, new(), new(), new(), new(), new(), new(), new());

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
        var report = _engine.ValidateMarea("B", 2026, 100, null, null, null, null, new(), capturas, new(), new(), new(), new(), new(), new(), new(), new(), new());

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
        var report = _engine.ValidateMarea("B", 2026, 100, null, null, null, null, new(), new() { c }, new(), new(), new(), new(), new(), new(), new(), new(), new());

        // Assert
        report.Issues.Should().Contain(i => i.Category == "Geografía" && i.Message.Contains("inicial (-5m)"));
        report.Issues.Should().Contain(i => i.Category == "Geografía" && i.Message.Contains("final (2500m)"));
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
        var report = _engine.ValidateMarea("B", 2026, 100, null, null, null, null, new(), capturas, new(), new(), new(), new(), new(), new(), new(), new(), new());

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
        var report = _engine.ValidateMarea("B", 2026, 100, null, null, null, null, new(), capturas, muestras, new(), new(), new(), new(), especiesDict, new(), new(), largoPeso);

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
        var report = _engine.ValidateMarea("B", 2026, 100, null, null, null, null, new(), capturas, new(), new(), new(), new(), new(), especiesDict, new(), new(), new());

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
}
