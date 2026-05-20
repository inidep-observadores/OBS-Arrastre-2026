using FluentAssertions;
using ControlMareas.App.Models.Import;
using ControlMareas.App.Services.Internal;
using Xunit;

namespace ControlMareas.Tests.Services;

public class LegacyDecoderTests
{
    [Fact]
    public void DecodeCoordinate_ShouldConvertCorrectly()
    {
        // 43.15 -> -43.25 (15 minutes = 0.25 degrees)
        var result = LegacyDecoder.DecodeCoordinate(43.15);
        result.Should().Be(-43.25);
    }

    [Fact]
    public void DecodeTime_ShouldConvertCorrectly()
    {
        // 14.30 -> 14h 30m
        var result = LegacyDecoder.DecodeTime(14.30);
        result.Hours.Should().Be(14);
        result.Minutes.Should().Be(30);
    }

    [Fact]
    public void DecodeTally_ShouldUnpackBlocks()
    {
        // Tally: 10020030040 (Size 10, M 2, H 3, I 4) -> Wait, size part len is totalLen - 12
        // If " 10020030040", total len 11, size part len -1? Bad example.
        // Format: [Talla][M(3)][H(3)][I(3)][T(3)] -> 14-15 chars
        // Example: "86001002003006" -> Talla 86, M 1, H 2, I 3, T 6
        var result = LegacyDecoder.DecodeTally("86001002003006");
        result.Size.Should().Be(86);
        result.Males.Should().Be(1);
        result.Females.Should().Be(2);
        result.Indeterminate.Should().Be(3);
        result.Total.Should().Be(6);
    }

    [Fact]
    public void DecodeTally_WithDecodedObject_ShouldRecalculateTotalIfInconsistent()
    {
        // 1 + 2 + 1 != 10 (Total should be 4)
        var obj = "10001002001010"; // Size 10, M 1, H 2, I 1, T 10
        var result = LegacyDecoder.DecodeTally(obj);
        result.Total.Should().Be(4);
    }

    [Fact]
    public void MergeExtendedMuestras_ShouldUpdateUltTallaAndTallies()
    {
        // Arrange
        var baseM = new LegacyMuestra
        {
            Lance = 1,
            CodEspec = "1",
            PrimTalla = 10,
            UltTalla = 15
        };
        baseM.Tallies.Add(new DecodedTally(10, 5, 0, 0, 5));
        baseM.Tallies.Add(new DecodedTally(11, 3, 0, 0, 3));

        var extension = new LegacyMuestra
        {
            Lance = 1, 
            CodEspec = "1", 
            PrimTalla = 20, 
            UltTalla = 25
        };
        extension.Tallies.Add(new DecodedTally(20, 2, 0, 0, 2));
        extension.Tallies.Add(new DecodedTally(25, 1, 0, 0, 1));

        // Act
        LegacyDecoder.MergeExtendedMuestras(baseM, extension);

        // Assert
        baseM.Tallies.Should().HaveCount(4);
        baseM.Tallies.Should().Contain(t => t.Size == 25);
        baseM.UltTalla.Should().Be(25);
    }

    [Fact]
    public void DecodeTally_WithMalformedString_ShouldReturnEmptyTally()
    {
        // Act
        var result = LegacyDecoder.DecodeTally("BADSTRING");

        // Assert
        result.Males.Should().Be(0);
        result.Females.Should().Be(0);
        result.Total.Should().Be(0);
    }

    [Fact]
    public void DecodeTally_WithNull_ShouldReturnEmptyTally()
    {
        // Act
        var result = LegacyDecoder.DecodeTally(null);

        // Assert
        result.Total.Should().Be(0);
    }

    [Fact]
    public void DecodeCoordinate_WithExtremeValues_ShouldReturnCorrectDecimal()
    {
        // -60.00 -> -60.0
        LegacyDecoder.DecodeCoordinate(60.00).Should().Be(-60.0);
        
        // -30.00 -> -30.0
        LegacyDecoder.DecodeCoordinate(30.00).Should().Be(-30.0);
    }

    [Fact]
    public void DecodeTally_With15DigitPaddedString_ShouldDecodeCorrectly()
    {
        // "84000003004007" se rellena a 15 dígitos -> "084000003004007"
        // Talla: 84, Machos: 0, Hembras: 3, Indeterminados: 4, Total: 7
        var result = LegacyDecoder.DecodeTally("84000003004007");
        result.Size.Should().Be(84);
        result.Males.Should().Be(0);
        result.Females.Should().Be(3);
        result.Indeterminate.Should().Be(4);
        result.Total.Should().Be(7);
    }

    [Fact]
    public void DecodeTally_WithOverflowPrevention_ShouldDecodeCorrectly()
    {
        // "174003004010017" (15 dígitos) -> supera int.MaxValue si se parseara completo
        // Talla: 174, Machos: 3, Hembras: 4, Indeterminados: 10, Total: 17
        var result = LegacyDecoder.DecodeTally("174003004010017");
        result.Size.Should().Be(174);
        result.Males.Should().Be(3);
        result.Females.Should().Be(4);
        result.Indeterminate.Should().Be(10);
        result.Total.Should().Be(17);
    }

    [Fact]
    public void DecodeTally_WithDecimalPointString_ShouldDecodeCorrectly()
    {
        // "84000003004007.0" con parte decimal residual de DBF
        // Talla: 84, Machos: 0, Hembras: 3, Indeterminados: 4, Total: 7
        var result = LegacyDecoder.DecodeTally("84000003004007.0");
        result.Size.Should().Be(84);
        result.Males.Should().Be(0);
        result.Females.Should().Be(3);
        result.Indeterminate.Should().Be(4);
        result.Total.Should().Be(7);
    }
}
