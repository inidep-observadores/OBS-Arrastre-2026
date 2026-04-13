using Microsoft.EntityFrameworkCore;
using OBSArrastre2026.App.Data.Configurations;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Buque> Buques => Set<Buque>();
    public DbSet<Especie> Especies => Set<Especie>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new BuqueConfiguration());
        modelBuilder.ApplyConfiguration(new EspecieConfiguration());
    }
}
