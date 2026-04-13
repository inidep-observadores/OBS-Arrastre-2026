using FluentAssertions;
using NSubstitute;
using OBSArrastre2026.App.Models.Import;
using OBSArrastre2026.App.Services;
using Xunit;

namespace OBSArrastre2026.Tests.Services;

public class MareaImportServiceTests
{
    private readonly IDbfExtractorService _extractor = Substitute.For<IDbfExtractorService>();
    private readonly IMareaReportService _report = Substitute.For<IMareaReportService>();
    private readonly MareaImportService _service;

    public MareaImportServiceTests()
    {
        _service = new MareaImportService(_extractor, _report);
    }

    [Fact]
    public async Task ProcessMareaImportAsync_ShouldOrchestrateCalls()
    {
        // Arrange
        _extractor.ReadCapturasAsync(Arg.Any<string>())
            .Returns(new List<LegacyCaptura> { new() { Lance = 1, Barco = "TEST", Marea = 100 } });
        
        _extractor.ReadMuestrasAsync(Arg.Any<string>())
            .Returns(new List<LegacyMuestra>());
        
        _extractor.ReadSubmuestrasAsync(Arg.Any<string>())
            .Returns(new List<LegacySubmuestra>());

        _report.GenerateValidationPdf(Arg.Any<MareaValidationReport>())
            .Returns(new byte[] { 1, 2, 3 });

        // Act
        var result = await _service.ProcessMareaImportAsync("C:\\Temp", "TEST", 100, 2026);

        // Assert
        result.Should().NotBeNull();
        await _extractor.Received(1).ReadCapturasAsync(Arg.Is<string>(s => s.Contains("C10026")));
        _report.Received(1).GenerateValidationPdf(Arg.Any<MareaValidationReport>());
    }
}
