using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OBSArrastre2026.App.Data.Entities;

namespace OBSArrastre2026.App.Data.Configurations;

public sealed class MuestraConfiguration : IEntityTypeConfiguration<Muestra>
{
    public void Configure(EntityTypeBuilder<Muestra> builder)
    {
        builder.ToTable("muestras");

        builder.HasKey(x => x.ID);

        builder.Property(x => x.ID).HasColumnName("ID").IsRequired();
        builder.Property(x => x.LanceID).HasColumnName("LanceID");
        builder.Property(x => x.EspecieID).HasColumnName("EspecieID");
        builder.Property(x => x.Comentarios).HasColumnName("Comentarios");
        builder.Property(x => x.EjemplaresPorKg).HasColumnName("EjemplaresPorKg").IsRequired();
        builder.Property(x => x.Intervalo).HasColumnName("Intervalo").IsRequired();
        builder.Property(x => x.UnidadMedidaTalla).HasColumnName("UnidadMedidaTalla").IsRequired();
        builder.Property(x => x.ModoMedicionTalla).HasColumnName("ModoMedicionTalla").IsRequired();
        builder.Property(x => x.Origen).HasColumnName("Origen").IsRequired();
        builder.Property(x => x.DiscriminaSexo).HasColumnName("DiscriminaSexo").IsRequired();
        builder.Property(x => x.HayIndeterminados).HasColumnName("HayIndeterminados").IsRequired();
        builder.Property(x => x.PesoMuestra_PesoGramos).HasColumnName("PesoMuestra_PesoGramos");
        builder.Property(x => x.NumeroOrden).HasColumnName("NumeroOrden").IsRequired();
        builder.Property(x => x.EspecieOriginal).HasColumnName("EspecieOriginal");
        builder.Property(x => x.Fuente).HasColumnName("Fuente");
        builder.Property(x => x.Tarte).HasColumnName("Tarte");
        builder.Property(x => x.Area).HasColumnName("Area");
        builder.Property(x => x.FactPond).HasColumnName("FactPond");
        builder.Property(x => x.PrimTalla).HasColumnName("PrimTalla");
        builder.Property(x => x.UltTalla).HasColumnName("UltTalla");

        builder.HasOne(x => x.Lance)
            .WithMany(l => l.Muestras)
            .HasForeignKey(x => x.LanceID)
            .HasConstraintName("fk_muestras_lances_lance_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Especie)
            .WithMany(e => e.Muestras)
            .HasForeignKey(x => x.EspecieID)
            .HasConstraintName("fk_muestras_especies_especie_id")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.LanceID).HasDatabaseName("ix_muestras_lance_id");
        builder.HasIndex(x => x.EspecieID).HasDatabaseName("ix_muestras_especie_id");
    }
}
