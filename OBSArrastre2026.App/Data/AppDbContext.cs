using Microsoft.EntityFrameworkCore;
using OBSArrastre2026.App.Data.Configurations;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Buque> Buques => Set<Buque>();
    public DbSet<Especie> Especies => Set<Especie>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Marea> Mareas => Set<Marea>();
    public DbSet<MareaEtapa> MareaEtapas => Set<MareaEtapa>();
    public DbSet<RegistroProduccion> RegistrosProduccion => Set<RegistroProduccion>();
    public DbSet<Lance> Lances => Set<Lance>();
    public DbSet<Muestra> Muestras => Set<Muestra>();
    public DbSet<FrecuenciaTalla> FrecuenciasTallas => Set<FrecuenciaTalla>();
    public DbSet<FrecuenciaTallaEstadio> FrecuenciasTallasEstadio => Set<FrecuenciaTallaEstadio>();
    public DbSet<ItemCaptura> ItemsCaptura => Set<ItemCaptura>();
    public DbSet<ItemSubmuestra> ItemsSubmuestras => Set<ItemSubmuestra>();
    public DbSet<ItemContenidoGastrico> ContenidosGastricos => Set<ItemContenidoGastrico>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
