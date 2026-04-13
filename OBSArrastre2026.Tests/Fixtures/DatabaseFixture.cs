using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OBSArrastre2026.App.Data;

namespace OBSArrastre2026.Tests.Fixtures;

public class DatabaseFixture : IDisposable
{
    private readonly SqliteConnection _connection;

    public DatabaseFixture()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        var context = new AppDbContext(options);
        
        // Aseguramos que el esquema se cree en la base de datos in-memory
        context.Database.EnsureCreated();
        
        return context;
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }
}
