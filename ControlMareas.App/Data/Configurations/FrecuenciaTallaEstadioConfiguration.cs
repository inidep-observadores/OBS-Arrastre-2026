using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ControlMareas.App.Data.Entities;

namespace ControlMareas.App.Data.Configurations;

public sealed class FrecuenciaTallaEstadioConfiguration : IEntityTypeConfiguration<FrecuenciaTallaEstadio>
{
    public void Configure(EntityTypeBuilder<FrecuenciaTallaEstadio> builder)
    {
        builder.ToTable("frecuencias_de_tallas_con_estadio");

        builder.HasKey(x => x.ID);

        builder.Property(x => x.ID).HasColumnName("ID").IsRequired();
        builder.Property(x => x.MuestraID).HasColumnName("MuestraID");
        builder.Property(x => x.Talla).HasColumnName("Talla").IsRequired();
        builder.Property(x => x.EstadiosHembras).HasColumnName("EstadiosHembras");
        builder.Property(x => x.EstadiosMachos).HasColumnName("EstadiosMachos");
        builder.Property(x => x.NroIndeterminados).HasColumnName("NroIndeterminados").IsRequired();
        builder.Property(x => x.Metadata).HasColumnName("Metadata");

        builder.HasOne(x => x.Muestra)
            .WithMany(m => m.FrecuenciasTallasEstadio)
            .HasForeignKey(x => x.MuestraID)
            .HasConstraintName("fk_frecuencias_de_tallas_con_estadio_muestras_muestra_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.MuestraID).HasDatabaseName("ix_frecuencias_de_tallas_con_estadio_muestra_id");
    }
}
