using FluentAssertions;
using OBSArrastre2026.App.Models.Import;
using OBSArrastre2026.App.Services.Internal;
using Xunit;

namespace OBSArrastre2026.Tests.Services;

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
        var report = _engine.ValidateMarea("BUQUE CORRECTO", 2026, 100, capturas, new(), new());

        // Assert
        report.Issues.Should().Contain(i => i.Level == ValidationLevel.Error && i.Category == "Consistencia");
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
        c.Especies[1] = 100;
        c.Especies[2] = 100;

        var capturas = new List<LegacyCaptura> { c };

        // Act
        var report = _engine.ValidateMarea("B", 2026, 100, capturas, new(), new());

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
        c.Especies[1] = 10;
        
        var submuestras = new List<LegacySubmuestra>
        {
            new() { Lance = 1, NEjemplar = 1, LargoTot = 260 } // > 250
        };

        // Act
        var report = _engine.ValidateMarea("B", 2026, 100, new() { c }, new(), submuestras);

        // Assert
        report.Issues.Should().Contain(i => i.Level == ValidationLevel.Warning && i.Category == "Biometría");
    }

    [Fact]
    public void ValidateMarea_ShouldDetectSequenceGap()
    {
        // Arrange
        var c1 = new LegacyCaptura { Lance = 1, Barco = "B", Marea = 100 };
        c1.Especies[1] = 10;
        var c2 = new LegacyCaptura { Lance = 3, Barco = "B", Marea = 100 };
        c2.Especies[1] = 10;

        var capturas = new List<LegacyCaptura> { c1, c2 };

        // Act
        var report = _engine.ValidateMarea("B", 2026, 100, capturas, new(), new());

        // Assert
        report.Issues.Should().Contain(i => i.Category == "Estructura" && i.Message.Contains("Salto"));
    }

    [Fact]
    public void ValidateMarea_ShouldDetectInvalidDepths()
    {
        // Arrange
        var c = new LegacyCaptura { Lance = 1, Barco = "B", Marea = 100, ProfInic = -5, ProfFinal = 2500 };
        c.Especies[1] = 10;

        // Act
        var report = _engine.ValidateMarea("B", 2026, 100, new() { c }, new(), new());

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
        var report = _engine.ValidateMarea("B", 2026, 100, capturas, new(), new());

        // Assert
        report.Issues.Should().Contain(i => i.Category == "Captura" && i.Message.Contains("sin registro de especies"));
    }
}
