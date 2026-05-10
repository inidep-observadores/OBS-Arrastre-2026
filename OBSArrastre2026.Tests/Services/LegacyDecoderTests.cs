using FluentAssertions;
using OBSArrastre2026.App.Models.Import;
using OBSArrastre2026.App.Services.Internal;
using Xunit;

namespace OBSArrastre2026.Tests.Services;

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
}
