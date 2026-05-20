using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ControlMareas.App.Data.Entities;

namespace ControlMareas.App.Data.Configurations;

public sealed class EspecieLargoPesoConfiguration : IEntityTypeConfiguration<EspecieLargoPeso>
{
    public void Configure(EntityTypeBuilder<EspecieLargoPeso> builder)
    {
        builder.ToTable("especies_largo_peso");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("ID")
            .IsRequired();

        builder.Property(x => x.EspecieId)
            .HasColumnName("EspecieID")
            .IsRequired();

        builder.Property(x => x.Sexo)
            .HasColumnName("Sexo")
            .IsRequired();

        builder.Property(x => x.ParamA)
            .HasColumnName("ParamA")
            .IsRequired();

        builder.Property(x => x.ParamB)
            .HasColumnName("ParamB")
            .IsRequired();

        builder.Property(x => x.TipoMedida)
            .HasColumnName("TipoMedida")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.Observaciones)
            .HasColumnName("Observaciones");

        // Relación con Especies
        builder.HasOne(x => x.Especie)
            .WithMany()
            .HasForeignKey(x => x.EspecieId)
            .OnDelete(DeleteBehavior.Cascade);

        // Índice para búsqueda rápida por especie y sexo
        builder.HasIndex(x => new { x.EspecieId, x.Sexo })
            .HasDatabaseName("ix_especies_largo_peso_especie_sexo");
    }
}
