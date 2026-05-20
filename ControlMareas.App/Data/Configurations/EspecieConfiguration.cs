using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ControlMareas.App.Data.Entities;

namespace ControlMareas.App.Data.Configurations;

public sealed class EspecieConfiguration : IEntityTypeConfiguration<Especie>
{
    public void Configure(EntityTypeBuilder<Especie> builder)
    {
        builder.ToTable("especies");

        builder.HasKey(x => x.ID);

        builder.Property(x => x.ID)
            .HasColumnName("ID")
            .IsRequired();

        builder.Property(x => x.CodigoInidep)
            .HasColumnName("CodigoInidep");

        builder.Property(x => x.DocumentoInformativo)
            .HasColumnName("DocumentoInformativo");

        builder.Property(x => x.Especifico)
            .HasColumnName("Especifico");

        builder.Property(x => x.Familia)
            .HasColumnName("Familia");

        builder.Property(x => x.Frecuente)
            .HasColumnName("Frecuente")
            .IsRequired();

        builder.Property(x => x.Genero)
            .HasColumnName("Genero");

        builder.Property(x => x.NombreCientifico)
            .HasColumnName("NombreCientifico");

        builder.Property(x => x.NombreVulgar)
            .HasColumnName("NombreVulgar");

        builder.Property(x => x.Orden)
            .HasColumnName("Orden");

        // Índices
        builder.HasIndex(x => x.CodigoInidep)
            .HasDatabaseName("ix_especies_codigo_inidep")
            .IsUnique();
    }
}
