using FluentAssertions;
using OBSArrastre2026.App.Data.Entities;
using OBSArrastre2026.Tests.Fixtures;

namespace OBSArrastre2026.Tests.Data;

public class DatabaseConnectionTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public DatabaseConnectionTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CanConnectAndCreateSchema()
    {
        // Act
        using var context = _fixture.CreateContext();
        
        // Assert
        var canConnect = await context.Database.CanConnectAsync();
        canConnect.Should().BeTrue();
    }

    [Fact]
    public async Task CanInsertAndRetrieveBuque()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var buque = new Buque
        {
            Id = Guid.NewGuid().ToString(),
            Nombre = "BUQUE TEST",
            Matricula = 1234
        };

        // Act
        context.Buques.Add(buque);
        await context.SaveChangesAsync();

        var retrieved = await context.Buques.FindAsync(buque.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Nombre.Should().Be("BUQUE TEST");
    }
}
