using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ControlMareas.App.Data.Entities;

namespace ControlMareas.App.Data.Configurations;

public sealed class BuqueConfiguration : IEntityTypeConfiguration<Buque>
{
    public void Configure(EntityTypeBuilder<Buque> builder)
    {
        builder.ToTable("buques");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("ID");
        builder.Property(x => x.Nombre).HasColumnName("Nombre").IsRequired();
        builder.Property(x => x.Matricula).HasColumnName("Matricula");
        builder.Property(x => x.IdRadial).HasColumnName("IdRadial");
        builder.Property(x => x.IMO).HasColumnName("IMO");
        builder.Property(x => x.MMSI).HasColumnName("MMSI");

        builder.HasIndex(x => x.Nombre).IsUnique();
        builder.HasIndex(x => x.IdRadial); // Mantenemos el índice para búsquedas ágiles, pero sin unicidad
    }
}
